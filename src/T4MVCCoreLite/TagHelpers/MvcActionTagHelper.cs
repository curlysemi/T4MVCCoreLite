using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    [HtmlTargetElement("a", Attributes = ActionAttribute)]
    public class MvcActionTagHelper : TagHelper
    {
        private const string ActionAttribute = "mvc-action";

        private readonly IUrlHelperFactory _urlHelperFactory;
        public MvcActionTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        [ViewContext]
        public ViewContext ViewContext { get; set; }
        [HtmlAttributeName(ActionAttribute)]
        public IActionResult Action { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.RemoveAll(ActionAttribute);
            var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);

            if (Action is IT4MVCCoreActionResult t4mvcActionResult)
            {
                var url = urlHelper.RouteUrl(t4mvcActionResult.RouteValueDictionary);
                output.Attributes.SetAttribute("href", url);
            }
        }
    }
}
