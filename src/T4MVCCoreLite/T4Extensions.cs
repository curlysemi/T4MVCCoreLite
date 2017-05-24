using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    public static class T4Extensions
    {
        public static void InitMVCT4Result(this IT4MVCCoreActionResult result, string area, string controller, string action, string protocol = null)
        {
            result.Controller = controller;
            result.Action = action;
            result.Protocol = protocol;
            result.RouteValueDictionary = new RouteValueDictionary()
            {
                { "Area", area ?? "" },
                { "Controller", controller },
                { "Action", action }
            };
        }
    }
}
