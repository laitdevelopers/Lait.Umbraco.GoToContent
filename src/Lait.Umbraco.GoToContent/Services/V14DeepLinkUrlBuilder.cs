using System.Globalization;

namespace Lait.GoToContent.Services;

internal sealed class V14DeepLinkUrlBuilder : IDeepLinkUrlBuilder
{
    // Phase 1 empirical verification, 2026-06-12, Umbraco 14.3.4.
    // Address bar captured while editing Home in the backoffice:
    //   /umbraco/section/content/workspace/document/edit/{key}/invariant
    // Variation segment is mandatory; "invariant" is the literal value for non-culture-varying nodes.
    public string BuildEditUrl(Guid contentKey)
        => string.Format(
            CultureInfo.InvariantCulture,
            "/umbraco/section/content/workspace/document/edit/{0}/invariant",
            contentKey.ToString("D", CultureInfo.InvariantCulture));
}
