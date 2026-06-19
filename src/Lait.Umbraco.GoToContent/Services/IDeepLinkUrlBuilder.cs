namespace Lait.GoToContent.Services;

internal interface IDeepLinkUrlBuilder
{
    string BuildEditUrl(Guid contentKey);
}
