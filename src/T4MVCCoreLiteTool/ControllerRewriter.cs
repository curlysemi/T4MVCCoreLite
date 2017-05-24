using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System;

namespace T4MVCCoreLiteTool
{
    public class ControllerRewriter : CSharpSyntaxRewriter
    {
        private IList<KeyValuePair<ClassDeclarationSyntax, MethodDeclarationSyntax>> _controllers = null;
        public ILookup<ClassDeclarationSyntax, MethodDeclarationSyntax> Controllers => _controllers.ToLookup(x => x.Key, x => x.Value, ClassDeclarationSyntaxComparer.Instance);

        private class ClassDeclarationSyntaxComparer : IEqualityComparer<ClassDeclarationSyntax>
        {
            public bool Equals(ClassDeclarationSyntax x, ClassDeclarationSyntax y)
            {
                return String.Equals(x.Identifier.Text, y.Identifier.Text);
            }

            public int GetHashCode(ClassDeclarationSyntax obj)
            {
                return obj.Identifier.Text.GetHashCode();
            }

            public static readonly ClassDeclarationSyntaxComparer Instance = new ClassDeclarationSyntaxComparer();
        }

        private SyntaxToken CreateModifierToken(SyntaxKind modifier)
        {
            return SyntaxFactory.Token(SyntaxFactory.TriviaList(), modifier, SyntaxFactory.TriviaList(SyntaxFactory.Space));
        }

        private bool IsClassNode(ClassDeclarationSyntax node)
        {
            if (node.BaseList == null)
                return false;
            var identifiers = node.BaseList?.Types.Select(t => t.Type).OfType<IdentifierNameSyntax>();
            return identifiers.Any(i => i.Identifier.Text == "Controller");
        }

        public void Reset()
        {
            _controllers = new List<KeyValuePair<ClassDeclarationSyntax, MethodDeclarationSyntax>>();
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            if (IsClassNode(node))
            {
                if (!node.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    node = node.AddModifiers(CreateModifierToken(SyntaxKind.PartialKeyword));
                }
            }
            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
            var controllerNode = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (IsClassNode(controllerNode))
            {
                if ((node.ReturnType as IdentifierNameSyntax)?.Identifier.Text == "IActionResult")
                {
                    _controllers.Add(new KeyValuePair<ClassDeclarationSyntax, MethodDeclarationSyntax>(controllerNode, node));
                    if (!node.Modifiers.Any(SyntaxKind.VirtualKeyword) && node.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {
                        node = node.AddModifiers(CreateModifierToken(SyntaxKind.VirtualKeyword));
                    }
                }
            }
            return node;
        }
    }
}
