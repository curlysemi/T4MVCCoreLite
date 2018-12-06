﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace T4MVCCoreLiteTool.Extensions
{
    /// <summary>
    /// A collection of helper and fluent extension methods to help manipulate SyntaxNodes
    /// </summary>
    public static class SyntaxNodeHelpers
    {
        public static bool InheritsFrom<T>(this ITypeSymbol symbol)
        {
            while (true)
            {
                if (symbol.TypeKind == TypeKind.Class && symbol.ToString() == typeof(T).FullName)
                {
                    return true;
                }
                if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }
                break;
            }
            return false;
        }

        public static bool InheritsFrom<T>(this IMethodSymbol symbol)
        {
            return symbol.ContainingType.InheritsFrom<T>();
        }

        public static NamespaceDeclarationSyntax CreateNamespace(string namespaceText)
        {
            var nameSyntax = ParseName(namespaceText);
            var declaration = NamespaceDeclaration(nameSyntax);
            return declaration;
        }

        private static SyntaxToken CreateModifier(SyntaxKind kind)
        {
            return Token(
                TriviaList(),
                kind,
                TriviaList(Space));
        }

        public static SyntaxToken[] CreateModifiers(params SyntaxKind[] kinds)
        {
            return kinds.Select(CreateModifier).ToArray();
        }

        public static ClassDeclarationSyntax CreateClass(string className, TypeParameterSyntax[] typeParams = null, params SyntaxKind[] modifiers)
        {
            var classSyntax = ClassDeclaration(className).WithModifiers(modifiers);

            if (typeParams != null)
                classSyntax = classSyntax
                    .AddTypeParameterListParameters(typeParams);

            return classSyntax;
        }

        public static AttributeSyntax CreateDebugNonUserCodeAttribute()
        {
            return Attribute(IdentifierName(@"DebuggerNonUserCode"));
        }

        public static AttributeSyntax CreateNonActionAttribute()
        {
            return Attribute(IdentifierName(@"NonAction"));
        }

        public static AttributeSyntax CreateGeneratedCodeAttribute()
        {
            var arguments =
                AttributeArgumentList(
                    SeparatedList(
                        new[]
                            {
                                AttributeArgument(
                                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("R4MVC"))),
                                    AttributeArgument(
                                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("1.0")))
                            }));
            return Attribute(IdentifierName("GeneratedCode"), arguments);
        }

        private static ConstructorDeclarationSyntax CreateDefaultConstructor(string className)
        {
            return
                ConstructorDeclaration(className)
                    .WithBody(Block());
        }

        public static IEnumerable<MemberDeclarationSyntax> CreateMethods(this ITypeSymbol mvcSymbol)
        {
            return mvcSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(x => x.DeclaredAccessibility == Accessibility.Public && x.MethodKind == MethodKind.Ordinary).Select(mvcControllerMethod => CreateMethod(mvcControllerMethod));
        }

        private static MemberDeclarationSyntax CreateMethod(IMethodSymbol methodSymbol)
        {
            // TODO decide whether to output full qualified name of return types to avoid issues with add usings
            // TODO add return type typeparameters
            // TODO determine what the args need to be
            // TODO add method body, currently returns null
            var returnType = methodSymbol.ReturnType;
            //var typeParameters = returnType.ContainingType.TypeParameters;

            var returnTypeSyntax = ParseTypeName(returnType.ToDisplayString());

            var methodNode =
                MethodDeclaration(returnTypeSyntax, methodSymbol.Name)
                    .WithAttributes(CreateGeneratedCodeAttribute(), CreateDebugNonUserCodeAttribute(), CreateNonActionAttribute())
                    .WithModifiers(SyntaxKind.PublicKeyword, SyntaxKind.VirtualKeyword)
                    .WithBody(
                        Block(
                            SingletonList<StatementSyntax>(
                                ReturnStatement(LiteralExpression(SyntaxKind.NullLiteralExpression)))));
            return methodNode;
        }

        public static FieldDeclarationSyntax CreateFieldWithDefaultInitializer(string fieldName, string typeName, string valueTypeName, params SyntaxKind[] modifiers)
        {
            return FieldDeclaration(
                VariableDeclaration(ParseTypeName(typeName))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(fieldName))
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(IdentifierName(valueTypeName))
                                            .WithArgumentList(ArgumentList())))))).WithModifiers(modifiers);
        }

        public static FieldDeclarationSyntax CreateStringFieldDeclaration(string fieldName, string fieldValue, params SyntaxKind[] modifiers)
        {
            return
                FieldDeclaration(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.StringKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(fieldName)
                                .WithInitializer(
                                    EqualsValueClause(
                                        LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(fieldValue)))))))
                    .WithModifiers(modifiers);
        }

        public static MethodDeclarationSyntax WithAttributes(this MethodDeclarationSyntax node, params AttributeSyntax[] attributes)
        {
            return node.AddAttributeLists(AttributeList(SeparatedList(attributes)));
        }

        public static ConstructorDeclarationSyntax WithAttributes(this ConstructorDeclarationSyntax node, params AttributeSyntax[] attributes)
        {
            return node.AddAttributeLists(AttributeList(SeparatedList(attributes)));
        }

        public static ClassDeclarationSyntax WithAttributes(this ClassDeclarationSyntax node, params AttributeSyntax[] attributes)
        {
            return node.AddAttributeLists(AttributeList(SeparatedList(attributes)));
        }

        public static FieldDeclarationSyntax WithAttributes(this FieldDeclarationSyntax node, params AttributeSyntax[] attributes)
        {
            return node.AddAttributeLists(AttributeList(SeparatedList(attributes)));
        }

        public static ClassDeclarationSyntax WithBaseTypes(this ClassDeclarationSyntax node, params string[] types)
        {
            return node.AddBaseListTypes(types.Select(x => SimpleBaseType(ParseTypeName(x))).Cast<BaseTypeSyntax>().ToArray());
        }

        public static string ToQualifiedName(this ClassDeclarationSyntax node)
        {
            return string.Format("{0}.{1}", ((NamespaceDeclarationSyntax)node?.Parent)?.Name, node.Identifier);
        }

        public static T WithPragmaCodes<T>(this T node, bool enable, params string[] codes) where T : SyntaxNode
        {
            // BUG prama is not put on newline with normalizewhitespace as expected
            var trivia = enable ? node.GetTrailingTrivia() : node.GetLeadingTrivia();
            var pramaStatus = enable ? SyntaxKind.RestoreKeyword : SyntaxKind.DisableKeyword;
            var pramaExpressions = SeparatedList(codes.Select(x => ParseExpression(x.ToString())));
            var prama =
                trivia.Add(ElasticCarriageReturnLineFeed)
                    .Add(
                        Trivia(
                            PragmaWarningDirectiveTrivia(Token(pramaStatus), pramaExpressions, true)
                                .NormalizeWhitespace()))
                    .Add(ElasticCarriageReturnLineFeed);

            return enable ? node.WithTrailingTrivia(prama) : node.WithLeadingTrivia(prama);
        }

        public static CompilationUnitSyntax WithUsings(this CompilationUnitSyntax node, params string[] namespaces)
        {
            var collection = namespaces.Select(ns => ParseName(ns)).Select(UsingDirective);
            var usings = List(collection);
            return node.WithUsings(usings);
        }

        public static T WithHeader<T>(this T node, string headerText) where T : SyntaxNode
        {
            var leadingTrivia =
                node.GetLeadingTrivia()
                .Add(Comment(headerText))
                .Add(CarriageReturnLineFeed);
            return node.WithLeadingTrivia(leadingTrivia);
        }

        public static ClassDeclarationSyntax WithDefaultConstructor(this ClassDeclarationSyntax node, bool includeGeneratedAttributes = true, params SyntaxKind[] modifiers)
        {
            var ctorNode = CreateDefaultConstructor(node.Identifier.ToString()).WithModifiers(modifiers);
            if (includeGeneratedAttributes)
            {
                ctorNode = ctorNode.WithAttributes(CreateGeneratedCodeAttribute(), CreateDebugNonUserCodeAttribute());
            }
            return node.AddMembers(ctorNode);
        }

        public static ClassDeclarationSyntax WithDefaultDummyBaseConstructor(this ClassDeclarationSyntax node, bool includeGeneratedAttributes = true, params SyntaxKind[] modifiers)
        {
            var ctorNode = CreateDefaultConstructor(node.Identifier.ToString())
                .WithModifiers(modifiers)
                .WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, ArgumentList(SeparatedList(new[] { Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Dummy"), IdentifierName("Instance"))) }))));
            if (includeGeneratedAttributes)
            {
                ctorNode = ctorNode.WithAttributes(CreateGeneratedCodeAttribute(), CreateDebugNonUserCodeAttribute());
            }
            return node.AddMembers(ctorNode);
        }

        public static ClassDeclarationSyntax WithDummyConstructor(this ClassDeclarationSyntax node, bool includeGeneratedAttributes = true, params SyntaxKind[] modifiers)
        {
            var ctorNode = CreateDefaultConstructor(node.Identifier.ToString())
                .WithModifiers(modifiers)
                .AddParameterListParameters(Parameter(Identifier("d")).WithType(ParseTypeName("Dummy")));
            if (includeGeneratedAttributes)
            {
                ctorNode = ctorNode.WithAttributes(CreateGeneratedCodeAttribute(), CreateDebugNonUserCodeAttribute());
            }
            return node.AddMembers(ctorNode);
        }

        public static ClassDeclarationSyntax WithMethods(this ClassDeclarationSyntax node, ITypeSymbol mvcSymbol)
        {
            return node;
            // TODO fix member generation
            return node.AddMembers(mvcSymbol.CreateMethods().ToArray());
        }

        public static ClassDeclarationSyntax WithSubClassMembersAsStrings(this ClassDeclarationSyntax node, ClassDeclarationSyntax controllerNode, string className, params SyntaxKind[] modifiers)
        {
            // create ActionConstants sub class
            var actionNameClass =
                CreateClass(className, null, SyntaxKind.PublicKeyword)
                    .WithAttributes(CreateGeneratedCodeAttribute(), CreateDebugNonUserCodeAttribute());
            foreach (var action in controllerNode.Members.OfType<MethodDeclarationSyntax>().Where(x => x.Modifiers.Any(SyntaxKind.PublicKeyword)).DistinctBy(x => x.Identifier.ToString()))
            {
                actionNameClass = actionNameClass.WithStringField(action.Identifier.ToString(), action.Identifier.ToString(), false, modifiers);
            }
            return node.AddMembers(actionNameClass);
        }

        public static ClassDeclarationSyntax WithField(this ClassDeclarationSyntax node, string fieldName, string typeName, params SyntaxKind[] modifiers)
        {
            var field = CreateFieldWithDefaultInitializer(fieldName, typeName, typeName, modifiers);
            return node.AddMembers(field);
        }

        public static ClassDeclarationSyntax WithStringField(this ClassDeclarationSyntax node, string name, string value, bool includeGeneratedAttribute = true, params SyntaxKind[] modifiers)
        {
            var fieldDeclaration = CreateStringFieldDeclaration(name, value, modifiers);
            if (includeGeneratedAttribute) fieldDeclaration = fieldDeclaration.WithAttributes(CreateGeneratedCodeAttribute());
            return node.AddMembers(fieldDeclaration);
        }

        public static ClassDeclarationSyntax WithModifiers(this ClassDeclarationSyntax node, params SyntaxKind[] modifiers)
        {
            return node.AddModifiers(CreateModifiers(modifiers));
        }

        public static ConstructorDeclarationSyntax WithModifiers(this ConstructorDeclarationSyntax node, params SyntaxKind[] modifiers)
        {
            return node.AddModifiers(CreateModifiers(modifiers));
        }

        public static MethodDeclarationSyntax WithModifiers(this MethodDeclarationSyntax node, params SyntaxKind[] modifiers)
        {
            return node.AddModifiers(CreateModifiers(modifiers));
        }

        public static FieldDeclarationSyntax WithModifiers(this FieldDeclarationSyntax node, params SyntaxKind[] modifiers)
        {
            return node.AddModifiers(CreateModifiers(modifiers));
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            return source.Where(element => seenKeys.Add(keySelector(element)));
        }

        public static void WriteFile(this SyntaxNode fileTree, string generatedFilePath)
        {
            using (var textWriter = new StreamWriter(new FileStream(generatedFilePath, FileMode.Create)))
            {
                fileTree.WriteTo(textWriter);
            }
        }
    }
}
