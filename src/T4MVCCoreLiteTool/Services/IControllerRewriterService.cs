using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace T4MVCCoreLiteTool.Services
{
    public interface IControllerRewriterService
    {
        ImmutableArray<ClassDeclarationSyntax> RewriteControllers(CSharpCompilation compiler, string outputFileName);
    }
}
