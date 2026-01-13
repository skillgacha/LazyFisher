using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using AutoClicker.Core;

namespace AutoClicker.Core
{
    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    public enum ClickType
    {
        Single,
        Double
    }

    public enum RepeatMode
    {
        Infinite,
        Count
    }

    public class ClickerService
    {
        private CancellationTokenSource? _cts;
        private Task? _clickingTask;

        public bool IsRunning => _clickingTask != null && !_clickingTask.IsCompleted;

        public event Action<int>? OnClickPerformed;
        public event Action? OnFinished;

        public void Start(ClickSettings settings)
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _clickingTask = Task.Run(async () =>
            {
                int count = 0;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (settings.Mode == RepeatMode.Count && count >= settings.RepeatCount)
                        {
                            break;
                        }

                        PerformClick(settings);
                        count++;
                        OnClickPerformed?.Invoke(count);

                        if (settings.Interval.TotalMilliseconds > 0)
                        {
                            await Task.Delay(settings.Interval, token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Stopped
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() => OnFinished?.Invoke());
                    _clickingTask = null;
                    _cts = null;
                }
            }, token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private void PerformClick(ClickSettings settings)
        {
            if (settings.FixedPosition.HasValue)
            {
                NativeMethods.SetCursorPos(settings.FixedPosition.Value.X, settings.FixedPosition.Value.Y);
            }

            uint downFlag = 0;
            uint upFlag = 0;

            switch (settings.Button)
            {
                case MouseButton.Left:
                    downFlag = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                    upFlag = NativeMethods.MOUSEEVENTF_LEFTUP;
                    break;
                case MouseButton.Right:
                    downFlag = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                    upFlag = NativeMethods.MOUSEEVENTF_RIGHTUP;
                    break;
                case MouseButton.Middle:
                    downFlag = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                    break;
            }

            SendMouseInput(downFlag);

            // Hold the button if duration is > 0
            if (settings.ClickDuration > 0)
            {
                Thread.Sleep(settings.ClickDuration);
            }

            SendMouseInput(upFlag);

            if (settings.Type == ClickType.Double)
            {
                Thread.Sleep(50);
                SendMouseInput(downFlag);
                if (settings.ClickDuration > 0)
                {
                    Thread.Sleep(settings.ClickDuration);
                }
                SendMouseInput(upFlag);
            }
        }

        private void SendMouseInput(uint flag)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].u.mi.dwFlags = flag;
            NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }
    }

    public class ClickSettings
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
        public MouseButton Button { get; set; } = MouseButton.Left;
        public ClickType Type { get; set; } = ClickType.Single;
        public RepeatMode Mode { get; set; } = RepeatMode.Infinite;
        public int RepeatCount { get; set; } = 100;
        public NativeMethods.POINT? FixedPosition { get; set; } = null;
        public int ClickDuration { get; set; } = 0; // Duration in ms to hold the button
    }
}
