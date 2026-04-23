using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AiAssistant.AI;
using AiAssistant.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    public class UnitPipe
    {
        #region Unit Instances

        public MouseUnit MouseUnit = new MouseUnit();
        public RequestUnit RequestUnit = new RequestUnit();
        public CSharpCodeUnit CSharpUnit = new CSharpCodeUnit();

        #endregion

        #region AI Memory

        private AIMemory AIMem = new AIMemory();
        private int _StepCounter = 0;

        /// <summary>Clears all recorded steps. Called when a new user request starts or task fully ends.</summary>
        public void ClearMemory()
        {
            AIMem.Memory.Clear();
            _StepCounter = 0;
        }

        /// <summary>Adds a step to the memory.</summary>
        private void AddToMemory(string Action, string Parameters, string Result, string Status, string Reason)
        {
            AIMem.Memory.Add(new MemoryEntry
            {
                StepNumber = ++_StepCounter,
                Action = Action ?? "?",
                Parameters = Parameters ?? "none",
                Result = Result ?? "(no output)",
                Status = Status,
                Reason = Reason ?? "(no reason)"
            });
        }

        /// <summary>Formats the whole memory as a readable text block.</summary>
        private string FormatMemory()
        {
            if (AIMem.Memory.Count == 0)
                return "No previous steps.";

            var NStringBuilder = new StringBuilder();
            NStringBuilder.AppendLine("=== HISTORY OF EXECUTED STEPS ===");
            foreach (var entry in AIMem.Memory)
            {
                NStringBuilder.AppendLine($"Step {entry.StepNumber}:");
                NStringBuilder.AppendLine($"  Action   : {entry.Action}");
                NStringBuilder.AppendLine($"  Status   : {entry.Status}");
                NStringBuilder.AppendLine($"  Reason   : {entry.Reason}");
                NStringBuilder.AppendLine($"  Params   : {entry.Parameters}");
                NStringBuilder.AppendLine($"  Result   : {entry.Result}");
                NStringBuilder.AppendLine();
            }
            return NStringBuilder.ToString();
        }

        #endregion

        #region Capability Registry

        /// <summary>Returns the merged capability list from every registered Unit.</summary>
        public List<CapabilityInfo> GetCapabilities()
        {
            var AllCapabilities = new List<CapabilityInfo>();
            if (MouseUnit.Enable) AllCapabilities.AddRange(MouseUnit.CapabilityManifest);
            if (RequestUnit.Enable) AllCapabilities.AddRange(RequestUnit.CapabilityManifest);
            if (CSharpUnit.Enable) AllCapabilities.AddRange(CSharpCodeUnit.CapabilityManifest);
            return AllCapabilities;
        }

        #endregion

        #region Prompt Generation

        /// <summary>
        /// Appends the shared role definition, JSON format rules (including the Continue field),
        /// efficiency rules, examples, and the full capability list into Builder.
        /// </summary>
        private void AppendRulesAndCapabilities(StringBuilder Builder, string ContextInstruction)
        {
            bool HasCSharp = AICenter.LocalSetting.EnableCSharpCodeUnit;

            // ── 1. ROLE ──────────────────────────────────────────────────────────────
            Builder.AppendLine("You are an AI assistant that controls a Windows PC for the user.");
            Builder.AppendLine("Respond ONLY with one JSON object. No markdown, no explanation, no extra text.");
            Builder.AppendLine();

            // ── 2. RESPONSE FORMAT ───────────────────────────────────────────────────
            Builder.AppendLine("## RESPONSE FORMAT");
            Builder.AppendLine("{");
            Builder.AppendLine("  \"Action\"      : \"<CapabilityName>\",");
            Builder.AppendLine("  \"HasMoreSteps\": <true|false>,");
            Builder.AppendLine("  \"Params\"      : { \"<ParamName>\": <value>, ... },");
            Builder.AppendLine("  \"Reason\"      : \"<one sentence: why you chose this action>\"");
            Builder.AppendLine("}");
            Builder.AppendLine();

            // ── 3. THIS TURN ─────────────────────────────────────────────────────────
            Builder.AppendLine("## THIS TURN");
            Builder.AppendLine(ContextInstruction?.Trim());
            Builder.AppendLine();

            // ── 4. HasMoreSteps RULES ─────────────────────────────────────────────────
            Builder.AppendLine("## HasMoreSteps RULES");
            Builder.AppendLine("  true  → task is NOT finished; more steps will be executed.");
            Builder.AppendLine("  false → task is FULLY completed by this step.");
            Builder.AppendLine("Never invert this logic.");
            Builder.AppendLine();

            // ── 5. EFFICIENCY RULES ───────────────────────────────────────────────────
            Builder.AppendLine("## EFFICIENCY RULES");
            Builder.AppendLine("- Complete the task in 1–3 steps whenever possible.");
            if (HasCSharp)
            {
                Builder.AppendLine("- Use batch capabilities (C#) when > 5 simple steps would be needed.");
            }


            Builder.AppendLine("- After 5 executed steps without completing the task, switch to CMD or C# immediately.");
            Builder.AppendLine();

            // ── 6. SCRIPT OUTPUT RULES ────────────────────────────────────────────────

            if (HasCSharp)
            {
                Builder.AppendLine("## SCRIPT OUTPUT RULES");

                Builder.AppendLine("- C# scripts MUST end with   return <expression>;   to produce a visible result.");
                Builder.AppendLine("  BAD  → Console.WriteLine(result);   // output is invisible");
                Builder.AppendLine("  GOOD → return result;                // result is captured and shown to the user");

                Builder.AppendLine("- If the task does NOT need to return data to the user (e.g. writing a file, moving, deleting), use return null;.");

                Builder.AppendLine();
            }

            if (HasCSharp)
            {
                // ── 7. C# SCRIPT STYLE ────────────────────────────────────────────────────
                Builder.AppendLine("## C# SCRIPT STYLE");
                Builder.AppendLine("- Top-level statements only (no class, no Main method).");
                Builder.AppendLine("- Pre-imported: System, System.IO, System.Linq, System.Collections.Generic,");
            }

            Builder.AppendLine();

            // ── 8. AVAILABLE CAPABILITIES ─────────────────────────────────────────────
            Builder.AppendLine("## AVAILABLE CAPABILITIES");
            foreach (var Cap in GetCapabilities())
            {
                Builder.AppendLine($"### {Cap.Name}");
                Builder.AppendLine($"  {Cap.Description}");
                if (Cap.Params != null && Cap.Params.Count > 0)
                {
                    foreach (var Param in Cap.Params)
                        Builder.AppendLine($"  - {Param.Name} ({Param.Type}): {Param.Description}");
                }
                else
                {
                    Builder.AppendLine("  (no parameters)");
                }
                Builder.AppendLine();
            }

            // ── 9. CRITICAL CONSTRAINTS ───────────────────────────────────────────────
            Builder.AppendLine("## CRITICAL CONSTRAINTS");
            Builder.AppendLine("- Action MUST be one of the names listed under AVAILABLE CAPABILITIES.");
            Builder.AppendLine("- Never produce output through Console.Write / print / log — only via return values.");
            Builder.AppendLine("- Each capability is a standalone operation. You CANNOT call other capabilities");
            Builder.AppendLine("  If you need multiple operations, use HasMoreSteps = true to chain them as separate steps.");
            Builder.AppendLine();
        }

        /// <summary>
        /// Builds the first prompt from a user's natural-language input.
        /// Clears the memory because a new conversation starts.
        /// </summary>
        public string BuildUserPrompt(string UserInput)
        {
            ClearMemory();
            var Builder = new StringBuilder();
            AppendRulesAndCapabilities(Builder,
                "Select the first capability to execute. Always begin by executing a capability.");
            Builder.AppendLine("## USER REQUEST");
            Builder.AppendLine(UserInput?.Trim());
            return Builder.ToString();
        }


        /// <summary>
        /// Builds a follow-up prompt after a capability has executed successfully.
        /// Feeds the result back to the AI, including the whole memory of previous steps.
        /// </summary>
        public string BuildResultPrompt(string UserInput, ExecutionResult Result)
        {
            var Builder = new StringBuilder();
            AppendRulesAndCapabilities(Builder,
                "A capability just executed successfully. " +
                "Review the result and the step history, then decide whether the task is complete " +
                "(HasMoreSteps = false) or another step is required (HasMoreSteps = true).");

            Builder.AppendLine("## USER REQUEST");
            Builder.AppendLine(UserInput?.Trim());
            Builder.AppendLine();

            Builder.AppendLine(FormatMemory());
            Builder.AppendLine();

            Builder.AppendLine("## LAST EXECUTION RESULT");
            Builder.AppendLine($"Action : {Result.Action}");
            Builder.AppendLine($"Status : {Result.Status}");
            Builder.AppendLine($"Reason : {Result.Reason}");

            if (Result.Status == "Failure")
            {
                Builder.AppendLine($"Error  : {Result.ErrorMessage}");
            }
            else if (Result.ReturnValue != null)
            {
                string Return = Result.ReturnValue is string Str
                    ? Str
                    : JsonConvert.SerializeObject(Result.ReturnValue, Formatting.Indented);
                Builder.AppendLine("Output :");
                Builder.AppendLine(Return);
            }
            else
            {
                Builder.AppendLine("Output : (none — void operation)");
            }

            return Builder.ToString();
        }

        /// <summary>
        /// Builds a retry prompt after a capability execution failed.
        /// Feeds the error message and execution history back to the AI,
        /// along with the full capability list so it can pick a different approach.
        /// Memory is intentionally preserved so the AI knows what it already tried.
        /// </summary>
        public string BuildErrorRetryPrompt(string UserInput, ExecutionResult FailedResult)
        {
            bool HasCSharp = AICenter.LocalSetting.EnableCSharpCodeUnit;


            string FailInstruction =
            "The previous action FAILED. " +
            "Read the error details carefully and choose a corrected approach. " +
            "Do NOT repeat the exact same Action + Params — that will fail again. ";

            if (HasCSharp)
                FailInstruction += "If you have already executed 5+ steps, switch to RunCSharpCode immediately.";

            var Builder = new StringBuilder();
            AppendRulesAndCapabilities(Builder,
                FailInstruction);


            Builder.AppendLine("## USER REQUEST");
            Builder.AppendLine(UserInput?.Trim());
            Builder.AppendLine();

            Builder.AppendLine(FormatMemory());
            Builder.AppendLine();

            // ── Full error block ──────────────────────────────────────────────────────
            Builder.AppendLine("## FAILED ACTION — FULL ERROR DETAILS");
            Builder.AppendLine($"Action       : {FailedResult.Action}");
            Builder.AppendLine($"Error type   : {FailedResult.Status}");
            Builder.AppendLine($"Error message: {FailedResult.ErrorMessage}");

            if (!string.IsNullOrWhiteSpace(FailedResult.RawResponse))
            {
                Builder.AppendLine();
                Builder.AppendLine("Raw JSON you sent that caused the failure:");
                Builder.AppendLine(FailedResult.RawResponse);
            }

            Builder.AppendLine();
            Builder.AppendLine("## INSTRUCTIONS");
            int Step = 1;
            Builder.AppendLine($"{Step++}. Identify the root cause from the error message above.");
            Builder.AppendLine($"{Step++}. Pick a different Action, fix the parameters, or rewrite the script.");

            if (HasCSharp)
            {
                Builder.AppendLine($"{Step++}. If the error is a C# compile/runtime error, fix the code and use RunCSharpCode.");
            }

            Builder.AppendLine($"{Step++}. Respond with a corrected JSON action.");

            return Builder.ToString();
        }
        #endregion

        #region AI Response Parsing & Dispatch


        public ExecutionResult AnalysisAndExecuteCapabilities(string AiJsonResponse)
        {
            string GetJson = "";
            var Match = Regex.Match(AiJsonResponse, @"\{[\s\S]*\}");
            if (Match.Success)
            {
                GetJson = Match.Value;
            }

            // --- Step 1: Parse JSON ---
            JObject ParsedJson;
            try
            {
                ParsedJson = JObject.Parse(GetJson);
            }
            catch (JsonException ParseException)
            {
                var FailResult = ExecutionResult.Failure(
                    "ParseError",
                    $"AI response is not valid JSON: {ParseException.Message}",
                    AiJsonResponse,
                    Continue: false   // Cannot recover from malformed JSON
                );
                AddToMemory("ParseError", AiJsonResponse, ParseException.Message, "Failure", "Invalid JSON");
                ClearMemory();        // Unrecoverable — start fresh next time
                return FailResult;
            }

            // --- Step 2: Extract fields ---
            string ActionName = ParsedJson["Action"]?.Value<string>();
            bool HasMoreSteps = ParsedJson["HasMoreSteps"]?.Value<bool>() ?? false; 
            string Reason = ParsedJson["Reason"]?.Value<string>() ?? "(no reason given)";
            bool Continue = ParsedJson["Continue"]?.Value<bool>() ?? false;
            JObject Params = ParsedJson["Params"] as JObject ?? new JObject();
            string ParamsString = Params.ToString(Formatting.None);

            if (string.IsNullOrWhiteSpace(ActionName))
            {
                var FailResult = ExecutionResult.Failure(
                    "MissingAction",
                    "The AI response JSON does not contain an 'Action' field.",
                    AiJsonResponse,
                    Continue: false   // Unrecoverable format error
                );
                AddToMemory("MissingAction", ParamsString, "No Action field", "Failure", Reason);
                ClearMemory();
                return FailResult;
            }

            // --- Step 3: Text-only reply — task is done ---
            if (HasMoreSteps)
            {
                string ReplyText = Params["Message"]?.Value<string>() ?? "";
                var ReplyResult = ExecutionResult.Keep(ActionName,ReplyText, Reason);
                AddToMemory("Reply", ParamsString, ReplyText, "Reply", Reason);
                ClearMemory();   // Task complete
                return ReplyResult;
            }

            // --- Step 4: Dispatch capability ---
            try
            {
                object ResultValue = Dispatch(ActionName, Params);
                string ResultString;
                if (ResultValue == null)
                    ResultString = "(void)";
                else if (ResultValue is string s)
                    ResultString = s;
                else
                    ResultString = JsonConvert.SerializeObject(ResultValue, Formatting.Indented);

                var SuccessResult = ExecutionResult.Success(ActionName, Reason, ResultValue, Continue);
                AddToMemory(ActionName, ParamsString, ResultString, "Success", Reason);

                if (!Continue)
                    ClearMemory();   // Task complete

                return SuccessResult;
            }
            catch (NotSupportedException)
            {
                // The AI named a capability that does not exist.
                // Recoverable: keep memory and let the caller retry with BuildErrorRetryPrompt.
                var FailResult = ExecutionResult.Failure(
                    "UnknownAction",
                    $"No capability named '{ActionName}' is registered. " +
                    $"Please choose one of the listed AVAILABLE CAPABILITIES.",
                    AiJsonResponse,
                    Continue: true   // Caller should retry via BuildErrorRetryPrompt
                );
                AddToMemory(ActionName, ParamsString, "Unknown capability", "Failure", Reason);
                // Do NOT clear memory — AI needs history to pick a valid alternative
                return FailResult;
            }
            catch (Exception DispatchException)
            {
                // The capability exists but threw at runtime (wrong params, IO error, etc.).
                // Recoverable: keep memory and let the caller retry with BuildErrorRetryPrompt.
                var FailResult = ExecutionResult.Failure(
                    "ExecutionError",
                    $"Capability '{ActionName}' threw an exception: {DispatchException.Message}",
                    AiJsonResponse,
                    Continue: true   // Caller should retry via BuildErrorRetryPrompt
                );
                AddToMemory(ActionName, ParamsString, DispatchException.Message, "Failure", Reason);
                // Do NOT clear memory — AI needs history to avoid repeating the same mistake
                return FailResult;
            }
        }

        /// <summary>
        /// Routes the action name to the correct Unit and invokes it.
        /// Throws NotSupportedException if no match is found.
        /// </summary>
        private object Dispatch(string ActionName, JObject Params)
        {
            // ---- MouseUnit ----
            if (MouseUnit.CapabilityManifest.Any(Cap => Cap.Name == ActionName))
            {
                switch (ActionName)
                {
                    case "Click":
                        MouseUnit.Click(
                            Params["X"]?.Value<double>() ?? 0,
                            Params["Y"]?.Value<double>() ?? 0,
                            Params["Mode"]?.Value<int>() ?? 0);
                        return null;
                    case "GetCursorPosition":
                        return MouseUnit.GetCursorPosition();
                }
            }

            // ---- RequestUnit ----
            if (RequestUnit.CapabilityManifest.Any(Cap => Cap.Name == ActionName))
            {
                switch (ActionName)
                {
                    case "HttpGet":
                        return RequestUnit.HttpGet(
                            Params["Url"]?.Value<string>(),
                            Params["TimeoutMs"]?.Value<int>() ?? 10000);
                    case "HttpPost":
                        return RequestUnit.HttpPost(
                            Params["Url"]?.Value<string>(),
                            Params["Body"]?.Value<string>(),
                            Params["ContentType"]?.Value<string>() ?? "application/json",
                            Params["TimeoutMs"]?.Value<int>() ?? 10000);
                }
            }

            // ---- CSharpCodeUnit ----
            if (CSharpCodeUnit.CapabilityManifest.Any(Cap => Cap.Name == ActionName))
            {
                switch (ActionName)
                {
                    case "RunCSharpCode":
                        return CSharpUnit.RunCSharpCode(Params["Code"]?.Value<string>());
                }
            }

            throw new NotSupportedException($"No capability named '{ActionName}' found in any Unit.");
        }

        #endregion
    }

   
    public class ExecutionResult
    {
        public string Status { get; set; }

        public string Action { get; set; }

        /// <summary>The AI's stated reason for choosing this action.</summary>
        public string Reason { get; set; }

        /// <summary>
        /// true  → caller must keep looping (another step is needed).
        /// false → task is done; no further AI calls are required.
        ///
        /// For recoverable failures (UnknownAction, ExecutionError) this is set to true
        /// so the caller knows to retry via BuildErrorRetryPrompt.
        /// </summary>
        public bool Continue { get; set; }

        /// <summary>Value returned by the capability. Null for void methods.</summary>
        public object ReturnValue { get; set; }

        /// <summary>Error description when Status == "Failure".</summary>
        public string ErrorMessage { get; set; }

        /// <summary>Raw AI response string, kept for diagnostics / retry prompts.</summary>
        public string RawResponse { get; set; }

        // ---- Factory methods ----

        public static ExecutionResult Success(string Action, string Reason, object ReturnValue, bool Continue)
            => new ExecutionResult
            {
                Status = "Success",
                Action = Action,
                Reason = Reason,
                ReturnValue = ReturnValue,
                Continue = Continue
            };

        public static ExecutionResult Failure(string Action, string ErrorMessage, string RawResponse, bool Continue)
            => new ExecutionResult
            {
                Status = "Failure",
                Action = Action,
                ErrorMessage = ErrorMessage,
                RawResponse = RawResponse,
                Continue = Continue
            };

        public static ExecutionResult Keep(string Action,string Message, string Reason)
            => new ExecutionResult
            {
                Status = "HasMoreSteps",
                Action = Action,
                Reason = Reason,
                ReturnValue = Message,
                Continue = true
            };
    }
}