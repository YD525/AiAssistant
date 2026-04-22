using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiAssistant.ExecuteSandbox;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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
    new CapabilityInfo
    {
        Name        = "RunCode",
        Description = "Execute C# code using Roslyn scripting engine. Code is a top-level script (no explicit 'return' allowed). To return a value, use the last expression or statement value.",
        Params      = new List<ParameterInfo>
        {
            new ParameterInfo { Name = "Code", Type = "string", Description = "C# code to evaluate (expression or full script). Do NOT use 'return' keyword." }
        }
    },
    new CapabilityInfo
    {
        Name        = "RunCodeWithGlobals",
        Description = "Execute C# script with external data object (exposed as 'Data' variable). Based on Roslyn CSharpScript engine. Top-level script does NOT support 'return' ¡ª use last expression value. Pre-imports System, System.IO, System.Linq, System.Collections.Generic.",
        Params      = new List<ParameterInfo>
        {
            new ParameterInfo { Name = "Code",    Type = "string", Description = "C# script. Do NOT use 'return'. Access globals via 'Data'." },
            new ParameterInfo { Name = "Globals", Type = "object", Description = "External data object, accessible as 'Data' inside script." }
        }
    },
    new CapabilityInfo
    {
        Name        = "RunCodeWithReturn",
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

        /// <summary>
        /// Evaluates a C# expression or script synchronously and returns its result.
        /// Returns a [ERROR] prefixed string if an exception is thrown.
        /// </summary>
        public object RunCode(string Code)
    => Sandbox.Exec(nameof(RunCode), () =>
    {
        try
        {
            var References = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            var Imports = new[]
            {
                "System", "System.IO", "System.Linq", "System.Collections.Generic",
                "System.Text", "System.Threading.Tasks", "System.Diagnostics"
            };

            var Options = ScriptOptions.Default
                .WithImports(Imports)
                .WithReferences(References);

            var EvalTask = CSharpScript.EvaluateAsync<object>(Code, Options);
            EvalTask.Wait();
            return EvalTask.Result;
        }
        catch (Exception ex)
        {
            return $"[ERROR] {ex.Message}";
        }
    }, Code);

        public object RunCodeWithReturn(string Code)
        {
            return Sandbox.Exec(nameof(RunCodeWithReturn), () =>
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

                    var Options = ScriptOptions.Default
                        .WithImports(DefaultImports)
                        .WithReferences(References);

                    var Task = CSharpScript.EvaluateAsync<object>(Code, Options);
                    Task.Wait();
                    return Task.Result;
                }
                catch (Exception ex)
                {
                    return $"[ERROR] {ex.Message}";
                }
            }, Code);
        }

        /// <summary>
        /// Evaluates a C# expression or script asynchronously and returns its result.
        /// Preferred over RunCode for long-running scripts to avoid blocking the thread pool.
        /// </summary>
        public async Task<object> RunCodeAsync(string Code)
        {
            return await Sandbox.Exec(nameof(RunCodeAsync), async () =>
            {
                try
                {
                    return await CSharpScript.EvaluateAsync(Code);
                }
                catch (Exception Exception)
                {
                    return $"[ERROR] {Exception.Message}";
                }
            }, Code);
        }

        /// <summary>
        /// Evaluates a C# script with a globals object that the script can access via the 'Data' variable.
        /// Common namespaces (System, IO, Linq, Collections) are pre-imported.
        /// </summary>
        public object RunCodeWithGlobals(string Code, object Globals)
      => Sandbox.Exec(nameof(RunCodeWithGlobals), () =>
      {
          try
          {
              var References = AppDomain.CurrentDomain.GetAssemblies()
                  .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                  .Select(a => MetadataReference.CreateFromFile(a.Location))
                  .ToList();

              ScriptOptions Options = ScriptOptions.Default
                  .WithImports(
                      "System",
                      "System.IO",
                      "System.Linq",
                      "System.Collections.Generic",
                      "System.Text",
                      "System.Threading.Tasks",
                      "System.Diagnostics"   
                  )
                  .WithReferences(References);  

              Task<object> EvalTask = CSharpScript.EvaluateAsync(Code, Options, Globals);
              EvalTask.Wait();
              return EvalTask.Result;
          }
          catch (Exception Exception)
          {
              return $"[ERROR] {Exception.Message}";
          }
      }, Code, Globals);

        #endregion
    }
}
