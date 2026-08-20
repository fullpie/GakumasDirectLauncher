using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GakumasSmartLauncher
{
    public sealed class MainForm : Form
    {
        private readonly LauncherEnvironment _environment;
        private readonly UiStrings _text;
        private readonly bool _autoLaunch;
        private readonly bool _skipElevation;
        private readonly Timer _monitorTimer;
        private readonly Icon _appIcon;

        private Label _statusValue;
        private Label _versionValue;
        private Label _cacheValue;
        private Label _dmmValue;
        private Label _pathValue;
        private Label _messageLabel;
        private SlimProgressBar _progress;
        private Button _launchButton;
        private Button _repairButton;

        private Process _monitoredProcess;
        private int _monitorSecondsRemaining;

        public MainForm(LauncherEnvironment environment, bool autoLaunch, bool skipElevation)
        {
            _environment = environment;
            _text = UiStrings.Detect();
            _autoLaunch = autoLaunch;
            _skipElevation = skipElevation;

            Text = _text.WindowTitle;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(940, 620);
            MinimumSize = new Size(860, 600);
            MaximizeBox = false;
            BackColor = Palette.Background;
            Font = new Font("Microsoft JhengHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            _appIcon = AppIconFactory.Create();
            Icon = _appIcon;

            BuildInterface();

            _monitorTimer = new Timer { Interval = 1000 };
            _monitorTimer.Tick += MonitorTimerOnTick;

            Shown += OnShown;
            FormClosed += OnFormClosed;
        }

        private void BuildInterface()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Palette.Background,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(34, 28, 34, 24)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            root.Controls.Add(CreateHeader(), 0, 0);
            root.Controls.Add(CreateHeroCard(), 0, 1);
            root.Controls.Add(CreateInformationGrid(), 0, 2);
            root.Controls.Add(CreateFooter(), 0, 3);
        }

        private Control CreateHeader()
        {
            var header = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Palette.Background,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 20)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var accent = new RoundedPanel
            {
                BackColor = Palette.Primary,
                CornerRadius = 26,
                Size = new Size(52, 52),
                Margin = new Padding(0, 4, 15, 0)
            };
            accent.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "★",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Symbol", 19F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            });
            header.Controls.Add(accent, 0, 0);

            var brand = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Palette.Background,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            brand.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            brand.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var title = new Label
            {
                AutoSize = true,
                Text = _text.HeaderTitle,
                Font = new Font("Yu Gothic UI", 21F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Palette.Indigo,
                Margin = Padding.Empty
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Text = _text.HeaderSubtitle,
                Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Palette.TextMuted,
                Margin = new Padding(2, 7, 0, 0)
            };
            brand.Controls.Add(title, 0, 0);
            brand.Controls.Add(subtitle, 0, 1);
            header.Controls.Add(brand, 1, 0);

            return header;
        }

        private Control CreateHeroCard()
        {
            var card = new IdolHeroPanel
            {
                BackColor = Color.FromArgb(255, 253, 253),
                CornerRadius = 20,
                BorderColor = Palette.Border,
                BorderWidth = 1,
                Dock = DockStyle.Top,
                Height = 178,
                Margin = new Padding(0, 0, 0, 18),
                Padding = new Padding(26, 22, 26, 22)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

            var statusLayout = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty
            };
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var eyebrow = new Label
            {
                AutoSize = true,
                Text = _text.StatusEyebrow,
                Font = new Font("Yu Gothic UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Palette.Primary,
                Margin = Padding.Empty
            };
            _statusValue = new Label
            {
                AutoSize = true,
                Text = _text.Checking,
                Font = new Font(Font.FontFamily, 18F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Palette.Text,
                Margin = new Padding(0, 6, 0, 0)
            };
            _messageLabel = new Label
            {
                AutoSize = true,
                AutoEllipsis = true,
                MaximumSize = new Size(560, 0),
                Text = _text.ReadingLocalState,
                Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Palette.TextMuted,
                Margin = new Padding(0, 7, 0, 0)
            };
            _progress = new SlimProgressBar
            {
                Dock = DockStyle.Top,
                Minimum = 0,
                Maximum = 35,
                Value = 0,
                Height = 8,
                Visible = false,
                Margin = new Padding(0, 14, 34, 0)
            };
            statusLayout.Controls.Add(eyebrow, 0, 0);
            statusLayout.Controls.Add(_statusValue, 0, 1);
            statusLayout.Controls.Add(_messageLabel, 0, 2);
            statusLayout.Controls.Add(_progress, 0, 3);
            layout.Controls.Add(statusLayout, 0, 0);

            var buttonStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20, 3, 0, 0),
                Margin = Padding.Empty
            };
            _launchButton = CreatePrimaryButton(_text.StartButton, 230, 66);
            _launchButton.Margin = new Padding(0, 0, 0, 10);
            _launchButton.Click += delegate { AttemptLaunch(); };
            _repairButton = CreateSecondaryButton(_text.UpdateButton, 230);
            _repairButton.Margin = Padding.Empty;
            _repairButton.Click += delegate { UpdateFromDmmAndClose(); };
            buttonStack.Controls.Add(_launchButton);
            buttonStack.Controls.Add(_repairButton);
            layout.Controls.Add(buttonStack, 1, 0);
            card.Controls.Add(layout);
            return card;
        }

        private Control CreateInformationGrid()
        {
            var grid = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Palette.Background,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 16)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));

            var versionCard = CreateInfoCard(_text.GameVersionLabel, "—", Palette.Sky, out _versionValue);
            versionCard.Margin = new Padding(0, 0, 8, 10);
            var cacheCard = CreateInfoCard(_text.LastSyncLabel, "—", Palette.Primary, out _cacheValue);
            cacheCard.Margin = new Padding(5, 0, 5, 10);
            var dmmCard = CreateInfoCard(_text.DmmStatusLabel, "—", Palette.Gold, out _dmmValue);
            dmmCard.Margin = new Padding(8, 0, 0, 10);
            var pathCard = CreateInfoCard(_text.GameLocationLabel, "—", Palette.Lavender, out _pathValue);
            pathCard.Margin = Padding.Empty;

            grid.Controls.Add(versionCard, 0, 0);
            grid.Controls.Add(cacheCard, 1, 0);
            grid.Controls.Add(dmmCard, 2, 0);
            grid.Controls.Add(pathCard, 0, 1);
            grid.SetColumnSpan(pathCard, 3);
            return grid;
        }

        private RoundedPanel CreateInfoCard(string title, string initialValue, Color accentColor, out Label valueLabel)
        {
            var card = new RoundedPanel
            {
                BackColor = Palette.SurfaceSoft,
                CornerRadius = 16,
                BorderColor = Color.FromArgb(105, accentColor),
                BorderWidth = 1,
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 14, 18, 13)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Palette.SurfaceSoft,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            var titleLabel = new Label
            {
                AutoSize = true,
                Text = "●  " + title,
                ForeColor = accentColor,
                Font = new Font("Yu Gothic UI", 8.7F, FontStyle.Bold, GraphicsUnit.Point),
                Margin = Padding.Empty
            };
            valueLabel = new Label
            {
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                Text = initialValue,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Palette.Indigo,
                Margin = new Padding(0, 4, 0, 0)
            };
            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(valueLabel, 0, 1);
            card.Controls.Add(layout);
            return card;
        }

        private Control CreateFooter()
        {
            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Palette.Background,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var version = new Label
            {
                AutoSize = true,
                Text = "v1.2",
                Font = new Font(Font.FontFamily, 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Palette.TextMuted,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            footer.Controls.Add(version, 1, 0);
            return footer;
        }

        private Button CreatePrimaryButton(string text, int width, int height)
        {
            return new ModernButton
            {
                Text = text,
                Size = new Size(width, height),
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                NormalBackColor = Palette.Primary,
                HoverBackColor = Palette.PrimaryHover,
                PressedBackColor = Palette.PrimaryPressed,
                BorderWidth = 0,
                CornerRadius = 16,
                TabStop = true
            };
        }

        private Button CreateSecondaryButton(string text, int width)
        {
            return new ModernButton
            {
                Text = text,
                Size = new Size(width, 48),
                Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Palette.Text,
                NormalBackColor = Palette.Surface,
                HoverBackColor = Palette.PrimarySoft,
                PressedBackColor = Color.FromArgb(250, 224, 230),
                DisabledBackColor = Color.FromArgb(239, 232, 234),
                BorderColor = Palette.Border,
                BorderWidth = 1,
                CornerRadius = 13,
                Margin = new Padding(0, 0, 10, 0),
                TabStop = true
            };
        }

        private void OnShown(object sender, EventArgs eventArgs)
        {
            RefreshStatus();
            if (_autoLaunch)
            {
                BeginInvoke(new Action(AttemptLaunch));
            }
        }

        private void OnFormClosed(object sender, FormClosedEventArgs eventArgs)
        {
            _monitorTimer.Stop();
            _monitorTimer.Dispose();
            _appIcon.Dispose();
        }

        private void RefreshStatus()
        {
            try
            {
                var snapshot = _environment.GetSnapshot();
                var ready = snapshot.GamePathExists && snapshot.CacheReadable;
                if (snapshot.GameRunning)
                {
                    _statusValue.Text = _text.GameRunning;
                    _statusValue.ForeColor = Palette.Success;
                }
                else if (ready)
                {
                    _statusValue.Text = _text.Ready;
                    _statusValue.ForeColor = Palette.Text;
                }
                else
                {
                    _statusValue.Text = _text.NeedsUpdate;
                    _statusValue.ForeColor = Palette.Warning;
                }

                _messageLabel.Text = snapshot.GameRunning
                    ? _text.GameRunningMessage
                    : (ready
                        ? _text.ReadyMessage
                        : _text.UpdateInstruction);
                _messageLabel.ForeColor = ready ? Palette.TextMuted : Palette.Warning;
                _versionValue.Text = string.IsNullOrWhiteSpace(snapshot.GameVersion) ? _text.Unknown : "v" + snapshot.GameVersion;
                _cacheValue.Text = snapshot.CacheReadable
                    ? FormatTime(snapshot.CacheCapturedAt)
                    : (snapshot.CachePresent ? _text.NeedsResync : _text.NeverSynced);
                _cacheValue.ForeColor = snapshot.CacheReadable ? Palette.Success : Palette.Warning;
                _dmmValue.Text = snapshot.DmmRunning ? _text.DmmRunning : _text.DmmClosed;
                _dmmValue.ForeColor = snapshot.DmmRunning ? Palette.Warning : Palette.Success;
                _pathValue.Text = string.IsNullOrWhiteSpace(snapshot.GamePath) ? _text.NotFound : snapshot.GamePath;
            }
            catch (Exception)
            {
                ShowError(_text.UnexpectedError);
            }
        }

        private void AttemptLaunch()
        {
            SetButtonsEnabled(false);
            _statusValue.Text = _text.StartingGame;
            _statusValue.ForeColor = Palette.Primary;
            _messageLabel.ForeColor = Palette.TextMuted;
            _messageLabel.Text = _text.StartingFromCache;
            Application.DoEvents();

            try
            {
                var result = _environment.StartGame(_skipElevation);
                if (result.AlreadyRunning)
                {
                    _messageLabel.Text = _text.AlreadyRunning;
                    SetButtonsEnabled(true);
                    RefreshStatus();
                    return;
                }

                _monitoredProcess = result.Process;
                _monitorSecondsRemaining = 35;
                _progress.Visible = true;
                _progress.Value = 0;
                _statusValue.Text = _text.ConfirmingLaunch;
                _messageLabel.Text = _text.ProcessAppeared;
                _monitorTimer.Start();
            }
            catch (Exception ex)
            {
                _environment.Log("launch_failed", ToSafeMessage(ex));
                BeginRecovery();
            }
        }

        private void MonitorTimerOnTick(object sender, EventArgs eventArgs)
        {
            var gameStillRunning = Process.GetProcessesByName("gakumas").Length > 0;
            var monitoredExited = false;
            try
            {
                monitoredExited = _monitoredProcess == null || _monitoredProcess.HasExited;
            }
            catch
            {
                monitoredExited = !gameStillRunning;
            }

            if (monitoredExited && !gameStillRunning)
            {
                _monitorTimer.Stop();
                _progress.Visible = false;
                _environment.Log("launch_exited_early", "within_seconds=" + (35 - _monitorSecondsRemaining).ToString(CultureInfo.InvariantCulture));
                BeginRecovery();
                return;
            }

            _monitorSecondsRemaining--;
            _progress.Value = Math.Min(_progress.Maximum, 35 - _monitorSecondsRemaining);
            _messageLabel.Text = _text.ConfirmingSeconds(Math.Max(0, _monitorSecondsRemaining));

            if (_monitorSecondsRemaining <= 0)
            {
                _monitorTimer.Stop();
                _progress.Visible = false;
                _environment.Log("launch_confirmed", "stable_seconds=35");
                _statusValue.Text = _text.LaunchSuccess;
                _statusValue.ForeColor = Palette.Success;
                _messageLabel.ForeColor = Palette.Success;
                _messageLabel.Text = _text.LaunchSuccessMessage;
                SetButtonsEnabled(true);

                if (_autoLaunch)
                {
                    var closeTimer = new Timer { Interval = 1800 };
                    closeTimer.Tick += delegate
                    {
                        closeTimer.Stop();
                        closeTimer.Dispose();
                        Close();
                    };
                    closeTimer.Start();
                }
            }
        }

        private void BeginRecovery()
        {
            _progress.Visible = false;
            _statusValue.Text = _text.NeedsUpdate;
            _statusValue.ForeColor = Palette.Warning;
            _messageLabel.ForeColor = Palette.Warning;
            _messageLabel.Text = _text.UpdateInstruction;
            SetButtonsEnabled(true);
            RefreshInformationValuesOnly();
        }

        private void UpdateFromDmmAndClose()
        {
            SetButtonsEnabled(false);
            _statusValue.Text = _text.UpdatingData;
            _statusValue.ForeColor = Palette.Primary;
            _messageLabel.ForeColor = Palette.TextMuted;
            _messageLabel.Text = _text.ReadingDmmLog;
            Application.DoEvents();

            try
            {
                string baselineFingerprint = null;
                try
                {
                    var current = _environment.GetCachedRecord();
                    baselineFingerprint = current == null ? null : current.SourceFingerprint;
                }
                catch (LauncherException)
                {
                    baselineFingerprint = null;
                }

                var sync = _environment.SyncLatest(baselineFingerprint);
                if (!sync.Updated)
                {
                    _statusValue.Text = _text.NoNewData;
                    _statusValue.ForeColor = Palette.Warning;
                    _messageLabel.ForeColor = Palette.Warning;
                    _messageLabel.Text = _text.StartFromDmmFirst;
                    return;
                }

                var closeResult = _environment.CloseDmm();
                _statusValue.Text = _text.UpdateComplete;
                _statusValue.ForeColor = closeResult.Remaining == 0 ? Palette.Success : Palette.Warning;
                _messageLabel.ForeColor = closeResult.Remaining == 0 ? Palette.Success : Palette.Warning;
                if (closeResult.Remaining == 0)
                {
                    _messageLabel.Text = _text.UpdatedAndClosed;
                }
                else
                {
                    _messageLabel.Text = _text.UpdatedButDmmOpen;
                }

                RefreshInformationValuesOnly();
            }
            catch (Exception)
            {
                ShowError(_text.UnexpectedError);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void RefreshInformationValuesOnly()
        {
            var statusText = _statusValue.Text;
            var statusColor = _statusValue.ForeColor;
            var messageText = _messageLabel.Text;
            var messageColor = _messageLabel.ForeColor;
            RefreshStatus();
            _statusValue.Text = statusText;
            _statusValue.ForeColor = statusColor;
            _messageLabel.Text = messageText;
            _messageLabel.ForeColor = messageColor;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _launchButton.Enabled = enabled;
            _repairButton.Enabled = enabled;
        }

        private void ShowError(string message)
        {
            _statusValue.Text = _text.ErrorTitle;
            _statusValue.ForeColor = Palette.Danger;
            _messageLabel.ForeColor = Palette.Danger;
            _messageLabel.Text = message;
        }

        private static string ToSafeMessage(Exception ex)
        {
            var launcherException = ex as LauncherException;
            if (launcherException != null)
            {
                return SafeLogger.Sanitize(launcherException.Message);
            }

            return "發生未預期的錯誤；請從 DMM 啟動遊戲一次後再試。";
        }

        private string FormatTime(string value)
        {
            DateTimeOffset parsed;
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
            {
                return parsed.ToLocalTime().ToString("MM/dd HH:mm", CultureInfo.CurrentCulture);
            }

            return _text.Unknown;
        }
    }
}
