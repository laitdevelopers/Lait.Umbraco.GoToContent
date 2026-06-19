using Microsoft.AspNetCore.Http;

namespace Lait.GoToContent.Services;

internal interface IBackOfficeAuthDetector
{
    Task<bool> IsAuthenticatedBackOfficeUserAsync(HttpContext context);
}
