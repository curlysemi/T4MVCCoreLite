using System.Collections.Generic;

namespace T4MVCCoreLiteTool.Services
{
    public interface IViewLocatorService
    {
        IEnumerable<View> FindViews();
    }
}
