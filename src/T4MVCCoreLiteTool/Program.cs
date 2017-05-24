using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace T4MVCCoreLiteTool
{
    class Program
    {
        private static string ProjectPath;
        private static string Namespace = "TestMvcApplication";
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Expecting project path as parameter");
                return;
            }

            ProjectPath = args[0];
            if (!Directory.Exists(ProjectPath))
            {
                Console.WriteLine("Invalid project path");
                return;
            }

            var rewriter = new ControllerRewriter();
            var files = Directory.GetFiles(Path.Combine(ProjectPath, "Controllers"), "*Controller.cs");

            var controllers = new Dictionary<String, ILookup<ClassDeclarationSyntax, MethodDeclarationSyntax>>();

            foreach (var controllerFile in files)
            {
                var controller = CSharpSyntaxTree.ParseText(File.ReadAllText(controllerFile));
                if (controller.GetDiagnostics().Any(f => f.Severity == DiagnosticSeverity.Error))
                {
                    Console.WriteLine($"Errors found in {controllerFile}. Skipping…");
                    continue;
                }

                var rootNote = controller.GetRoot();
                rewriter.Reset();
                var updatedNode = rewriter.Visit(rootNote);
                if (!rootNote.IsEquivalentTo(updatedNode))
                {
                    // Controller updated
                    File.WriteAllText(controllerFile, updatedNode.ToString());
                }
                controllers[controllerFile] = rewriter.Controllers;
            }

            BuildControllers(controllers);

            BuildMVCFile(controllers);
        }

        private static void BuildControllers(Dictionary<String, ILookup<ClassDeclarationSyntax, MethodDeclarationSyntax>> controllers)
        {
            foreach (var controllerFile in controllers)
            {
                var generatedFileName = controllerFile.Key.Substring(0, controllerFile.Key.Length - 2) + "generated.cs";
                var sb = new StringBuilder();
                sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
                foreach (var controllerGroup in controllerFile.Value)
                {
                    var controllerNode = controllerGroup.Key;
                    Console.WriteLine($"Processing {controllerNode.Identifier.Text}");
                    var namespaceNode = controllerNode.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
                    sb.AppendLine();
                    sb.AppendLine($"namespace {namespaceNode.Name.ToString()}");
                    sb.AppendLine("{");
                    sb.AppendLine($"    public partial class {controllerNode.Identifier.Text}");
                    sb.AppendLine("    {");
                    sb.AppendLine("        public const string Area = \"\";");
                    sb.AppendLine($"        public const string Name = \"{controllerNode.Identifier.Text.Substring(0, controllerNode.Identifier.Text.Length - 10)}\";");
                    sb.AppendLine("    }");
                    sb.AppendLine($"    public partial class T4MVCCore_{controllerNode.Identifier.Text} : {namespaceNode.Name.ToString()}.{controllerNode.Identifier.Text}");
                    sb.AppendLine("    {");

                    foreach (var method in controllerGroup)
                    {
                        Console.WriteLine($"Processing {controllerNode.Identifier.Text}.{method.Identifier.Text}");
                        sb.AppendLine("        [NonAction]");
                        sb.Append($"        public override Microsoft.AspNetCore.Mvc.IActionResult {method.Identifier.Text}(");
                        if (method.ParameterList != null)
                            for (int i = 0; i < method.ParameterList.Parameters.Count; i++)
                            {
                                if (i > 0)
                                    sb.Append(", ");
                                sb.Append(method.ParameterList.Parameters[i].ToString());
                            }
                        sb.AppendLine(")");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            var callInfo = new T4MVCCode_ActionResult(Area, Name, nameof({method.Identifier.Text}));");
                        if (method.ParameterList != null)
                            foreach (var parameter in method.ParameterList.Parameters)
                                sb.AppendLine($"            ModelUnbinderHelpers.AddRouteValues(callInfo.RouteValueDictionary, \"{parameter.Identifier.Text}\", {parameter.Identifier.Text});");
                        sb.AppendLine("            return callInfo;");
                        sb.AppendLine("        }");
                    }

                    sb.AppendLine("    }");

                    sb.AppendLine("}");
                }

                File.WriteAllText(generatedFileName, sb.ToString());
            }
        }

        private static void BuildMVCFile(Dictionary<String, ILookup<ClassDeclarationSyntax, MethodDeclarationSyntax>> controllers)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.AspNetCore.Routing;");
            sb.AppendLine("using System;");
            sb.AppendLine("");

            sb.AppendLine("public static partial class MVC");
            sb.AppendLine("{");
            /// Areas!
            //foreach (var controllerGroup in controllers.SelectMany(c => c.Value))
            //{
            //    var controllerName = controllerGroup.Key.Identifier.Text.Substring(0, controllerGroup.Key.Identifier.Text.Length - 10);
            //    sb.AppendLine($"    public static {controllerName}Class {controllerName} {{ get; }} = new {controllerName}Class();");
            //}
            foreach (var controllerGroup in controllers.SelectMany(c => c.Value))
            {
                var controllerName = controllerGroup.Key.Identifier.Text.Substring(0, controllerGroup.Key.Identifier.Text.Length - 10);
                sb.AppendLine($"    public static {Namespace}.Controllers.{controllerName}Controller {{ get; }} = new {Namespace}.Controllers.T4MVCCore_{controllerName}Controller();");
            }
            sb.AppendLine("}");

            sb.AppendLine("namespace T4MVCCore");
            sb.AppendLine("{");
            /// Areas
            //foreach (var controllerGroup in controllers.SelectMany(c => c.Value))
            //{
            //    var controllerName = controllerGroup.Key.Identifier.Text.Substring(0, controllerGroup.Key.Identifier.Text.Length - 10);

            //    sb.AppendLine($"    public class {controllerName}Class");
            //    sb.AppendLine("    {");
            //    sb.AppendLine($"        public const string Name = \"{controllerName}\";");
            //    sb.AppendLine("    }");
            //}
            sb.AppendLine("}");

            sb.AppendLine("internal partial class T4MVCCode_ActionResult : Microsoft.AspNetCore.Mvc.IActionResult, IT4MVCCoreActionResult");
            sb.AppendLine("{");
            sb.AppendLine("    public T4MVCCode_ActionResult(string area, string controller, string action, string protocol = null)");
            sb.AppendLine("    {");
            sb.AppendLine("        this.InitMVCT4Result(area, controller, action, protocol);");
            sb.AppendLine("    }");
            sb.AppendLine("    public System.Threading.Tasks.Task ExecuteResultAsync(ActionContext context) { throw new NotImplementedException(); }");
            sb.AppendLine("    public string Controller { get; set; }");
            sb.AppendLine("    public string Action { get; set; }");
            sb.AppendLine("    public string Protocol { get; set; }");
            sb.AppendLine("    public RouteValueDictionary RouteValueDictionary { get; set; }");
            sb.AppendLine("}");

            File.WriteAllText(Path.Combine(ProjectPath, "T4MVCCore.generated.cs"), sb.ToString());
        }
    }
}
