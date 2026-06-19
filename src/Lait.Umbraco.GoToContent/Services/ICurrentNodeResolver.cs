namespace Lait.GoToContent.Services;

internal interface ICurrentNodeResolver
{
    Guid? GetCurrentNodeKey();
}
