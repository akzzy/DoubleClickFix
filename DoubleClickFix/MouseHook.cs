﻿using DoubleClickFix.Properties;
using Microsoft.Win32;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static DoubleClickFix.NativeMethods;

namespace DoubleClickFix
{
    internal class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private readonly Settings settings;
        private readonly ILogger logger;

        // make sure we keep a reference so it's not garbage collected
        private LowLevelMouseProc? mouseProc;
        private IntPtr hookHandle = IntPtr.Zero;

        private uint previousUpTime = 0;

        public MouseHook(Settings settings, ILogger logger)
        {
            this.settings = settings;
            this.logger = logger;
        }

        public bool Install()
        {
            if (settings.UseHook && hookHandle == IntPtr.Zero)
            {
                mouseProc = this.HookCallback;
                hookHandle = SetHook(mouseProc);
            }
            return hookHandle != IntPtr.Zero;
        }

        public void Uninstall()
        {
            if (settings.UseHook && hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
                mouseProc = null;
            }
        }

        /// <summary>
        /// Same as calling Uninstall().
        /// </summary>
        public void Dispose()
        {
            Uninstall();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // We take the elapsed time between the last mouse up and the current mouse down event.
            // If it's smaller than the minimal delay, we ignore the current mouse down event.
            if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_LBUTTONUP))
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT))!;
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    long timeDifference = hookStruct.time - previousUpTime;
                    bool ignore = timeDifference < settings.MinimumDoubleClickDelayMilliseconds;
                    if (ignore)
                    {
                        logger.Log($"{Resources.IgnoredDoubleClick}: {timeDifference} ms");
                        previousUpTime = 0;
                        return (IntPtr)1;
                    } else
                    {
                        if (timeDifference < settings.WindowsDoubleClickTimeMilliseconds)
                        {
                            logger.Log($"{timeDifference} ms");
                        }
                    }
                }
                else if (wParam == (IntPtr)WM_LBUTTONUP)
                {
                    previousUpTime = hookStruct.time;

                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using ProcessModule currentModule = Process.GetCurrentProcess().MainModule!;
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(currentModule.ModuleName), 0);
        }
    }
}