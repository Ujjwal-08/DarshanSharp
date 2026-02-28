using DarshanPlayer.Services;
using DarshanPlayer.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Shell;
using System.Windows.Interop;

namespace DarshanPlayer
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm = null!;
        private DispatcherTimer _hideControlsTimer;
        
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_NCMOUSEMOVE = 0x00A0;
        private const int VK_ESCAPE = 0x1B;
        private const int MONITOR_DEFAULTTONEAREST = 2;

        private WindowState _lastWindowState;
        private double _preWidth;
        private double _preHeight;
        private double _preLeft;
        private double _preTop;
        private bool _wasAlwaysOnTop;
        private WindowChrome? _savedChrome;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left; public int Top; public int Right; public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS        = 0x80000000,
            ES_DISPLAY_REQUIRED  = 0x00000002,
            ES_SYSTEM_REQUIRED   = 0x00000001
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public MainWindow()
        {
            InitializeComponent();

            var media    = ServiceLocator.MediaService!;
            var playlist = ServiceLocator.PlaylistService!;
            var settings = ServiceLocator.SettingsService!;
            var lang     = ServiceLocator.LanguageManager!;

            _vm = new MainViewModel(media, playlist, settings, lang);
            _vm.PlaylistVM = new PlaylistViewModel(playlist);

            DataContext = _vm;
            VideoPlayer.MediaPlayer = media.MediaPlayer;

            Width  = Math.Max(1100, settings.Current.WindowWidth);
            Height = Math.Max(700, settings.Current.WindowHeight);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            UseLayoutRounding = true;
            lang.Apply(settings.Current.Language);

            _vm.PropertyChanged += VmOnPropertyChanged;

            _hideControlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _hideControlsTimer.Tick += HideControlsTimer_Tick;

            this.SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(hwnd).AddHook(HwndHook);
            };

            this.SizeChanged += MainWindow_SizeChanged;
            _lastWindowState = WindowState;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // ESC Handling
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                if (wParam.ToInt32() == VK_ESCAPE)
                {
                    if (_vm.IsPipMode || _vm.IsFullscreen)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (_vm.IsPipMode) ExitPipMode();
                            else if (_vm.IsFullscreen) _vm.IsFullscreen = false;
                        }));
                        handled = true;
                    }
                }
            }
            // Mouse Wake Handling
            else if (msg == WM_MOUSEMOVE || msg == WM_NCMOUSEMOVE)
            {
                Dispatcher.BeginInvoke(new Action(WakeControls));
            }
            return IntPtr.Zero;
        }

   private void WakeControls()
{
    if (!_vm.IsFullscreen && !_vm.IsPipMode) 
    {
        ControlsPopup.IsOpen = false;
        return;
    }

    Mouse.OverrideCursor = null;

    // Reposition popup to ensure it's not clipping outside monitor bounds
    if (_vm.IsFullscreen)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var screenPos = VideoArea.PointToScreen(new Point(0, 0));
        ControlsPopup.HorizontalOffset = screenPos.X / dpi.DpiScaleX;
        ControlsPopup.VerticalOffset = screenPos.Y / dpi.DpiScaleY;
    }

    ControlsPopup.IsOpen = true;

    if (_vm.IsFullscreen)
    {
        FullscreenOverlay.Visibility = Visibility.Visible;
        PipOverlay.Visibility = Visibility.Collapsed;
        
        ControlsBar.Visibility = Visibility.Collapsed;
        InfoBar.Visibility = Visibility.Collapsed;
    }
    else if (_vm.IsPipMode)
    {
        FullscreenOverlay.Visibility = Visibility.Collapsed;
        PipOverlay.Visibility = Visibility.Visible;
    }

    _hideControlsTimer.Stop();
    _hideControlsTimer.Start();
}

