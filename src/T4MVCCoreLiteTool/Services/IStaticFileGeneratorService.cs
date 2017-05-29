using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace T4MVCCoreLiteTool.Services
{
    public interface IStaticFileGeneratorService
    {
        MemberDeclarationSyntax GenerateStaticFiles(ISettings settings);
    }
}
