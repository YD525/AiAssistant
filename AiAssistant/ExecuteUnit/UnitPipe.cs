using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AiAssistant.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    public class UnitPipe
    {
        #region Unit Instances

        public IOUnit IoUnit = new IOUnit();
        public CMDUnit CmdUnit = new CMDUnit();
        public MouseUnit MouseUnit = new MouseUnit();
        public RequestUnit RequestUnit = new RequestUnit();
        public WinApiUnit WinApiUnit = new WinApiUnit();
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
            if (IoUnit.Enable) AllCapabilities.AddRange(IOUnit.CapabilityManifest);
            if (CmdUnit.Enable) AllCapabilities.AddRange(CMDUnit.CapabilityManifest);
            if (MouseUnit.Enable) AllCapabilities.AddRange(MouseUnit.CapabilityManifest);
            if (RequestUnit.Enable) AllCapabilities.AddRange(RequestUnit.CapabilityManifest);
            if (WinApiUnit.Enable) AllCapabilities.AddRange(WinApiUnit.CapabilityManifest);
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
            Builder.AppendLine("You are an AI assistant that controls a Windows PC on behalf of the user.");
            Builder.AppendLine("You must respond ONLY with a single JSON object — no explanation, no markdown, no extra text.");
            Builder.AppendLine();

            Builder.AppendLine("=== EFFICIENCY RULES ===");
            Builder.AppendLine("1. If completing the user request would require MORE THAN 5 STEPS using simple capabilities (e.g., GetFiles, DeleteToRecycleBin, Copy, Move, Click, etc.), you MUST instead use a batch capability.");
            Builder.AppendLine("2. Only use single-item capabilities when the task involves exactly one item or when a batch command is impossible (e.g., conditional logic per file).");
            Builder.AppendLine("3. Always prefer completing the entire task in 1–3 steps over many small steps.");
            Builder.AppendLine("4. If you already executed 5 or more steps and the task is not finished, stop and switch to a CMD or C# approach immediately.");
            Builder.AppendLine();

            Builder.AppendLine("=== RESPONSE FORMAT ===");
            Builder.AppendLine("{");
            Builder.AppendLine("  \"Action\"       : \"<CapabilityName>\",");
            Builder.AppendLine("  \"HasMoreSteps\" : <true | false>,");
            Builder.AppendLine("  \"Params\"       : { \"<ParamName>\": <value> },");
            Builder.AppendLine("  \"Reason\"       : \"<one sentence: why you chose this action>\"");
            Builder.AppendLine("}");
            Builder.AppendLine();

            Builder.AppendLine("=== STEP CONTROL RULES ===");
            Builder.AppendLine("HasMoreSteps indicates whether the task requires additional execution steps.");
            Builder.AppendLine();
            Builder.AppendLine("  true  — the task is NOT finished and more capabilities must be executed.");
            Builder.AppendLine("  false — the task is fully completed in this step.");
            Builder.AppendLine();
            Builder.AppendLine("STRICT RULES:");
            Builder.AppendLine("1. If the task is not fully completed, you MUST set HasMoreSteps = true.");
            Builder.AppendLine("2. If the task is fully completed in this step, you MUST set HasMoreSteps = false.");
            Builder.AppendLine("3. NEVER invert this logic.");
            Builder.AppendLine();

            Builder.AppendLine(ContextInstruction);
            Builder.AppendLine();

            Builder.AppendLine("=== AVAILABLE CAPABILITIES ===");

            foreach (CapabilityInfo Capability in GetCapabilities())
            {
                Builder.AppendLine($"[{Capability.Name}]");
                Builder.AppendLine($"  Description : {Capability.Description}");

                if (Capability.Params != null && Capability.Params.Count > 0)
                {
                    Builder.AppendLine("  Parameters  :");
                    foreach (UnitHelper.ParameterInfo Param in Capability.Params)
                        Builder.AppendLine($"    - {Param.Name} ({Param.Type}): {Param.Description}");
                }
                else
                {
                    Builder.AppendLine("  Parameters  : none");
                }

                Builder.AppendLine();
            }

            Builder.AppendLine("=== CRITICAL RULES ===");
            Builder.AppendLine("1. Action MUST ALWAYS be one of the AVAILABLE CAPABILITIES.");
            Builder.AppendLine("2. All outputs must be produced through capability execution results.");
            Builder.AppendLine("3. The model must always select an Action before responding.");
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
                 "Select the first capability to execute based on the user request. " +
                "You must always start by executing a capability.");

            Builder.AppendLine("=== USER REQUEST ===");
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
                 "Analyze the execution result below. " +
                 "Use the history to avoid repeating actions. " +
                 "Decide whether further steps are required based ONLY on HasMoreSteps rules.");

            Builder.AppendLine("=== ORIGINAL USER REQUEST ===");
            Builder.AppendLine(UserInput?.Trim());
            Builder.AppendLine();

            Builder.AppendLine(FormatMemory());
            Builder.AppendLine();

            Builder.AppendLine("=== MOST RECENT EXECUTION RESULT ===");
            Builder.AppendLine($"Action : {Result.Action}");
            Builder.AppendLine($"Status : {Result.Status}");
            Builder.AppendLine($"Reason : {Result.Reason}");

            if (Result.Status == "Failure")
            {
                Builder.AppendLine($"Error  : {Result.ErrorMessage}");
            }
            else if (Result.ReturnValue != null)
            {
                string ReturnText = Result.ReturnValue is string StringValue
                    ? StringValue
                    : JsonConvert.SerializeObject(Result.ReturnValue, Formatting.Indented);

                Builder.AppendLine("Output :");
                Builder.AppendLine(ReturnText);
            }
            else
            {
                Builder.AppendLine("Output : (none)");
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
            var Builder = new StringBuilder();

            AppendRulesAndCapabilities(Builder,
                "The previous action FAILED with an error. " +
                "Carefully read the error message and the step history below. " +
                "Do NOT repeat the exact same action with the same parameters — that will fail again. " +
                "Choose a different capability, fix the parameters, or switch to a CMD / C# approach. " +
                "If you already used 5 or more steps, you MUST switch to a batch CMD or C# solution immediately. " +
                "Set Continue = true if more steps are still needed after your fix.");

            Builder.AppendLine("=== ORIGINAL USER REQUEST ===");
            Builder.AppendLine(UserInput?.Trim());
            Builder.AppendLine();

            // Include full step history so AI knows what has already been attempted
            Builder.AppendLine(FormatMemory());
            Builder.AppendLine();

            Builder.AppendLine("=== FAILED ACTION (just now) ===");
            Builder.AppendLine($"Action : {FailedResult.Action}");
            Builder.AppendLine($"Error  : {FailedResult.ErrorMessage}");

            if (!string.IsNullOrWhiteSpace(FailedResult.RawResponse))
            {
                Builder.AppendLine($"Raw AI Response that caused the failure:");
                Builder.AppendLine(FailedResult.RawResponse);
            }

            Builder.AppendLine();
            Builder.AppendLine("Analyze the error, then respond with a corrected JSON action.");

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
                string ResultString = ResultValue != null
                    ? JsonConvert.SerializeObject(ResultValue, Formatting.Indented)
                    : "(void)";

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
            // ---- IOUnit ----
            if (IOUnit.CapabilityManifest.Any(Cap => Cap.Name == ActionName))
            {
                switch (ActionName)
                {
                    case "GetFiles":
                        return IoUnit.GetFiles(
                            Params["Path"]?.Value<string>(),
                            Params["Recursive"]?.Value<bool>() ?? false);
                    case "ReadText":
                        return IoUnit.ReadText(Params["Path"]?.Value<string>());
                    case "WriteText":
                        IoUnit.WriteText(
                            Params["Path"]?.Value<string>(),
                            Params["Content"]?.Value<string>(),
                            Params["Overwrite"]?.Value<bool>() ?? true);
                        return null;
                    case "Move":
                        IoUnit.Move(
                            Params["Source"]?.Value<string>(),
                            Params["Dest"]?.Value<string>(),
                            Params["Overwrite"]?.Value<bool>() ?? true);
                        return null;
                    case "Copy":
                        IoUnit.Copy(
                            Params["Source"]?.Value<string>(),
                            Params["Dest"]?.Value<string>(),
                            Params["Overwrite"]?.Value<bool>() ?? true);
                        return null;
                    case "DeleteToRecycleBin":
                        IoUnit.DeleteToRecycleBin(Params["Path"]?.Value<string>());
                        return null;
                    case "CreateDirectory":
                        IoUnit.CreateDirectory(Params["Path"]?.Value<string>());
                        return null;
                }
            }

            // ---- CMDUnit ----
            if (CMDUnit.CapabilityManifest.Any(Cap => Cap.Name == ActionName))
            {
                switch (ActionName)
                {
                    case "ExecuteAndGetOutput":
                        return CmdUnit.ExecuteAndGetOutput(
                            Params["Command"]?.Value<string>(),
                            Params["TimeoutMs"]?.Value<int>() ?? 10000);
                }
            }

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

            // ---- WinApiUnit ----
            if (WinApiUnit.CapabilityManifest.Any(Cap => Cap.Name == ActionName))
            {
                switch (ActionName)
                {
                    case "GetForegroundWindow":
                        return WinApiUnit.GetForegroundWindowHandle();
                    case "FindWindow":
                        return WinApiUnit.FindWindowHandle(
                            Params["ClassName"]?.Value<string>(),
                            Params["Title"]?.Value<string>());
                    case "FindWindows":
                        return WinApiUnit.FindAllWindows();
                    case "GetWindowText":
                        return WinApiUnit.GetWindowTitle(
                            new IntPtr(Params["Hwnd"]?.Value<long>() ?? 0));
                    case "SetWindowText":
                        return WinApiUnit.SetWindowTitle(
                            new IntPtr(Params["Hwnd"]?.Value<long>() ?? 0),
                            Params["Text"]?.Value<string>());
                    case "EnumProcesses":
                        return WinApiUnit.EnumProcesses();
                    case "KillProcess":
                        return WinApiUnit.KillProcess(
                            Params["Pid"]?.Value<int>() ?? -1,
                            Params["Name"]?.Value<string>());
                    case "GetProcessIdUnderMouse":
                        return WinApiUnit.GetProcessIdUnderMouse();
                    case "GetProcessInfo":
                        return WinApiUnit.GetProcessInfo(
                            Params["Pid"]?.Value<int>() ?? -1);
                    case "GetProcessUnderMouse":
                        return WinApiUnit.GetProcessUnderMouse();
                    case "SendMessage":
                        return WinApiUnit.SendWindowMessage(
                            new IntPtr(Params["Hwnd"]?.Value<long>() ?? 0),
                            Params["Msg"]?.Value<int>() ?? 0,
                            new IntPtr(Params["WParam"]?.Value<long>() ?? 0),
                            new IntPtr(Params["LParam"]?.Value<long>() ?? 0));
                    case "PostMessage":
                        return WinApiUnit.PostWindowMessage(
                            new IntPtr(Params["Hwnd"]?.Value<long>() ?? 0),
                            Params["Msg"]?.Value<int>() ?? 0,
                            new IntPtr(Params["WParam"]?.Value<long>() ?? 0),
                            new IntPtr(Params["LParam"]?.Value<long>() ?? 0));
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