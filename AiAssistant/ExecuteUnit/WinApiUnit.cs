using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AiAssistant.ExecuteSandbox;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    /// <summary>
    /// Wraps common Win32 window and process management APIs.
    /// Includes window enumeration, process inspection, kill, and message sending.
    /// All public methods are sandboxed via Sandbox.Exec.
    /// </summary>
    public class WinApiUnit
    {
        public bool Enable = false;
        #region Data Models

        /// <summary>Holds basic information about a running process.</summary>
        public class ProcessInfo
        {
            /// <summary>Process identifier.</summary>
            public int    Pid;

            /// <summary>Process name without the .exe extension.</summary>
            public string Name;

            /// <summary>Full path to the executable, or "ACCESS_DENIED" if the process is protected.</summary>
            public string Path;
        }

        #endregion

        #region Capability Manifest (AI readable)

        public static List<CapabilityInfo> CapabilityManifest = new List<CapabilityInfo>
        {
            new CapabilityInfo
            {
                Name        = "GetForegroundWindow",
                Description = "Returns the handle of the window that currently has keyboard focus"
            },
            new CapabilityInfo
            {
                Name        = "FindWindow",
                Description = "Find a top-level window by its class name and/or title text",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "ClassName", Type = "string", Description = "Window class name (pass null to ignore)" },
                    new ParameterInfo { Name = "Title",     Type = "string", Description = "Window title text (pass null to ignore)" }
                }
            },
            new CapabilityInfo
            {
                Name        = "FindWindows",
                Description = "Enumerate and return all top-level window handles"
            },
            new CapabilityInfo
            {
                Name        = "GetWindowText",
                Description = "Get the title bar text of a window",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Hwnd", Type = "IntPtr", Description = "Target window handle" }
                }
            },
            new CapabilityInfo
            {
                Name        = "SetWindowText",
                Description = "Change the title bar text of a window",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Hwnd", Type = "IntPtr", Description = "Target window handle" },
                    new ParameterInfo { Name = "Text", Type = "string", Description = "New title text" }
                }
            },
            new CapabilityInfo
            {
                Name        = "EnumProcesses",
                Description = "Enumerate all top-level windows and return their handles paired with process IDs"
            },
            new CapabilityInfo
            {
                Name        = "KillProcess",
                Description = "Terminate a process by PID or by process name",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Pid",  Type = "int",    Description = "Process ID to kill; pass -1 to use Name instead" },
                    new ParameterInfo { Name = "Name", Type = "string", Description = "Process name (without .exe); used when Pid is -1" }
                }
            },
            new CapabilityInfo
            {
                Name        = "GetProcessIdUnderMouse",
                Description = "Returns the PID of the window currently under the mouse cursor"
            },
            new CapabilityInfo
            {
                Name        = "GetProcessInfo",
                Description = "Returns the name and executable path of a process given its PID",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Pid", Type = "int", Description = "Target process identifier" }
                }
            },
            new CapabilityInfo
            {
                Name        = "GetProcessUnderMouse",
                Description = "High-level helper that returns full ProcessInfo for the window under the mouse cursor"
            },
            new CapabilityInfo
            {
                Name        = "PostMessage",
                Description = "Post an asynchronous Win32 message to a window's message queue",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "Hwnd",   Type = "IntPtr", Description = "Target window handle" },
                    new ParameterInfo { Name = "Msg",    Type = "int",    Description = "Windows message identifier" },
                    new ParameterInfo { Name = "WParam", Type = "IntPtr", Description = "First message parameter" },
                    new ParameterInfo { Name = "LParam", Type = "IntPtr", Description = "Second message parameter" }
                }
            }
        };

        #endregion

        #region WinAPI Native Declarations

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr WindowHandle, StringBuilder TextBuffer, int BufferSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetWindowText(IntPtr WindowHandle, string Text);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string ClassName, string WindowTitle);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc Callback, IntPtr Parameter);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr WindowHandle, out int ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr WindowHandle, int Message, IntPtr WParam, IntPtr LParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr WindowHandle, int Message, IntPtr WParam, IntPtr LParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT Point);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        public delegate bool EnumWindowsProc(IntPtr WindowHandle, IntPtr Parameter);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        #region Window APIs

        /// <summary>Returns the handle of the currently active (foreground) window.</summary>
        public IntPtr GetForegroundWindowHandle()
            => Sandbox.Exec(nameof(GetForegroundWindowHandle), () => GetForegroundWindow());

        /// <summary>Finds a top-level window matching the given class name and/or title.</summary>
        public IntPtr FindWindowHandle(string ClassName, string Title)
            => Sandbox.Exec(nameof(FindWindowHandle), () => FindWindow(ClassName, Title), ClassName, Title);

        /// <summary>Returns handles of all currently visible top-level windows.</summary>
        public List<IntPtr> FindAllWindows()
            => Sandbox.Exec(nameof(FindAllWindows), () =>
            {
                var WindowList = new List<IntPtr>();

                EnumWindows((WindowHandle, Parameter) =>
                {
                    WindowList.Add(WindowHandle);
                    return true;
                }, IntPtr.Zero);

                return WindowList;
            });

        /// <summary>Returns the title bar text of the specified window.</summary>
        public string GetWindowTitle(IntPtr WindowHandle)
            => Sandbox.Exec(nameof(GetWindowTitle), () =>
            {
                var TextBuffer = new StringBuilder(256);
                GetWindowText(WindowHandle, TextBuffer, TextBuffer.Capacity);
                return TextBuffer.ToString();
            }, WindowHandle);

        /// <summary>Changes the title bar text of the specified window.</summary>
        public bool SetWindowTitle(IntPtr WindowHandle, string Text)
            => Sandbox.Exec(nameof(SetWindowTitle), () =>
                SetWindowText(WindowHandle, Text), WindowHandle, Text);

        #endregion

        #region Process APIs

        /// <summary>Enumerates all top-level windows and returns (handle, PID) pairs.</summary>
        public List<(IntPtr Handle, int Pid)> EnumProcesses()
            => Sandbox.Exec(nameof(EnumProcesses), () =>
            {
                var ResultList = new List<(IntPtr, int)>();

                EnumWindows((WindowHandle, Parameter) =>
                {
                    GetWindowThreadProcessId(WindowHandle, out int ProcessId);
                    ResultList.Add((WindowHandle, ProcessId));
                    return true;
                }, IntPtr.Zero);

                return ResultList;
            });

        /// <summary>
        /// Kills a process by PID or by name.
        /// Pass Pid > 0 to target by identifier; otherwise supply a Name.
        /// Returns true if at least one process was terminated.
        /// </summary>
        public bool KillProcess(int Pid = -1, string Name = null)
            => Sandbox.Exec(nameof(KillProcess), () =>
            {
                try
                {
                    if (Pid > 0)
                    {
                        Process.GetProcessById(Pid).Kill();
                        return true;
                    }

                    if (!string.IsNullOrEmpty(Name))
                    {
                        Process[] MatchingProcesses = Process.GetProcessesByName(Name);

                        foreach (Process TargetProcess in MatchingProcesses)
                            try { TargetProcess.Kill(); } catch { }

                        return MatchingProcesses.Length > 0;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
            }, Pid, Name);

        #endregion

        #region Mouse → Process Chain

        /// <summary>Returns the PID of the window that is currently under the mouse cursor.</summary>
        public int GetProcessIdUnderMouse()
            => Sandbox.Exec(nameof(GetProcessIdUnderMouse), () =>
            {
                GetCursorPos(out POINT CursorPoint);

                IntPtr WindowHandle = WindowFromPoint(CursorPoint);
                if (WindowHandle == IntPtr.Zero)
                    return -1;

                GetWindowThreadProcessId(WindowHandle, out int ProcessId);
                return ProcessId;
            });

        /// <summary>Returns the name and executable path for the given process ID.</summary>
        public ProcessInfo GetProcessInfo(int Pid)
            => Sandbox.Exec(nameof(GetProcessInfo), () =>
            {
                try
                {
                    Process TargetProcess = Process.GetProcessById(Pid);

                    string ExecutablePath;
                    try
                    {
                        ExecutablePath = TargetProcess.MainModule?.FileName;
                    }
                    catch
                    {
                        ExecutablePath = "ACCESS_DENIED";
                    }

                    return new ProcessInfo
                    {
                        Pid  = Pid,
                        Name = TargetProcess.ProcessName,
                        Path = ExecutablePath
                    };
                }
                catch
                {
                    return null;
                }
            }, Pid);

        /// <summary>Combines GetProcessIdUnderMouse and GetProcessInfo into a single convenient call.</summary>
        public ProcessInfo GetProcessUnderMouse()
            => Sandbox.Exec(nameof(GetProcessUnderMouse), () =>
            {
                int ProcessId = GetProcessIdUnderMouse();
                if (ProcessId <= 0)
                    return null;

                return GetProcessInfo(ProcessId);
            });

        #endregion

        #region Message APIs

        /// <summary>Sends a synchronous Win32 message to the specified window.</summary>
        public IntPtr SendWindowMessage(IntPtr WindowHandle, int Message, IntPtr WParam, IntPtr LParam)
            => Sandbox.Exec(nameof(SendWindowMessage), () =>
                SendMessage(WindowHandle, Message, WParam, LParam),
                WindowHandle, Message, WParam, LParam);

        /// <summary>Posts an asynchronous Win32 message to the specified window's message queue.</summary>
        public bool PostWindowMessage(IntPtr WindowHandle, int Message, IntPtr WParam, IntPtr LParam)
            => Sandbox.Exec(nameof(PostWindowMessage), () =>
                PostMessage(WindowHandle, Message, WParam, LParam),
                WindowHandle, Message, WParam, LParam);

        #endregion
    }
}
