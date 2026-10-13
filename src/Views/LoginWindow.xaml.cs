using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Project.Launch.Tools;
using Project.Launch.Tools.Auth;

namespace Project.Launch.Views
{
    /// <summary>
    /// 登录窗口：仅支持 Microsoft（Xbox Live）账号登录。
    /// Login window: Microsoft (Xbox Live) account sign-in only.
    /// </summary>
    public partial class LoginWindow : Window
    {
        // 登录状态面板
        private StackPanel? _msIdlePanel;
        private StackPanel? _msWaitingPanel;
        private StackPanel? _msProgressPanel;
        private TextBlock? _msVerificationUri;
        private TextBlock? _msUserCode;
        private TextBlock? _msStatus;
        private TextBlock? _msProgressText;

        // 登录流程状态
        private MicrosoftAuthFlow? _msFlow;
        private CancellationTokenSource? _msCts;
        private string _msCurrentUserCode = "";
        private string _msCurrentVerificationUri = "";

        private enum AuthState
        {
            Idle,
            Waiting,
            Progress
        }

        public LoginWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            InitializeComponent();

            _msIdlePanel = this.FindControl<StackPanel>("MsIdlePanel");
            _msWaitingPanel = this.FindControl<StackPanel>("MsWaitingPanel");
            _msProgressPanel = this.FindControl<StackPanel>("MsProgressPanel");
            _msVerificationUri = this.FindControl<TextBlock>("MsVerificationUri");
            _msUserCode = this.FindControl<TextBlock>("MsUserCode");
            _msStatus = this.FindControl<TextBlock>("MsStatus");
            _msProgressText = this.FindControl<TextBlock>("MsProgressText");

            ShowState(AuthState.Idle);

            var gitHubButton = this.FindControl<Button>("GitHubButton");
            if (gitHubButton != null)
                gitHubButton.Click += OnGitHubClicked;

            Opened += (s, e) => SoundService.Play("Open");
        }

        private void ShowState(AuthState state)
        {
            if (_msIdlePanel != null) _msIdlePanel.IsVisible = state == AuthState.Idle;
            if (_msWaitingPanel != null) _msWaitingPanel.IsVisible = state == AuthState.Waiting;
            if (_msProgressPanel != null) _msProgressPanel.IsVisible = state == AuthState.Progress;
        }

        // ============================================================
        // 点击"开始登录"
        // ============================================================
        private async void OnMsLoginClicked(object? sender, RoutedEventArgs e)
        {
            if (_msUserCode != null) _msUserCode.Text = "";
            if (_msVerificationUri != null) _msVerificationUri.Text = "";
            if (_msStatus != null) _msStatus.Text = "正在申请设备代码...";

            ShowState(AuthState.Waiting);

            _msCts = new CancellationTokenSource();
            _msFlow = new MicrosoftAuthFlow();

            _msFlow.DeviceCodeReady += (userCode, verificationUri) =>
            {
                _msCurrentUserCode = userCode;
                _msCurrentVerificationUri = verificationUri;

                Dispatcher.UIThread.Post(() =>
                {
                    if (_msUserCode != null) _msUserCode.Text = userCode;
                    if (_msVerificationUri != null) _msVerificationUri.Text = verificationUri;

                    try
                    {
                        string url = verificationUri + "?otc=" + Uri.EscapeDataString(userCode);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                });
            };

            _msFlow.Progress += msg =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_msStatus != null) _msStatus.Text = msg;

                    bool stillWaiting = msg.Contains("等待授权");
                    bool isError = msg.Contains("❌");

                    // 设备代码授权完成后，切到"认证链推进"状态
                    if (!stillWaiting && !isError && _msWaitingPanel?.IsVisible == true)
                    {
                        if (_msProgressText != null) _msProgressText.Text = msg;
                        ShowState(AuthState.Progress);
                    }
                    else if (_msProgressPanel?.IsVisible == true && _msProgressText != null)
                    {
                        _msProgressText.Text = msg;
                    }
                });
            };

            var account = await _msFlow.LoginAsync(_msCts.Token);

            if (account == null)
            {
                ShowState(AuthState.Idle);
                return;
            }

            try
            {
                App.PlayerName = account.Username;
                App.PlayerUuid = account.Uuid;
                App.IsLoggedIn = true;
                App.LoginCheck = true;

                AccountService.SetSelectedAccount($"microsoft_{account.Uuid}", account);
                AccountService.SetLoginCheck(true);

                LogHelper.Write(LauncherPaths.AppLog,
                    $"Microsoft 账户已保存: {account.Username}",
                    $"Microsoft account saved: {account.Username}");

                Close();
            }
            catch (Exception ex)
            {
                LogHelper.Write(LauncherPaths.AppLog,
                    $"保存账户失败: {ex.Message}",
                    $"Save account failed: {ex.Message}");
            }
        }

        // ============================================================
        // 复制设备代码
        // ============================================================
        private async void OnMsCopyCodeClicked(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_msCurrentUserCode)) return;
            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(_msCurrentUserCode);

                if (_msStatus != null)
                    _msStatus.Text = "已复制代码";
            }
            catch { }
        }

        // ============================================================
        // 手动打开浏览器
        // ============================================================
        private void OnMsOpenBrowserClicked(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_msCurrentVerificationUri)) return;
            try
            {
                string url = _msCurrentVerificationUri;
                if (!string.IsNullOrEmpty(_msCurrentUserCode))
                    url += "?otc=" + Uri.EscapeDataString(_msCurrentUserCode);

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        // ============================================================
        // 取消登录
        // ============================================================
        private void OnMsCancelClicked(object? sender, RoutedEventArgs e)
        {
            _msCts?.Cancel();
            ShowState(AuthState.Idle);
        }

        // ============================================================
        // 打开项目仓库
        // ============================================================
        private async void OnGitHubClicked(object? sender, EventArgs e)
        {
            try
            {
                var uri = new Uri("https://github.com/Zhouxuanyao2333/KilimMinecraftLauncher");
                var launcher = TopLevel.GetTopLevel(this)?.Launcher;
                if (launcher != null)
                    await launcher.LaunchUriAsync(uri);
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = uri.ToString(),
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write(LauncherPaths.AppLog,
                    $"打开仓库链接失败: {ex.Message}",
                    $"Open repository link failed: {ex.Message}");
            }
        }
    }
}
