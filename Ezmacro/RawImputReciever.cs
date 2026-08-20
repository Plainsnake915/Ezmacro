using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Ezmacro
{
    

    public static class RawInputReceiver
    {
        private const int RID_INPUT = 0x10000003;
        private const int WM_INPUT = 0x00FF;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWMOUSE
        {
            public ushort usFlags;
            public uint ulButtons;
            public uint ulRawButtons;
            public int lLastX; // Relative movement X
            public int lLastY; // Relative movement Y
            public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWMOUSE mouse;
        }

        [DllImport("User32.dll")]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, out RAWINPUT pData, ref uint pcbSize, uint cbSizeHeader);

        // Call this during Window Initialization
        public static void Register(IntPtr windowHandle)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // Generic desktop controls
            rid[0].usUsage = 0x02;     // Mouse
            rid[0].dwFlags = 0x00000100; // RIDEV_INPUTSINK: Capture input even when app is in background
            rid[0].hwndTarget = windowHandle;

            RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        public static bool ProcessMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, out int deltaX, out int deltaY)
        {
            deltaX = 0;
            deltaY = 0;

            if (msg == WM_INPUT)
            {
                uint dwSize = (uint)Marshal.SizeOf(typeof(RAWINPUT));
                if (GetRawInputData(lParam, RID_INPUT, out RAWINPUT raw, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != uint.MaxValue)
                {
                    // lLastX and lLastY contain raw relative movement values
                    deltaX = raw.mouse.lLastX;
                    deltaY = raw.mouse.lLastY;
                    return true;
                }
            }
            return false;
        }
    }
}
