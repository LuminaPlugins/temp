using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace OpenPrestige
{
    public class MainForm : Form
    {
        private CrlServer _crlServer;
        private ReplayServer _replayServer;

        private Label _lblTitle;
        private Label _lblDevBadge;
        private Label _lblSubtitle;
        private Label _lblThemeTag;
        private GlassComboBox _cmbTheme;
        private Label _lblCrlStatus;
        private Label _lblReplayStatus;

        private GlassCard _cardProcess;
        private Label _lblProcessHeader;
        private GlassComboBox _cmbProcesses;
        private GlassButton _btnRefresh;
        private GlassButton _btnInject;
        private Label _lblProcessStatus;

        private GlassCard _cardLogs;
        private Label _lblLogHeader;
        private GlassButton _btnClearLog;
        private RichTextBox _rtbLog;

        private Label _lblFooter;

        private System.Windows.Forms.Timer _processScanTimer;

        public MainForm()
        {
            UITheme.Initialize();
            InitializeComponent();
            UITheme.ThemeChanged += OnThemeChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UITheme.EnableDarkMode(Handle);
        }

        private void InitializeComponent()
        {
            Text = Variables.WindowTitle;
            Size = new Size(UITheme.Scale(840), UITheme.Scale(670));
            MinimumSize = new Size(UITheme.Scale(720), UITheme.Scale(560));
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UITheme.BgDark;
            ForeColor = UITheme.TextPrimary;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            int pad = UITheme.Scale(24);

            // --- HEADER ---
            _lblTitle = new Label
            {
                Text = Variables.AppTitle,
                Font = UITheme.FontDisplay,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(pad, UITheme.Scale(18)),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _lblDevBadge = new Label
            {
                Text = Variables.AuthorCredit,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.HighlightTag,
                BackColor = Color.FromArgb(140, 24, 28, 44),
                Location = new Point(_lblTitle.Right + UITheme.Scale(12), UITheme.Scale(21)),
                Padding = new Padding(UITheme.Scale(8), UITheme.Scale(3), UITheme.Scale(8), UITheme.Scale(3)),
                AutoSize = true
            };

            _lblSubtitle = new Label
            {
                Text = Variables.AppSubtitle,
                Font = UITheme.FontSubtitle,
                ForeColor = UITheme.TextMuted,
                Location = new Point(pad, UITheme.Scale(46)),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Theme selector on header right
            int themeComboW = UITheme.Scale(125);
            int themeComboX = Width - UITheme.Scale(310);

            _cmbTheme = new GlassComboBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(themeComboX, UITheme.Scale(18)),
                Size = new Size(themeComboW, UITheme.Scale(28)),
                Font = UITheme.FontSmall
            };
            _cmbTheme.Items.AddRange(new object[] {
                "Cyber Indigo",
                "Emerald Matrix",
                "Obsidian Crimson",
                "Sapphire Ice",
                "Amethyst Sunset"
            });
            _cmbTheme.SelectedIndex = (int)UITheme.CurrentTheme;
            _cmbTheme.SelectedIndexChanged += (s, e) =>
            {
                if (_cmbTheme.SelectedIndex >= 0)
                {
                    UITheme.ApplyTheme((ThemePreset)_cmbTheme.SelectedIndex);
                }
            };

            _lblThemeTag = new Label
            {
                Text = Variables.ThemeLabel,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextMuted,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _lblThemeTag.Location = new Point(_cmbTheme.Left - _lblThemeTag.PreferredWidth - UITheme.Scale(8), UITheme.Scale(22));

            _lblCrlStatus = new Label
            {
                Text = Variables.CrlBadgeOnline,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.StatusAmber,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - UITheme.Scale(170), UITheme.Scale(22)),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _lblReplayStatus = new Label
            {
                Text = Variables.ReplayBadgeOnline,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.StatusAmber,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - UITheme.Scale(85), UITheme.Scale(22)),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            Controls.Add(_lblTitle);
            Controls.Add(_lblDevBadge);
            Controls.Add(_lblSubtitle);
            Controls.Add(_lblThemeTag);
            Controls.Add(_cmbTheme);
            Controls.Add(_lblCrlStatus);
            Controls.Add(_lblReplayStatus);

            // --- PROCESS CARD (Frosted Glass) ---
            int cardW = Width - pad * 2 - UITheme.Scale(16);
            _cardProcess = new GlassCard
            {
                Location = new Point(pad, UITheme.Scale(76)),
                Size = new Size(cardW, UITheme.Scale(114)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 14
            };

            _lblProcessHeader = new Label
            {
                Text = Variables.ProcessSectionHeader,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.HighlightTag,
                Location = new Point(UITheme.Scale(16), UITheme.Scale(14)),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            int btnScanW = UITheme.Scale(96);
            int btnInjectW = UITheme.Scale(104);
            int btnH = UITheme.Scale(34);
            int gap = UITheme.Scale(10);

            _btnInject = new GlassButton
            {
                Text = Variables.BtnInjectText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_cardProcess.Width - btnInjectW - UITheme.Scale(16), UITheme.Scale(36)),
                Size = new Size(btnInjectW, btnH),
                IsAccent = true,
                BorderRadius = 8
            };
            _btnInject.Click += (s, e) => OnInjectClicked();

            _btnRefresh = new GlassButton
            {
                Text = Variables.BtnScanText,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_btnInject.Left - btnScanW - gap, UITheme.Scale(36)),
                Size = new Size(btnScanW, btnH),
                IsAccent = false,
                BorderRadius = 8
            };
            _btnRefresh.Click += (s, e) => RefreshProcesses();

            _cmbProcesses = new GlassComboBox
            {
                Location = new Point(UITheme.Scale(16), UITheme.Scale(36)),
                Size = new Size(_btnRefresh.Left - UITheme.Scale(26), btnH),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblProcessStatus = new Label
            {
                Text = Variables.StatusScanning,
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextMuted,
                Location = new Point(UITheme.Scale(16), UITheme.Scale(80)),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _cardProcess.Controls.Add(_lblProcessHeader);
            _cardProcess.Controls.Add(_cmbProcesses);
            _cardProcess.Controls.Add(_btnRefresh);
            _cardProcess.Controls.Add(_btnInject);
            _cardProcess.Controls.Add(_lblProcessStatus);
            Controls.Add(_cardProcess);

            // --- LOG CARD (Frosted Glass) ---
            int logTop = _cardProcess.Bottom + UITheme.Scale(14);
            int logH = Height - logTop - UITheme.Scale(70);
            _cardLogs = new GlassCard
            {
                Location = new Point(pad, logTop),
                Size = new Size(cardW, logH),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 14
            };

            _lblLogHeader = new Label
            {
                Text = Variables.LogSectionHeader,
                Font = UITheme.FontSmallBold,
                ForeColor = UITheme.TextMuted,
                Location = new Point(UITheme.Scale(16), UITheme.Scale(12)),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            int btnClearW = UITheme.Scale(84);
            int btnClearH = UITheme.Scale(26);
            _btnClearLog = new GlassButton
            {
                Text = Variables.BtnClearLogText,
                Location = new Point(_cardLogs.Width - btnClearW - UITheme.Scale(16), UITheme.Scale(10)),
                Size = new Size(btnClearW, btnClearH),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                IsAccent = false,
                Font = UITheme.FontSmall,
                BorderRadius = 6
            };
            _btnClearLog.Click += (s, e) => _rtbLog.Clear();

            _rtbLog = new RichTextBox
            {
                Location = new Point(UITheme.Scale(16), UITheme.Scale(42)),
                Size = new Size(_cardLogs.Width - UITheme.Scale(32), _cardLogs.Height - UITheme.Scale(56)),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = UITheme.ConsoleBg,
                ForeColor = Color.FromArgb(226, 232, 240),
                BorderStyle = BorderStyle.None,
                Font = UITheme.FontMono,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            _cardLogs.Controls.Add(_lblLogHeader);
            _cardLogs.Controls.Add(_btnClearLog);
            _cardLogs.Controls.Add(_rtbLog);
            Controls.Add(_cardLogs);

            // --- FOOTER ---
            _lblFooter = new Label
            {
                Text = Variables.FooterCredit,
                Font = UITheme.FontSmall,
                ForeColor = UITheme.TextFaint,
                Dock = DockStyle.Bottom,
                Height = UITheme.Scale(28),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            Controls.Add(_lblFooter);

            // Form Events
            Load += OnMainFormLoad;
            FormClosing += OnMainFormClosing;

            _processScanTimer = new System.Windows.Forms.Timer { Interval = 2500 };
            _processScanTimer.Tick += (s, e) =>
            {
                if (_cmbProcesses.Items.Count == 0)
                {
                    RefreshProcesses(silent: true);
                }
            };
        }

        private void OnThemeChanged()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(OnThemeChanged));
                return;
            }

            BackColor = UITheme.BgDark;
            _lblDevBadge.ForeColor = UITheme.HighlightTag;
            _lblProcessHeader.ForeColor = UITheme.HighlightTag;
            if (_cardProcess != null) _cardProcess.BackColor = UITheme.CardBgSolid;
            if (_cardLogs != null) _cardLogs.BackColor = UITheme.CardBgSolid;
            if (_btnInject != null) _btnInject.BackColor = UITheme.CardBgSolid;
            if (_btnRefresh != null) _btnRefresh.BackColor = UITheme.CardBgSolid;
            if (_btnClearLog != null) _btnClearLog.BackColor = UITheme.CardBgSolid;
            if (_cmbProcesses != null) _cmbProcesses.BackColor = UITheme.CardBgSolid;
            Invalidate(true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            UITheme.PaintGlassBackground(e.Graphics, ClientRectangle);
        }

        private void OnMainFormLoad(object sender, EventArgs e)
        {
            AppendLog(Variables.LogStartup, Color.FromArgb(96, 165, 250));

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    // Ensure Root CA
                    CertManager.EnsureRootCaTrusted(AppendLog);

                    // Start CRL server on :8081
                    try
                    {
                        _crlServer = new CrlServer(8081, AppendLog);
                        _crlServer.Start();
                        if (IsHandleCreated)
                        {
                            Invoke(new Action(() =>
                            {
                                if (_crlServer.IsRunning)
                                {
                                    _lblCrlStatus.Text = Variables.CrlBadgeOnline;
                                    _lblCrlStatus.ForeColor = UITheme.StatusGreen;
                                }
                                else
                                {
                                    _lblCrlStatus.Text = Variables.CrlBadgeOffline;
                                    _lblCrlStatus.ForeColor = UITheme.StatusRed;
                                }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog(string.Format(Variables.LogCrlStartFailed, 8081, ex.Message), UITheme.StatusRed);
                    }

                    // Start Replay server on :443
                    try
                    {
                        _replayServer = new ReplayServer(443, AppendLog);
                        _replayServer.Start();
                        if (IsHandleCreated)
                        {
                            Invoke(new Action(() =>
                            {
                                if (_replayServer.IsRunning)
                                {
                                    _lblReplayStatus.Text = Variables.ReplayBadgeOnline;
                                    _lblReplayStatus.ForeColor = UITheme.StatusGreen;
                                }
                                else
                                {
                                    _lblReplayStatus.Text = Variables.ReplayBadgeOffline;
                                    _lblReplayStatus.ForeColor = UITheme.StatusRed;
                                }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog(string.Format(Variables.LogReplayStartFailed, 443, ex.Message), UITheme.StatusRed);
                    }

                    // Pre-extract DLL for standalone portability
                    ProcessInjector.EnsureDllExtracted(AppendLog);

                    // Initial process scan and timer on UI thread
                    if (IsHandleCreated)
                    {
                        Invoke(new Action(() =>
                        {
                            RefreshProcesses();
                            if (_processScanTimer != null) _processScanTimer.Start();
                        }));
                    }
                }
                catch (Exception ex)
                {
                    AppendLog("[!] Initialization error: " + ex.Message, UITheme.StatusRed);
                }
            });
        }

        private void OnMainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_processScanTimer != null)
            {
                _processScanTimer.Stop();
                _processScanTimer.Dispose();
            }

            if (_crlServer != null)
            {
                _crlServer.Stop();
            }

            if (_replayServer != null)
            {
                _replayServer.Stop();
            }
        }

        private void RefreshProcesses(bool silent = false)
        {
            int previouslySelectedPid = -1;
            if (_cmbProcesses.SelectedItem is ProcessItem)
            {
                previouslySelectedPid = ((ProcessItem)_cmbProcesses.SelectedItem).Pid;
            }

            var list = ProcessInjector.GetRunningJavaProcesses();
            _cmbProcesses.Items.Clear();

            int selectIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                _cmbProcesses.Items.Add(list[i]);
                if (list[i].Pid == previouslySelectedPid)
                {
                    selectIndex = i;
                }
            }

            if (selectIndex >= 0)
            {
                _cmbProcesses.SelectedIndex = selectIndex;
            }
            else if (_cmbProcesses.Items.Count > 0)
            {
                _cmbProcesses.SelectedIndex = 0;
            }

            if (!silent)
            {
                if (list.Count > 0)
                {
                    _lblProcessStatus.Text = string.Format(Variables.StatusFoundProcesses, list.Count);
                    _lblProcessStatus.ForeColor = UITheme.StatusGreen;
                    AppendLog(string.Format(Variables.LogFoundProcesses, list.Count), UITheme.TextMuted);
                }
                else
                {
                    _lblProcessStatus.Text = Variables.StatusNoProcesses;
                    _lblProcessStatus.ForeColor = UITheme.StatusAmber;
                }
            }
        }

        private void OnInjectClicked()
        {
            ProcessItem selected = _cmbProcesses.SelectedItem as ProcessItem;
            if (selected == null)
            {
                MessageBox.Show(
                    Variables.MsgNoProcessSelectedBody,
                    Variables.MsgNoProcessSelectedTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _btnInject.Enabled = false;
            _lblProcessStatus.Text = string.Format(Variables.StatusInjecting, selected.Pid, selected.ProcessName);
            _lblProcessStatus.ForeColor = UITheme.HighlightTag;

            ThreadPool.QueueUserWorkItem(state =>
            {
                bool success = ProcessInjector.Inject(selected.Pid, AppendLog);
                Invoke(new Action(() =>
                {
                    _btnInject.Enabled = true;
                    if (success)
                    {
                        _lblProcessStatus.Text = string.Format(Variables.StatusInjectSuccess, selected.Pid);
                        _lblProcessStatus.ForeColor = UITheme.StatusGreen;
                    }
                    else
                    {
                        _lblProcessStatus.Text = string.Format(Variables.StatusInjectFailed, selected.Pid);
                        _lblProcessStatus.ForeColor = UITheme.StatusRed;
                    }
                }));
            });
        }

        public void AppendLog(string message)
        {
            Color color = UITheme.TextPrimary;
            if (message.StartsWith("[+]")) color = UITheme.StatusGreen;
            else if (message.StartsWith("[*]")) color = UITheme.HighlightTag;
            else if (message.StartsWith("[!]")) color = UITheme.StatusRed;
            else if (message.StartsWith("[CRL]")) color = UITheme.StatusAmber;
            else if (message.StartsWith("[CFG]")) color = Color.FromArgb(192, 132, 252);
            else if (message.StartsWith("[SERVE]")) color = UITheme.StatusGreen;
            else if (message.StartsWith("[MISS]")) color = UITheme.StatusAmber;

            AppendLog(message, color);
        }

        public void AppendLog(string message, Color color)
        {
            if (InvokeRequired)
            {
                try
                {
                    Invoke(new Action(() => AppendLog(message, color)));
                }
                catch { }
                return;
            }

            string time = DateTime.Now.ToString("HH:mm:ss.ff");
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;

            // Timestamp in muted color
            _rtbLog.SelectionColor = Color.FromArgb(100, 116, 139);
            _rtbLog.AppendText(time + " ");

            // Message in theme color
            _rtbLog.SelectionColor = color;
            _rtbLog.AppendText(message + "\n");

            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.ScrollToCaret();
        }
    }
}
