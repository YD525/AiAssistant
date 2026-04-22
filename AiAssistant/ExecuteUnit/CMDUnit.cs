using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using AiAssistant.ExecuteSandbox;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    /// <summary>
    /// Manages a persistent cmd.exe process for executing shell commands
    /// and capturing their output. Automatically shuts down after idle timeout.
    /// </summary>
    public class CMDUnit
    {
        public bool Enable = false;
        #region State

        /// <summary>The underlying cmd.exe process instance.</summary>
        private Process CmdProcess;

        /// <summary>Timestamp of the last command execution, used for idle detection.</summary>
        private DateTime LastActiveTime;

        /// <summary>Timer that periodically checks whether the process has been idle too long.</summary>
        private Timer IdleTimer;

        /// <summary>Milliseconds of inactivity before the process is automatically stopped.</summary>
        private int IdleTimeoutMs = 30000;

        private readonly object StartLock = new object();
        private readonly object ExecLock  = new object();
        private readonly object EventLock = new object();

        /// <summary>Flag set to true while the process is being stopped, to suppress stale output events.</summary>
        private volatile bool IsStopping = false;

        /// <summary>Tracks how many commands are currently being executed.</summary>
        private int RunningCommandCount = 0;

        /// <summary>Event raised whenever a line of output (stdout or stderr) is received.</summary>
        public event Action<string> OnOutput;

        #endregion

        #region Capability Manifest (AI readable)

        public static List<CapabilityInfo> CapabilityManifest = new List<CapabilityInfo>
        {
            new CapabilityInfo
            {
                Name        = "ExecuteAndGetOutput",
                Description = "Execute a CMD command and return its full output as a string",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Command",   Type = "string", Description = "The CMD command to run" },
                    new ParameterInfo { Name = "TimeoutMs", Type = "int",    Description = "Maximum wait time in milliseconds" }
                }
            }
        };

        #endregion

        #region Start / Stop

        /// <summary>Starts the cmd.exe process if it is not already running.</summary>
        public void Start()
            => Sandbox.Exec(nameof(Start), () =>
            {
                // Fast path: already running
                if (CmdProcess != null && !CmdProcess.HasExited)
                    return;

                lock (StartLock)
                {
                    if (CmdProcess != null && !CmdProcess.HasExited)
                        return;

                    IsStopping = false;

                    CmdProcess = new Process();
                    CmdProcess.StartInfo.FileName               = "cmd.exe";
                    CmdProcess.StartInfo.UseShellExecute        = false;
                    CmdProcess.StartInfo.RedirectStandardInput  = true;
                    CmdProcess.StartInfo.RedirectStandardOutput = true;
                    CmdProcess.StartInfo.RedirectStandardError  = true;
                    CmdProcess.StartInfo.CreateNoWindow         = true;
                    CmdProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    CmdProcess.StartInfo.StandardErrorEncoding  = Encoding.UTF8;

                    CmdProcess.OutputDataReceived += (Sender, EventArgs) =>
                    {
                        if (IsStopping || EventArgs.Data == null) return;
                        SafeInvokeOutput(EventArgs.Data);
                    };

                    CmdProcess.ErrorDataReceived += (Sender, EventArgs) =>
                    {
                        if (IsStopping || EventArgs.Data == null) return;
                        SafeInvokeOutput("[ERR] " + EventArgs.Data);
                    };

                    CmdProcess.Start();
                    CmdProcess.BeginOutputReadLine();
                    CmdProcess.BeginErrorReadLine();

                    LastActiveTime = DateTime.Now;

                    IdleTimer?.Dispose();
                    IdleTimer = new Timer(CheckIdle, null, 5000, 5000);
                }
            });

        /// <summary>Sends "exit" to cmd.exe and terminates the process gracefully.</summary>
        public void Stop()
            => Sandbox.Exec(nameof(Stop), () =>
            {
                lock (StartLock)
                {
                    IsStopping = true;

                    IdleTimer?.Dispose();
                    IdleTimer = null;

                    try
                    {
                        if (CmdProcess != null && !CmdProcess.HasExited)
                        {
                            try { CmdProcess.StandardInput.WriteLine("exit"); } catch { }

                            CmdProcess.WaitForExit(2000);
                            CmdProcess.Close();
                        }
                    }
                    catch { }
                }
            });

        /// <summary>Ensures the cmd.exe process is running, starting it if necessary.</summary>
        private void EnsureStarted()
        {
            if (CmdProcess != null && !CmdProcess.HasExited)
                return;

            Start();
        }

        #endregion

        #region Execute

        /// <summary>
        /// Sends a command to cmd.exe and blocks until the output is fully received
        /// or the timeout expires. Returns the captured output as a string.
        /// </summary>
        public string ExecuteAndGetOutput(string Command, int TimeoutMs = 10000)
            => Sandbox.Exec(nameof(ExecuteAndGetOutput), () =>
            {
                lock (ExecLock)
                {
                    EnsureStarted();

                    Interlocked.Increment(ref RunningCommandCount);
                    LastActiveTime = DateTime.Now;

                    var OutputBuffer = new StringBuilder();

                    // A unique marker echoed after the command so we know output is complete
                    string EndMarker = "__END_" + Guid.NewGuid().ToString("N");

                    using (var CompletionEvent = new ManualResetEvent(false))
                    {
                        void OutputHandler(string Line)
                        {
                            if (Line == EndMarker)
                            {
                                RemoveOutputHandler(OutputHandler);
                                CompletionEvent.Set();
                            }
                            else
                            {
                                OutputBuffer.AppendLine(Line);
                            }
                        }

                        AddOutputHandler(OutputHandler);

                        try
                        {
                            CmdProcess.StandardInput.WriteLine(Command);
                            CmdProcess.StandardInput.WriteLine($"echo {EndMarker}");

                            if (!CompletionEvent.WaitOne(TimeoutMs))
                            {
                                RemoveOutputHandler(OutputHandler);
                                throw new TimeoutException($"Command timed out after {TimeoutMs} ms.");
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref RunningCommandCount);
                        }
                    }

                    return OutputBuffer.ToString();
                }
            }, Command, TimeoutMs);

        #endregion

        #region Idle Management

        /// <summary>Timer callback: stops the process if it has been idle longer than IdleTimeoutMs.</summary>
        private void CheckIdle(object State)
        {
            if (CmdProcess == null || CmdProcess.HasExited)
                return;

            if (RunningCommandCount > 0)
                return;

            if ((DateTime.Now - LastActiveTime).TotalMilliseconds > IdleTimeoutMs)
                Stop();
        }

        /// <summary>Sets the idle timeout in milliseconds before the process auto-stops.</summary>
        public void SetIdleTimeout(int Milliseconds) => IdleTimeoutMs = Milliseconds;

        #endregion

        #region Thread-Safe Output Helpers

        /// <summary>Invokes the OnOutput event in a thread-safe manner.</summary>
        private void SafeInvokeOutput(string Text)
        {
            Action<string> Handler;

            lock (EventLock)
                Handler = OnOutput;

            Handler?.Invoke(Text);
        }

        /// <summary>Subscribes an output handler to the OnOutput event under the event lock.</summary>
        private void AddOutputHandler(Action<string> Handler)
        {
            lock (EventLock)
                OnOutput += Handler;
        }

        /// <summary>Unsubscribes an output handler from the OnOutput event under the event lock.</summary>
        private void RemoveOutputHandler(Action<string> Handler)
        {
            lock (EventLock)
                OnOutput -= Handler;
        }

        #endregion
    }
}