private void HideControlsTimer_Tick(object? sender, EventArgs e)
{
    _hideControlsTimer.Stop();

    // Hide the UI elements
    FullscreenOverlay.Visibility = Visibility.Collapsed;
    PipOverlay.Visibility = Visibility.Collapsed;

    if (_vm.IsFullscreen)
    {
        // Hide cursor only in fullscreen after timeout
        Mouse.OverrideCursor = Cursors.None;
    }
}
        private void Window_MouseEnter(object sender, MouseEventArgs e) => WakeControls();
        private void Root_MouseEnter(object sender, MouseEventArgs e) => WakeControls();
        
        private void VideoArea_MouseEnter(object sender, MouseEventArgs e) => WakeControls();

        private void Root_MouseMove(object sender, MouseEventArgs e) => WakeControls();
        private void VideoArea_MouseMove(object sender, MouseEventArgs e) => WakeControls();

      
        
        private void OverlayEventGrid_MouseMove(object sender, MouseEventArgs e) 
        {
            // If we are moving inside the popup, keep it awake
            WakeControls();
        }
        
        private void OverlayEventGrid_Click(object sender, MouseButtonEventArgs e) => VideoArea_Click(sender, e);
        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsFullscreen))
                ApplyFullscreen(_vm.IsFullscreen);
            else if (e.PropertyName == nameof(MainViewModel.IsPlaying))
                UpdateExecutionState(_vm.IsPlaying);
        }

      private void ApplyFullscreen(bool full)
{
    if (full)
    {
        if (_vm.IsPipMode) ExitPipMode();
        _preWidth = Width; _preHeight = Height; _preLeft = Left; _preTop = Top;

        _savedChrome = WindowChrome.GetWindowChrome(this);
        WindowChrome.SetWindowChrome(this, null);

        // 1. Set Background to pure black to hide "White Strips"
        this.Background = Brushes.Black;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;

        var hwnd = new WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };

        if (GetMonitorInfo(monitor, ref mi))
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            // We use mi.rcMonitor (Full Screen) instead of rcWork (Space minus Taskbar)
            Left = mi.rcMonitor.Left / dpi.DpiScaleX;
            Top = mi.rcMonitor.Top / dpi.DpiScaleY;
            Width = (mi.rcMonitor.Right - mi.rcMonitor.Left) / dpi.DpiScaleX;
            Height = (mi.rcMonitor.Bottom - mi.rcMonitor.Top) / dpi.DpiScaleY;
            
            // Lock dimensions to prevent white strips leaking from resize handles
            MaxWidth = Width; MaxHeight = Height;
        }

        Topmost = true;
        TitleRow.Height = InfoRow.Height = ControlsRow.Height = new GridLength(0);

        UpdateLayout();
        
        // 2. Refresh Popup Placement
        var screenPos = VideoArea.PointToScreen(new Point(0, 0));
        ControlsPopup.HorizontalOffset = screenPos.X / VisualTreeHelper.GetDpi(this).DpiScaleX;
        ControlsPopup.VerticalOffset = screenPos.Y / VisualTreeHelper.GetDpi(this).DpiScaleY;
        
        WakeControls();

        this.Activate();
        this.Focus();
        Keyboard.Focus(this);
        _hideControlsTimer.Start();
    }
    else
    {
        // Restore dimensions first
        MaxWidth = MaxHeight = double.PositiveInfinity;
        
        // Reset Style before State to prevent Taskbar "White Flash"
        if (_savedChrome != null) WindowChrome.SetWindowChrome(this, _savedChrome);
        
        this.Background = (Brush)FindResource("BgDeepBrush"); // Restore your original color
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        WindowState = WindowState.Normal;

        Width = _preWidth; Height = _preHeight; Left = _preLeft; Top = _preTop;

        TitleBar.Visibility = ControlsBar.Visibility = InfoBar.Visibility = Visibility.Visible;
        TitleRow.Height = new GridLength(40);
        InfoRow.Height = ControlsRow.Height = GridLength.Auto;

        ControlsPopup.IsOpen = false;
        Mouse.OverrideCursor = null;
        _hideControlsTimer.Stop();
        Topmost = _vm.AlwaysOnTop;
    }
}
        private void EnterPipMode()
{
    if (_vm.IsPipMode) return;
    _vm.IsPipMode = true;
    _wasAlwaysOnTop = _vm.AlwaysOnTop;
    _vm.AlwaysOnTop = true;

    _preWidth = Width; _preHeight = Height;
    WindowState = WindowState.Normal;
    Width = 320; Height = 180;
    
    var wa = SystemParameters.WorkArea;
    Left = wa.Right - Width - 20;
    Top  = wa.Bottom - Height - 20;

    TitleRow.Height = InfoRow.Height = ControlsRow.Height = new GridLength(0);
    
    // Show Overlays for PiP
    UpdateLayout();
    WakeControls();

    Topmost = true;
    Activate();
    Keyboard.Focus(this);
    _hideControlsTimer.Start();
}

     private void ExitPipMode()
{
    if (!_vm.IsPipMode) return;
    _vm.IsPipMode = false;
    _vm.AlwaysOnTop = _wasAlwaysOnTop;

    // Restoring position explicitly
    if (_preLeft > 0 || _preTop > 0)
    {
        Left = _preLeft;
        Top = _preTop;
    }
    else
    {
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
    
    TitleBar.Visibility = ControlsBar.Visibility = InfoBar.Visibility = Visibility.Visible;
    TitleRow.Height = new GridLength(40);
    InfoRow.Height = ControlsRow.Height = GridLength.Auto;

    // COMPLETELY close the popup and overlays
    ControlsPopup.IsOpen = false;
    PipOverlay.Visibility = Visibility.Collapsed;
    FullscreenOverlay.Visibility = Visibility.Collapsed;

    Topmost = _vm.AlwaysOnTop;
    WindowState = WindowState.Normal;
    Activate();
    _hideControlsTimer.Stop();
}
        private void UpdateExecutionState(bool isPlaying)
        {
            SetThreadExecutionState(isPlaying 
                ? (EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED)
                : EXECUTION_STATE.ES_CONTINUOUS);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth < 900) PlaylistPanel.Visibility = Visibility.Collapsed;
            else PlaylistPanel.Visibility = _vm.IsPlaylistVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.IsPlaying && !_vm.IsPipMode) EnterPipMode();
            else WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.IsPipMode) ExitPipMode();
            else WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.IsPipMode) ExitPipMode();
            else if (_vm.IsFullscreen) _vm.IsFullscreen = false;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.SettingsService?.Save();
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var s = ServiceLocator.SettingsService!;
            s.Current.WindowWidth = ActualWidth; s.Current.WindowHeight = ActualHeight;
            s.Save();
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            base.OnClosing(e);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                if (!_vm.IsFullscreen)
                {
                    MaxHeight = SystemParameters.WorkArea.Height + 16;
                    MaxWidth  = SystemParameters.WorkArea.Width  + 16;
                }
            }
            else if (WindowState == WindowState.Normal)
            {
                if (_vm.IsFullscreen) _vm.IsFullscreen = false;
                if (_lastWindowState == WindowState.Minimized && _vm.IsPipMode) ExitPipMode();
            }
            _lastWindowState = WindowState;
        }

        private void VideoArea_Click(object sender, MouseButtonEventArgs e)
        {
            if (_vm.IsPipMode) return;
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (!IsOverInteractiveControl(e.OriginalSource as DependencyObject))
                {
                    _vm.IsFullscreen = !_vm.IsFullscreen;
                    e.Handled = true;
                }
            }
        }

        private bool IsOverInteractiveControl(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (current is ButtonBase || current is Slider || current is ScrollViewer || current is TextBox || 
                    current is RepeatButton || current is Thumb || current is ComboBox || current is ListBox || current is MenuItem)
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:  _vm.PlayPauseCommand.Execute(null);   e.Handled = true; break;
                case Key.Left:   _vm.SkipBackCommand.Execute(null);    e.Handled = true; break;
                case Key.Right:  _vm.SkipForwardCommand.Execute(null); e.Handled = true; break;
                case Key.Up:     _vm.Volume = Math.Min(200, _vm.Volume + 5); e.Handled = true; break;
                case Key.Down:   _vm.Volume = Math.Max(0, _vm.Volume - 5);   e.Handled = true; break;
                case Key.M:      _vm.ToggleMuteCommand.Execute(null);  e.Handled = true; break;
                case Key.F:
                case Key.F11:    _vm.ToggleFullscreenCommand.Execute(null); e.Handled = true; break;
                case Key.N:      _vm.NextTrackCommand.Execute(null);   e.Handled = true; break;
                case Key.P:      _vm.PrevTrackCommand.Execute(null);   e.Handled = true; break;
                case Key.S:      _vm.StopCommand.Execute(null);        e.Handled = true; break;
                case Key.L:
                    if (Keyboard.Modifiers == ModifierKeys.Control) { _vm.TogglePlaylistCommand.Execute(null); e.Handled = true; }
                    break;
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effects = isFile ? DragDropEffects.Copy : DragDropEffects.None;
            DropHintOverlay.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            DropHintOverlay.Visibility = Visibility.Collapsed;
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files) _vm.AddDroppedFiles(files);
        }

        private void SeekBar_PreviewMouseDown(object sender, MouseButtonEventArgs e) { }
        private void SeekBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)   { }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)        => _vm.OpenFileCommand.Execute(null);
        private void MenuOpenFolder_Click(object sender, RoutedEventArgs e)  => _vm.OpenFolderCommand.Execute(null);

        private void MenuRecent_Click(object sender, RoutedEventArgs e)
        {
            var btn  = RecentBtn;
            var menu = new ContextMenu();
            if (!_vm.RecentFiles.Any()) menu.Items.Add(new MenuItem { Header = "No recent files", IsEnabled = false });
            else
            {
                foreach (var f in _vm.RecentFiles)
                {
                    var item = new MenuItem { Header = System.IO.Path.GetFileName(f), Tag = f };
                    item.Click += (s, args) => _vm.PlayRecentCommand.Execute(((MenuItem)s).Tag);
                    menu.Items.Add(item);
                }
            }
            menu.PlacementTarget = btn; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        private void MenuSleepTimer_Click(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)FindResource("SleepMenu");
            menu.PlacementTarget = sender as UIElement; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        private void SleepTimer_5(object sender, RoutedEventArgs e)   => _vm.SetSleepTimerCommand.Execute(5);
        private void SleepTimer_10(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(10);
        private void SleepTimer_15(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(15);
        private void SleepTimer_30(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(30);
        private void SleepTimer_60(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(60);
        private void SleepTimer_Cancel(object sender, RoutedEventArgs e) => _vm.CancelSleepTimerCommand.Execute(null);

        private void Playlist_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.PlaylistVM?.SelectedItem != null) ServiceLocator.PlaylistService!.PlayItem(_vm.PlaylistVM.SelectedItem);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }

        private void BtnSupportPatreon_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.patreon.com/cw/BABU_ISHU");
        }

        private void BtnSupportPaypal_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.paypal.com/ncp/payment/SECBQ62TRZZ6Y");
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
