using System.Collections.Generic;

namespace T4MVCCoreLiteTool.Locators
{
    public interface IViewLocator
    {
        IEnumerable<View> Find();
    }
}
