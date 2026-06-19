using System.Text.Encodings.Web;
using System.Text.Json;

namespace Lait.GoToContent.Services;

internal sealed class SnippetBuilder : ISnippetBuilder
{
    private const string Style =
        "<style>" +
        ".lait-gtc-pill{position:fixed;top:16px;right:16px;z-index:2147483000;display:inline-flex;align-items:center;gap:8px;padding:8px 14px;background:#1b264f;color:#fff;font:600 13px/1 system-ui,-apple-system,Segoe UI,Roboto,sans-serif;text-decoration:none;border-radius:999px;box-shadow:0 2px 10px rgba(0,0,0,.25);transition:transform .12s ease,background .12s ease;user-select:none}" +
        ".lait-gtc-pill:hover{background:#3544b1;transform:translateY(-1px)}" +
        ".lait-gtc-pill:focus-visible{outline:2px solid #fff;outline-offset:2px}" +
        ".lait-gtc-pill svg{width:14px;height:14px;flex:0 0 14px}" +
        "@media (prefers-color-scheme: light){.lait-gtc-pill{background:#1b264f;color:#fff}}" +
        "@media print{.lait-gtc-pill{display:none!important}}" +
        "</style>";

    private const string Script =
        "<script>" +
        "(function(){" +
            "var c=document.getElementById('lait-gotocontent-config');" +
            "if(!c)return;" +
            "var cfg;try{cfg=JSON.parse(c.textContent||'{}');}catch(e){return;}" +
            "if(!cfg||!cfg.editUrl)return;" +
            "if(document.querySelector('.lait-gtc-pill'))return;" +
            "var a=document.createElement('a');" +
            "a.className='lait-gtc-pill';" +
            "a.href=cfg.editUrl;" +
            "a.target='_blank';" +
            "a.rel='noopener';" +
            "a.setAttribute('aria-label',cfg.label||'Edit in Umbraco');" +
            "a.innerHTML='<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M12 20h9\"/><path d=\"M16.5 3.5a2.121 2.121 0 1 1 3 3L7 19l-4 1 1-4 12.5-12.5z\"/></svg><span>'+(cfg.label||'Edit in Umbraco')+'</span>';" +
            "document.body.appendChild(a);" +
        "})();" +
        "</script>";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly IDeepLinkUrlBuilder _urlBuilder;

    public SnippetBuilder(IDeepLinkUrlBuilder urlBuilder)
    {
        _urlBuilder = urlBuilder;
    }

    public string Build(Guid contentKey)
    {
        var editUrl = _urlBuilder.BuildEditUrl(contentKey);
        var configJson = JsonSerializer.Serialize(
            new { editUrl, label = "Edit in Umbraco" },
            JsonOptions);

        return Style
            + "<script type=\"application/json\" id=\"lait-gotocontent-config\">"
            + configJson
            + "</script>"
            + Script;
    }
}
