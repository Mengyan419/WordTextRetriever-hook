using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HookSendInput1
{
    public class ViewModel
    {
        public static IntPtr hookId = IntPtr.Zero;

        public static LowLevelMouseProc mouseProc = MouseHookCallback;

        public const int WH_MOUSE_LL = 14;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;

        public static bool isDragging = false;
        public static POINT startPoint;


        public static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_LBUTTONDOWN)
                {
                    isDragging = true;
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    startPoint = hookStruct.pt;
                }
                else if (msg == WM_LBUTTONUP)
                {
                    if (isDragging)
                    {
                        MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        POINT endPoint = hookStruct.pt;

                        int deltaX = endPoint.x - startPoint.x;
                        int deltaY = endPoint.y - startPoint.y;
                        double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                        if (distance > 5)
                        {
                            // 使用异步方法，避免阻塞 UI 线程
                            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                            {
                                // 使用 SendInput 模拟 Ctrl+C
                                SimulateCtrlCWithSendInput();

                                // 延迟一段时间，等待复制操作完成
                                await Task.Delay(100);

                                // 从剪贴板获取内容
                                GetClipboardText();
                            }));
                        }
                        isDragging = false;
                    }
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        // 使用 SendInput 替代 keybd_event
        public static void SimulateCtrlCWithSendInput()
        {
            INPUT[] inputs = new INPUT[4];

            // 按下 Ctrl
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = VK_CONTROL;
            inputs[0].U.ki.dwFlags = 0;

            // 按下 C
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = C_KEY;
            inputs[1].U.ki.dwFlags = 0;

            // 释放 C
            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].U.ki.wVk = C_KEY;
            inputs[2].U.ki.dwFlags = KEYEVENTF_KEYUP;

            // 释放 Ctrl
            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].U.ki.wVk = VK_CONTROL;
            inputs[3].U.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        public static void GetClipboardText()
        {
            try
            {
                // 确保在 UI 线程中访问剪贴板
                IDataObject data = Clipboard.GetDataObject();
                if (data.GetDataPresent(DataFormats.Text))
                {
                    string selectedText = (string)data.GetData(DataFormats.Text);
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        // 清空并更新 TextBox
                        mainWindow.OutputTextBox.Clear();
                        mainWindow.OutputTextBox.AppendText("选中的文字：" + selectedText + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.OutputTextBox.AppendText("获取剪贴板内容时出错：" + ex.Message + Environment.NewLine);
                }
            }
        }

        // 定义 INPUT 结构和相关常量
        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;

            public static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT)); }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        public const int INPUT_KEYBOARD = 1;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const ushort VK_CONTROL = 0x11;
        public const ushort C_KEY = 0x43;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
    LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk,
            int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto,
            SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }



}

