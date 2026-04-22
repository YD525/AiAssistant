using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiAssistant.ExecuteSandbox;
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
                Description = "Execute a C# expression or script and return the result",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Code", Type = "string", Description = "C# code to evaluate (expression or full script)" }
                }
            },
            new CapabilityInfo
            {
                Name        = "RunCodeWithGlobals",
                Description = "Execute a C# script that has access to an external data object via the 'Data' global variable",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Code",    Type = "string", Description = "C# script to execute; use 'Data' to access the globals object" },
                    new ParameterInfo { Name = "Globals", Type = "object", Description = "External data object exposed as 'Data' inside the script" }
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
                    Task<object> EvalTask = CSharpScript.EvaluateAsync(Code);
                    EvalTask.Wait();
                    return EvalTask.Result;
                }
                catch (Exception Exception)
                {
                    return $"[ERROR] {Exception.Message}";
                }
            }, Code);

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
                    ScriptOptions Options = ScriptOptions.Default
                        .WithImports(
                            "System",
                            "System.IO",
                            "System.Linq",
                            "System.Collections.Generic"
                        );

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
