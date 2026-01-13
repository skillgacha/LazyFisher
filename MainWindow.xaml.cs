using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using AutoClicker.Core;

namespace AutoClicker
{
    public partial class MainWindow : Window
    {
        private ClickerService _clickerService;
        private HotKeyManager? _hotKeyManager;
        private bool _isPickingLocation = false;
        private DispatcherTimer? _pickingTimer;

        // Current Hotkey State
        private int _currentHotKey = 0x75; // F6
        private int _currentModifiers = 0; // None

        public MainWindow()
        {
            InitializeComponent();
            _clickerService = new ClickerService();
            _clickerService.OnFinished += ClickerService_OnFinished;
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = AppSettings.Load();

            TxtHours.Text = settings.Hours.ToString();
            TxtMins.Text = settings.Minutes.ToString();
            TxtSecs.Text = settings.Seconds.ToString();
            TxtMillis.Text = settings.Milliseconds.ToString();

            CboButton.SelectedIndex = settings.MouseButtonIndex;
            CboType.SelectedIndex = settings.ClickTypeIndex;

            if (settings.RepeatInfinite) RadioRepeatInfinite.IsChecked = true;
            else RadioRepeatCount.IsChecked = true;
            TxtRepeatCount.Text = settings.RepeatCount.ToString();

            if (settings.PositionCurrent) RadioPosCurrent.IsChecked = true;
            else RadioPosFixed.IsChecked = true;

            TxtPosX.Text = settings.FixedX.ToString();
            TxtPosY.Text = settings.FixedY.ToString();

            TxtDuration.Text = settings.ClickDuration.ToString();

            ChkAlwaysOnTop.IsChecked = settings.AlwaysOnTop;

            if (settings.WindowWidth > 100 && settings.WindowHeight > 100)
            {
                Width = settings.WindowWidth;
                Height = settings.WindowHeight;
            }

            // Load Hotkey
            _currentHotKey = settings.HotKey;
            _currentModifiers = settings.HotKeyModifiers;
            UpdateHotKeyText();
        }

        private void SaveSettings()
        {
            var settings = new AppSettings();

            int.TryParse(TxtHours.Text, out int h); settings.Hours = h;
            int.TryParse(TxtMins.Text, out int m); settings.Minutes = m;
            int.TryParse(TxtSecs.Text, out int s); settings.Seconds = s;
            int.TryParse(TxtMillis.Text, out int ms); settings.Milliseconds = ms;

            settings.MouseButtonIndex = CboButton.SelectedIndex;
            settings.ClickTypeIndex = CboType.SelectedIndex;

            settings.RepeatInfinite = RadioRepeatInfinite.IsChecked == true;
            int.TryParse(TxtRepeatCount.Text, out int rc); settings.RepeatCount = rc;

            settings.PositionCurrent = RadioPosCurrent.IsChecked == true;
            int.TryParse(TxtPosX.Text, out int x); settings.FixedX = x;
            int.TryParse(TxtPosY.Text, out int y); settings.FixedY = y;

            int.TryParse(TxtDuration.Text, out int cd); settings.ClickDuration = cd;

            settings.AlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;

            settings.WindowHeight = ActualHeight;

            settings.HotKey = _currentHotKey;
            settings.HotKeyModifiers = _currentModifiers;

            settings.Save();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr handle = new WindowInteropHelper(this).Handle;
            _hotKeyManager = new HotKeyManager(handle);
            _hotKeyManager.HotKeyPressed += OnHotKeyPressed;

            // Register Initial Hotkey
            RegisterCurrentHotKey();
        }

        private void RegisterCurrentHotKey()
        {
            if (_hotKeyManager == null) return;

            _hotKeyManager.Unregister();
            if (!_hotKeyManager.Register((uint)_currentModifiers, (uint)_currentHotKey))
            {
                // Optionally warn on failure, but be careful not to spam on startup
                // MessageBox.Show("ホットキーの登録に失敗しました。");
            }
            UpdateStatus(_clickerService.IsRunning);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            _hotKeyManager?.Unregister();
            _clickerService?.Stop();
            base.OnClosed(e);
        }

