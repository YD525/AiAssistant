using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AiAssistant.ExecuteSandbox;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    /// <summary>
    /// Executes C# code at runtime using the Roslyn scripting engine.
    /// Supports simple expressions, full scripts, async execution, and scripts
    /// that receive an external data object via ScriptGlobals.
    /// </summary>
    public class CSharpCodeUnit
    {
        public bool Enable = false;
        #region Capability Manifest (AI readable)

        public static List<CapabilityInfo> CapabilityManifest = new List<CapabilityInfo>
{
    //new CapabilityInfo
    //{
    //    Name        = "RunCode",
    //    Description = "Execute C# code using Roslyn scripting engine. Code is a top-level script (no explicit 'return' allowed). To return a value, use the last expression or statement value.",
    //    Params      = new List<ParameterInfo>
    //    {
    //        new ParameterInfo { Name = "Code", Type = "string", Description = "C# code to evaluate (expression or full script). Do NOT use 'return' keyword." }
    //    }
    //},
    //new CapabilityInfo
    //{
    //    Name        = "RunCodeWithGlobals",
    //    Description = "Execute C# script with external data object (exposed as 'Data' variable). Based on Roslyn CSharpScript engine. Top-level script does NOT support 'return' ¡ª use last expression value. Pre-imports System, System.IO, System.Linq, System.Collections.Generic.",
    //    Params      = new List<ParameterInfo>
    //    {
    //        new ParameterInfo { Name = "Code",    Type = "string", Description = "C# script. Do NOT use 'return'. Access globals via 'Data'." },
    //        new ParameterInfo { Name = "Globals", Type = "object", Description = "External data object, accessible as 'Data' inside script." }
    //    }
    //},
    new CapabilityInfo
    {
        Name        = "RunCSharpCode",
        Description = "Execute C# code that supports explicit 'return' statements. Wraps code in a Func<object> lambda, so 'return expression;' works. Based on Roslyn scripting engine.",
        Params      = new List<ParameterInfo>
        {
            new ParameterInfo { Name = "Code", Type = "string", Description = "C# code. Must contain 'return' to produce a value; otherwise returns null." }
        }
    }
};
        #endregion

        #region Globals Wrapper

        /// <summary>
        /// Container passed as the globals object when running a script that needs
        /// to read or write external state. Access it inside the script as 'Data'.
        /// </summary>
        public class ScriptGlobals
        {
            public object Data;
        }

        #endregion

        #region Code Execution

        public string GetAvailableNamespaces()
        {
            var Whitelist = new[]
            {
        "System",
        "System.IO",
        "System.IO.Compression",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Collections.Generic",
        "System.Collections.Concurrent",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Diagnostics",
        "System.Reflection",
        "System.Net",
        "System.Net.Http",
        "System.Net.Sockets",
        "System.Net.Mail",
        "System.Net.NetworkInformation",
        "System.Runtime.InteropServices",
        "System.Security.Cryptography",
        "System.Xml",
        "System.Xml.Linq",
        "System.Data",
        "System.Data.SqlClient",
        "System.Drawing",
        "System.Windows.Forms",
        "System.Timers",
        "System.Environment",
        "System.Math",
        "System.Convert",
        "Newtonsoft.Json",
        "Newtonsoft.Json.Linq",
        "Microsoft.Win32",
    };
            var LoadedNamespaces = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; }
                })
                .Select(t => t.Namespace)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            var Available = Whitelist
                .Where(n => LoadedNamespaces.Contains(n))
                .ToList();

            return string.Join("\n", Available);
        }

        public object RunCSharpCode(string Code)
        {
            return Sandbox.Exec(nameof(RunCSharpCode), () =>
            {
                try
                {
                    var References = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                        .Select(a => MetadataReference.CreateFromFile(a.Location))
                        .ToList();

                    var DefaultImports = new[]
                    {
                "System", "System.IO", "System.Linq", "System.Collections.Generic",
                "System.Text", "System.Threading.Tasks", "System.Diagnostics"
            };

                    var UsingMatches = Regex.Matches(Code, @"using\s+([\w\.]+)\s*;");
                    var ExtraImports = UsingMatches.Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Where(ns => !string.IsNullOrEmpty(ns))
                        .ToList();

                    string CleanCode = Regex.Replace(Code, @"using\s+[\w\.]+\s*;\s*", "").Trim();

                    var AllImports = DefaultImports.Concat(ExtraImports).Distinct().ToArray();

                    var Options = ScriptOptions.Default
                        .WithImports(AllImports)
                        .WithReferences(References);

                    string WrappedCode = $"new Func<object>(() => {{ {CleanCode} }})()";
                    var Script = CSharpScript.Create<object>(WrappedCode, Options);
                    var ScriptTask = Script.RunAsync();
                    ScriptTask.Wait();

                    if (ScriptTask.Exception != null)
                        throw new Exception($"[ERROR] {JsonConvert.SerializeObject(ScriptTask.Exception, Formatting.Indented)}");

                    var ReturnValue = ScriptTask.Result.ReturnValue;
                    if (ReturnValue == null)
                        return null;
                    else if (ReturnValue is string s)
                        return s;
                    else
                        return JsonConvert.SerializeObject(ReturnValue, Formatting.Indented);
                }
                catch (AggregateException aggEx)
                {
                    throw new Exception($"[ERROR] {aggEx.InnerException?.Message ?? aggEx.Message}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"[ERROR] {ex.Message}");
                }
            }, Code);
        }
        #endregion
    }

}
