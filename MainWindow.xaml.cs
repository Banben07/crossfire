using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CrossfireCrosshair.Models;
using CrossfireCrosshair.Services;
using Forms = System.Windows.Forms;

namespace CrossfireCrosshair;

public partial class MainWindow : Window
{
    private const int ToggleOverlayHotkeyId = 0xA111;
    private const int CycleProfileHotkeyId = 0xA112;

    private readonly SettingsService _settingsService;
    private readonly AutoStartService _autoStartService;
    private readonly ProfileShareService _profileShareService;
    private readonly OverlayWindow _overlayWindow;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly DispatcherTimer _saveTimer;
    private readonly Dictionary<CrosshairProfile, PropertyChangedEventHandler> _profileSubscriptions = [];
    private HwndSource? _hwndSource;
    private string _hotkeyWarning = string.Empty;
    private string _startupWarning = string.Empty;
    private bool _isApplyingStartupSetting;
    private bool _trayHintShown;

    public ObservableCollection<Key> AvailableKeys { get; } = [];
    public ObservableCollection<MonitorOption> AvailableMonitors { get; } = [];

    private AppSettings Settings => (AppSettings)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = new SettingsService();
        _autoStartService = new AutoStartService("CrossfireCrosshair");
        _profileShareService = new ProfileShareService();

        AppSettings settings = _settingsService.Load();
        EnsureSettingsSanity(settings);
        settings.StartWithWindows = _autoStartService.IsEnabled();
        DataContext = settings;

        _notifyIcon = CreateNotifyIcon();
        _overlayWindow = new OverlayWindow();

