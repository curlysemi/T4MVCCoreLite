using System.Collections.Generic;

namespace T4MVCCoreLiteTool.Locators
{
    public interface IStaticFileLocator
    {
        IEnumerable<StaticFile> Find();
    }
}
