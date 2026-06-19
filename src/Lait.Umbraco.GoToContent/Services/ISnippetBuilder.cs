namespace Lait.GoToContent.Services;

internal interface ISnippetBuilder
{
    string Build(Guid contentKey);
}
