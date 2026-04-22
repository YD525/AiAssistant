using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using AiAssistant.ExecuteSandbox;
using WindowsInput;
using static AiAssistant.ExecuteUnit.UnitHelper;

namespace AiAssistant.ExecuteUnit
{
    /// <summary>
    /// Provides mouse control capabilities: click at screen coordinates and get cursor position.
    /// Supports three click implementations selectable via the Mode parameter.
    /// </summary>
    public class MouseUnit
    {
        #region Capability Manifest (AI readable)

        public static List<CapabilityInfo> CapabilityManifest = new List<CapabilityInfo>
        {
            new CapabilityInfo
            {
                Name        = "Click",
                Description = "Move the mouse to the given screen coordinates and perform a left click",
                Params      = new List<ParameterInfo>
                {
                    new ParameterInfo { Name = "X",    Type = "double", Description = "Screen X coordinate in pixels" },
                    new ParameterInfo { Name = "Y",    Type = "double", Description = "Screen Y coordinate in pixels" },
                    new ParameterInfo { Name = "Mode", Type = "int",    Description = "Click implementation: 0 = InputSimulator, 1 = mouse_event, 2 = SendInput" }
                }
            },
            new CapabilityInfo
            {
                Name        = "GetCursorPosition",
                Description = "Returns the current mouse cursor position as {X, Y} screen coordinates",
                Params      = new List<ParameterInfo>()
            }
        };

        #endregion

        #region WinAPI Declarations

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT Point);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint Flags, int DeltaX, int DeltaY, uint Data, IntPtr ExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint InputCount, INPUT[] Inputs, int StructSize);

        private const uint INPUT_MOUSE          = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP   = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint       Type;
            public MOUSEINPUT MouseInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int    DeltaX;
            public int    DeltaY;
            public uint   MouseData;
            public uint   Flags;
            public uint   Time;
            public IntPtr ExtraInfo;
        }

        #endregion

        #region Public API (sandboxed)

        /// <summary>Returns the current position of the mouse cursor.</summary>
        public Point GetCursorPosition()
            => Sandbox.Exec(nameof(GetCursorPosition), () =>
            {
                GetCursorPos(out POINT CursorPoint);
                return new Point(CursorPoint.X, CursorPoint.Y);
            });

        /// <summary>
        /// Performs a left mouse button click at the specified screen coordinates.
        /// Mode selects the underlying Win32 mechanism used.
        /// </summary>
        public void Click(double X, double Y, int Mode = 0)
            => Sandbox.Exec(nameof(Click), () =>
            {
                ExecuteClickDown(X, Y, Mode);
                ExecuteClickUp(X, Y, Mode);
            }, X, Y, Mode);

        #endregion

        #region Internal Click Helpers

        /// <summary>Sends the mouse button-down event using the selected mode.</summary>
        private void ExecuteClickDown(double X, double Y, int Mode)
        {
            if (Mode == 0)
            {
                var Simulator = new InputSimulator();
                Simulator.Mouse.MoveMouseTo(X, Y);
                Simulator.Mouse.LeftButtonDown();
                return;
            }

            if (Mode == 1)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, Convert.ToInt32(X), Convert.ToInt32(Y), 0, IntPtr.Zero);
                return;
            }

            // Mode == 2: SendInput
            SendMouseInput(Convert.ToInt32(X), Convert.ToInt32(Y), MOUSEEVENTF_LEFTDOWN);
        }

        /// <summary>Sends the mouse button-up event using the selected mode.</summary>
        private void ExecuteClickUp(double X, double Y, int Mode)
        {
            if (Mode == 0)
            {
                var Simulator = new InputSimulator();
                Simulator.Mouse.MoveMouseTo(X, Y);
                Simulator.Mouse.LeftButtonUp();
                return;
            }

            if (Mode == 1)
            {
                mouse_event(MOUSEEVENTF_LEFTUP, Convert.ToInt32(X), Convert.ToInt32(Y), 0, IntPtr.Zero);
                return;
            }

            // Mode == 2: SendInput
            SendMouseInput(Convert.ToInt32(X), Convert.ToInt32(Y), MOUSEEVENTF_LEFTUP);
        }

        /// <summary>Constructs and sends a single mouse INPUT structure via SendInput.</summary>
        private static void SendMouseInput(int X, int Y, uint Flags)
        {
            INPUT[] InputArray = new INPUT[1];

            InputArray[0] = new INPUT
            {
                Type = INPUT_MOUSE,
                MouseInput = new MOUSEINPUT
                {
                    DeltaX    = X,
                    DeltaY    = Y,
                    Flags     = Flags
                }
            };

            SendInput(1, InputArray, Marshal.SizeOf(typeof(INPUT)));
        }

        #endregion
    }
}
