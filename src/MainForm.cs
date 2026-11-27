using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace AntigProxy
{
    public class MainForm : Form
    {
        private AppConfig _config;
        private ProxyDetectionResult _lastEnv;
        private bool _advancedExpanded;

        // Brand header
        private PictureBox _picLogo;
        private StatusPill _pillStatus;

        // Main card controls
        private RoundedCard _mainCard;
        private ModernInputBox _txtHost;
        private ModernInputBox _txtPort;
        private ModernButton _btnDetect;
        private ModernButton _btnTest;
        private Label _lblTestHint;

        private ModernInputBox _txtAgPath;
        private ModernButton _btnBrowseAg;

        private CheckBox _chkChromiumArgs;
        private CheckBox _chkRestartRunning;

        // Collapsible advanced section
        private LinkLabel _lnkAdvanced;
        private RoundedCard _advCard;
        private ModernInputBox _txtNoProxy;
        private CheckBox _chkAutoDetect;
        private CheckBox _chkAllProxy;
        private CheckBox _chkCloseAfter;

        // Actions
        private Panel _actionPanel;
        private ModernButton _btnLaunch;
        private ModernButton _btnShortcut;
        private ModernButton _btnAgyCmd;

        // Footer
        private Label _lblFooter;

        // Palette
        private static readonly Color BgCanvas = Color.FromArgb(244, 246, 250);
        private static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
        private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
        private static readonly Color GreenBg = Color.FromArgb(236, 253, 245);
        private static readonly Color GreenBorder = Color.FromArgb(167, 243, 208);
        private static readonly Color GreenText = Color.FromArgb(5, 150, 105);
        private static readonly Color AmberBg = Color.FromArgb(255, 251, 235);
        private static readonly Color AmberBorder = Color.FromArgb(253, 230, 138);
        private static readonly Color AmberText = Color.FromArgb(180, 83, 9);
        private static readonly Color RedBg = Color.FromArgb(254, 242, 242);
        private static readonly Color RedBorder = Color.FromArgb(254, 202, 202);
        private static readonly Color RedText = Color.FromArgb(220, 38, 38);
        private static readonly Color BlueText = Color.FromArgb(37, 99, 235);

        private const int CollapsedHeight = 450;
        private const int ExpandedHeight = 570;

        public MainForm()
        {
            _config = ProxyService.LoadConfig();
            InitializeUi();
            LoadConfigToUi();
            RefreshDetection(true);
        }

        private void InitializeUi()
        {
            SuspendLayout();

            Text = "AntigProxy";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BgCanvas;
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);
            ClientSize = new Size(520, CollapsedHeight);

            Icon appIcon = null;
            try
            {
                appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                }
            }
            catch { }

            // 1. Header (y = 20..64)
            _picLogo = new PictureBox();
            _picLogo.Location = new Point(24, 18);
            _picLogo.Size = new Size(44, 44);
            _picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            _picLogo.BackColor = Color.Transparent;
            Image hiResLogo = LoadEmbeddedLogo();
            if (hiResLogo != null)
            {
                _picLogo.Image = hiResLogo;
            }
            else if (appIcon != null)
            {
                _picLogo.Image = appIcon.ToBitmap();
            }
            Controls.Add(_picLogo);

            Label lblTitle = new Label();
            lblTitle.Text = "AntigProxy";
            lblTitle.Font = new Font("Microsoft YaHei UI", 14.5F, FontStyle.Bold);
            lblTitle.ForeColor = TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(76, 17);
            Controls.Add(lblTitle);

            Label lblSub = new Label();
            lblSub.Text = "Antigravity 通用免 TUN 代理启动器";
            lblSub.Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Regular);
            lblSub.ForeColor = TextMuted;
            lblSub.AutoSize = true;
            lblSub.Location = new Point(78, 44);
            Controls.Add(lblSub);

            _pillStatus = new StatusPill();
            _pillStatus.Location = new Point(346, 26);
            _pillStatus.Size = new Size(150, 28);
            _pillStatus.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            _pillStatus.Text = "检测中...";
            _pillStatus.Click += delegate { RefreshDetection(false); };
            Controls.Add(_pillStatus);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(_pillStatus, "点击重新扫描本地代理状态");

            // 2. Main Configuration Card (y = 78..294)
            _mainCard = new RoundedCard();
            _mainCard.Location = new Point(24, 78);
            _mainCard.Size = new Size(472, 216);
            Controls.Add(_mainCard);

            // Row 1: Local Proxy Host : Port + Detect + Test
            Label lblProxy = CreateFieldLabel("本地代理", 20, 16);
            _mainCard.Controls.Add(lblProxy);

            _txtHost = new ModernInputBox();
            _txtHost.Location = new Point(20, 40);
            _txtHost.Size = new Size(154, 34);
            _txtHost.Font = new Font("Consolas", 10F);
            _mainCard.Controls.Add(_txtHost);

            Label lblColon = new Label();
            lblColon.Text = ":";
            lblColon.Font = new Font("Consolas", 12F, FontStyle.Bold);
            lblColon.ForeColor = TextMuted;
            lblColon.AutoSize = true;
            lblColon.Location = new Point(179, 46);
            _mainCard.Controls.Add(lblColon);

            _txtPort = new ModernInputBox();
            _txtPort.Location = new Point(195, 40);
            _txtPort.Size = new Size(80, 34);
            _txtPort.Font = new Font("Consolas", 10F);
            _txtPort.TextAlign = HorizontalAlignment.Center;
            _mainCard.Controls.Add(_txtPort);

            _btnDetect = new ModernButton();
            _btnDetect.Text = "自动识别";
            _btnDetect.Location = new Point(287, 40);
            _btnDetect.Size = new Size(82, 34);
            _btnDetect.Font = new Font("Microsoft YaHei UI", 8.75F);
            _btnDetect.Click += OnDetectClick;
            _mainCard.Controls.Add(_btnDetect);

            _btnTest = new ModernButton();
            _btnTest.Text = "测速";
            _btnTest.Location = new Point(379, 40);
            _btnTest.Size = new Size(73, 34);
            _btnTest.Font = new Font("Microsoft YaHei UI", 8.75F);
            _btnTest.Click += OnTestProxyClick;
            _mainCard.Controls.Add(_btnTest);

            _lblTestHint = new Label();
            _lblTestHint.Text = "支持 Clash / Mihomo / v2rayN / Swell / FlClash / Sing-box 等所有本地代理";
            _lblTestHint.Font = new Font("Microsoft YaHei UI", 8.25F);
            _lblTestHint.ForeColor = TextMuted;
            _lblTestHint.Location = new Point(20, 80);
            _lblTestHint.Size = new Size(432, 18);
            _mainCard.Controls.Add(_lblTestHint);

            // Row 2: Antigravity Path
            Label lblAg = CreateFieldLabel("Antigravity 路径", 20, 108);
            _mainCard.Controls.Add(lblAg);

            _txtAgPath = new ModernInputBox();
            _txtAgPath.Location = new Point(20, 132);
            _txtAgPath.Size = new Size(349, 34);
            _txtAgPath.Font = new Font("Microsoft YaHei UI", 8.75F);
            _mainCard.Controls.Add(_txtAgPath);

            _btnBrowseAg = new ModernButton();
            _btnBrowseAg.Text = "浏览";
            _btnBrowseAg.Location = new Point(379, 132);
            _btnBrowseAg.Size = new Size(73, 34);
            _btnBrowseAg.Font = new Font("Microsoft YaHei UI", 8.75F);
            _btnBrowseAg.Click += OnBrowseAgClick;
            _mainCard.Controls.Add(_btnBrowseAg);

            // Row 3: Two essential toggles (clean side-by-side with generous gap)
            _chkChromiumArgs = new CheckBox();
            _chkChromiumArgs.Text = "独立代理模式（免开系统代理）";
            _chkChromiumArgs.AutoSize = true;
            _chkChromiumArgs.Font = new Font("Microsoft YaHei UI", 8.75F);
            _chkChromiumArgs.ForeColor = TextPrimary;
            _chkChromiumArgs.Location = new Point(20, 178);
            _mainCard.Controls.Add(_chkChromiumArgs);

            _chkRestartRunning = new CheckBox();
            _chkRestartRunning.Text = "启动前自动重启旧进程";
            _chkRestartRunning.AutoSize = true;
            _chkRestartRunning.Font = new Font("Microsoft YaHei UI", 8.75F);
            _chkRestartRunning.ForeColor = TextPrimary;
            _chkRestartRunning.Location = new Point(268, 178);
            _mainCard.Controls.Add(_chkRestartRunning);

            // 3. Collapsible Advanced Toggle (y = 304)
            _lnkAdvanced = new LinkLabel();
            _lnkAdvanced.Text = "+ 更多选项（直连规则 / 环境变量）";
            _lnkAdvanced.Font = new Font("Microsoft YaHei UI", 8.5F);
            _lnkAdvanced.LinkColor = TextMuted;
            _lnkAdvanced.ActiveLinkColor = BlueText;
            _lnkAdvanced.VisitedLinkColor = TextMuted;
            _lnkAdvanced.LinkBehavior = LinkBehavior.HoverUnderline;
            _lnkAdvanced.AutoSize = true;
            _lnkAdvanced.Location = new Point(26, 304);
            _lnkAdvanced.Click += OnToggleAdvancedClick;
            Controls.Add(_lnkAdvanced);

            // Advanced Card (hidden by default, y = 330..438)
            _advCard = new RoundedCard();
            _advCard.Location = new Point(24, 330);
            _advCard.Size = new Size(472, 108);
            _advCard.Visible = false;
            Controls.Add(_advCard);

            Label lblNoProxy = CreateFieldLabel("直连列表 (NO_PROXY)", 20, 12);
            _advCard.Controls.Add(lblNoProxy);

            _txtNoProxy = new ModernInputBox();
            _txtNoProxy.Location = new Point(20, 34);
            _txtNoProxy.Size = new Size(432, 32);
            _txtNoProxy.Font = new Font("Consolas", 9.25F);
            _advCard.Controls.Add(_txtNoProxy);

            _chkAutoDetect = new CheckBox();
            _chkAutoDetect.Text = "启动时自动匹配可用端口";
            _chkAutoDetect.AutoSize = true;
            _chkAutoDetect.Font = new Font("Microsoft YaHei UI", 8.5F);
            _chkAutoDetect.ForeColor = TextPrimary;
            _chkAutoDetect.Location = new Point(20, 76);
            _advCard.Controls.Add(_chkAutoDetect);

            _chkAllProxy = new CheckBox();
            _chkAllProxy.Text = "注入 ALL_PROXY";
            _chkAllProxy.AutoSize = true;
            _chkAllProxy.Font = new Font("Microsoft YaHei UI", 8.5F);
            _chkAllProxy.ForeColor = TextPrimary;
            _chkAllProxy.Location = new Point(204, 76);
            _advCard.Controls.Add(_chkAllProxy);

            _chkCloseAfter = new CheckBox();
            _chkCloseAfter.Text = "启动后关闭本窗口";
            _chkCloseAfter.AutoSize = true;
            _chkCloseAfter.Font = new Font("Microsoft YaHei UI", 8.5F);
            _chkCloseAfter.ForeColor = TextPrimary;
            _chkCloseAfter.Location = new Point(332, 76);
            _advCard.Controls.Add(_chkCloseAfter);

            // 4. Action Panel (y = 330 when collapsed, 450 when expanded)
            _actionPanel = new Panel();
            _actionPanel.Location = new Point(24, 330);
            _actionPanel.Size = new Size(472, 92);
            _actionPanel.BackColor = Color.Transparent;
            Controls.Add(_actionPanel);

            _btnLaunch = new ModernButton();
            _btnLaunch.IsPrimary = true;
            _btnLaunch.CornerRadius = 10;
            _btnLaunch.Text = "▶   免 TUN 启动 Antigravity";
            _btnLaunch.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
            _btnLaunch.Location = new Point(0, 0);
            _btnLaunch.Size = new Size(472, 46);
            _btnLaunch.Click += OnLaunchClick;
            _actionPanel.Controls.Add(_btnLaunch);

            _btnShortcut = new ModernButton();
            _btnShortcut.Text = "创建桌面快捷方式";
            _btnShortcut.Font = new Font("Microsoft YaHei UI", 9F);
            _btnShortcut.Location = new Point(0, 56);
            _btnShortcut.Size = new Size(230, 34);
            _btnShortcut.Click += OnCreateShortcutClick;
            _actionPanel.Controls.Add(_btnShortcut);

            _btnAgyCmd = new ModernButton();
            _btnAgyCmd.Text = "写入终端 agy 命令";
            _btnAgyCmd.Font = new Font("Microsoft YaHei UI", 9F);
            _btnAgyCmd.Location = new Point(242, 56);
            _btnAgyCmd.Size = new Size(230, 34);
            _btnAgyCmd.Click += OnInstallAgyClick;
            _actionPanel.Controls.Add(_btnAgyCmd);

            // 5. Minimal Footer Status
            _lblFooter = new Label();
            _lblFooter.Text = "就绪";
            _lblFooter.Font = new Font("Microsoft YaHei UI", 8.25F);
            _lblFooter.ForeColor = TextMuted;
            _lblFooter.Location = new Point(26, 428);
            _lblFooter.Size = new Size(468, 18);
            _lblFooter.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_lblFooter);

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ResumeLayout(false);
        }

        private Image LoadEmbeddedLogo()
        {
            try
            {
                Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AntigProxy.logo.png");
                if (stream != null)
                {
                    using (stream)
                    using (Image raw = Image.FromStream(stream))
                    {
                        Bitmap crisp = new Bitmap(88, 88, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(crisp))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.DrawImage(raw, 0, 0, 88, 88);
                        }
                        return crisp;
                    }
                }
            }
            catch { }
            return null;
        }

        private Label CreateFieldLabel(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(71, 85, 105);
            lbl.AutoSize = true;
            lbl.Location = new Point(x, y);
            return lbl;
        }

        private void LoadConfigToUi()
        {
            _txtHost.Text = _config.ProxyHost;
            _txtPort.Text = _config.ProxyPort.ToString();
            _txtAgPath.Text = _config.AntigravityPath;
            _txtNoProxy.Text = _config.NoProxy;
            _chkAutoDetect.Checked = _config.AutoDetectProxy;
            _chkChromiumArgs.Checked = _config.PassChromiumProxyArgs;
            _chkAllProxy.Checked = _config.SetAllProxyEnv;
            _chkRestartRunning.Checked = _config.RestartRunningAntigravity;
            _chkCloseAfter.Checked = _config.CloseLauncherAfterLaunch;
        }

        private bool ReadUiToConfig(out string error)
        {
            error = "";
            string host = _txtHost.Text.Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                host = "127.0.0.1";
            }

            int port;
            if (!int.TryParse(_txtPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                error = "请输入 1 ~ 65535 之间的有效端口号";
                return false;
            }

            _config.ProxyHost = host;
            _config.ProxyPort = port;
            _config.AntigravityPath = _txtAgPath.Text.Trim();
            _config.NoProxy = string.IsNullOrWhiteSpace(_txtNoProxy.Text) ? "localhost,127.0.0.1,::1" : _txtNoProxy.Text.Trim();
            _config.AutoDetectProxy = _chkAutoDetect.Checked;
            _config.PassChromiumProxyArgs = _chkChromiumArgs.Checked;
            _config.SetAllProxyEnv = _chkAllProxy.Checked;
            _config.RestartRunningAntigravity = _chkRestartRunning.Checked;
            _config.CloseLauncherAfterLaunch = _chkCloseAfter.Checked;
            return true;
        }

        private void RefreshDetection(bool applyDetectedToInputs)
        {
            int currentPort = _config.ProxyPort;
            int parsed;
            if (int.TryParse(_txtPort.Text.Trim(), out parsed) && parsed > 0 && parsed <= 65535)
            {
                currentPort = parsed;
            }
            string currentHost = string.IsNullOrWhiteSpace(_txtHost.Text) ? _config.ProxyHost : _txtHost.Text.Trim();

            _lastEnv = ProxyService.DetectEnvironment(currentHost, currentPort);

            if (_lastEnv.ProxyAvailable)
            {
                if (applyDetectedToInputs && _lastEnv.DetectedPort > 0)
                {
                    _txtHost.Text = _lastEnv.DetectedHost;
                    _txtPort.Text = _lastEnv.DetectedPort.ToString();
                }

                string pillText = string.Format("代理就绪 · {0}", _lastEnv.DetectedPort);
                _pillStatus.SetState(pillText, GreenBg, GreenBorder, GreenText, GreenText);

                string agText = _lastEnv.AntigravityRunning
                    ? string.Format("Antigravity 运行中 ({0})", _lastEnv.AntigravityProcessCount)
                    : "Antigravity 未运行";
                SetFooter(string.Format("已识别 {0}:{1}（{2}） · {3}", _lastEnv.DetectedHost, _lastEnv.DetectedPort, _lastEnv.Source, agText), TextMuted);
            }
            else
            {
                _pillStatus.SetState("未检测到代理", AmberBg, AmberBorder, AmberText, AmberText);
                SetFooter("未检测到正在监听的本地代理端口，请先开启代理软件或手动填写端口", AmberText);
            }

            if (string.IsNullOrWhiteSpace(_txtAgPath.Text) && !string.IsNullOrEmpty(_lastEnv.DetectedAntigravityPath))
            {
                _txtAgPath.Text = _lastEnv.DetectedAntigravityPath;
            }
        }

        private void OnDetectClick(object sender, EventArgs e)
        {
            RefreshDetection(true);
            if (_lastEnv != null && _lastEnv.ProxyAvailable)
            {
                _lblTestHint.Text = string.Format("✓ 已自动识别本地代理：{0}:{1}（{2}）", _lastEnv.DetectedHost, _lastEnv.DetectedPort, _lastEnv.Source);
                _lblTestHint.ForeColor = GreenText;
            }
            else
            {
                _lblTestHint.Text = "× 未扫描到可用代理端口，请先启动你的代理客户端";
                _lblTestHint.ForeColor = AmberText;
            }
        }

        private async void OnTestProxyClick(object sender, EventArgs e)
        {
            string err;
            if (!ReadUiToConfig(out err))
            {
                SetFooter(err, RedText);
                return;
            }

            _btnTest.Enabled = false;
            _btnTest.Text = "测速中";
            _lblTestHint.Text = string.Format("正在通过 {0} 访问 Google...", _config.GetProxyUrl());
            _lblTestHint.ForeColor = BlueText;

            try
            {
                ProxyTestResult res = await ProxyService.TestProxyConnectivityAsync(_config.ProxyHost, _config.ProxyPort);
                if (res.HttpSuccess)
                {
                    _lblTestHint.Text = "✓ " + res.Message;
                    _lblTestHint.ForeColor = GreenText;
                    _pillStatus.SetState(string.Format("代理就绪 · {0}", _config.ProxyPort), GreenBg, GreenBorder, GreenText, GreenText);
                    SetFooter("代理测试通过，点击下方按钮即可免 TUN 启动 Antigravity", GreenText);
                }
                else
                {
                    _lblTestHint.Text = "× " + res.Message;
                    _lblTestHint.ForeColor = res.TcpReachable ? AmberText : RedText;
                    SetFooter(res.Message, res.TcpReachable ? AmberText : RedText);
                }
            }
            finally
            {
                _btnTest.Text = "测速";
                _btnTest.Enabled = true;
            }
        }

        private void OnBrowseAgClick(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "选择 Antigravity.exe";
                dlg.Filter = "Antigravity (Antigravity.exe)|Antigravity.exe|可执行文件 (*.exe)|*.exe";
                if (!string.IsNullOrWhiteSpace(_txtAgPath.Text) && File.Exists(_txtAgPath.Text))
                {
                    dlg.InitialDirectory = Path.GetDirectoryName(_txtAgPath.Text);
                }
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _txtAgPath.Text = dlg.FileName;
                }
            }
        }

        private void OnToggleAdvancedClick(object sender, EventArgs e)
        {
            _advancedExpanded = !_advancedExpanded;
            _advCard.Visible = _advancedExpanded;
            _lnkAdvanced.Text = _advancedExpanded
                ? "− 收起更多选项"
                : "+ 更多选项（直连规则 / 环境变量）";

            float scaleY = CurrentAutoScaleDimensions.Height / 96f;
            if (scaleY <= 0.1f) scaleY = 1f;

            int delta = (int)Math.Round(120 * scaleY);
            if (_advancedExpanded)
            {
                _actionPanel.Top += delta;
                _lblFooter.Top += delta;
                ClientSize = new Size(ClientSize.Width, ClientSize.Height + delta);
            }
            else
            {
                _actionPanel.Top -= delta;
                _lblFooter.Top -= delta;
                ClientSize = new Size(ClientSize.Width, ClientSize.Height - delta);
            }
        }

        private void OnLaunchClick(object sender, EventArgs e)
        {
            string err;
            if (!ReadUiToConfig(out err))
            {
                SetFooter(err, RedText);
                return;
            }

            bool portOpen = ProxyService.IsPortOpen(_config.ProxyHost, _config.ProxyPort, 220);
            if (!portOpen)
            {
                DialogResult dr = MessageBox.Show(
                    this,
                    string.Format("本地代理 {0}:{1} 当前似乎未监听。\n\n请确认你的代理软件已启动，仍要继续启动 Antigravity 吗？", _config.ProxyHost, _config.ProxyPort),
                    "代理端口未监听",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (dr != DialogResult.Yes) return;
            }

            ProxyDetectionResult env = ProxyService.DetectEnvironment(_config.ProxyHost, _config.ProxyPort);
            bool tempForceRestart = false;
            if (env.AntigravityRunning && !_config.RestartRunningAntigravity)
            {
                DialogResult dr = MessageBox.Show(
                    this,
                    "检测到 Antigravity 已在运行。\n\n是否先关闭旧进程再重新启动，以确保新注入的代理环境变量完整生效？\n• [是] 结束旧进程并重新启动（推荐）\n• [否] 保留旧进程直接拉起\n• [取消] 取消操作",
                    "Antigravity 正在运行",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );
                if (dr == DialogResult.Cancel) return;
                if (dr == DialogResult.Yes) tempForceRestart = true;
            }

            try { ProxyService.SaveConfig(_config); } catch { }

            bool origRestart = _config.RestartRunningAntigravity;
            if (tempForceRestart) _config.RestartRunningAntigravity = true;
            LaunchResult res = ProxyService.LaunchAntigravity(_config);
            _config.RestartRunningAntigravity = origRestart;

            if (!res.Success)
            {
                SetFooter(res.Message, RedText);
                MessageBox.Show(this, res.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetFooter("✓ " + res.Message, GreenText);
            if (_config.CloseLauncherAfterLaunch)
            {
                Close();
            }
        }

        private void OnCreateShortcutClick(object sender, EventArgs e)
        {
            string err;
            if (!ReadUiToConfig(out err))
            {
                SetFooter(err, RedText);
                return;
            }
            try { ProxyService.SaveConfig(_config); } catch { }

            string msg;
            bool ok = ProxyService.CreateDesktopShortcut(out msg);
            SetFooter((ok ? "✓ " : "× ") + msg, ok ? GreenText : RedText);
        }

        private void OnInstallAgyClick(object sender, EventArgs e)
        {
            string err;
            if (!ReadUiToConfig(out err))
            {
                SetFooter(err, RedText);
                return;
            }
            try { ProxyService.SaveConfig(_config); } catch { }

            string msg;
            bool ok = ProxyService.InstallPowerShellAgyCommand(out msg);
            SetFooter((ok ? "✓ " : "× ") + msg, ok ? GreenText : RedText);
        }

        private void SetFooter(string text, Color color)
        {
            _lblFooter.Text = text;
            _lblFooter.ForeColor = color;
        }
    }
}
