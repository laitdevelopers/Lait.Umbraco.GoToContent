using Lait.GoToContent.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace Lait.GoToContent.Middleware;

internal sealed class GoToContentMiddleware : IMiddleware
{
    // The Umbraco backoffice preview cookie. Presence means the editor is using
    // the in-backoffice preview frame; the host injects its own preview banner,
    // so we stay out of the way.
    private const string PreviewCookieName = "UMB_PREVIEW";

    private readonly IRuntimeState _runtimeState;
    private readonly IBackOfficeAuthDetector _auth;
    private readonly ICurrentNodeResolver _node;
    private readonly ISnippetBuilder _snippet;
    private readonly ILogger<GoToContentMiddleware> _log;

    public GoToContentMiddleware(
        IRuntimeState runtimeState,
        IBackOfficeAuthDetector auth,
        ICurrentNodeResolver node,
        ISnippetBuilder snippet,
        ILogger<GoToContentMiddleware> log)
    {
        _runtimeState = runtimeState;
        _auth = auth;
        _node = node;
        _snippet = snippet;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (_runtimeState.Level != RuntimeLevel.Run)
        {
            await next(context);
            return;
        }

        if (!ShouldConsider(context))
        {
            await next(context);
            return;
        }

        bool isBackOffice;
        try
        {
            isBackOffice = await _auth.IsAuthenticatedBackOfficeUserAsync(context);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[GoToContent] auth probe failed — passing through unchanged");
            await next(context);
            return;
        }

        if (!isBackOffice)
        {
            await next(context);
            return;
        }

        await InjectAsync(context, next);
    }

    private static bool ShouldConsider(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/App_Plugins", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/install", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/signin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // UMB_PREVIEW present → the editor is in the backoffice preview frame; skip.
        if (context.Request.Cookies.ContainsKey(PreviewCookieName))
        {
            return false;
        }

        return true;
    }

    private async Task InjectAsync(HttpContext context, RequestDelegate next)
    {
        var originalBody = context.Response.Body;
        InjectingBodyStream? wrapper = null;

        try
        {
            wrapper = new InjectingBodyStream(originalBody, BuildSnippetSafe(context));
            context.Response.Body = wrapper;

            // Run the rest of the pipeline; downstream writes flow through the wrapper.
            await next(context);

            if (!IsInjectableResponse(context))
            {
                // Wrong content-type / status / encoding — we did not actually inject; the
                // wrapper still passed bytes through, but the trailing window may hold the
                // last few bytes. Flush them so the response is byte-identical to no-wrap.
                await wrapper.FlushTailAsync(context.RequestAborted);
                return;
            }

            // Mark the response un-cacheable by shared caches; vary on Cookie so a CDN /
            // output cache that ignores Vary still gets a hint not to share editor + anon.
            ApplyEditorCacheHeaders(context);

            await wrapper.FlushTailAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[GoToContent] body-rewrite path faulted — host response preserved");
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private string BuildSnippetSafe(HttpContext context)
    {
        // Defer the snippet content until we're certain we have a node key. But the wrapper
        // is constructed up-front so we can swap the body before next() runs. We pass an
        // empty string if there's no node yet — the wrapper will emit nothing on match.
        // In practice, the node is resolved by the time the response body is written.
        try
        {
            var key = _node.GetCurrentNodeKey();
            return key.HasValue ? _snippet.Build(key.Value) : string.Empty;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[GoToContent] snippet build faulted — no pill will be injected");
            return string.Empty;
        }
    }

    private static bool IsInjectableResponse(HttpContext context)
    {
        var response = context.Response;
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            return false;
        }

        var contentType = response.ContentType;
        if (string.IsNullOrEmpty(contentType)
            || !contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // If something downstream pre-encoded the response (gzip/br), we cannot safely
        // rewrite bytes — the InjectingBodyStream would see compressed bytes, not HTML.
        // The recommended placement is OUTSIDE response-compression, so this is a safety net.
        if (!string.IsNullOrEmpty(response.Headers.ContentEncoding.ToString()))
        {
            return false;
        }

        return true;
    }

    private static void ApplyEditorCacheHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers[HeaderNames.CacheControl] = "private, no-store";
        var vary = headers.Vary.ToString();
        if (string.IsNullOrEmpty(vary))
        {
            headers.Vary = "Cookie";
        }
        else if (!vary.Contains("Cookie", StringComparison.OrdinalIgnoreCase))
        {
            headers.Vary = vary + ", Cookie";
        }
    }
}
