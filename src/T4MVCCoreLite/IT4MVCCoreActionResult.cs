using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    public interface IT4MVCCoreActionResult
    {
        string Controller { get; set; }
        string Action { get; set; }
        string Protocol { get; set; }
        RouteValueDictionary RouteValueDictionary { get; set; }
    }
}
