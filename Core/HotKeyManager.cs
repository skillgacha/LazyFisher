using System;
using System.Windows.Interop;
using LazyFisher.Core;

namespace LazyFisher.Core
{
    public class HotKeyManager
    {
        private IntPtr _hWnd;
        private const int HOTKEY_ID = 9000;

        public event Action? HotKeyPressed;

        public HotKeyManager(IntPtr hWnd)
        {
            _hWnd = hWnd;
            HwndSource source = HwndSource.FromHwnd(_hWnd);
            source?.AddHook(HwndHook);
        }

        public bool Register(uint modifiers, uint vk)
        {
            Unregister();
            return NativeMethods.RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, vk);
        }

        public void Unregister()
        {
            NativeMethods.UnregisterHotKey(_hWnd, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                if (wParam.ToInt32() == HOTKEY_ID)
                {
                    HotKeyPressed?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
