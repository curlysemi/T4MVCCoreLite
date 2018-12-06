using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using T4MVCCoreLiteTool.Extensions;

namespace T4MVCCoreLiteTool.Services
{
    public class ControllerRewriterService : IControllerRewriterService
    {
        public ImmutableArray<ClassDeclarationSyntax> RewriteControllers(CSharpCompilation compiler, params string[] blacklistedExtensions)
        {
            var mvcControllerNodes = new List<ClassDeclarationSyntax>();

            foreach (var tree in compiler.SyntaxTrees.Where(x => blacklistedExtensions.Any(b => !x.FilePath.EndsWith(b))))
            {
                // if syntaxtree has errors, skip code generation
                if (tree.GetDiagnostics().Any(x => x.Severity == DiagnosticSeverity.Error)) continue;

                // this first part, finds all the controller classes, modifies them and saves the changes
                var controllerRewriter = new ControllerRewriter(compiler);
                var newNode = controllerRewriter.Visit(tree.GetRoot());

                if (!newNode.IsEquivalentTo(tree.GetRoot()))
                {
                    // node has changed, update syntaxtree and persist to file
                    compiler = compiler.ReplaceSyntaxTree(tree, newNode.SyntaxTree);
                    newNode.WriteFile(tree.FilePath);
                }

                // save the controller nodes from each visit to pass to the generator
                mvcControllerNodes.AddRange(controllerRewriter.MvcControllerClassNodes);
            }

            return mvcControllerNodes.ToImmutableArray();
        }
    }
}
