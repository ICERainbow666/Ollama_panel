using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Application = System.Windows.Application;
using DrawingColor = System.Drawing.Color;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingIcon = System.Drawing.Icon;
using DrawingSolidBrush = System.Drawing.SolidBrush;
using DrawingBrushes = System.Drawing.Brushes;

namespace Ollama_panel;

public partial class MainWindow : Window
{
    private OllamaService _service = null!;
    private NotifyIcon _trayIcon = null!;
    private DispatcherTimer _statusTimer = null!;

    public MainWindow()
    {
        InitializeComponent();
        InitTrayIcon();
        InitService();
        LoadSettings();
        InitStatusTimer();
    }

    private void InitService()
    {
        _service = new OllamaService(OllamaPathBox.Text);
        _service.StatusChanged += OnStatusChanged;
        _service.LogReceived += OnLogReceived;
    }

    private void InitTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Text = "Ollama 控制面板",
            Visible = false
        };

        // 用程序化绘制一个简单图标（避免依赖外部资源）
        var bmp = new DrawingBitmap(16, 16);
        using (var g = DrawingGraphics.FromImage(bmp))
        {
            g.Clear(DrawingColor.Transparent);
            g.FillEllipse(new DrawingSolidBrush(DrawingColor.FromArgb(66, 133, 244)), 1, 1, 14, 14);
            g.FillEllipse(DrawingBrushes.White, 4, 4, 8, 8);
        }
        _trayIcon.Icon = DrawingIcon.FromHandle(bmp.GetHicon());

        // 右键菜单
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示窗口", null, (_, _) => ShowWindow());
        menu.Items.Add("-");
        menu.Items.Add("退出", null, (_, _) => ExitApp());
        _trayIcon.ContextMenuStrip = menu;

        // 双击恢复窗口
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
    }

    private void InitStatusTimer()
    {
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusTimer.Tick += async (_, _) => await UpdateVersionInfo();
        _statusTimer.Start();
    }

    private void LoadSettings()
    {
        var key = @"Software\Ollama_panel";
        using var reg = Registry.CurrentUser.OpenSubKey(key);
        if (reg != null)
        {
            var path = reg.GetValue("OllamaPath") as string;
            if (!string.IsNullOrEmpty(path)) OllamaPathBox.Text = path;

            var autoStart = reg.GetValue("AutoStart") as string;
            AutoStartCheckBox.IsChecked = autoStart == "1";

            var showLog = reg.GetValue("ShowLog") as string;
            ShowLogCheckBox.IsChecked = showLog != "0";

            var minimizeToTray = reg.GetValue("MinimizeToTray") as string;
            MinimizeToTrayCheckBox.IsChecked = minimizeToTray != "0";
        }

        AutoStartCheckBox.Checked += (_, _) => { SetAutoStart(true); SaveSettings(); };
        AutoStartCheckBox.Unchecked += (_, _) => { SetAutoStart(false); SaveSettings(); };
        ShowLogCheckBox.Checked += (_, _) => { LogBox.Visibility = Visibility.Visible; SaveSettings(); };
        ShowLogCheckBox.Unchecked += (_, _) => { LogBox.Visibility = Visibility.Collapsed; SaveSettings(); };
        MinimizeToTrayCheckBox.Checked += (_, _) => SaveSettings();
        MinimizeToTrayCheckBox.Unchecked += (_, _) => SaveSettings();
        OllamaPathBox.LostFocus += (_, _) => SaveSettings();
    }

    private void SaveSettings()
    {
        var key = @"Software\Ollama_panel";
        using var reg = Registry.CurrentUser.CreateSubKey(key);
        reg.SetValue("OllamaPath", OllamaPathBox.Text);
        reg.SetValue("AutoStart", AutoStartCheckBox.IsChecked == true ? "1" : "0");
        reg.SetValue("ShowLog", ShowLogCheckBox.IsChecked == true ? "1" : "0");
        reg.SetValue("MinimizeToTray", MinimizeToTrayCheckBox.IsChecked == true ? "1" : "0");
    }

    private static void SetAutoStart(bool enable)
    {
        var key = @"Software\Microsoft\Windows\CurrentVersion\Run";
        using var reg = Registry.CurrentUser.OpenSubKey(key, writable: true);
        if (reg == null) return;

        if (enable)
            reg.SetValue("Ollama_panel", $"\"{Environment.ProcessPath}\"");
        else
            reg.DeleteValue("Ollama_panel", throwOnMissingValue: false);
    }

    private void OnStatusChanged(bool running)
    {
        Dispatcher.Invoke(() =>
        {
            StatusDot.Fill = running ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.Red);
            StatusText.Text = running ? "运行中" : "已停止";
            BtnStart.IsEnabled = !running;
            BtnStop.IsEnabled = running;
        });
    }

    private void OnLogReceived(string message)
    {
        Dispatcher.Invoke(() =>
        {
            if (ShowLogCheckBox.IsChecked != true) return;
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            LogBox.ScrollToEnd();
        });
    }

    private async Task UpdateVersionInfo()
    {
        if (await _service.IsApiReachableAsync())
        {
            var ver = await _service.GetVersionAsync();
            if (!string.IsNullOrEmpty(ver))
                VersionText.Text = $"API 已就绪 | {ver}";
        }
        else if (_service.IsRunning)
        {
            VersionText.Text = "API 启动中...";
        }
        else
        {
            VersionText.Text = "";
        }
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        _service = new OllamaService(OllamaPathBox.Text);
        _service.StatusChanged += OnStatusChanged;
        _service.LogReceived += OnLogReceived;
        _service.Start();
        SaveSettings();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _service.Stop();
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择 Ollama 可执行文件",
            Filter = "可执行文件 (*.exe)|*.exe",
            FileName = OllamaPathBox.Text
        };
        if (dialog.ShowDialog() == true)
        {
            OllamaPathBox.Text = dialog.FileName;
            SaveSettings();
        }
    }

    // 最小化到托盘
    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        _trayIcon.Visible = false;
    }

    private void MinimizeToTray()
    {
        Hide();
        _trayIcon.Visible = true;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized && MinimizeToTrayCheckBox.IsChecked == true)
            MinimizeToTray();
    }

    private bool _forceExit;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        SaveSettings();
        if (_forceExit) return;
        if (MinimizeToTrayCheckBox.IsChecked == true)
        {
            e.Cancel = true;
            MinimizeToTray();
        }
    }

    private void ExitApp()
    {
        _forceExit = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        SaveSettings();
        Application.Current.Shutdown();
    }
}