        private void OnHotKeyPressed()
        {
            if (_isPickingLocation)
            {
                // If picking, F6 could select the location
                StopPicking();
            }
            else
            {
                ToggleClicker();
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            ToggleClicker();
        }

        private void ToggleClicker()
        {
            if (_clickerService.IsRunning)
            {
                _clickerService.Stop();
                UpdateStatus(false);
            }
            else
            {
                StartClicker();
            }
        }

        private void StartClicker()
        {
            try
            {
                var settings = new ClickSettings
                {
                    Interval = GetInterval(),
                    Button = GetMouseButton(),
                    Type = GetClickType(),
                    Mode = RadioRepeatInfinite.IsChecked == true ? RepeatMode.Infinite : RepeatMode.Count,
                    RepeatCount = int.TryParse(TxtRepeatCount.Text, out int rc) ? rc : 100,
                    FixedPosition = GetFixedPosition(),
                    ClickDuration = int.TryParse(TxtDuration.Text, out int cd) ? cd : 0
                };

                _clickerService.Start(settings);
                UpdateStatus(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"開始エラー: {ex.Message}");
            }
        }

        private void ClickerService_OnFinished()
        {
            UpdateStatus(false);
        }

        private void UpdateStatus(bool isRunning)
        {
            string keyName = GetKeyName(_currentHotKey, _currentModifiers);
            BtnStart.Content = isRunning ? $"停止 ({keyName})" : $"開始 ({keyName})";
            BtnStart.Background = isRunning ?
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc3545")) : // Red
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28a745")); // Green

            // Disable inputs while running
            if (Content is Grid grid)
            {
                foreach (UIElement child in grid.Children)
                {
                    if (child is GroupBox gb)
                    {
                        gb.IsEnabled = !isRunning;
                    }
                }
            }
        }

        private TimeSpan GetInterval()
        {
            int h = int.TryParse(TxtHours.Text, out int hv) ? hv : 0;
            int m = int.TryParse(TxtMins.Text, out int mv) ? mv : 0;
            int s = int.TryParse(TxtSecs.Text, out int sv) ? sv : 0;
            int ms = int.TryParse(TxtMillis.Text, out int msv) ? msv : 0;
            return new TimeSpan(0, h, m, s, ms);
        }

        private AutoClicker.Core.MouseButton GetMouseButton()
        {
            return (AutoClicker.Core.MouseButton)CboButton.SelectedIndex;
        }

        private ClickType GetClickType()
        {
            return (ClickType)CboType.SelectedIndex;
        }

        private NativeMethods.POINT? GetFixedPosition()
        {
            if (RadioPosFixed.IsChecked == true)
            {
                if (int.TryParse(TxtPosX.Text, out int x) && int.TryParse(TxtPosY.Text, out int y))
                {
                    return new NativeMethods.POINT { X = x, Y = y };
                }
                // Fallback to current if parsing fails? Or throw?
                // For now, let's treat 0,0 or throw.
            }
            return null;
        }

        private void RadioPos_Checked(object sender, RoutedEventArgs e)
        {
            if (PanelFixedPos == null) return;
            PanelFixedPos.IsEnabled = RadioPosFixed.IsChecked == true;
        }

        private void BtnPickPos_Click(object sender, RoutedEventArgs e)
        {
            if (_isPickingLocation)
            {
                StopPicking();
            }
            else
            {
                StartPicking();
            }
        }

        private void StartPicking()
        {
            _isPickingLocation = true;
            BtnPickPos.Content = "F6で決定"; // Reuse F6 hotkey logic for simplicity?
            // Or use a timer to show live coords
            _pickingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _pickingTimer.Tick += (s, args) =>
            {
                if (NativeMethods.GetCursorPos(out var point))
                {
                    TxtPosX.Text = point.X.ToString();
                    TxtPosY.Text = point.Y.ToString();
                }
            };
            _pickingTimer.Start();
        }

        private void StopPicking()
        {
            _isPickingLocation = false;
            BtnPickPos.Content = "位置をクリップ";
            _pickingTimer?.Stop();
        }

        // --- Hotkey Configuration ---

        private void TxtHotKey_GotFocus(object sender, RoutedEventArgs e)
        {
            TxtHotKey.Text = "キーを押す...";
            TxtHotKey.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444"));
        }

        private void TxtHotKey_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateHotKeyText();
            TxtHotKey.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
        }

        private void TxtHotKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            // Get Key
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Ignore modifier keys themselves
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // Get Modifiers
            int modifiers = 0;
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) modifiers |= (int)NativeMethods.MOD_ALT;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) modifiers |= (int)NativeMethods.MOD_CONTROL;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) modifiers |= (int)NativeMethods.MOD_SHIFT;
            if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) modifiers |= (int)NativeMethods.MOD_WIN;

            // Convert to Virtual Key
            int vk = KeyInterop.VirtualKeyFromKey(key);

            // Update State
            _currentHotKey = vk;
            _currentModifiers = modifiers;

            UpdateHotKeyText();
            RegisterCurrentHotKey();

            // Move focus away to finish editing
            ChkAlwaysOnTop.Focus();
        }

        private void UpdateHotKeyText()
        {
            TxtHotKey.Text = GetKeyName(_currentHotKey, _currentModifiers);
        }

        private string GetKeyName(int vk, int modifiers)
        {
            try
            {
                Key key = KeyInterop.KeyFromVirtualKey(vk);
                string keyStr = key.ToString();

                // Simple mapping for F-keys and others
                if (key >= Key.F1 && key <= Key.F24) keyStr = key.ToString();
                else if (key >= Key.A && key <= Key.Z) keyStr = key.ToString();
                else if (key >= Key.D0 && key <= Key.D9) keyStr = key.ToString().Replace("D", "");
                else if (key >= Key.NumPad0 && key <= Key.NumPad9) keyStr = key.ToString();

                string modStr = "";
                if ((modifiers & NativeMethods.MOD_CONTROL) != 0) modStr += "Ctrl + ";
                if ((modifiers & NativeMethods.MOD_SHIFT) != 0) modStr += "Shift + ";
                if ((modifiers & NativeMethods.MOD_ALT) != 0) modStr += "Alt + "; // Alt is problematic for menu access but okay

                return modStr + keyStr;
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}