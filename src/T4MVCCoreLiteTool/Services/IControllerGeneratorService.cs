using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace T4MVCCoreLiteTool.Services
{
    public interface IControllerGeneratorService
    {
        IEnumerable<NamespaceDeclarationSyntax> GenerateControllers(CSharpCompilation compiler, IEnumerable<ClassDeclarationSyntax> controllerNodes);
    }
}