        BuildKeyList();
        BuildMonitorList();
        AttachSettingsHandlers(settings);

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            _settingsService.Save(Settings);
        };

        Loaded += OnLoaded;
        Closing += OnClosing;
        StateChanged += OnWindowStateChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        RegisterHotkeys();
        ApplyOverlayState();
        UpdateHotkeySummary();
        UpdateStatusText();
        ShareCodeStatusText.Text = "可将当前配置复制为分享码，并在其他设备导入。";
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_hwndSource is not null)
        {
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, ToggleOverlayHotkeyId);
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, CycleProfileHotkeyId);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        _settingsService.Save(Settings);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _overlayWindow.Close();
    }

    private Forms.NotifyIcon CreateNotifyIcon()
    {
        Forms.NotifyIcon notifyIcon = new()
        {
            Text = "CrossfireCrosshair",
            Icon = SystemIcons.Application,
            Visible = true
        };

        Forms.ContextMenuStrip menu = new();
        Forms.ToolStripMenuItem openItem = new("打开控制面板");
        openItem.Click += (_, _) => Dispatcher.Invoke(RestoreFromTray);
        menu.Items.Add(openItem);

        Forms.ToolStripMenuItem toggleOverlayItem = new("切换准星叠加");
        toggleOverlayItem.Click += (_, _) => Dispatcher.Invoke(() => Settings.OverlayEnabled = !Settings.OverlayEnabled);
        menu.Items.Add(toggleOverlayItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        Forms.ToolStripMenuItem exitItem = new("退出");
        exitItem.Click += (_, _) => Dispatcher.Invoke(() => Close());
        menu.Items.Add(exitItem);

        notifyIcon.ContextMenuStrip = menu;
        notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(RestoreFromTray);
        return notifyIcon;
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (!Settings.MinimizeToTray)
        {
            return;
        }

        if (WindowState == WindowState.Minimized)
        {
            HideToTray(showHint: true);
        }
    }

    private void AttachSettingsHandlers(AppSettings settings)
    {
        settings.PropertyChanged += OnSettingsPropertyChanged;
        settings.Profiles.CollectionChanged += OnProfilesCollectionChanged;

        settings.ToggleOverlayHotkey.PropertyChanged += OnHotkeyPropertyChanged;
        settings.CycleProfileHotkey.PropertyChanged += OnHotkeyPropertyChanged;

        foreach (CrosshairProfile profile in settings.Profiles)
        {
            SubscribeProfile(profile);
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppSettings.SelectedProfileIndex) or nameof(AppSettings.OverlayEnabled) or nameof(AppSettings.TargetMonitorIndex))
        {
            EnsureSelectedProfileIndex();
            ApplyOverlayState();
            UpdateStatusText();
        }

        if (e.PropertyName == nameof(AppSettings.StartWithWindows) && !_isApplyingStartupSetting)
        {
            ApplyStartupSetting();
        }

        QueueSave();
    }

    private void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (CrosshairProfile profile in e.OldItems)
            {
                UnsubscribeProfile(profile);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (CrosshairProfile profile in e.NewItems)
            {
                SubscribeProfile(profile);
            }
        }

        EnsureSelectedProfileIndex();
        ApplyOverlayState();
        QueueSave();
    }

    private void SubscribeProfile(CrosshairProfile profile)
    {
        if (_profileSubscriptions.ContainsKey(profile))
        {
            return;
        }

        PropertyChangedEventHandler handler = (_, _) =>
        {
            if (ReferenceEquals(profile, CurrentProfile))
            {
                ApplyOverlayState();
            }

            QueueSave();
        };

        profile.PropertyChanged += handler;
        _profileSubscriptions[profile] = handler;
    }

    private void UnsubscribeProfile(CrosshairProfile profile)
    {
        if (!_profileSubscriptions.TryGetValue(profile, out PropertyChangedEventHandler? handler))
        {
            return;
        }

        profile.PropertyChanged -= handler;
        _profileSubscriptions.Remove(profile);
    }

    private void OnHotkeyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RegisterHotkeys();
        UpdateHotkeySummary();
        QueueSave();
    }

    private void BuildKeyList()
    {
        AvailableKeys.Clear();

        Key[] keys =
        [
            Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J, Key.K, Key.L, Key.M,
            Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T, Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
            Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
            Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
            Key.Insert, Key.Delete, Key.Home, Key.End, Key.PageUp, Key.PageDown
        ];

        foreach (Key key in keys)
        {
            AvailableKeys.Add(key);
        }
    }

    private void BuildMonitorList()
    {
        AvailableMonitors.Clear();
        Forms.Screen[] screens = Forms.Screen.AllScreens;

        for (int i = 0; i < screens.Length; i++)
        {
            Forms.Screen screen = screens[i];
            string label = $"{i}: {screen.DeviceName} ({screen.Bounds.Width}x{screen.Bounds.Height})";
            AvailableMonitors.Add(new MonitorOption(i, label));
        }

        if (screens.Length > 0)
        {
            Settings.TargetMonitorIndex = Math.Clamp(Settings.TargetMonitorIndex, 0, screens.Length - 1);
        }
        else
        {
            Settings.TargetMonitorIndex = 0;
        }
    }

    private void RegisterHotkeys()
    {
        if (_hwndSource is null)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_hwndSource.Handle, ToggleOverlayHotkeyId);
        NativeMethods.UnregisterHotKey(_hwndSource.Handle, CycleProfileHotkeyId);

        bool toggleOk = NativeMethods.RegisterHotKey(
            _hwndSource.Handle,
            ToggleOverlayHotkeyId,
            Settings.ToggleOverlayHotkey.ToNativeModifiers(),
            Settings.ToggleOverlayHotkey.ToVirtualKeyCode());

        bool cycleOk = NativeMethods.RegisterHotKey(
            _hwndSource.Handle,
            CycleProfileHotkeyId,
            Settings.CycleProfileHotkey.ToNativeModifiers(),
            Settings.CycleProfileHotkey.ToVirtualKeyCode());

        if (!toggleOk || !cycleOk)
        {
            _hotkeyWarning = "部分热键注册失败，请修改热键或关闭冲突软件。";
        }
        else
        {
            _hotkeyWarning = string.Empty;
        }

        UpdateStatusText();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WM_HOTKEY)
        {
            return IntPtr.Zero;
        }

        int id = wParam.ToInt32();
        if (id == ToggleOverlayHotkeyId)
        {
            Settings.OverlayEnabled = !Settings.OverlayEnabled;
            handled = true;
            return IntPtr.Zero;
        }

        if (id == CycleProfileHotkeyId)
        {
            CycleProfile();
            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private CrosshairProfile? CurrentProfile
    {
        get
        {
            if (Settings.Profiles.Count == 0)
            {
                return null;
            }

            int idx = Math.Clamp(Settings.SelectedProfileIndex, 0, Settings.Profiles.Count - 1);
            return Settings.Profiles[idx];
        }
    }

    private void EnsureSelectedProfileIndex()
    {
        if (Settings.Profiles.Count == 0)
        {
            return;
        }

        int safeIndex = Math.Clamp(Settings.SelectedProfileIndex, 0, Settings.Profiles.Count - 1);
        if (safeIndex != Settings.SelectedProfileIndex)
        {
            Settings.SelectedProfileIndex = safeIndex;
        }
    }

    private void EnsureSettingsSanity(AppSettings settings)
    {
        settings.Profiles ??= [];
        if (settings.Profiles.Count == 0)
        {
            settings.Profiles = [.. ProfileFactory.CreatePresetPack()];
        }

        settings.ToggleOverlayHotkey ??= HotkeyBinding.DefaultToggle();
        settings.CycleProfileHotkey ??= HotkeyBinding.DefaultCycle();
        settings.SelectedProfileIndex = Math.Clamp(settings.SelectedProfileIndex, 0, settings.Profiles.Count - 1);
        settings.TargetMonitorIndex = Math.Max(0, settings.TargetMonitorIndex);
    }

    private void QueueSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void ApplyOverlayState()
    {
        _overlayWindow.SetProfile(CurrentProfile);

        if (Settings.OverlayEnabled && CurrentProfile is not null)
        {
            if (!_overlayWindow.IsVisible)
            {
                _overlayWindow.Show();
            }

            _overlayWindow.ApplyMonitor(Settings.TargetMonitorIndex);
        }
        else if (_overlayWindow.IsVisible)
        {
            _overlayWindow.Hide();
        }
    }

    private void UpdateHotkeySummary()
    {
        HotkeyText.Text =
            $"开关叠加：{Settings.ToggleOverlayHotkey.ToDisplayString()}\n" +
            $"轮换配置：{Settings.CycleProfileHotkey.ToDisplayString()}";
    }

    private void UpdateStatusText()
    {
        string overlayState = Settings.OverlayEnabled ? "叠加状态：已启用" : "叠加状态：已禁用";
        List<string> lines = [overlayState];
        if (!string.IsNullOrWhiteSpace(_hotkeyWarning))
        {
            lines.Add(_hotkeyWarning);
        }

        if (!string.IsNullOrWhiteSpace(_startupWarning))
        {
            lines.Add(_startupWarning);
        }

        StatusText.Text = string.Join("\n", lines);
    }

    private void ApplyStartupSetting()
    {
        if (_autoStartService.TrySetEnabled(Settings.StartWithWindows, out string? error))
        {
            _startupWarning = string.Empty;
            UpdateStatusText();
            return;
        }

        _startupWarning = $"开机自启设置失败：{error}";
        _isApplyingStartupSetting = true;
        Settings.StartWithWindows = _autoStartService.IsEnabled();
        _isApplyingStartupSetting = false;
        UpdateStatusText();
    }

    private void HideToTray(bool showHint)
    {
        Hide();
        ShowInTaskbar = false;

        if (showHint && !_trayHintShown)
        {
            _notifyIcon.BalloonTipTitle = "CrossfireCrosshair";
            _notifyIcon.BalloonTipText = "程序已在托盘运行，双击托盘图标可恢复窗口。";
            _notifyIcon.ShowBalloonTip(1800);
            _trayHintShown = true;
        }
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
    }

    private void CycleProfile()
    {
        if (Settings.Profiles.Count == 0)
        {
            return;
        }

        Settings.SelectedProfileIndex = (Settings.SelectedProfileIndex + 1) % Settings.Profiles.Count;
    }

    private string BuildUniqueProfileName(string seed)
    {
        HashSet<string> names = new(Settings.Profiles.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(seed))
        {
            return seed;
        }

        int n = 2;
        while (names.Contains($"{seed} {n}"))
        {
            n++;
        }

        return $"{seed} {n}";
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
        CrosshairProfile profile = CurrentProfile?.Clone() ?? ProfileFactory.CreateCsStyle();
        profile.Name = BuildUniqueProfileName("新配置");
        Settings.Profiles.Add(profile);
        Settings.SelectedProfileIndex = Settings.Profiles.Count - 1;
    }

    private void DuplicateProfile_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentProfile is null)
        {
            return;
        }

        CrosshairProfile copy = CurrentProfile.Clone();
        copy.Name = BuildUniqueProfileName($"{CurrentProfile.Name} 副本");
        Settings.Profiles.Add(copy);
        Settings.SelectedProfileIndex = Settings.Profiles.Count - 1;
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (Settings.Profiles.Count <= 1)
        {
            System.Windows.MessageBox.Show(
                this,
                "至少需要保留一个配置档。",
                "删除配置档",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        if (CurrentProfile is null)
        {
            return;
        }

        int index = Settings.SelectedProfileIndex;
        Settings.Profiles.RemoveAt(index);
        Settings.SelectedProfileIndex = Math.Clamp(index, 0, Settings.Profiles.Count - 1);
    }

    private void ResetPresets_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
            this,
            "确定要用默认预设替换所有配置档吗？",
            "重置预设",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        Settings.Profiles.Clear();
        foreach (CrosshairProfile profile in ProfileFactory.CreatePresetPack())
        {
            Settings.Profiles.Add(profile);
        }

        Settings.SelectedProfileIndex = 0;
    }

    private void ApplyMonitor_Click(object sender, RoutedEventArgs e)
    {
        ApplyOverlayState();
    }

    private void ReregisterHotkeys_Click(object sender, RoutedEventArgs e)
    {
        RegisterHotkeys();
        UpdateHotkeySummary();
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e)
    {
        HideToTray(showHint: true);
    }

    private void CopyShareCode_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentProfile is null)
        {
            ShareCodeStatusText.Text = "当前没有选中的配置档。";
            return;
        }

        try
        {
            string code = _profileShareService.Export(CurrentProfile);
            System.Windows.Clipboard.SetText(code);
            ShareCodeStatusText.Text = $"已复制分享码：{CurrentProfile.Name}";
        }
        catch (Exception ex)
        {
            ShareCodeStatusText.Text = $"复制分享码失败：{ex.Message}";
        }
    }

    private void ImportShareCodeFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string code = System.Windows.Clipboard.GetText();
            ImportShareCode(code);
        }
        catch (Exception ex)
        {
            ShareCodeStatusText.Text = $"读取剪贴板失败：{ex.Message}";
        }
    }

    private void ImportShareCodeManual_Click(object sender, RoutedEventArgs e)
    {
        ShareCodeInputDialog dialog = new()
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ImportShareCode(dialog.ShareCode);
    }

    private void ImportShareCode(string code)
    {
        if (!_profileShareService.TryImport(code, out CrosshairProfile? imported, out string? error) || imported is null)
        {
            ShareCodeStatusText.Text = $"导入失败：{error}";
            return;
        }

        imported.Name = BuildUniqueProfileName(imported.Name);
        Settings.Profiles.Add(imported);
        Settings.SelectedProfileIndex = Settings.Profiles.Count - 1;
        ShareCodeStatusText.Text = $"导入成功：{imported.Name}";
    }

    private void PickMainColor_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentProfile is null)
        {
            return;
        }

        Color initial = ParseHtmlColor(CurrentProfile.ColorHex, Color.Lime);
        using Forms.ColorDialog dialog = new()
        {
            AllowFullOpen = true,
            FullOpen = true,
            Color = initial
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            CurrentProfile.ColorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }
    }

    private void PickOutlineColor_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentProfile is null)
        {
            return;
        }

        Color initial = ParseHtmlColor(CurrentProfile.OutlineColorHex, Color.Black);
        using Forms.ColorDialog dialog = new()
        {
            AllowFullOpen = true,
            FullOpen = true,
            Color = initial
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            CurrentProfile.OutlineColorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }
    }

    private static Color ParseHtmlColor(string value, Color fallback)
    {
        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }

    public sealed record MonitorOption(int Index, string DisplayName);
}
