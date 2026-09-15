using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetOptimizerV2
{
    internal sealed class MainForm : Form
    {
        private static readonly Color Background = Color.FromArgb(10, 20, 28);
        private static readonly Color PanelBackground = Color.FromArgb(16, 29, 38);
        private static readonly Color TextColor = Color.FromArgb(241, 244, 247);
        private static readonly Color MutedText = Color.FromArgb(158, 177, 193);
        private static readonly Color Accent = Color.FromArgb(20, 224, 205);
        private static readonly Color Warning = Color.FromArgb(255, 199, 42);
        private static readonly Color Error = Color.FromArgb(244, 91, 105);
        private static readonly Color DisabledGlyph = Color.FromArgb(83, 104, 116);
        private const int BeginnerPanelBaseHeight = 305;
        private const int BeginnerDashboardBaseHeight = 255;
        private const int BeginnerInlineLogHeight = 190;
        private const int BeginnerInlineLogRowHeight = 198;
        private const int HeaderRowHeight = 56;
        private const int HeaderBottomMargin = 2;

        private readonly MonitorEngine engine = new MonitorEngine();
        private readonly Queue<string> visibleLog = new Queue<string>();
        private readonly bool startMinimized;
        private readonly bool autoStartMonitoring;
        private readonly bool updateChecksEnabled;
        private MonitorSettings settings;
        private UpdateState updateState;
        private UpdateInfo availableUpdate;
        private CancellationTokenSource updateCheckCancellation;
        private bool updateCheckInProgress;
        private bool closing;

        private ComboBox interfaceBox;
        private ComboBox primaryInterfaceBox;
        private ComboBox backupInterfaceBox;
        private TextBox targetsBox;
        private TextBox failoverTargetBox;
        private NumericUpDown portBox;
        private NumericUpDown thresholdBox;
        private NumericUpDown timeoutBox;
        private NumericUpDown failuresBox;
        private NumericUpDown cooldownBox;
        private NumericUpDown failoverThresholdBox;
        private NumericUpDown failoverTimeoutBox;
        private NumericUpDown failoverBadSamplesBox;
        private NumericUpDown failoverRecoveryBox;
        private NumericUpDown failoverSwitchCooldownBox;
        private NumericUpDown failoverSwitchBackoffBox;
        private NumericUpDown failoverPrimaryMetricBox;
        private NumericUpDown failoverBackupMetricBox;
        private NumericUpDown smartDecisionIntervalBox;
        private NumericUpDown smartMinDwellBox;
        private NumericUpDown smartSwitchHoldBox;
        private NumericUpDown smartMarginBox;
        private CheckBox enableRefreshBox;
        private CheckBox failoverEnabledBox;
        private CheckBox smartSelectionBox;
        private CheckBox suppressRefreshBox;
        private CheckBox dnsBox;
        private CheckBox arpBox;
        private CheckBox mtuBox;
        private Label statusValue;
        private Label lastProbeValue;
        private LinkLabel updateLink;
        private Label permissionValue;
        private Label failoverStatusValue;
        private Label failoverHealthValue;
        private Label beginnerPrimaryValue;
        private Label beginnerPrimaryHealthValue;
        private Label beginnerBackupValue;
        private Label beginnerBackupHealthValue;
        private SignalGlyph beginnerPrimarySignal;
        private SignalGlyph beginnerBackupSignal;
        private ModernCard beginnerPrimaryCard;
        private ModernCard beginnerBackupCard;
        private NetworkGlyph beginnerPrimaryGlyph;
        private NetworkGlyph beginnerBackupGlyph;
        private StatePill beginnerPrimaryStatePill;
        private StatePill beginnerBackupStatePill;
        private RichTextBox logBox;
        private Label logEmptyLabel;
        private PictureBox brandImage;
        private Label titleLabel;
        private Bitmap brandBitmap;
        private Icon appIcon;
        private Icon trayIcon;
        private Button adminButton;
        private Button startButton;
        private Button stopButton;
        private Button refreshButton;
        private Button saveButton;
        private Button exportButton;
        private Button supportButton;
        private Button modeButton;
        private Button beginnerLogButton;
        private Button beginnerStartButton;
        private Button beginnerDetectButton;
        private Button beginnerRestoreButton;
        private CheckBox startWithWindowsBox;
        private Button failoverAdvancedButton;
        private Panel failoverAdvancedPanel;
        private GroupBox beginnerPanel;
        private GroupBox monitorGroup;
        private GroupBox actionsGroup;
        private GroupBox failoverGroup;
        private TableLayoutPanel permissionPanel;
        private GroupBox logGroup;
        private TableLayoutPanel layoutRoot;
        private TableLayoutPanel mainLayout;
        private TableLayoutPanel layoutShell;
        private Panel contentViewport;
        private TableLayoutPanel headerLayout;
        private TableLayoutPanel headerStatusLayout;
        private TableLayoutPanel headerActionsLayout;
        private TableLayoutPanel headerInfoLayout;
        private TableLayoutPanel customTitleBar;
        private Button chromeMinimizeButton;
        private Button chromeMaximizeButton;
        private Button chromeCloseButton;
        private TableLayoutPanel footerLayout;
        private FlowLayoutPanel footerActionFlow;
        private FlowLayoutPanel permissionActions;
        private TableLayoutPanel beginnerDashboard;
        private Panel advancedDrawer;
        private Panel advancedDrawerBody;
        private TableLayoutPanel advancedDrawerLayout;
        private Label advancedDrawerTitle;
        private Button advancedDrawerCloseButton;
        private AdvancedSettingsDialog settingsDialog;
        private ContextMenuStrip logMenu;
        private ToolStripMenuItem followLogItem;
        private ToolTip uiToolTip;
        private NotifyIcon tray;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem trayStart;
        private ToolStripMenuItem trayStop;
        private ToolStripMenuItem trayAdmin;
        private ToolStripMenuItem trayStartup;
        private ToolStripMenuItem trayCheckUpdates;
        private ToolStripMenuItem trayUpdate;
        private ToolStripMenuItem trayIgnoreUpdate;
        private FailoverStatus latestFailoverStatus;
        private bool exportingDiagnostics;
        private bool followLog = true;
        private bool advancedExpanded;
        private bool beginnerMode = true;
        private bool advancedDrawerOpen;
        private bool settingsDialogOpen;
        private bool beginnerLogExpanded;
        private bool restoringNetwork;
        private AppLanguage currentLanguage;
        private bool applyingLanguage;
        private bool applyingStartupPreference;
        private bool startupConfigurationInProgress;
        private int windowHeightBeforeInlineLog;
        private bool windowHeightExpandedForLog;
        private string lastProbeSummary = string.Empty;
        private bool hasProbeResult;
        private string lastProbeTarget = string.Empty;
        private int lastProbePort;
        private int lastProbeLatencyMs;
        private ProbeState lastProbeState;
        private int lastProbeIntervalMs;
        private bool lastProbeHealthy;
        private bool monitoringEverStarted;
        private ComboBox beginnerPrimaryInterfaceBox;
        private ComboBox beginnerBackupInterfaceBox;
        private CheckBox beginnerFailoverBox;
        private CheckBox beginnerAutoRepairBox;
        private ComboBox languageBox;

        private sealed class AutoDetectSelection
        {
            public List<InterfaceSnapshot> Ready;
            public InterfaceSnapshot Primary;
            public InterfaceSnapshot Backup;
        }

        [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDarkTitleBar();
            ApplyWindowShape();
            ApplyResponsiveLayout();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyDarkTitleBar();
        }

        private void ApplyDarkTitleBar()
        {
            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(Handle, 20, ref darkMode, sizeof(int));
                DwmSetWindowAttribute(Handle, 19, ref darkMode, sizeof(int));
                int captionColor = 0x00141F29;
                int captionTextColor = 0x00F1F4F7;
                DwmSetWindowAttribute(Handle, 35, ref captionColor, sizeof(int));
                DwmSetWindowAttribute(Handle, 36, ref captionTextColor, sizeof(int));
            }
            catch
            {
                // Dark title bars are a cosmetic enhancement and are not required
                // for the monitor, tray, or failover functionality.
            }
        }

        private void ApplyWindowShape()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) { return; }
            using (GraphicsPath path = ModernDrawing.RoundedRectangle(
                new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 12))
            {
                Region previous = Region;
                Region next = new Region(path);
                Region = next;
                if (previous != null) { previous.Dispose(); }
            }
        }

        public MainForm() : this(false, false)
        {
        }

        public MainForm(bool startMinimized)
            : this(startMinimized, false)
        {
        }

        public MainForm(bool startMinimized, bool autoStartMonitoring)
            : this(startMinimized, autoStartMonitoring, true)
        {
        }

        private MainForm(bool startMinimized, bool autoStartMonitoring, bool enableUpdateChecks)
        {
            this.startMinimized = startMinimized;
            this.autoStartMonitoring = autoStartMonitoring;
            updateChecksEnabled = enableUpdateChecks;
            string warning;
            settings = SettingsStore.Load(out warning);
            string updateStateWarning;
            updateState = UpdateStateStore.Load(out updateStateWarning);
            currentLanguage = Localization.Normalize(settings.Language);
            settings.Language = currentLanguage;
            RecoveryReport recovery = null;
            try
            {
                recovery = FailoverManager.RecoverPendingAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                recovery = new RecoveryReport();
                recovery.Messages.Add(L("A/B recovery 檢查失敗：") + ex.Message);
            }
            BuildUi();
            ApplyLanguageToUi(false);
            PopulateInterfaces();
            ApplySettingsToUi();
            engine.LogRaised += Engine_LogRaised;
            engine.ProbeCompleted += Engine_ProbeCompleted;
            engine.FailoverStatusChanged += Engine_FailoverStatusChanged;

            if (!string.IsNullOrWhiteSpace(warning))
            {
                AppendLog(warning, true);
            }
            if (!string.IsNullOrWhiteSpace(updateStateWarning))
            {
                AppendLog(updateStateWarning, true);
            }
            if (recovery != null)
            {
                foreach (string message in recovery.Messages)
                {
                    AppendLog(message, !recovery.Restored);
                }
            }
            UpdatePermissionText();
            UpdateButtons();
            if (startMinimized || autoStartMonitoring)
            {
                Shown += MainForm_StartupShown;
            }
            if (updateChecksEnabled)
            {
                Shown += MainForm_CheckUpdatesOnShown;
            }
        }

        private void BuildUi()
        {
            Text = "NetOptimizer";
            BackColor = Background;
            ForeColor = TextColor;
            Font = new Font("Microsoft JhengHei UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            // The collapsed quick-start content needs roughly 460 logical
            // pixels.  Keep a small breathing margin instead of making the
            // shell reserve the former 500-pixel blank lower area, while
            // leaving enough room for the footer without a vertical scrollbar.
            ClientSize = new Size(1024, 480);
            MinimumSize = new Size(760, 400);
            AutoScaleMode = AutoScaleMode.Dpi;

            uiToolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 350,
                ReshowDelay = 100,
                ShowAlways = true
            };

            layoutShell = new TableLayoutPanel
            {
                Dock = DockStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Background,
                ColumnCount = 1,
                RowCount = 2
            };
            layoutShell.Padding = new Padding(0);
            layoutShell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            Controls.Add(layoutShell);

            contentViewport = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Background,
                Padding = new Padding(10, 4, 10, 0),
                Margin = new Padding(0)
            };
            layoutShell.Controls.Add(contentViewport, 0, 0);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Background,
                ColumnCount = 1,
                RowCount = 7,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            this.mainLayout = mainLayout;
            layoutRoot = mainLayout;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderRowHeight));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentViewport.Controls.Add(mainLayout);
            BuildAdvancedDrawer();

            headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, HeaderBottomMargin)
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            // Keep the status, mode switch, update notice, and language picker
            // together as a compact right-hand control cluster. The previous
            // flexible first action column made the mode button grow with the
            // window and left the header visually unbalanced.
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 302F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            brandImage = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Background,
                TabStop = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 8, 6)
            };
            LoadApplicationIcon();
            headerLayout.Controls.Add(brandImage, 0, 0);

            titleLabel = LabelOf("NetOptimizer", 0, 0, 0, 0, 25F, TextColor, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Margin = new Padding(4, 0, 0, 0);
            headerLayout.Controls.Add(titleLabel, 1, 0);

            headerStatusLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            headerStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            headerStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));

            statusValue = new StatusBadge
            {
                Text = "狀態：未啟動",
                ForeColor = MutedText,
                Font = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 4, 0),
                AutoEllipsis = true
            };
            Localization.Mark(statusValue, "狀態：未啟動");
            lastProbeValue = LabelOf("最近探測：尚未測試", 0, 0, 0, 0, 8.5F, MutedText,
                                      FontStyle.Regular, ContentAlignment.MiddleRight);
            lastProbeValue.Dock = DockStyle.Fill;
            lastProbeValue.AutoEllipsis = true;
            TableLayoutPanel headerActions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            headerActionsLayout = headerActions;
            headerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            headerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
            headerActions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
            headerActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerActions.Controls.Add(statusValue, 0, 0);
            modeButton = ButtonOf("顯示進階設定 ▸", 0, 0, 154, 30, false);
            modeButton.Dock = DockStyle.Fill;
            modeButton.Margin = new Padding(0, 2, 4, 2);
            ModernButton modernModeButton = modeButton as ModernButton;
            if (modernModeButton != null)
            {
                modernModeButton.Glyph = "gear";
                modernModeButton.IconOnly = true;
            }
            modeButton.AccessibleRole = AccessibleRole.PushButton;
            modeButton.Click += delegate { OpenAdvancedSettingsDialog(); };
            headerActions.Controls.Add(modeButton, 1, 0);
            languageBox = new ModernComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(17, 18, 20),
                ForeColor = TextColor,
                Font = new Font("Microsoft JhengHei UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                AutoSize = false,
                DropDownWidth = 140,
                Margin = new Padding(0, 2, 4, 2),
                ShowGlobe = true,
                IconOnly = true,
                AccessibleRole = AccessibleRole.ComboBox,
                AccessibleName = "選擇介面語言。"
            };
            languageBox.Items.Add(Localization.LanguageName(AppLanguage.TraditionalChinese));
            languageBox.Items.Add(Localization.LanguageName(AppLanguage.English));
            languageBox.SelectedIndex = 0;
            languageBox.SelectedIndexChanged += LanguageBox_SelectedIndexChanged;
            headerActions.Controls.Add(languageBox, 2, 0);

            headerInfoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            headerInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerInfoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            headerInfoLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerInfoLayout.Controls.Add(lastProbeValue, 0, 0);
            updateLink = new LinkLabel
            {
                Text = "檢查更新",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                LinkColor = Accent,
                ActiveLinkColor = TextColor,
                VisitedLinkColor = Accent,
                BackColor = Background,
                Margin = new Padding(0, 0, 0, 0),
                Visible = false
            };
            Localization.Mark(updateLink, "檢查更新");
            updateLink.LinkClicked += delegate { OpenAvailableUpdate(); };
            headerInfoLayout.Controls.Add(updateLink, 1, 0);
            headerStatusLayout.Controls.Add(headerActions, 0, 0);
            headerStatusLayout.Controls.Add(headerInfoLayout, 0, 1);
            headerLayout.Controls.Add(headerStatusLayout, 2, 0);
            mainLayout.Controls.Add(headerLayout, 0, 0);
            uiToolTip.SetToolTip(brandImage, "NetOptimizer 網路監測與備援工具");
            uiToolTip.SetToolTip(statusValue, "監測器目前狀態與下一次背景探測時間。");
            uiToolTip.SetToolTip(lastProbeValue, "最近一次 TCP 探測結果。");

            beginnerPanel = BuildBeginnerPanel();
            mainLayout.Controls.Add(beginnerPanel, 0, 1);

            monitorGroup = AutoGroupOf("監測設定");
            TableLayoutPanel monitorGrid = GridOf(3);
            int row = AddGridRow(monitorGrid);

            interfaceBox = InterfaceBox();
            targetsBox = TextBoxEditor();
            monitorGrid.Controls.Add(FieldOf("網卡", interfaceBox, null), 0, row);
            TableLayoutPanel targetField = FieldOf("測試目標", targetsBox, null);
            monitorGrid.Controls.Add(targetField, 1, row);
            monitorGrid.SetColumnSpan(targetField, 2);

            row = AddGridRow(monitorGrid);
            portBox = NumberBox(1, 65535, 443);
            thresholdBox = NumberBox(1, 60000, 90);
            timeoutBox = NumberBox(100, 60000, 1500);
            monitorGrid.Controls.Add(FieldOf("連接埠", portBox, null), 0, row);
            monitorGrid.Controls.Add(FieldOf("高延遲", thresholdBox, "ms"), 1, row);
            monitorGrid.Controls.Add(FieldOf("逾時", timeoutBox, "ms"), 2, row);

            row = AddGridRow(monitorGrid);
            failuresBox = NumberBox(1, 20, 3);
            cooldownBox = NumberBox(0, 86400, 60);
            monitorGrid.Controls.Add(FieldOf("連續異常", failuresBox, "次"), 0, row);
            monitorGrid.Controls.Add(FieldOf("刷新冷卻", cooldownBox, "秒"), 1, row);
            monitorGroup.Controls.Add(monitorGrid);
            mainLayout.Controls.Add(monitorGroup, 0, 2);
            uiToolTip.SetToolTip(targetsBox, "可輸入 IP 或網域，使用逗號分隔。");

            actionsGroup = AutoGroupOf("異常時動作");
            FlowLayoutPanel actionsFlow = FlowOf();
            enableRefreshBox = CheckBoxText("自動刷新", true);
            dnsBox = CheckBoxText("清除 DNS 快取", true);
            arpBox = CheckBoxText("清除 ARP 快取", true);
            mtuBox = CheckBoxText("MTU 測試", true);
            actionsFlow.Controls.Add(enableRefreshBox);
            actionsFlow.Controls.Add(dnsBox);
            actionsFlow.Controls.Add(arpBox);
            actionsFlow.Controls.Add(mtuBox);
            actionsGroup.Controls.Add(actionsFlow);
            mainLayout.Controls.Add(actionsGroup, 0, 3);
            enableRefreshBox.CheckedChanged += delegate { UpdateActionEnabled(); };
            uiToolTip.SetToolTip(enableRefreshBox, "連續異常時執行已勾選的刷新動作。");
            uiToolTip.SetToolTip(dnsBox, "清除 DNS 快取。");
            uiToolTip.SetToolTip(arpBox, "清除 ARP 快取。");
            uiToolTip.SetToolTip(mtuBox, "暫時套用 1471 MTU，再復原原值。");

            failoverGroup = AutoGroupOf("A/B 自動切換與智慧選路");
            TableLayoutPanel failoverContent = GridOf(1);
            TableLayoutPanel failoverGrid = GridOf(2);

            row = AddGridRow(failoverGrid);
            failoverEnabledBox = CheckBoxText("啟用 A/B 自動切換", false);
            failoverGrid.Controls.Add(failoverEnabledBox, 0, row);
            failoverTargetBox = TextBoxEditor();
            failoverGrid.Controls.Add(FieldOf("故障目標", failoverTargetBox, null), 1, row);

            row = AddGridRow(failoverGrid);
            primaryInterfaceBox = InterfaceBox();
            backupInterfaceBox = InterfaceBox();
            failoverGrid.Controls.Add(FieldOf("主線 A", primaryInterfaceBox, null), 0, row);
            failoverGrid.Controls.Add(FieldOf("備援 B", backupInterfaceBox, null), 1, row);

            row = AddGridRow(failoverGrid);
            failoverThresholdBox = NumberBox(1, 60000, 200);
            failoverTimeoutBox = NumberBox(100, 60000, 700);
            failoverGrid.Controls.Add(FieldOf("故障門檻", failoverThresholdBox, "ms"), 0, row);
            failoverGrid.Controls.Add(FieldOf("故障逾時", failoverTimeoutBox, "ms"), 1, row);

            row = AddGridRow(failoverGrid);
            smartSelectionBox = CheckBoxText("EWMA 智慧選路", false);
            failoverGrid.Controls.Add(smartSelectionBox, 0, row);
            failoverStatusValue = LabelOf("A/B：未啟用", 0, 0, 0, 27, 8.5F, MutedText,
                                          FontStyle.Bold, ContentAlignment.MiddleLeft);
            failoverStatusValue.Dock = DockStyle.Fill;
            failoverStatusValue.AutoEllipsis = true;
            failoverGrid.Controls.Add(failoverStatusValue, 1, row);

            row = AddGridRow(failoverGrid);
            failoverHealthValue = LabelOf("健康度：尚未測試", 0, 0, 0, 27, 8.5F, MutedText,
                                          FontStyle.Regular, ContentAlignment.MiddleLeft);
            failoverHealthValue.Dock = DockStyle.Fill;
            failoverHealthValue.AutoEllipsis = true;
            failoverGrid.Controls.Add(failoverHealthValue, 0, row);
            failoverGrid.SetColumnSpan(failoverHealthValue, 2);
            failoverContent.Controls.Add(failoverGrid, 0, AddGridRow(failoverContent));

            failoverAdvancedButton = new ModernButton
            {
                Text = "進階設定 ▸",
                BackColor = PanelBackground,
                ForeColor = MutedText,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 0)
            };
            ModernButton modernFailoverAdvanced = failoverAdvancedButton as ModernButton;
            if (modernFailoverAdvanced != null)
            {
                modernFailoverAdvanced.AccentBorder = false;
                modernFailoverAdvanced.Glyph = "gear";
            }
            Localization.Mark(failoverAdvancedButton, "進階設定 ▸");
            failoverAdvancedButton.Click += delegate
            {
                advancedExpanded = !advancedExpanded;
                UpdateFailoverAdvanced();
            };
            failoverContent.Controls.Add(failoverAdvancedButton, 0, AddGridRow(failoverContent));
            uiToolTip.SetToolTip(failoverAdvancedButton, "顯示或隱藏 A/B 與 EWMA 調校參數。");

            failoverAdvancedPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = PanelBackground,
                Padding = new Padding(0, 4, 0, 0),
                Visible = false,
                Margin = new Padding(0)
            };
            TableLayoutPanel advancedGrid = GridOf(3);

            row = AddGridRow(advancedGrid);
            failoverBadSamplesBox = NumberBox(1, 20, 2);
            failoverRecoveryBox = NumberBox(1, 3600, 5);
            failoverSwitchCooldownBox = NumberBox(0, 3600, 5);
            advancedGrid.Controls.Add(FieldOf("連續失敗", failoverBadSamplesBox, "次"), 0, row);
            advancedGrid.Controls.Add(FieldOf("恢復穩定", failoverRecoveryBox, "秒"), 1, row);
            advancedGrid.Controls.Add(FieldOf("切換冷卻", failoverSwitchCooldownBox, "秒"), 2, row);

            row = AddGridRow(advancedGrid);
            failoverSwitchBackoffBox = NumberBox(1, 3600, 10);
            failoverPrimaryMetricBox = NumberBox(1, 9999, 5);
            failoverBackupMetricBox = NumberBox(1, 9999, 50);
            advancedGrid.Controls.Add(FieldOf("失敗退避", failoverSwitchBackoffBox, "秒"), 0, row);
            advancedGrid.Controls.Add(FieldOf("A 優先值", failoverPrimaryMetricBox, null), 1, row);
            advancedGrid.Controls.Add(FieldOf("B 優先值", failoverBackupMetricBox, null), 2, row);

            row = AddGridRow(advancedGrid);
            smartDecisionIntervalBox = NumberBox(5, 3600, 30);
            smartMinDwellBox = NumberBox(5, 86400, 30);
            smartMarginBox = NumberBox(1, 60000, 8);
            advancedGrid.Controls.Add(FieldOf("決策間隔", smartDecisionIntervalBox, "秒"), 0, row);
            advancedGrid.Controls.Add(FieldOf("最小停留", smartMinDwellBox, "秒"), 1, row);
            advancedGrid.Controls.Add(FieldOf("差距", smartMarginBox, "ms"), 2, row);

            row = AddGridRow(advancedGrid);
            smartSwitchHoldBox = NumberBox(1, 3600, 10);
            advancedGrid.Controls.Add(FieldOf("選路停留", smartSwitchHoldBox, "秒"), 0, row);
            suppressRefreshBox = CheckBoxText("故障時停用一般刷新", true);
            advancedGrid.Controls.Add(suppressRefreshBox, 1, row);
            advancedGrid.SetColumnSpan(suppressRefreshBox, 2);
            failoverAdvancedPanel.Controls.Add(advancedGrid);
            failoverContent.Controls.Add(failoverAdvancedPanel, 0, AddGridRow(failoverContent));
            failoverGroup.Controls.Add(failoverContent);
            mainLayout.Controls.Add(failoverGroup, 0, 4);
            failoverEnabledBox.CheckedChanged += delegate { UpdateFailoverEnabled(); };
            uiToolTip.SetToolTip(failoverStatusValue, "目前使用中的介面、備援介面與切換模式。");
            uiToolTip.SetToolTip(failoverHealthValue, "完整健康度會在滑鼠停留時顯示。");

            permissionPanel = GridOf(2);
            permissionPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 100F);
            permissionPanel.ColumnStyles[1] = new ColumnStyle(SizeType.AutoSize);
            row = AddGridRow(permissionPanel);
            permissionValue = LabelOf(string.Empty, 0, 0, 0, 28, 8.5F, Warning);
            permissionValue.Dock = DockStyle.Fill;
            permissionValue.AutoEllipsis = true;
            permissionPanel.Controls.Add(permissionValue, 0, row);
            permissionActions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Background
            };
            startWithWindowsBox = CheckBoxText("開機自動啟動", false);
            startWithWindowsBox.Margin = new Padding(0, 0, 10, 4);
            startWithWindowsBox.CheckedChanged += StartWithWindowsBox_CheckedChanged;
            permissionActions.Controls.Add(startWithWindowsBox);
            supportButton = ButtonOf("☕ 支持開發", 0, 0, 128, 28, true);
            supportButton.Dock = DockStyle.None;
            supportButton.Margin = new Padding(8, 0, 0, 0);
            ModernButton modernSupportButton = supportButton as ModernButton;
            if (modernSupportButton != null)
            {
                modernSupportButton.Accent = false;
                modernSupportButton.AccentBorder = true;
                modernSupportButton.Glyph = "heart";
            }
            supportButton.Click += delegate { OpenSupportDialog(); };
            permissionActions.Controls.Add(supportButton);
            adminButton = ButtonOf("重新以管理員啟動", 0, 0, 180, 28, false);
            adminButton.Dock = DockStyle.None;
            adminButton.Margin = new Padding(8, 0, 0, 0);
            ModernButton modernAdminButton = adminButton as ModernButton;
            if (modernAdminButton != null) { modernAdminButton.Glyph = "launch"; }
            adminButton.Click += delegate { RestartAsAdministrator(); };
            permissionActions.Controls.Add(adminButton);
            permissionPanel.Controls.Add(permissionActions, 1, row);
            permissionPanel.BackColor = Background;
            permissionPanel.AutoSize = false;
            permissionPanel.Dock = DockStyle.Fill;
            permissionPanel.MinimumSize = new Size(0, 34);
            permissionPanel.Margin = new Padding(0);
            permissionPanel.Padding = new Padding(0, 2, 0, 0);
            uiToolTip.SetToolTip(adminButton, "重新啟動並要求系統管理員權限；不會自動提權。");
            uiToolTip.SetToolTip(supportButton, "開啟支持開發選項：Ko-fi 與加密貨幣地址。");
            uiToolTip.SetToolTip(startWithWindowsBox, "登入 Windows 後啟動並縮到系統匣。");

            logGroup = AutoGroupOf("執行紀錄");
            logGroup.AutoSize = false;
            logGroup.Dock = DockStyle.Fill;
            logGroup.MinimumSize = new Size(0, 180);
            logGroup.Height = 190;
            Panel logSurface = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 15, 17),
                Padding = new Padding(0)
            };
            logBox = new RichTextBox
            {
                ReadOnly = true,
                DetectUrls = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(14, 15, 17),
                ForeColor = Color.FromArgb(205, 209, 216),
                Font = new Font("Consolas", 9F),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            logEmptyLabel = new Label
            {
                Text = "尚未開始監測",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = MutedText,
                BackColor = Color.FromArgb(14, 15, 17),
                Font = new Font("Microsoft JhengHei UI", 9F),
                Visible = true
            };
            Localization.Mark(logEmptyLabel, "尚未開始監測");
            logSurface.Controls.Add(logBox);
            logSurface.Controls.Add(logEmptyLabel);
            logGroup.Controls.Add(logSurface);
            mainLayout.Controls.Add(logGroup, 0, 6);
            BuildLogMenu();
            AttachLogToBeginnerDashboard();

            TableLayoutPanel footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(10, 4, 10, 8),
                BackColor = Background
            };
            footerLayout = footer;
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            FlowLayoutPanel buttonFlow = FlowOf();
            footerActionFlow = buttonFlow;
            buttonFlow.BackColor = Background;
            buttonFlow.WrapContents = false;
            buttonFlow.Margin = new Padding(0);
            startButton = ButtonOf("開始監測", 0, 0, 112, 30, true);
            stopButton = ButtonOf("停止", 0, 0, 88, 30, false);
            refreshButton = ButtonOf("立即刷新", 0, 0, 104, 30, false);
            saveButton = ButtonOf("儲存設定", 0, 0, 104, 30, false);
            exportButton = ButtonOf("匯出診斷", 0, 0, 104, 30, false);
            foreach (Button button in new[] { startButton, stopButton, refreshButton, saveButton, exportButton })
            {
                button.Margin = new Padding(0, 0, 8, 0);
                buttonFlow.Controls.Add(button);
            }
            startButton.Click += StartButton_Click;
            stopButton.Click += delegate { StopMonitoring(); };
            refreshButton.Click += RefreshButton_Click;
            saveButton.Click += SaveButton_Click;
            exportButton.Click += ExportButton_Click;
            ModernButton modernStartButton = startButton as ModernButton;
            if (modernStartButton != null) { modernStartButton.Glyph = "shield"; }
            ModernButton modernStopButton = stopButton as ModernButton;
            if (modernStopButton != null) { modernStopButton.Glyph = "shield-stop"; }
            ModernButton modernRefreshButton = refreshButton as ModernButton;
            if (modernRefreshButton != null) { modernRefreshButton.Glyph = "refresh"; }
            ModernButton modernExportButton = exportButton as ModernButton;
            if (modernExportButton != null) { modernExportButton.Glyph = "log"; }
            footer.Controls.Add(buttonFlow, 0, 0);
            footer.Controls.Add(permissionPanel, 0, 1);
            layoutShell.Controls.Add(footer, 0, 1);

            BuildCustomTitleBar();
            LayoutCustomChrome();

            trayIcon = (Icon)appIcon.Clone();
            tray = new NotifyIcon
            {
                Icon = trayIcon,
                Text = "NetOptimizer",
                Visible = true
            };
            trayMenu = new ContextMenuStrip();
            trayStart = new ToolStripMenuItem("開始監測");
            trayStop = new ToolStripMenuItem("停止監測");
            Localization.Mark(trayStart, "開始監測");
            Localization.Mark(trayStop, "停止監測");
            trayStart.Click += StartButton_Click;
            trayStop.Click += delegate { StopMonitoring(); };
            ToolStripMenuItem trayShowItem = new ToolStripMenuItem("顯示主視窗", null, delegate { ShowFromTray(); });
            Localization.Mark(trayShowItem, "顯示主視窗");
            trayMenu.Items.Add(trayShowItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayStart);
            trayMenu.Items.Add(trayStop);
            ToolStripMenuItem trayRefreshItem = new ToolStripMenuItem("立即刷新", null, delegate { RefreshButton_Click(this, EventArgs.Empty); });
            Localization.Mark(trayRefreshItem, "立即刷新");
            trayMenu.Items.Add(trayRefreshItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayCheckUpdates = new ToolStripMenuItem("檢查更新", null, delegate { CheckForUpdates(true); });
            Localization.Mark(trayCheckUpdates, "檢查更新");
            trayMenu.Items.Add(trayCheckUpdates);
            trayUpdate = new ToolStripMenuItem("查看更新", null, delegate { OpenAvailableUpdate(); })
            {
                Visible = false,
                Enabled = false
            };
            Localization.Mark(trayUpdate, "查看更新");
            trayMenu.Items.Add(trayUpdate);
            trayIgnoreUpdate = new ToolStripMenuItem("忽略此版本", null, delegate { IgnoreAvailableUpdate(); })
            {
                Visible = false,
                Enabled = false
            };
            Localization.Mark(trayIgnoreUpdate, "忽略此版本");
            trayMenu.Items.Add(trayIgnoreUpdate);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayStartup = new ToolStripMenuItem("開機自動啟動")
            {
                CheckOnClick = true
            };
            Localization.Mark(trayStartup, "開機自動啟動");
            trayStartup.Click += delegate { ApplyStartupPreference(trayStartup.Checked); };
            trayMenu.Items.Add(trayStartup);
            trayAdmin = new ToolStripMenuItem("以系統管理員重新啟動", null, delegate { RestartAsAdministrator(); });
            Localization.Mark(trayAdmin, "以系統管理員重新啟動");
            trayMenu.Items.Add(trayAdmin);
            ToolStripMenuItem traySupportItem = new ToolStripMenuItem("支持開發", null, delegate { OpenSupportDialog(); });
            Localization.Mark(traySupportItem, "支持開發");
            trayMenu.Items.Add(traySupportItem);
            ToolStripMenuItem trayExitItem = new ToolStripMenuItem("結束", null, delegate { Close(); });
            Localization.Mark(trayExitItem, "結束");
            trayMenu.Items.Add(trayExitItem);
            tray.ContextMenuStrip = trayMenu;
            tray.BalloonTipClicked += delegate { OpenAvailableUpdate(); };
            tray.MouseDoubleClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) { ShowFromTray(); }
            };

            Resize += MainForm_Resize;
            FormClosing += MainForm_FormClosing;
            FormClosed += MainForm_FormClosed;

            interfaceBox.TextChanged += delegate { UpdateInterfaceTooltip(interfaceBox); };
            primaryInterfaceBox.TextChanged += delegate { UpdateInterfaceTooltip(primaryInterfaceBox); };
            backupInterfaceBox.TextChanged += delegate { UpdateInterfaceTooltip(backupInterfaceBox); };
        }

        private void BuildAdvancedDrawer()
        {
            advancedDrawer = new Panel
            {
                Dock = DockStyle.Right,
                Width = 520,
                BackColor = PanelBackground,
                Padding = new Padding(14, 10, 10, 10),
                Visible = false,
                TabStop = true
            };
            advancedDrawer.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Color.FromArgb(63, 91, 108), 1F))
                {
                    e.Graphics.DrawLine(pen, 0, 0, 0, Math.Max(0, advancedDrawer.Height - 1));
                }
            };

            TableLayoutPanel drawerHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(0),
                BackColor = PanelBackground
            };
            drawerHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            drawerHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36F));
            drawerHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            advancedDrawerTitle = LabelOf("進階設定", 0, 0, 0, 32, 11F, TextColor,
                                         FontStyle.Bold, ContentAlignment.MiddleLeft);
            advancedDrawerTitle.Dock = DockStyle.Fill;
            advancedDrawerTitle.Margin = new Padding(0, 0, 4, 0);
            drawerHeader.Controls.Add(advancedDrawerTitle, 0, 0);
            advancedDrawerCloseButton = ButtonOf("×", 0, 0, 32, 30, false);
            advancedDrawerCloseButton.Dock = DockStyle.Fill;
            advancedDrawerCloseButton.Margin = new Padding(0, 2, 0, 2);
            advancedDrawerCloseButton.AccessibleName = "Close advanced settings";
            advancedDrawerCloseButton.Click += delegate
            {
                if (settingsDialog != null && !settingsDialog.IsDisposed)
                {
                    settingsDialog.Close();
                }
            };
            drawerHeader.Controls.Add(advancedDrawerCloseButton, 1, 0);

            advancedDrawerBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PanelBackground,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            advancedDrawerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 0,
                BackColor = PanelBackground,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            advancedDrawerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            advancedDrawerBody.Controls.Add(advancedDrawerLayout);
            advancedDrawer.Controls.Add(advancedDrawerBody);
            advancedDrawer.Controls.Add(drawerHeader);
            contentViewport.Controls.Add(advancedDrawer);
            advancedDrawer.BringToFront();
        }

        private void BuildCustomTitleBar()
        {
            customTitleBar = new TableLayoutPanel
            {
                Dock = DockStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height = 38,
                BackColor = Background,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 2, 0)
            };
            customTitleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            customTitleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
            customTitleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
            customTitleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
            customTitleBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Panel dragSurface = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                Margin = new Padding(0)
            };
            dragSurface.MouseDown += CustomTitleBar_MouseDown;
            dragSurface.DoubleClick += CustomTitleBar_DoubleClick;
            customTitleBar.Controls.Add(dragSurface, 0, 0);

            chromeMinimizeButton = ChromeButtonOf("—", false);
            chromeMinimizeButton.AccessibleName = "Minimize";
            chromeMinimizeButton.Click += delegate { WindowState = FormWindowState.Minimized; };
            customTitleBar.Controls.Add(chromeMinimizeButton, 1, 0);

            chromeMaximizeButton = ChromeButtonOf("□", false);
            chromeMaximizeButton.AccessibleName = "Maximize";
            chromeMaximizeButton.Click += delegate { ToggleMaximizedWindow(); };
            customTitleBar.Controls.Add(chromeMaximizeButton, 2, 0);

            chromeCloseButton = ChromeButtonOf("×", true);
            chromeCloseButton.AccessibleName = "Close";
            chromeCloseButton.Click += delegate { Close(); };
            customTitleBar.Controls.Add(chromeCloseButton, 3, 0);

            Controls.Add(customTitleBar);
            customTitleBar.BringToFront();
            UpdateChromeButtons();
        }

        private void CustomTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && WindowState != FormWindowState.Maximized)
            {
                ReleaseCapture();
                SendMessage(Handle, 0x00A1, new IntPtr(2), IntPtr.Zero);
            }
        }

        private void CustomTitleBar_DoubleClick(object sender, EventArgs e)
        {
            ToggleMaximizedWindow();
        }

        private void ToggleMaximizedWindow()
        {
            WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
            UpdateChromeButtons();
        }

        private void UpdateChromeButtons()
        {
            if (chromeMaximizeButton != null)
            {
                chromeMaximizeButton.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";
            }
        }

        private void LayoutCustomChrome()
        {
            if (customTitleBar == null || layoutShell == null) { return; }
            int titleBarHeight = customTitleBar.Height;
            customTitleBar.SetBounds(0, 0, Math.Max(0, ClientSize.Width), titleBarHeight);
            layoutShell.SetBounds(0, titleBarHeight, Math.Max(0, ClientSize.Width),
                                  Math.Max(0, ClientSize.Height - titleBarHeight));
        }

        private float GetDpiScale()
        {
            if (!IsHandleCreated) { return 1F; }
            try
            {
                using (Graphics graphics = CreateGraphics())
                {
                    return Math.Max(1F, graphics.DpiX / 96F);
                }
            }
            catch
            {
                return 1F;
            }
        }

        private static int ScaleLayoutMetric(int value, float scale)
        {
            return Math.Max(1, (int)Math.Round(value * scale));
        }

        private void ApplyResponsiveLayout()
        {
            float scale = GetDpiScale();
            // AutoScaleMode.Dpi already scales the child controls and fonts.  The
            // compact shell metrics below are logical layout values; multiplying
            // them a second time at 125/150% DPI makes the quick-start action row
            // overflow behind the fixed footer.  Keep the shell compact and let
            // the normal WinForms DPI pass scale the controls themselves.
            Func<int, int> metric = delegate(int value) { return ScaleLayoutMetric(value, 1F); };

            if (customTitleBar != null)
            {
                customTitleBar.Height = metric(38);
                if (customTitleBar.ColumnStyles.Count >= 4)
                {
                    customTitleBar.ColumnStyles[1].Width = metric(46);
                    customTitleBar.ColumnStyles[2].Width = metric(46);
                    customTitleBar.ColumnStyles[3].Width = metric(46);
                }
            }
            if (layoutShell != null && layoutShell.RowStyles.Count > 1)
            {
                layoutShell.RowStyles[1].Height = metric(advancedDrawerOpen ? 90 : 60);
            }
            if (contentViewport != null)
            {
                contentViewport.Padding = new Padding(metric(10), metric(4), metric(10), 0);
            }
            if (mainLayout != null && mainLayout.RowStyles.Count > 0)
            {
                // The status cluster is 34 + 20 logical pixels.  Keep only a
                // two-pixel safety margin so the next group starts close to
                // the header instead of inheriting the old 64 + 7 gap.
                mainLayout.RowStyles[0].Height = metric(HeaderRowHeight);
            }
            if (headerLayout != null && headerLayout.ColumnStyles.Count >= 3)
            {
                headerLayout.ColumnStyles[0].Width = metric(64);
                int available = Math.Max(metric(270), ClientSize.Width - metric(64) - metric(302));
                headerLayout.ColumnStyles[2].Width = Math.Min(metric(302), available);
            }
            if (headerStatusLayout != null && headerStatusLayout.RowStyles.Count >= 2)
            {
                headerStatusLayout.RowStyles[0].Height = metric(34);
                headerStatusLayout.RowStyles[1].Height = metric(20);
            }
            if (headerActionsLayout != null && headerActionsLayout.ColumnStyles.Count >= 3)
            {
                headerActionsLayout.ColumnStyles[0].Width = metric(210);
                headerActionsLayout.ColumnStyles[1].Width = metric(46);
                headerActionsLayout.ColumnStyles[2].Width = metric(46);
            }
            if (beginnerDashboard != null && beginnerDashboard.RowStyles.Count >= 3)
            {
                int inlineLogRowHeight = beginnerLogExpanded ? BeginnerInlineLogRowHeight : 0;
                if (beginnerPanel != null)
                {
                    beginnerPanel.Height = metric(BeginnerPanelBaseHeight + inlineLogRowHeight);
                }
                beginnerDashboard.Height = metric(BeginnerDashboardBaseHeight + inlineLogRowHeight);
                beginnerDashboard.RowStyles[0].Height = metric(159);
                beginnerDashboard.RowStyles[1].Height = metric(42);
                beginnerDashboard.RowStyles[2].Height = metric(52);
                if (beginnerDashboard.RowStyles.Count >= 4)
                {
                    beginnerDashboard.RowStyles[3].SizeType = SizeType.Absolute;
                    beginnerDashboard.RowStyles[3].Height = metric(inlineLogRowHeight);
                }
            }
            if (beginnerPrimaryCard != null) { beginnerPrimaryCard.MaximumSize = new Size(0, metric(151)); }
            if (beginnerBackupCard != null) { beginnerBackupCard.MaximumSize = new Size(0, metric(151)); }
            if (footerLayout != null && footerLayout.RowStyles.Count >= 2)
            {
                footerLayout.RowStyles[0].Height = metric(advancedDrawerOpen ? 38 : 0);
                footerLayout.RowStyles[1].Height = metric(advancedDrawerOpen ? 42 : 48);
                footerLayout.Padding = new Padding(metric(10), metric(4), metric(10), metric(8));
            }
            if (advancedDrawer != null && contentViewport != null)
            {
                int availableWidth = Math.Max(metric(360), contentViewport.ClientSize.Width);
                int minimumWidth = metric(360);
                int preferredWidth = (int)Math.Round(availableWidth * 0.58F);
                int drawerLimit = scale >= 1.5F ? 480 : 520;
                advancedDrawer.Width = Math.Min(metric(drawerLimit), Math.Max(minimumWidth, preferredWidth));
                advancedDrawer.Padding = new Padding(metric(14), metric(10), metric(10), metric(10));
            }
            LayoutCustomChrome();
        }

        protected override void WndProc(ref Message m)
        {
            const int wmNcHitTest = 0x0084;
            const int htClient = 1;
            const int htLeft = 10;
            const int htRight = 11;
            const int htTop = 12;
            const int htTopLeft = 13;
            const int htTopRight = 14;
            const int htBottom = 15;
            const int htBottomLeft = 16;
            const int htBottomRight = 17;
            if (m.Msg == wmNcHitTest && WindowState != FormWindowState.Maximized)
            {
                base.WndProc(ref m);
                if (m.Result.ToInt32() == htClient)
                {
                    int screenX = (short)(m.LParam.ToInt64() & 0xFFFF);
                    int screenY = (short)((m.LParam.ToInt64() >> 16) & 0xFFFF);
                    Point point = PointToClient(new Point(screenX, screenY));
                    const int grip = 6;
                    bool left = point.X <= grip;
                    bool right = point.X >= ClientSize.Width - grip;
                    bool top = point.Y <= grip;
                    bool bottom = point.Y >= ClientSize.Height - grip;
                    if (top && left) { m.Result = new IntPtr(htTopLeft); }
                    else if (top && right) { m.Result = new IntPtr(htTopRight); }
                    else if (bottom && left) { m.Result = new IntPtr(htBottomLeft); }
                    else if (bottom && right) { m.Result = new IntPtr(htBottomRight); }
                    else if (left) { m.Result = new IntPtr(htLeft); }
                    else if (right) { m.Result = new IntPtr(htRight); }
                    else if (top) { m.Result = new IntPtr(htTop); }
                    else if (bottom) { m.Result = new IntPtr(htBottom); }
                }
                return;
            }
            base.WndProc(ref m);
        }

        private string L(string source)
        {
            return Localization.Get(currentLanguage, source);
        }

        private void LanguageBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (applyingLanguage || languageBox == null || languageBox.SelectedIndex < 0)
            {
                return;
            }

            currentLanguage = languageBox.SelectedIndex == 1
                ? AppLanguage.English
                : AppLanguage.TraditionalChinese;
            ApplyLanguageToUi(true);
        }

        private void ApplyLanguageToUi(bool persist)
        {
            currentLanguage = Localization.Normalize(currentLanguage);
            applyingLanguage = true;
            try
            {
                if (languageBox != null)
                {
                    languageBox.SelectedIndex = currentLanguage == AppLanguage.English ? 1 : 0;
                }
            }
            finally
            {
                applyingLanguage = false;
            }

            Localization.Apply(this, currentLanguage);
            Localization.ApplyItems(trayMenu, currentLanguage);
            Localization.ApplyItems(logMenu, currentLanguage);
            Text = L("NetOptimizer");
            if (tray != null) { tray.Text = L("NetOptimizer"); }
            lastProbeSummary = FormatLastProbeSummary();
            if (lastProbeValue != null)
            {
                lastProbeValue.Text = hasProbeResult
                    ? L("最近探測：") + lastProbeSummary
                    : L("最近探測：尚未測試");
            }
            if (statusValue != null)
            {
                if (engine.IsRunning)
                {
                    statusValue.Text = L("狀態：") + L("監測中");
                    statusValue.ForeColor = GetMonitorStatusColor();
                }
                else
                {
                    statusValue.Text = L("狀態：") + L(monitoringEverStarted ? "已停止" : "未啟動");
                    statusValue.ForeColor = MutedText;
                }
            }
            UpdateLocalizedTooltips();
            UpdatePermissionText();
            UpdateFailoverStatusDisplay();
            UpdateFailoverAdvanced();
            UpdateBeginnerStatus();
            UpdateUiMode();
            UpdateUpdateUi();

            if (settings != null)
            {
                settings.Language = currentLanguage;
            }
            if (persist && !closing)
            {
                SaveLanguagePreference();
            }
        }

        private void SaveLanguagePreference()
        {
            try
            {
                MonitorSettings next;
                try
                {
                    next = ReadSettingsFromUi();
                }
                catch
                {
                    next = settings == null
                        ? MonitorSettings.CreateDefault(Localization.DetectWindowsDefault())
                        : settings.Clone();
                }
                next.Language = currentLanguage;
                next.Normalize();
                SettingsStore.Save(next);
                settings = next;
            }
            catch (Exception ex)
            {
                AppendLog(L("語言設定儲存失敗：") + ex.Message, true);
            }
        }

        private void UpdateLocalizedTooltips()
        {
            if (uiToolTip == null) { return; }
            uiToolTip.SetToolTip(brandImage, L("NetOptimizer 網路監測與備援工具"));
            uiToolTip.SetToolTip(statusValue, L("監測器目前狀態與下一次背景探測時間。"));
            uiToolTip.SetToolTip(lastProbeValue, lastProbeSummary.Length == 0
                ? L("最近一次 TCP 探測結果。")
                : L("完整結果：") + lastProbeSummary);
            uiToolTip.SetToolTip(languageBox, L("選擇介面語言。"));
            if (languageBox != null)
            {
                languageBox.AccessibleName = L("選擇介面語言。");
                languageBox.AccessibleDescription = L("選擇介面語言。");
            }
            string startupTooltip = StartupManager.IsProtectedInstallPath(Application.ExecutablePath)
                ? L("安裝版登入後會以系統管理員啟動並自動開始監測。")
                : L("可攜版登入後啟動程式並縮到系統匣。");
            uiToolTip.SetToolTip(startWithWindowsBox, startupTooltip);
            uiToolTip.SetToolTip(targetsBox, L("可輸入 IP 或網域，使用逗號分隔。"));
            uiToolTip.SetToolTip(enableRefreshBox, L("連續異常時執行已勾選的刷新動作。"));
            uiToolTip.SetToolTip(dnsBox, L("清除 DNS 快取。"));
            uiToolTip.SetToolTip(arpBox, L("清除 ARP 快取。"));
            uiToolTip.SetToolTip(mtuBox, L("暫時套用 1471 MTU，再復原原值。"));
            uiToolTip.SetToolTip(failoverAdvancedButton, L("顯示或隱藏 A/B 與 EWMA 調校參數。"));
            uiToolTip.SetToolTip(failoverStatusValue, failoverStatusValue == null
                ? string.Empty : failoverStatusValue.Text);
            uiToolTip.SetToolTip(failoverHealthValue, failoverHealthValue == null
                ? string.Empty : failoverHealthValue.Text);
            uiToolTip.SetToolTip(adminButton, L("重新啟動並要求系統管理員權限；不會自動提權。"));
            uiToolTip.SetToolTip(supportButton, L("開啟支持開發選項：Ko-fi 與加密貨幣地址。"));
            uiToolTip.SetToolTip(updateLink, L("檢查 GitHub 是否有較新的穩定版本。"));
            uiToolTip.SetToolTip(logBox, L("右鍵可複製或清除紀錄，也可暫停自動捲動。"));
            UpdateInterfaceTooltip(interfaceBox);
            UpdateInterfaceTooltip(primaryInterfaceBox);
            UpdateInterfaceTooltip(backupInterfaceBox);
            UpdateInterfaceTooltip(beginnerPrimaryInterfaceBox);
            UpdateInterfaceTooltip(beginnerBackupInterfaceBox);
        }

        private string FormatLastProbeSummary()
        {
            if (!hasProbeResult) { return string.Empty; }
            string resultText = lastProbeState == ProbeState.Success
                ? lastProbeLatencyMs + " ms"
                : (lastProbeState == ProbeState.Timeout ? "timeout" : L("失敗"));
            return lastProbeTarget + ":" + lastProbePort + " → " + resultText;
        }

        private GroupBox BuildBeginnerPanel()
        {
            GroupBox group = AutoGroupOf("快速開始");
            group.AutoSize = false;
            group.Dock = DockStyle.Top;
            group.Height = 305;
            group.Padding = new Padding(14, 18, 14, 10);
            group.MinimumSize = new Size(0, 0);

            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 255,
                BackColor = PanelBackground,
                ColumnCount = 2,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0, 2, 0, 0)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 159F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            beginnerDashboard = content;

            beginnerPrimaryCard = (ModernCard)BeginnerCard(new Padding(0, 0, 4, 8));
            beginnerPrimaryCard.MaximumSize = new Size(0, 151);
            Panel primaryCard = beginnerPrimaryCard;
            TableLayoutPanel primaryGrid = BeginnerCardGrid(1);
            primaryGrid.RowCount = 4;
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            TableLayoutPanel primaryHeader = CardHeaderGrid();
            beginnerPrimaryGlyph = new NetworkGlyph
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 8, 8),
                GlyphKind = NetworkGlyphKind.WiFi,
                GlyphColor = Accent
            };
            primaryHeader.Controls.Add(beginnerPrimaryGlyph, 0, 0);
            TableLayoutPanel primaryIdentity = CardIdentityGrid();
            primaryIdentity.Controls.Add(BeginnerCardTitle("主要網路 A"), 0, 0);
            beginnerPrimaryValue = LabelOf("尚未選擇", 0, 0, 0, 24, 10.5F, TextColor,
                                          FontStyle.Bold, ContentAlignment.MiddleLeft);
            beginnerPrimaryValue.Dock = DockStyle.Fill;
            beginnerPrimaryValue.AutoEllipsis = true;
            primaryIdentity.Controls.Add(beginnerPrimaryValue, 0, 1);
            primaryHeader.Controls.Add(primaryIdentity, 1, 0);
            beginnerPrimaryStatePill = StatePillOf("未啟動", MutedText);
            primaryHeader.Controls.Add(beginnerPrimaryStatePill, 2, 0);
            primaryGrid.Controls.Add(primaryHeader, 0, 0);

            beginnerPrimaryInterfaceBox = BeginnerInterfaceBox();
            beginnerPrimaryInterfaceBox.Dock = DockStyle.Fill;
            beginnerPrimaryInterfaceBox.Font = new Font("Microsoft JhengHei UI", 10F);
            beginnerPrimaryInterfaceBox.Margin = new Padding(0, 3, 0, 3);
            primaryGrid.Controls.Add(beginnerPrimaryInterfaceBox, 0, 1);
            primaryGrid.Controls.Add(SeparatorPanel(), 0, 2);
            beginnerPrimaryHealthValue = LabelOf("最新延遲", 0, 0, 0, 24, 9F, MutedText,
                                                 FontStyle.Regular, ContentAlignment.MiddleLeft);
            beginnerPrimaryHealthValue.Dock = DockStyle.Fill;
            beginnerPrimaryHealthValue.AutoEllipsis = true;
            beginnerPrimarySignal = new SignalGlyph
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 8, 4),
                Level = 0,
                ActiveColor = Accent,
                InactiveColor = Color.FromArgb(66, 94, 109)
            };
            primaryGrid.Controls.Add(BeginnerHealthGrid(beginnerPrimarySignal, beginnerPrimaryHealthValue), 0, 3);
            primaryCard.Controls.Add(primaryGrid);
            content.Controls.Add(primaryCard, 0, 0);

            beginnerBackupCard = (ModernCard)BeginnerCard(new Padding(4, 0, 0, 8));
            beginnerBackupCard.MaximumSize = new Size(0, 151);
            Panel backupCard = beginnerBackupCard;
            TableLayoutPanel backupGrid = BeginnerCardGrid(1);
            backupGrid.RowCount = 4;
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            TableLayoutPanel backupHeader = CardHeaderGrid();
            beginnerBackupGlyph = new NetworkGlyph
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 8, 8),
                GlyphKind = NetworkGlyphKind.Unknown,
                GlyphColor = Color.FromArgb(60, 170, 255)
            };
            backupHeader.Controls.Add(beginnerBackupGlyph, 0, 0);
            TableLayoutPanel backupIdentity = CardIdentityGrid();
            backupIdentity.Controls.Add(BeginnerCardTitle("備援網路 B"), 0, 0);
            beginnerBackupValue = LabelOf("未啟用", 0, 0, 0, 24, 10.5F, TextColor,
                                         FontStyle.Bold, ContentAlignment.MiddleLeft);
            beginnerBackupValue.Dock = DockStyle.Fill;
            beginnerBackupValue.AutoEllipsis = true;
            backupIdentity.Controls.Add(beginnerBackupValue, 0, 1);
            backupHeader.Controls.Add(backupIdentity, 1, 0);
            beginnerBackupStatePill = StatePillOf("未啟用", MutedText);
            backupHeader.Controls.Add(beginnerBackupStatePill, 2, 0);
            backupGrid.Controls.Add(backupHeader, 0, 0);

            beginnerBackupInterfaceBox = BeginnerInterfaceBox();
            beginnerBackupInterfaceBox.Dock = DockStyle.Fill;
            beginnerBackupInterfaceBox.Font = new Font("Microsoft JhengHei UI", 10F);
            beginnerBackupInterfaceBox.Margin = new Padding(0, 3, 0, 3);
            backupGrid.Controls.Add(beginnerBackupInterfaceBox, 0, 1);
            backupGrid.Controls.Add(SeparatorPanel(), 0, 2);
            beginnerBackupHealthValue = LabelOf("未啟用", 0, 0, 0, 24, 9F, MutedText,
                                                FontStyle.Regular, ContentAlignment.MiddleLeft);
            beginnerBackupHealthValue.Dock = DockStyle.Fill;
            beginnerBackupHealthValue.AutoEllipsis = true;
            beginnerBackupSignal = new SignalGlyph
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 8, 4),
                Level = 0,
                ActiveColor = Color.FromArgb(255, 199, 42),
                InactiveColor = Color.FromArgb(66, 94, 109)
            };
            backupGrid.Controls.Add(BeginnerHealthGrid(beginnerBackupSignal, beginnerBackupHealthValue), 0, 3);
            backupCard.Controls.Add(backupGrid);
            content.Controls.Add(backupCard, 1, 0);

            TableLayoutPanel options = new TableLayoutPanel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = PanelBackground,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(2, 4, 0, 0)
            };
            options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            options.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            beginnerFailoverBox = CheckBoxText("自動切換備援", false);
            beginnerAutoRepairBox = CheckBoxText("自動修復", true);
            options.Controls.Add(beginnerFailoverBox, 0, 0);
            options.Controls.Add(beginnerAutoRepairBox, 1, 0);
            options.Controls.Add(new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(63, 91, 108),
                Margin = new Padding(16, 20, 0, 0),
                MinimumSize = new Size(16, 1)
            }, 2, 0);
            content.Controls.Add(options, 0, 1);
            content.SetColumnSpan(options, 2);

            TableLayoutPanel tools = new TableLayoutPanel
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = PanelBackground,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(2, 4, 0, 0)
            };
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23F));
            tools.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            beginnerStartButton = ButtonOf("開始自動保護", 0, 0, 220, 42, true);
            beginnerStartButton.Dock = DockStyle.Fill;
            beginnerStartButton.Margin = new Padding(0, 0, 8, 0);
            ModernButton modernBeginnerStart = beginnerStartButton as ModernButton;
            if (modernBeginnerStart != null) { modernBeginnerStart.Glyph = "shield"; }
            beginnerStartButton.Click += delegate
            {
                if (engine.IsRunning) { StopMonitoring(); }
                else { StartButton_Click(this, EventArgs.Empty); }
            };
            tools.Controls.Add(beginnerStartButton, 0, 0);
            beginnerDetectButton = ButtonOf("重新偵測網路", 0, 0, 176, 36, false);
            beginnerDetectButton.Dock = DockStyle.Fill;
            beginnerDetectButton.Margin = new Padding(0, 3, 6, 0);
            ModernButton modernBeginnerDetect = beginnerDetectButton as ModernButton;
            if (modernBeginnerDetect != null) { modernBeginnerDetect.Glyph = "refresh"; }
            beginnerDetectButton.Click += delegate { AutoDetectBeginnerInterfaces(); };
            tools.Controls.Add(beginnerDetectButton, 1, 0);
            beginnerRestoreButton = ButtonOf("復原上一筆變更", 0, 0, 176, 36, false);
            beginnerRestoreButton.Dock = DockStyle.Fill;
            beginnerRestoreButton.Margin = new Padding(0, 3, 6, 0);
            ModernButton modernBeginnerRestore = beginnerRestoreButton as ModernButton;
            if (modernBeginnerRestore != null) { modernBeginnerRestore.Glyph = "undo"; }
            beginnerRestoreButton.Click += BeginnerRestoreButton_Click;
            tools.Controls.Add(beginnerRestoreButton, 2, 0);
            beginnerLogButton = ButtonOf("查看執行紀錄 ▸", 0, 0, 150, 36, false);
            beginnerLogButton.Dock = DockStyle.Fill;
            beginnerLogButton.Margin = new Padding(0, 3, 0, 0);
            ModernButton modernBeginnerLog = beginnerLogButton as ModernButton;
            if (modernBeginnerLog != null) { modernBeginnerLog.Glyph = "log"; }
            tools.Controls.Add(beginnerLogButton, 3, 0);
            content.Controls.Add(tools, 0, 2);
            content.SetColumnSpan(tools, 2);

            beginnerPrimaryInterfaceBox.TextChanged += delegate { UpdateBeginnerStatus(); };
            beginnerBackupInterfaceBox.TextChanged += delegate { UpdateBeginnerStatus(); };
            beginnerFailoverBox.CheckedChanged += delegate
            {
                if (beginnerMode && failoverEnabledBox != null)
                {
                    failoverEnabledBox.Checked = beginnerFailoverBox.Checked;
                }
                UpdateBeginnerStatus();
            };
            beginnerAutoRepairBox.CheckedChanged += delegate
            {
                if (beginnerMode && enableRefreshBox != null)
                {
                    enableRefreshBox.Checked = beginnerAutoRepairBox.Checked;
                }
            };
            beginnerLogButton.Click += delegate
            {
                beginnerLogExpanded = !beginnerLogExpanded;
                UpdateUiMode();
                if (beginnerLogExpanded && logBox != null)
                {
                    ScrollLogToEnd();
                }
            };

            group.Controls.Add(content);
            return group;
        }

        private void AutoDetectBeginnerInterfaces()
        {
            AutoDetectBeginnerInterfaces(true);
        }

        private void AutoDetectBeginnerInterfaces(bool showPrompt)
        {
            try
            {
                List<InterfaceSnapshot> snapshots = NetworkInfo.GetInterfaceSnapshots();
                string oldPrimary = beginnerPrimaryInterfaceBox.Text.Trim();
                string oldBackup = beginnerBackupInterfaceBox.Text.Trim();
                AutoDetectSelection selection = SelectReadyPair(snapshots, oldPrimary, oldBackup);
                List<string> readyNames = selection.Ready
                    .Select(delegate(InterfaceSnapshot item) { return item.Name; })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (selection.Primary == null)
                {
                    DisableFailoverWithoutReadyBackup();
                    PopulateInterfaceBox(beginnerPrimaryInterfaceBox, readyNames, string.Empty);
                    PopulateInterfaceBox(beginnerBackupInterfaceBox, readyNames, string.Empty);
                    beginnerPrimaryInterfaceBox.Text = string.Empty;
                    beginnerBackupInterfaceBox.Text = string.Empty;
                    if (interfaceBox != null) { interfaceBox.Text = string.Empty; }
                    if (beginnerMode) { SyncBeginnerSettingsToAdvanced(true); }
                    UpdateBeginnerStatus();
                    string noReadyMessage =
                        L("自動偵測未找到任何就緒網路；主要與備援已留空。") + Environment.NewLine +
                        L("沒有偵測到就緒備援，自動切換已關閉。") + Environment.NewLine +
                        L("請先連線 Wi‑Fi、藍牙或乙太網路，再按「重新偵測網路」。");
                    AppendLog(L("自動偵測：沒有找到具 IPv4 與 gateway 的就緒網路。"), true);
                    if (showPrompt)
                    {
                        MessageBox.Show(this, noReadyMessage, "NetOptimizer",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return;
                }

                PopulateInterfaceBox(beginnerPrimaryInterfaceBox, readyNames, selection.Primary.Name);
                PopulateInterfaceBox(beginnerBackupInterfaceBox, readyNames,
                                     selection.Backup == null ? string.Empty : selection.Backup.Name);
                beginnerPrimaryInterfaceBox.Text = selection.Primary.Name;
                beginnerBackupInterfaceBox.Text = selection.Backup == null ? string.Empty : selection.Backup.Name;
                if (!HasReadyBackup(selection.Backup))
                {
                    DisableFailoverWithoutReadyBackup();
                }
                if (interfaceBox != null) { interfaceBox.Text = selection.Primary.Name; }
                if (beginnerMode)
                {
                    SyncBeginnerSettingsToAdvanced(true);
                }
                UpdateBeginnerStatus();

                if (selection.Backup == null)
                {
                    string oneReadyMessage =
                        L("已找到主要網路「") + selection.Primary.Name + L("」，但沒有第二條就緒線路。") +
                        Environment.NewLine +
                        L("備援已留空；請再連線另一條具 IPv4 與 gateway 的網路。") +
                        Environment.NewLine +
                        L("沒有偵測到就緒備援，自動切換已關閉。");
                    AppendLog(L("已重新偵測網路：主要「") + selection.Primary.Name +
                              L("」；備援未找到，已留空。"), true);
                    if (showPrompt)
                    {
                        MessageBox.Show(this, oneReadyMessage, "NetOptimizer",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    AppendLog(L("已重新偵測網路：主要「") + selection.Primary.Name + L("」；備援「") +
                              selection.Backup.Name + L("」。"), false);
                }
            }
            catch (Exception ex)
            {
                DisableFailoverWithoutReadyBackup();
                AppendLog(L("自動偵測網路失敗：") + ex.Message, true);
                if (showPrompt)
                {
                    MessageBox.Show(this, L("無法完成網路偵測。") + Environment.NewLine + ex.Message,
                                    "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static AutoDetectSelection SelectReadyPair(
            List<InterfaceSnapshot> snapshots,
            string oldPrimary,
            string oldBackup)
        {
            List<InterfaceSnapshot> ready = (snapshots ?? new List<InterfaceSnapshot>())
                .Where(delegate(InterfaceSnapshot item)
                {
                    return item != null && item.IsReady && !string.IsNullOrWhiteSpace(item.Name);
                })
                .ToList();

            InterfaceSnapshot primary = ready.FirstOrDefault(delegate(InterfaceSnapshot item)
            {
                return string.Equals(item.Name, oldPrimary, StringComparison.OrdinalIgnoreCase);
            });
            if (primary == null) { primary = ready.FirstOrDefault(); }

            InterfaceSnapshot backup = null;
            if (primary != null)
            {
                backup = ready.FirstOrDefault(delegate(InterfaceSnapshot item)
                {
                    return !string.Equals(item.Name, primary.Name, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(item.Name, oldBackup, StringComparison.OrdinalIgnoreCase);
                });
                if (backup == null)
                {
                    backup = ready.FirstOrDefault(delegate(InterfaceSnapshot item)
                    {
                        return !string.Equals(item.Name, primary.Name, StringComparison.OrdinalIgnoreCase);
                    });
                }
            }

            return new AutoDetectSelection
            {
                Ready = ready,
                Primary = primary,
                Backup = backup
            };
        }

        internal static bool HasReadyBackup(InterfaceSnapshot backup)
        {
            return backup != null && backup.IsReady;
        }

        private void DisableFailoverWithoutReadyBackup()
        {
            if (settings != null)
            {
                settings.FailoverEnabled = false;
                settings.SmartSelectionEnabled = false;
            }
            if (beginnerFailoverBox != null) { beginnerFailoverBox.Checked = false; }
            if (failoverEnabledBox != null) { failoverEnabledBox.Checked = false; }
            if (smartSelectionBox != null) { smartSelectionBox.Checked = false; }
            UpdateFailoverEnabled();
            UpdateBeginnerStatus();
        }

        private async void BeginnerRestoreButton_Click(object sender, EventArgs e)
        {
            if (restoringNetwork) { return; }
            if (engine.IsRunning)
            {
                MessageBox.Show(this, L("請先停止自動保護，再執行復原。"),
                                "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            restoringNetwork = true;
            UpdateButtons();
            try
            {
                RecoveryReport report = await FailoverManager.RecoverPendingAsync(CancellationToken.None);
                foreach (string message in report.Messages)
                {
                    AppendLog(L("復原：") + message, !report.Restored);
                }

                string summary;
                MessageBoxIcon icon;
                if (!report.FoundJournal)
                {
                    summary = L("目前沒有待復原的 A/B 網路變更。");
                    icon = MessageBoxIcon.Information;
                }
                else if (report.Restored)
                {
                    summary = L("上一筆 A/B 網路變更已完成復原。");
                    icon = MessageBoxIcon.Information;
                }
                else
                {
                    summary = L("目前無法完整復原上一筆變更。請以系統管理員身分重試，或查看執行紀錄。");
                    icon = MessageBoxIcon.Warning;
                }
                MessageBox.Show(this, summary, "NetOptimizer", MessageBoxButtons.OK, icon);
            }
            catch (Exception ex)
            {
                AppendLog(L("手動復原失敗：") + ex.Message, true);
                MessageBox.Show(this, L("復原失敗。") + Environment.NewLine + ex.Message,
                                "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                restoringNetwork = false;
                UpdateButtons();
            }
        }

        internal static void RunUiLayoutSelfTest()
        {
            RunAutoDetectSelectionSelfTest();
            RunNetworkGlyphSelfTest();
            UiIconRenderer.RunSelfTest();
            using (MainForm form = new MainForm(false, false, false))
            {
                form.CreateControl();
                form.ShowInTaskbar = false;
                form.Opacity = 0.0;
                form.Show();
                Application.DoEvents();
                form.beginnerMode = true;
                form.advancedDrawerOpen = false;
                form.UpdateBeginnerStatus();
                form.UpdateUiMode();
                form.UpdateButtons();
                form.PerformLayout();
                if (form.layoutRoot == null || form.layoutRoot.Controls.Count != 2 ||
                      form.layoutShell == null || form.footerLayout == null ||
                      form.beginnerPanel == null || !form.beginnerMode || !form.beginnerPanel.Visible ||
                      form.beginnerDashboard == null ||
                     form.permissionPanel == null || form.permissionPanel.Parent != form.footerLayout ||
                     !form.footerLayout.Visible ||
                      form.beginnerPrimaryValue == null || form.beginnerPrimaryHealthValue == null ||
                     form.beginnerBackupValue == null || form.beginnerBackupHealthValue == null ||
                     form.beginnerStartButton == null || !form.beginnerStartButton.Visible ||
                    form.beginnerDetectButton == null || !form.beginnerDetectButton.Visible ||
                    form.beginnerRestoreButton == null || !form.beginnerRestoreButton.Visible ||
                    form.startWithWindowsBox == null || !form.startWithWindowsBox.Visible ||
                     form.trayStartup == null ||
                     form.monitorGroup.Visible || form.actionsGroup.Visible ||
                     form.failoverGroup.Visible || form.logGroup.Visible ||
                     form.monitorGroup.Parent != form.advancedDrawerLayout ||
                     form.actionsGroup.Parent != form.advancedDrawerLayout ||
                     form.failoverGroup.Parent != form.advancedDrawerLayout ||
                     form.logGroup.Parent != form.beginnerDashboard)
                {
                    throw new InvalidOperationException(
                        "Beginner controls/layout invalid: rows=" +
                        (form.layoutRoot == null ? "null" : form.layoutRoot.Controls.Count.ToString()) +
                        ", panelVisible=" + (form.beginnerPanel != null && form.beginnerPanel.Visible) +
                        ", dashboard=" + (form.beginnerDashboard == null ? "null" : form.beginnerDashboard.Size.ToString()));
                }
                AssertBeginnerDashboard(form, "初始");
                AssertHeaderVisible(form, "初始");
                ModernButton headerModeButton = form.modeButton as ModernButton;
                ModernComboBox headerLanguageBox = form.languageBox as ModernComboBox;
                if (form.modeButton.Text != form.L("顯示進階設定 ▸") ||
                    headerModeButton == null || !headerModeButton.IconOnly ||
                    !string.Equals(headerModeButton.Glyph, "gear", StringComparison.OrdinalIgnoreCase) ||
                    form.modeButton.Width < 36 || form.modeButton.Height < 24 ||
                    headerLanguageBox == null || !headerLanguageBox.IconOnly ||
                    !headerLanguageBox.ShowGlobe ||
                    form.languageBox.Width < 38)
                {
                    throw new InvalidOperationException("標題區的圖示入口或尺寸不正確。");
                }
                form.languageBox.Focus();
                form.languageBox.DroppedDown = true;
                Application.DoEvents();
                bool languageMenuOpened = form.languageBox.DroppedDown;
                form.languageBox.DroppedDown = false;
                Application.DoEvents();
                if (!languageMenuOpened || form.languageBox.Items.Count < 2)
                {
                    throw new InvalidOperationException("語言圖示入口無法展開語言選單。");
                }
                if (form.brandImage == null || form.brandImage.Image == null)
                {
                    throw new InvalidOperationException("品牌圖示未載入。");
                }
                form.availableUpdate = new UpdateInfo(
                    new Version(3, 0, 16, 0),
                    "v3.0.16",
                    UpdateChecker.RepositoryReleaseUrl + "/tag/v3.0.16");
                form.UpdateUpdateUi();
                form.PerformLayout();
                Rectangle updateBounds = new Rectangle(
                    form.updateLink.PointToScreen(Point.Empty), form.updateLink.ClientSize);
                Rectangle updateHeaderBounds = new Rectangle(
                    form.headerLayout.PointToScreen(Point.Empty), form.headerLayout.ClientSize);
                if (!form.updateLink.Visible || form.updateLink.Text != form.L("更新") + " v3.0.16" ||
                    form.trayUpdate.Owner != form.trayMenu ||
                    form.trayIgnoreUpdate.Owner != form.trayMenu || !form.trayUpdate.Enabled ||
                    !form.trayIgnoreUpdate.Enabled ||
                    updateBounds.Width <= 0 || updateBounds.Height < 18 ||
                    !updateHeaderBounds.Contains(updateBounds))
                {
                    throw new InvalidOperationException(
                        "更新提示入口布局或系統匣入口未正確顯示：link=" + form.updateLink.Visible +
                        ", trayUpdateOwner=" + (form.trayUpdate.Owner == form.trayMenu) +
                        ", trayIgnoreOwner=" + (form.trayIgnoreUpdate.Owner == form.trayMenu) +
                        ", trayUpdateEnabled=" + form.trayUpdate.Enabled + ", trayIgnoreEnabled=" +
                        form.trayIgnoreUpdate.Enabled + ", linkBounds=" + updateBounds +
                        ", headerBounds=" + updateHeaderBounds);
                }
                form.availableUpdate = null;
                form.UpdateUpdateUi();
                AppLanguage savedLanguage = form.currentLanguage;
                form.currentLanguage = AppLanguage.English;
                form.ApplyLanguageToUi(false);
                bool englishUiReady = form.languageBox != null &&
                                      form.languageBox.SelectedIndex == 1 &&
                                      form.beginnerPanel.Text == "Quick start" &&
                                      form.beginnerStartButton.Text == "Start protection" &&
                                      form.modeButton.Text == "Advanced ▸" &&
                                      form.supportButton.Text == "☕ Support" &&
                                      form.startWithWindowsBox.Text == "Start with Windows" &&
                                      form.trayStartup.Text == "Start with Windows";
                form.currentLanguage = savedLanguage;
                form.ApplyLanguageToUi(false);
                if (!englishUiReady)
                {
                    throw new InvalidOperationException("English UI localization did not apply completely.");
                }
                AssertNoNotPresentInterfaceItems(form, "初始");
                SupportDialog.RunUiSelfTest();
                if (form.FormBorderStyle != FormBorderStyle.None || form.MaximizeBox || form.MinimizeBox ||
                     form.customTitleBar == null || form.customTitleBar.Height < 32 ||
                     !form.customTitleBar.Visible || form.customTitleBar.Parent != form ||
                     form.layoutShell == null || form.layoutShell.Top < form.customTitleBar.Bottom ||
                     form.chromeMinimizeButton == null || form.chromeMinimizeButton.Height < 28 ||
                     form.chromeMaximizeButton == null || form.chromeMaximizeButton.Height < 28 ||
                     form.chromeCloseButton == null || form.chromeCloseButton.Height < 28 ||
                     form.MinimumSize.Width < 760 || form.MinimumSize.Height < 400)
                {
                    throw new InvalidOperationException("自訂標題列或視窗縮放設定不正確。");
                }
                form.chromeMaximizeButton.PerformClick();
                Application.DoEvents();
                if (form.WindowState != FormWindowState.Maximized)
                {
                    throw new InvalidOperationException("自訂最大化按鈕未正常運作。");
                }
                form.chromeMaximizeButton.PerformClick();
                Application.DoEvents();
                if (form.WindowState != FormWindowState.Normal)
                {
                    throw new InvalidOperationException("自訂還原按鈕未正常運作。");
                }
                if (form.ClientSize.Height > 560)
                {
                    throw new InvalidOperationException("新手模式預設視窗高度仍然過大。");
                }
                if (form.contentViewport == null || form.contentViewport.VerticalScroll.Visible)
                {
                    throw new InvalidOperationException("新手模式預設視窗不應出現垂直捲軸。");
                }
                if (form.failoverAdvancedPanel == null || form.failoverAdvancedPanel.Visible ||
                    form.logEmptyLabel == null || form.logGroup.Parent != form.beginnerDashboard ||
                    form.logGroup.Visible)
                {
                    throw new InvalidOperationException("進階設定初始狀態不正確。");
                }
                form.beginnerLogButton.PerformClick();
                form.PerformLayout();
                if (!form.logGroup.Visible || form.logGroup.Parent != form.beginnerDashboard ||
                    form.logGroup.Height < 180 || form.beginnerPanel.Height <= BeginnerPanelBaseHeight)
                {
                    throw new InvalidOperationException("執行紀錄未在新手主畫面下方展開。");
                }
                form.ClearLogContent();
                if (!form.logEmptyLabel.Visible)
                {
                    throw new InvalidOperationException("清除紀錄後的空狀態未正確顯示。");
                }
                form.beginnerLogButton.PerformClick();
                form.PerformLayout();
                if (form.advancedDrawerOpen || form.logGroup.Visible ||
                    form.beginnerPanel.Height != BeginnerPanelBaseHeight)
                {
                    throw new InvalidOperationException("新手畫面的執行紀錄收合狀態不正確。");
                }

                bool settingsDialogOpened = false;
                bool settingsDialogLayoutValid = false;
                using (System.Windows.Forms.Timer closeSettingsTimer = new System.Windows.Forms.Timer())
                {
                    closeSettingsTimer.Interval = 100;
                    closeSettingsTimer.Tick += delegate
                    {
                        closeSettingsTimer.Stop();
                        foreach (Form window in Application.OpenForms)
                        {
                            AdvancedSettingsDialog settingsWindow = window as AdvancedSettingsDialog;
                            if (settingsWindow != null)
                            {
                                settingsDialogOpened = true;
                                settingsDialogLayoutValid = form.advancedDrawer.Parent != null &&
                                    form.monitorGroup.Parent == form.advancedDrawerLayout &&
                                    form.actionsGroup.Parent == form.advancedDrawerLayout &&
                                    form.failoverGroup.Parent == form.advancedDrawerLayout &&
                                    form.monitorGroup.Visible && form.actionsGroup.Visible &&
                                    form.failoverGroup.Visible;
                                settingsWindow.Close();
                                break;
                            }
                        }
                    };
                    closeSettingsTimer.Start();
                    form.modeButton.PerformClick();
                }
                if (!settingsDialogOpened || !settingsDialogLayoutValid || form.settingsDialog != null ||
                    form.advancedDrawerOpen || form.monitorGroup.Visible || form.actionsGroup.Visible ||
                    form.failoverGroup.Visible || form.logGroup.Parent != form.beginnerDashboard)
                {
                    throw new InvalidOperationException("設定按鈕未以獨立視窗開啟進階設定。");
                }

                List<InterfaceSnapshot> readyBeforeDetect = NetworkInfo.GetInterfaceSnapshots()
                    .Where(delegate(InterfaceSnapshot item) { return item != null && item.IsReady; })
                    .ToList();
                form.AutoDetectBeginnerInterfaces(false);
                AssertNoNotPresentInterfaceItems(form, "自動偵測後");
                if (readyBeforeDetect.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(form.beginnerPrimaryInterfaceBox.Text) ||
                        !string.IsNullOrWhiteSpace(form.beginnerBackupInterfaceBox.Text))
                    {
                        throw new InvalidOperationException("沒有就緒網路時，自動偵測未清空 A/B。");
                    }
                }
                else
                {
                    InterfaceSnapshot detectedPrimary =
                        NetworkInfo.GetInterfaceSnapshot(form.beginnerPrimaryInterfaceBox.Text.Trim());
                    if (detectedPrimary == null || !detectedPrimary.IsReady)
                    {
                        throw new InvalidOperationException("自動偵測選出的主要網路不是 IsReady。");
                    }
                    if (readyBeforeDetect.Count < 2)
                    {
                        if (!string.IsNullOrWhiteSpace(form.beginnerBackupInterfaceBox.Text))
                        {
                            throw new InvalidOperationException("只有一條就緒網路時，備援沒有留空。");
                        }
                    }
                    else
                    {
                        InterfaceSnapshot detectedBackup =
                            NetworkInfo.GetInterfaceSnapshot(form.beginnerBackupInterfaceBox.Text.Trim());
                        if (detectedBackup == null || !detectedBackup.IsReady ||
                            string.Equals(detectedPrimary.Name, detectedBackup.Name,
                                          StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("自動偵測選出的備援不是第二條 IsReady 網路。");
                        }
                    }
                }

                form.settings = form.settings.Clone();
                form.settings.BeginnerMode = true;
                form.settings.BackupInterface = string.Empty;
                form.settings.FailoverEnabled = true;
                form.settings.SmartSelectionEnabled = true;
                form.ApplySettingsToUi();
                if (form.beginnerFailoverBox.Checked || form.failoverEnabledBox.Checked ||
                    form.smartSelectionBox.Checked)
                {
                    throw new InvalidOperationException("沒有就緒備援時，自動切換預設未關閉。");
                }

                if (!NetworkInfo.IsAdministrator())
                {
                    bool originalFailover = form.failoverEnabledBox.Checked;
                    form.failoverEnabledBox.Checked = true;
                    if (form.TryStartMonitoring(false) || form.engine.IsRunning ||
                        form.adminButton == null || !form.adminButton.Visible)
                    {
                        throw new InvalidOperationException("非管理員啟動 A/B 時未被阻止。");
                    }
                    form.failoverEnabledBox.Checked = originalFailover;
                }

                form.ClientSize = new Size(760, 700);
                form.PerformLayout();
                AssertBeginnerDashboard(form, "窄視窗");
                AssertHeaderVisible(form, "窄視窗");
                AssertLayoutHasWidth(form, "窄視窗");
                AssertFooterVisible(form, "窄視窗");

                form.ClientSize = new Size(1200, 1000);
                form.PerformLayout();
                AssertBeginnerDashboard(form, "寬視窗");
                AssertHeaderVisible(form, "寬視窗");
                AssertLayoutHasWidth(form, "寬視窗");
                AssertFooterVisible(form, "寬視窗");

                form.beginnerLogButton.PerformClick();
                form.PerformLayout();
                if (!form.logGroup.Visible || form.logGroup.Parent != form.beginnerDashboard ||
                    form.logGroup.Bottom > form.beginnerDashboard.ClientSize.Height + 1)
                {
                    throw new InvalidOperationException("寬視窗無法在主畫面下方展開執行紀錄。");
                }
                form.beginnerLogButton.PerformClick();
                form.PerformLayout();
                if (form.logGroup.Visible || form.beginnerPanel.Height != BeginnerPanelBaseHeight)
                {
                    throw new InvalidOperationException("寬視窗無法收合主畫面執行紀錄。");
                }

                form.advancedDrawer.Visible = true;
                form.failoverGroup.Visible = true;
                form.advancedExpanded = true;
                form.UpdateFailoverAdvanced();
                form.PerformLayout();
                if (!form.failoverAdvancedPanel.Visible || form.failoverAdvancedPanel.Height <= 0)
                {
                    throw new InvalidOperationException("A/B 進階設定展開布局失敗。");
                }

                form.advancedExpanded = false;
                form.UpdateFailoverAdvanced();
                form.PerformLayout();
                if (form.failoverAdvancedPanel.Visible)
                {
                    throw new InvalidOperationException("A/B 進階設定收合布局失敗。");
                }
                form.failoverGroup.Visible = false;
                form.advancedDrawer.Visible = false;

                form.PerformLayout();
                if (!form.beginnerMode || !form.beginnerPanel.Visible || form.advancedDrawerOpen ||
                    form.monitorGroup.Visible ||
                    form.actionsGroup.Visible || form.failoverGroup.Visible || form.logGroup.Visible ||
                     form.monitorGroup.Parent != form.advancedDrawerLayout ||
                     form.actionsGroup.Parent != form.advancedDrawerLayout ||
                     form.failoverGroup.Parent != form.advancedDrawerLayout ||
                     form.logGroup.Parent != form.beginnerDashboard ||
                     form.permissionPanel.Parent != form.footerLayout)
                {
                    throw new InvalidOperationException("設定視窗關閉後的新手模式布局失敗。");
                }
                AssertFooterVisible(form, "設定視窗關閉後");

                Console.WriteLine("NetOptimizer UI layout test: PASS");
            }
        }

        internal static void RunGuiStartupSelfTest()
        {
            string hiddenFailure = null;
            using (MainForm hiddenForm = new MainForm(true, false, false))
            using (System.Windows.Forms.Timer hiddenTimer = new System.Windows.Forms.Timer())
            {
                hiddenForm.Opacity = 0.0;
                hiddenTimer.Interval = 1000;
                hiddenTimer.Tick += delegate
                {
                    try
                    {
                        if (hiddenForm.Visible || hiddenForm.WindowState != FormWindowState.Minimized ||
                            hiddenForm.ShowInTaskbar)
                        {
                            throw new InvalidOperationException(
                                "startup-hidden state invalid: visible=" + hiddenForm.Visible +
                                ", windowState=" + hiddenForm.WindowState +
                                ", showInTaskbar=" + hiddenForm.ShowInTaskbar);
                        }
                    }
                    catch (Exception ex)
                    {
                        hiddenFailure = ex.Message;
                    }
                    finally
                    {
                        hiddenTimer.Stop();
                        hiddenForm.Close();
                    }
                };
                hiddenTimer.Start();
                Application.Run(hiddenForm);
            }

            if (!string.IsNullOrWhiteSpace(hiddenFailure))
            {
                throw new InvalidOperationException(hiddenFailure);
            }

            string failure = null;
            using (MainForm form = new MainForm(false, false, false))
            using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
            {
                form.Opacity = 0.0;
                timer.Interval = 1500;
                timer.Tick += delegate
                {
                    try
                    {
                        if (!form.Visible || form.WindowState != FormWindowState.Normal ||
                            !string.Equals(form.Text, "NetOptimizer", StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                "initial state invalid: visible=" + form.Visible +
                                ", windowState=" + form.WindowState + ", title=" + form.Text);
                        }

                        if (form.chromeMinimizeButton == null)
                        {
                            throw new InvalidOperationException("custom minimize button was not created");
                        }
                        form.chromeMinimizeButton.PerformClick();
                        Application.DoEvents();
                        form.ShowFromTray();
                        if (!form.Visible || form.WindowState != FormWindowState.Normal ||
                            !form.ShowInTaskbar)
                        {
                            throw new InvalidOperationException(
                                "tray restore invalid: visible=" + form.Visible +
                                ", windowState=" + form.WindowState +
                                ", showInTaskbar=" + form.ShowInTaskbar);
                        }
                    }
                    catch (Exception ex)
                    {
                        failure = ex.Message;
                    }
                    finally
                    {
                        timer.Stop();
                        form.Close();
                    }
                };
                timer.Start();
                Application.Run(form);
            }

            if (!string.IsNullOrWhiteSpace(failure))
            {
                throw new InvalidOperationException(failure);
            }
            Console.WriteLine("NetOptimizer GUI startup test: PASS");
        }

        internal static void SaveUiSnapshot(string outputPath)
        {
            SaveUiSnapshot(outputPath, AppLanguage.TraditionalChinese);
        }

        internal static void SaveUiSnapshot(string outputPath, AppLanguage language)
        {
            SaveUiSnapshot(outputPath, language, string.Empty, false);
        }

        internal static void SaveUiSnapshot(string outputPath, AppLanguage language,
                                             string snapshotNetwork)
        {
            SaveUiSnapshot(outputPath, language, snapshotNetwork, false);
        }

        internal static void SaveUiSnapshot(string outputPath, AppLanguage language,
                                             string snapshotNetwork, bool snapshotLog)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Snapshot output path is required.", "outputPath");
            }
            string fullPath = Path.GetFullPath(outputPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (MainForm form = new MainForm(false, false, false))
            {
                form.currentLanguage = Localization.Normalize(language);
                form.ApplyLanguageToUi(false);
                form.ShowInTaskbar = false;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(20, 20);
                form.Show();
                Application.DoEvents();
                form.ApplySnapshotNetworkOverride(snapshotNetwork);
                if (snapshotLog)
                {
                    form.beginnerLogExpanded = true;
                    form.UpdateUiMode();
                }
                Application.DoEvents();
                form.PerformLayout();
                AssertBeginnerDashboard(form, "snapshot");
                AssertHeaderVisible(form, "snapshot");
                AssertFooterVisible(form, "snapshot");

                using (Bitmap snapshot = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(snapshot, new Rectangle(Point.Empty, form.Size));
                    snapshot.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                form.Close();
            }
            Console.WriteLine("NetOptimizer UI snapshot: PASS " + fullPath);
        }

        private void ApplySnapshotNetworkOverride(string snapshotNetwork)
        {
            if (string.IsNullOrWhiteSpace(snapshotNetwork)) { return; }

            NetworkGlyphKind kind;
            string normalized = snapshotNetwork.Trim().ToLowerInvariant();
            if (normalized == "wifi" || normalized == "wi-fi" || normalized == "wireless")
            {
                kind = NetworkGlyphKind.WiFi;
            }
            else if (normalized == "bluetooth" || normalized == "bt" || normalized == "藍牙")
            {
                kind = NetworkGlyphKind.Bluetooth;
            }
            else if (normalized == "ethernet" || normalized == "lan" ||
                     normalized == "乙太網路" || normalized == "以太網路")
            {
                kind = NetworkGlyphKind.Ethernet;
            }
            else
            {
                throw new ArgumentException(
                    "Unsupported snapshot network. Use wifi, bluetooth, or ethernet.",
                    "snapshotNetwork");
            }

            if (beginnerPrimaryGlyph != null)
            {
                beginnerPrimaryGlyph.GlyphKind = kind;
                beginnerPrimaryGlyph.GlyphColor = Accent;
            }
            if (beginnerPrimaryInterfaceBox != null)
            {
                for (int index = 0; index < beginnerPrimaryInterfaceBox.Items.Count; index++)
                {
                    string item = Convert.ToString(beginnerPrimaryInterfaceBox.Items[index]);
                    if (string.Equals(item, "藍牙網路連線", StringComparison.OrdinalIgnoreCase) ||
                        (kind == NetworkGlyphKind.WiFi &&
                         item.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (kind == NetworkGlyphKind.Ethernet &&
                         (item.IndexOf("乙太", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          item.IndexOf("Ethernet", StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        beginnerPrimaryInterfaceBox.SelectedIndex = index;
                        break;
                    }
                }
            }
            if (beginnerPrimaryValue != null)
            {
                beginnerPrimaryValue.Text = kind == NetworkGlyphKind.WiFi ? "Wi-Fi" :
                    kind == NetworkGlyphKind.Bluetooth ? "藍牙網路連線" : "乙太網路";
            }
        }

        private static void AssertLayoutHasWidth(MainForm form, string stage)
        {
            if (form.advancedDrawerOpen)
            {
                Control[] drawerControls =
                {
                    form.monitorGroup,
                    form.actionsGroup,
                    form.failoverGroup
                };
                foreach (Control control in drawerControls)
                {
                    if (control == null || control.Parent != form.advancedDrawerLayout ||
                        control.Width <= 0 || control.Height <= 0)
                    {
                        throw new InvalidOperationException(stage + "進階抽屜沒有取得有效寬度。");
                    }
                }
                return;
            }
            if (form.beginnerPanel == null || form.beginnerPanel.Width <= 0 ||
                form.beginnerDashboard == null || form.beginnerDashboard.Width <= 0)
            {
                throw new InvalidOperationException(stage + "新手布局沒有取得有效寬度。");
            }
        }

        private static void AssertNoNotPresentInterfaceItems(MainForm form, string stage)
        {
            ComboBox[] boxes =
            {
                form.interfaceBox,
                form.primaryInterfaceBox,
                form.backupInterfaceBox,
                form.beginnerPrimaryInterfaceBox,
                form.beginnerBackupInterfaceBox
            };
            foreach (ComboBox box in boxes)
            {
                for (int index = 0; index < box.Items.Count; index++)
                {
                    string name = Convert.ToString(box.Items[index]);
                    InterfaceSnapshot snapshot = NetworkInfo.GetInterfaceSnapshot(name);
                    if (snapshot != null && snapshot.Status == OperationalStatus.NotPresent)
                    {
                        throw new InvalidOperationException(
                            stage + "下拉選單包含 NotPresent 介面：「" + name + "」。");
                    }
                }
            }
        }

        private static void RunAutoDetectSelectionSelfTest()
        {
            InterfaceSnapshot readyA = new InterfaceSnapshot
            {
                Name = "Ready-A",
                Status = OperationalStatus.Up,
                IPv4 = "192.0.2.10",
                Gateway = "192.0.2.1"
            };
            InterfaceSnapshot readyB = new InterfaceSnapshot
            {
                Name = "Ready-B",
                Status = OperationalStatus.Up,
                IPv4 = "198.51.100.10",
                Gateway = "198.51.100.1"
            };
            InterfaceSnapshot notPresent = new InterfaceSnapshot
            {
                Name = "NotPresent-B",
                Status = OperationalStatus.NotPresent,
                IPv4 = "203.0.113.10",
                Gateway = "203.0.113.1"
            };

            AutoDetectSelection one = SelectReadyPair(
                new List<InterfaceSnapshot> { notPresent, readyA },
                notPresent.Name,
                notPresent.Name);
            if (one.Primary != readyA || one.Backup != null)
            {
                throw new InvalidOperationException("自動偵測選路測試未排除 NotPresent 或未清空單線路備援。");
            }

            AutoDetectSelection two = SelectReadyPair(
                new List<InterfaceSnapshot> { readyA, notPresent, readyB },
                readyA.Name,
                notPresent.Name);
            if (two.Primary != readyA || two.Backup != readyB)
            {
                throw new InvalidOperationException("自動偵測選路測試未選出第二條 IsReady 備援。");
            }

            if (HasReadyBackup(null) || HasReadyBackup(notPresent) || !HasReadyBackup(readyB))
            {
                throw new InvalidOperationException("沒有就緒備援時，A/B 自動切換預設未關閉。");
            }
        }

        private static void RunNetworkGlyphSelfTest()
        {
            if (ClassifyNetworkGlyph(NetworkInterfaceType.Wireless80211, "Wi-Fi") !=
                NetworkGlyphKind.WiFi)
            {
                throw new InvalidOperationException("Wi-Fi 圖示判斷失敗。");
            }
            if (ClassifyNetworkGlyph(NetworkInterfaceType.Unknown, "藍牙網路連線") !=
                NetworkGlyphKind.Bluetooth)
            {
                throw new InvalidOperationException("藍牙圖示判斷失敗。");
            }
            if (ClassifyNetworkGlyph(NetworkInterfaceType.Ethernet,
                                     "藍牙網路連線 Bluetooth Device (Personal Area Network)") !=
                NetworkGlyphKind.Bluetooth)
            {
                throw new InvalidOperationException("藍牙 PAN 介面不可被誤判為乙太網路。");
            }
            if (ClassifyNetworkGlyph(NetworkInterfaceType.Ethernet, "乙太網路") !=
                NetworkGlyphKind.Ethernet)
            {
                throw new InvalidOperationException("乙太網路圖示判斷失敗。");
            }
            if (ClassifyNetworkGlyph(NetworkInterfaceType.Unknown, string.Empty) !=
                NetworkGlyphKind.Unknown)
            {
                throw new InvalidOperationException("未選擇介面時的中性圖示判斷失敗。");
            }
        }

        private static void AssertBeginnerDashboard(MainForm form, string stage)
        {
            if (form.beginnerPanel == null || form.beginnerDashboard == null ||
                form.beginnerPanel.Height <= 0 || form.layoutRoot == null ||
                form.beginnerDashboard.Width <= 0 || form.beginnerDashboard.Height <= 0 ||
                form.beginnerPanel.Bottom > form.layoutRoot.ClientSize.Height + 1 ||
                form.beginnerDashboard.Bottom > form.beginnerPanel.ClientSize.Height + 1)
            {
                throw new InvalidOperationException(
                    "Beginner layout size invalid at " + stage +
                    ": panel=" + (form.beginnerPanel == null ? "null" : form.beginnerPanel.Size.ToString()) +
                    ", dashboard=" + (form.beginnerDashboard == null ? "null" : form.beginnerDashboard.Size.ToString()));
            }

            foreach (Control control in new Control[]
            {
                form.beginnerPrimaryValue,
                form.beginnerPrimaryInterfaceBox,
                form.beginnerPrimaryHealthValue,
                form.beginnerBackupValue,
                form.beginnerBackupInterfaceBox,
                form.beginnerBackupHealthValue,
                form.beginnerStartButton,
                form.beginnerDetectButton,
                form.beginnerRestoreButton,
                form.beginnerLogButton
            })
            {
                if (control == null || control.Width <= 0 || control.Height <= 0)
                {
                    throw new InvalidOperationException(stage + "新手儀表板控制項尺寸無效。 ");
                }
            }
        }

        private static void AssertHeaderVisible(MainForm form, string stage)
        {
            if (form.headerLayout == null || form.headerStatusLayout == null ||
                form.headerInfoLayout == null ||
                form.titleLabel == null || form.statusValue == null ||
                form.lastProbeValue == null || form.modeButton == null ||
                form.languageBox == null || form.updateLink == null)
            {
                throw new InvalidOperationException(stage + " header controls were not fully created.");
            }

            Rectangle headerBounds = new Rectangle(
                form.headerLayout.PointToScreen(Point.Empty), form.headerLayout.ClientSize);
            Rectangle beginnerBounds = new Rectangle(
                form.beginnerPanel.PointToScreen(Point.Empty), form.beginnerPanel.ClientSize);
            List<Control> controls = new List<Control>
            {
                form.brandImage,
                form.titleLabel,
                form.statusValue,
                form.modeButton,
                form.languageBox
            };
            if (form.lastProbeValue.Visible) { controls.Add(form.lastProbeValue); }
            foreach (Control control in controls)
            {
                Rectangle bounds = new Rectangle(control.PointToScreen(Point.Empty), control.ClientSize);
                if (!control.Visible || bounds.Width <= 0 || bounds.Height < 18 ||
                    !headerBounds.Contains(bounds))
                {
                    throw new InvalidOperationException(
                        "Header control clipped (" + control.GetType().Name + "): header=" +
                        headerBounds + ", control=" + bounds + ", visible=" + control.Visible);
                }
            }

            if (form.modeButton.Height < 24 || headerBounds.Bottom > beginnerBounds.Top)
            {
                throw new InvalidOperationException(stage + " header overlaps quick start or mode button is too short.");
            }
        }

        private static void AssertFooterVisible(MainForm form, string stage)
        {
            if (!form.footerLayout.Visible || form.footerLayout.Height < 40 ||
                form.footerLayout.Bottom > form.layoutShell.ClientSize.Height ||
                form.permissionPanel == null || form.permissionPanel.Parent != form.footerLayout ||
                form.permissionPanel.Width <= 0 || form.permissionPanel.Height <= 0 ||
                form.supportButton == null || !form.supportButton.Visible ||
                form.permissionActions == null || form.supportButton.Parent != form.permissionActions ||
                form.supportButton.Width <= 0 || form.supportButton.Height <= 0 ||
                form.supportButton.Right > form.permissionActions.ClientSize.Width ||
                form.supportButton.Bottom > form.permissionActions.ClientSize.Height)
            {
                throw new InvalidOperationException(stage + "固定底欄未完整顯示。");
            }

            if (!form.advancedDrawerOpen)
            {
                if (form.footerActionFlow == null || form.footerActionFlow.Visible ||
                    form.footerLayout.RowStyles[0].Height > 1)
                {
                    throw new InvalidOperationException(stage + "新手底欄仍顯示進階操作列。");
                }
                return;
            }

            Button[] buttons = new[] { form.startButton, form.stopButton, form.refreshButton,
                                      form.saveButton, form.exportButton };
            foreach (Button button in buttons)
            {
                if (button == null || !button.Visible || button.Width <= 0 || button.Height <= 0 ||
                    button.Right > form.footerActionFlow.ClientSize.Width ||
                    button.Bottom > form.footerActionFlow.ClientSize.Height)
                {
                    throw new InvalidOperationException(stage + "進階操作按鈕未完整顯示。");
                }
            }
        }

        private void LoadApplicationIcon()
        {
            try
            {
                appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                appIcon = null;
            }
            if (appIcon == null)
            {
                appIcon = (Icon)SystemIcons.Application.Clone();
            }

            Icon = appIcon;
            brandBitmap = appIcon.ToBitmap();
            brandImage.Image = brandBitmap;
        }

        private void PopulateInterfaces()
        {
            List<string> names = NetworkInfo.GetInterfaceNames();
            PopulateInterfaceBox(interfaceBox, names, settings.InterfaceName);
            PopulateInterfaceBox(primaryInterfaceBox, names, settings.PrimaryInterface);
            PopulateInterfaceBox(backupInterfaceBox, names, settings.BackupInterface);
            PopulateInterfaceBox(beginnerPrimaryInterfaceBox, names, settings.PrimaryInterface);
            PopulateInterfaceBox(beginnerBackupInterfaceBox, names, settings.BackupInterface);
        }

        private static void PopulateInterfaceBox(ComboBox box, List<string> names, string selected)
        {
            box.Items.Clear();
            foreach (string name in names)
            {
                box.Items.Add(name);
            }
            if (!string.IsNullOrWhiteSpace(selected) && names.Any(delegate(string name)
            {
                return string.Equals(name, selected, StringComparison.OrdinalIgnoreCase);
            }))
            {
                box.Text = selected;
            }
        }

        private void BuildLogMenu()
        {
            logMenu = new ContextMenuStrip();

            ToolStripMenuItem copyAllItem = new ToolStripMenuItem("複製全部紀錄");
            Localization.Mark(copyAllItem, "複製全部紀錄");
            copyAllItem.Click += delegate { CopyLog(); };
            logMenu.Items.Add(copyAllItem);

            ToolStripMenuItem clearItem = new ToolStripMenuItem("清除紀錄");
            Localization.Mark(clearItem, "清除紀錄");
            clearItem.Click += delegate { ClearLog(); };
            logMenu.Items.Add(clearItem);
            logMenu.Items.Add(new ToolStripSeparator());

            followLogItem = new ToolStripMenuItem("自動捲到最新")
            {
                CheckOnClick = true,
                Checked = followLog
            };
            Localization.Mark(followLogItem, "自動捲到最新");
            followLogItem.CheckedChanged += delegate
            {
                followLog = followLogItem.Checked;
                if (followLog) { ScrollLogToEnd(); }
            };
            logMenu.Items.Add(followLogItem);
            logBox.ContextMenuStrip = logMenu;
            uiToolTip.SetToolTip(logBox, "右鍵可複製或清除紀錄，也可暫停自動捲動。");
        }

        private void AttachLogToBeginnerDashboard()
        {
            if (beginnerDashboard == null || logGroup == null)
            {
                return;
            }
            if (logGroup.Parent != null)
            {
                logGroup.Parent.Controls.Remove(logGroup);
            }
            beginnerDashboard.RowCount = 4;
            while (beginnerDashboard.RowStyles.Count < 4)
            {
                beginnerDashboard.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            }
            beginnerDashboard.RowStyles[3].SizeType = SizeType.Absolute;
            beginnerDashboard.RowStyles[3].Height = 0F;
            logGroup.Dock = DockStyle.Fill;
            logGroup.Margin = new Padding(0, 8, 0, 0);
            logGroup.Height = BeginnerInlineLogHeight;
            beginnerDashboard.Controls.Add(logGroup, 0, 3);
            beginnerDashboard.SetColumnSpan(logGroup, 2);
            logGroup.Visible = false;
        }

        private void CopyLog()
        {
            if (logBox == null || string.IsNullOrEmpty(logBox.Text)) { return; }
            try
            {
                Clipboard.SetText(logBox.Text);
                uiToolTip.Show("已複製紀錄", logBox, 1000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L("無法複製紀錄。") + Environment.NewLine + ex.Message,
                                "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearLog()
        {
            if (logBox == null || visibleLog.Count == 0) { return; }
            DialogResult result = MessageBox.Show(
                this,
                L("確定要清除目前執行紀錄嗎？清除後仍可重新匯出之後的新紀錄。"),
                "NetOptimizer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) { return; }
            ClearLogContent();
        }

        private void ClearLogContent()
        {
            visibleLog.Clear();
            logBox.Clear();
            if (logEmptyLabel != null) { logEmptyLabel.Visible = true; }
        }

        private void ScrollLogToEnd()
        {
            if (logBox == null || logBox.IsDisposed) { return; }
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.ScrollToCaret();
        }

        private void UpdateInterfaceTooltip(ComboBox box)
        {
            if (box == null || uiToolTip == null) { return; }
            string name = (box.Text ?? string.Empty).Trim();
            if (name.Length == 0)
            {
                uiToolTip.SetToolTip(box, L("可直接輸入 Windows 網路介面名稱。"));
                return;
            }

            InterfaceSnapshot snapshot = NetworkInfo.GetInterfaceSnapshot(name);
            if (snapshot == null)
            {
                uiToolTip.SetToolTip(box, L("尚未取得這張介面的 IPv4 與 gateway。"));
                return;
            }

            uiToolTip.SetToolTip(
                box,
                L("狀態：") + snapshot.Status + Environment.NewLine +
                L("IPv4：") + (string.IsNullOrWhiteSpace(snapshot.IPv4) ? L("無") : snapshot.IPv4) + Environment.NewLine +
                L("Gateway：") + (string.IsNullOrWhiteSpace(snapshot.Gateway) ? L("無") : snapshot.Gateway) + Environment.NewLine +
                L("就緒：") + (snapshot.IsReady ? L("是") : L("否")));
        }

        private void ApplySettingsToUi()
        {
            // The quick-start surface is the permanent home screen. Advanced
            // controls open in a separate settings window.
            beginnerMode = true;
            advancedDrawerOpen = false;
            InterfaceSnapshot backupSnapshot = NetworkInfo.GetInterfaceSnapshot(settings.BackupInterface);
            bool backupReady = backupSnapshot != null && backupSnapshot.IsReady;
            bool failoverWasDisabled = settings.FailoverEnabled && !backupReady;
            bool failoverEnabled = settings.FailoverEnabled && backupReady;
            if (failoverWasDisabled)
            {
                settings.FailoverEnabled = false;
                settings.SmartSelectionEnabled = false;
            }
            bool startupEnabled = StartupManager.IsEnabled(Application.ExecutablePath);
            applyingStartupPreference = true;
            try
            {
                startWithWindowsBox.Checked = startupEnabled;
                trayStartup.Checked = startupEnabled;
            }
            finally
            {
                applyingStartupPreference = false;
            }
            settings.StartWithWindows = startupEnabled;
            interfaceBox.Text = settings.InterfaceName;
            primaryInterfaceBox.Text = settings.PrimaryInterface;
            backupInterfaceBox.Text = settings.BackupInterface;
            beginnerPrimaryInterfaceBox.Text = settings.PrimaryInterface;
            beginnerBackupInterfaceBox.Text = settings.BackupInterface;
            targetsBox.Text = string.Join(", ", settings.Targets);
            failoverTargetBox.Text = string.Join(", ", settings.FailoverTargets ?? new List<string> { settings.FailoverTarget });
            portBox.Value = settings.Port;
            thresholdBox.Value = settings.ThresholdMs;
            timeoutBox.Value = settings.ProbeTimeoutMs;
            failuresBox.Value = settings.ConsecutiveFailuresBeforeRefresh;
            cooldownBox.Value = settings.CooldownSeconds;
            failoverThresholdBox.Value = settings.FailoverThresholdMs;
            failoverTimeoutBox.Value = settings.FailoverPingTimeoutMs;
            failoverBadSamplesBox.Value = settings.FailoverBadSamples;
            failoverRecoveryBox.Value = settings.FailoverRecoverySeconds;
            failoverSwitchCooldownBox.Value = settings.FailoverSwitchCooldownSeconds;
            failoverSwitchBackoffBox.Value = settings.FailoverSwitchBackoffSeconds;
            failoverPrimaryMetricBox.Value = settings.FailoverPrimaryMetric;
            failoverBackupMetricBox.Value = settings.FailoverBackupMetric;
            smartDecisionIntervalBox.Value = settings.SmartDecisionIntervalSeconds;
            smartMinDwellBox.Value = settings.SmartMinDwellSeconds;
            smartSwitchHoldBox.Value = settings.SmartSwitchHoldSeconds;
            smartMarginBox.Value = settings.SmartMarginMs;
            enableRefreshBox.Checked = settings.EnableRefresh;
            failoverEnabledBox.Checked = failoverEnabled;
            smartSelectionBox.Checked = settings.SmartSelectionEnabled && failoverEnabled;
            suppressRefreshBox.Checked = settings.SuppressRefreshDuringFailover;
            dnsBox.Checked = settings.FlushDns;
            arpBox.Checked = settings.ClearArp;
            mtuBox.Checked = settings.PulseMtu;
            beginnerFailoverBox.Checked = failoverEnabled;
            beginnerAutoRepairBox.Checked = settings.EnableRefresh;
            UpdateActionEnabled();
            UpdateFailoverEnabled();
            UpdateBeginnerStatus();
            UpdateUiMode();
            if (failoverWasDisabled)
            {
                AppendLog(L("沒有偵測到就緒備援，自動切換已關閉。"), true);
            }
        }

        private void SyncBeginnerSettingsToAdvanced()
        {
            SyncBeginnerSettingsToAdvanced(false);
        }

        private void SyncBeginnerSettingsToAdvanced(bool force)
        {
            if (!beginnerMode || (!force && advancedDrawerOpen) || beginnerPrimaryInterfaceBox == null)
            {
                return;
            }
            interfaceBox.Text = beginnerPrimaryInterfaceBox.Text.Trim();
            primaryInterfaceBox.Text = beginnerPrimaryInterfaceBox.Text.Trim();
            backupInterfaceBox.Text = beginnerBackupInterfaceBox.Text.Trim();
            failoverEnabledBox.Checked = beginnerFailoverBox.Checked;
            enableRefreshBox.Checked = beginnerAutoRepairBox.Checked;
        }

        private void SyncAdvancedSettingsToBeginner()
        {
            if (beginnerPrimaryInterfaceBox == null) { return; }
            beginnerPrimaryInterfaceBox.Text = primaryInterfaceBox.Text;
            beginnerBackupInterfaceBox.Text = backupInterfaceBox.Text;
            beginnerFailoverBox.Checked = failoverEnabledBox.Checked;
            beginnerAutoRepairBox.Checked = enableRefreshBox.Checked;
        }

        private MonitorSettings ReadSettingsFromUi()
        {
            if (!settingsDialogOpen)
            {
                SyncBeginnerSettingsToAdvanced();
            }
            MonitorSettings next = settings.Clone();
            next.BeginnerMode = true;
            next.StartWithWindows = startWithWindowsBox != null && startWithWindowsBox.Checked;
            next.InterfaceName = interfaceBox.Text.Trim();
            next.PrimaryInterface = primaryInterfaceBox.Text.Trim();
            next.BackupInterface = backupInterfaceBox.Text.Trim();
            next.Targets = targetsBox.Text
                .Split(new[] { ',', ';', '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(delegate(string target) { return target.Trim(); })
                .Where(delegate(string target) { return target.Length > 0; })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            next.Port = Decimal.ToInt32(portBox.Value);
            next.ThresholdMs = Decimal.ToInt32(thresholdBox.Value);
            next.ProbeTimeoutMs = Decimal.ToInt32(timeoutBox.Value);
            next.ConsecutiveFailuresBeforeRefresh = Decimal.ToInt32(failuresBox.Value);
            next.CooldownSeconds = Decimal.ToInt32(cooldownBox.Value);
            next.FailoverEnabled = failoverEnabledBox.Checked;
            next.FailoverTargets = failoverTargetBox.Text
                .Split(new[] { ',', ';', '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(delegate(string target) { return target.Trim(); })
                .Where(delegate(string target) { return target.Length > 0; })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            next.FailoverTarget = next.FailoverTargets.Count == 0 ? string.Empty : next.FailoverTargets[0];
            next.FailoverThresholdMs = Decimal.ToInt32(failoverThresholdBox.Value);
            next.FailoverPingTimeoutMs = Decimal.ToInt32(failoverTimeoutBox.Value);
            next.FailoverBadSamples = Decimal.ToInt32(failoverBadSamplesBox.Value);
            next.FailoverRecoverySeconds = Decimal.ToInt32(failoverRecoveryBox.Value);
            next.FailoverSwitchCooldownSeconds = Decimal.ToInt32(failoverSwitchCooldownBox.Value);
            next.FailoverSwitchBackoffSeconds = Decimal.ToInt32(failoverSwitchBackoffBox.Value);
            next.FailoverPrimaryMetric = Decimal.ToInt32(failoverPrimaryMetricBox.Value);
            next.FailoverBackupMetric = Decimal.ToInt32(failoverBackupMetricBox.Value);
            next.SmartSelectionEnabled = smartSelectionBox.Checked && next.FailoverEnabled;
            next.SmartDecisionIntervalSeconds = Decimal.ToInt32(smartDecisionIntervalBox.Value);
            next.SmartMinDwellSeconds = Decimal.ToInt32(smartMinDwellBox.Value);
            next.SmartSwitchHoldSeconds = Decimal.ToInt32(smartSwitchHoldBox.Value);
            next.SmartMarginMs = Decimal.ToInt32(smartMarginBox.Value);
            next.SuppressRefreshDuringFailover = suppressRefreshBox.Checked;
            next.EnableRefresh = enableRefreshBox.Checked;
            next.FlushDns = dnsBox.Checked;
            next.ClearArp = arpBox.Checked;
            next.PulseMtu = mtuBox.Checked;
            if (next.InterfaceName.Length == 0)
            {
                throw new InvalidOperationException(L("請先選擇網卡。 "));
            }
            if (next.Targets.Count == 0)
            {
                throw new InvalidOperationException(L("請至少填寫一個測試目標。 "));
            }
            if (next.FailoverEnabled)
            {
                if (next.PrimaryInterface.Length == 0 || next.BackupInterface.Length == 0)
                {
                    throw new InvalidOperationException(L("啟用 A/B 切換時，請同時指定主線 A 與備援 B。 "));
                }
                if (string.Equals(next.PrimaryInterface, next.BackupInterface, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(L("主線 A 與備援 B 不能是同一張網卡。 "));
                }
                if (next.FailoverTargets.Count == 0)
                {
                    throw new InvalidOperationException(L("啟用 A/B 切換時，請至少填寫一個故障切換測試目標。 "));
                }
                if (next.FailoverPrimaryMetric >= next.FailoverBackupMetric)
                {
                    throw new InvalidOperationException(L("A metric 必須小於 B metric，才能讓 A/B 優先順序明確。 "));
                }
            }
            next.Normalize();
            return next;
        }

        private bool TrySaveSettings(bool showError)
        {
            try
            {
                settings = ReadSettingsFromUi();
                SettingsStore.Save(settings);
                AppendLog(L("設定已儲存：") + SettingsStore.SettingsPath, false);
                return true;
            }
            catch (Exception ex)
            {
                AppendLog(L("儲存設定失敗：") + ex.Message, true);
                if (showError)
                {
                    MessageBox.Show(this, ex.Message, "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
        }

        private void StartWithWindowsBox_CheckedChanged(object sender, EventArgs e)
        {
            if (applyingStartupPreference || startupConfigurationInProgress || closing ||
                settings == null || startWithWindowsBox == null)
            {
                return;
            }

            ApplyStartupPreference(startWithWindowsBox.Checked);
        }

        private void ApplyStartupPreference(bool enabled)
        {
            if (applyingStartupPreference || startupConfigurationInProgress || settings == null)
            {
                return;
            }

            string executablePath = Application.ExecutablePath;
            StartupRegistrationMode previousMode = StartupManager.GetMode(executablePath);
            bool useElevatedTask = (enabled && StartupManager.IsProtectedInstallPath(executablePath)) ||
                                   previousMode == StartupRegistrationMode.ElevatedTask;
            if (useElevatedTask && !NetworkInfo.IsAdministrator())
            {
                RequestElevatedStartupPreference(enabled, previousMode != StartupRegistrationMode.None);
                return;
            }

            try
            {
                if (useElevatedTask)
                {
                    bool previousTaskEnabled = previousMode == StartupRegistrationMode.ElevatedTask;
                    try
                    {
                        ElevatedStartupManager.SetEnabled(enabled, executablePath);
                        StartupManager.SetEnabled(false, executablePath);
                    }
                    catch
                    {
                        try
                        {
                            if (previousTaskEnabled)
                            {
                                ElevatedStartupManager.SetEnabled(true, executablePath);
                            }
                            else
                            {
                                ElevatedStartupManager.SetEnabled(false, executablePath);
                            }
                        }
                        catch { }
                        throw;
                    }
                }
                else
                {
                    StartupManager.SetEnabled(enabled, executablePath);
                }

                CommitStartupPreference(enabled);
            }
            catch (Exception ex)
            {
                TryRestoreStartupRegistration(previousMode, executablePath);
                RestoreStartupControls(previousMode != StartupRegistrationMode.None);
                ShowStartupFailure(ex.Message);
            }
        }

        private void RequestElevatedStartupPreference(bool enabled, bool previousEnabled)
        {
            startupConfigurationInProgress = true;
            UpdateButtons();
            AppendLog(L("正在要求系統管理員權限以設定開機自動保護。"), false);

            Process process;
            try
            {
                process = Process.Start(new ProcessStartInfo(Application.ExecutablePath)
                {
                    Arguments = "--elevated-startup=" + (enabled ? "enable" : "disable"),
                    UseShellExecute = true,
                    Verb = "runas"
                });
                if (process == null)
                {
                    throw new InvalidOperationException("Unable to start the elevated startup configurator.");
                }
            }
            catch (Win32Exception)
            {
                startupConfigurationInProgress = false;
                RestoreStartupControls(previousEnabled);
                UpdateButtons();
                AppendLog(L("使用者取消系統管理員權限，未變更開機自動啟動。"), true);
                return;
            }
            catch (Exception ex)
            {
                startupConfigurationInProgress = false;
                RestoreStartupControls(previousEnabled);
                UpdateButtons();
                ShowStartupFailure(ex.Message);
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                int exitCode = -1;
                Exception waitError = null;
                try
                {
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
                catch (Exception ex)
                {
                    waitError = ex;
                }
                finally
                {
                    process.Dispose();
                }

                RunOnUi(delegate
                {
                    CompleteElevatedStartupPreference(enabled, previousEnabled, exitCode, waitError);
                });
            });
        }

        private void CompleteElevatedStartupPreference(
            bool enabled,
            bool previousEnabled,
            int exitCode,
            Exception waitError)
        {
            startupConfigurationInProgress = false;
            bool actualEnabled = StartupManager.IsEnabled(Application.ExecutablePath);
            bool success = waitError == null && exitCode == 0 && actualEnabled == enabled;
            if (success)
            {
                try
                {
                    CommitStartupPreference(enabled);
                }
                catch (Exception ex)
                {
                    RestoreStartupControls(actualEnabled);
                    ShowStartupFailure(ex.Message);
                }
            }
            else
            {
                RestoreStartupControls(actualEnabled ? true : previousEnabled);
                string detail = waitError == null
                    ? "exit code " + exitCode
                    : waitError.Message;
                ShowStartupFailure(detail);
            }
            UpdateButtons();
        }

        private void CommitStartupPreference(bool enabled)
        {
            MonitorSettings next = settings.Clone();
            next.StartWithWindows = enabled;
            SettingsStore.Save(next);
            settings = next;

            RestoreStartupControls(enabled);
            StartupRegistrationMode mode = StartupManager.GetMode(Application.ExecutablePath);
            if (enabled && mode == StartupRegistrationMode.ElevatedTask)
            {
                AppendLog(L("已啟用安裝版高權限自動保護。"), false);
            }
            else
            {
                AppendLog(enabled ? L("已啟用開機自動啟動。") : L("已停用開機自動啟動。"), false);
            }
        }

        private void RestoreStartupControls(bool enabled)
        {
            applyingStartupPreference = true;
            try
            {
                if (startWithWindowsBox != null) { startWithWindowsBox.Checked = enabled; }
                if (trayStartup != null) { trayStartup.Checked = enabled; }
            }
            finally
            {
                applyingStartupPreference = false;
            }
        }

        private void TryRestoreStartupRegistration(
            StartupRegistrationMode previousMode,
            string executablePath)
        {
            try
            {
                if (previousMode == StartupRegistrationMode.ElevatedTask &&
                    NetworkInfo.IsAdministrator())
                {
                    ElevatedStartupManager.SetEnabled(true, executablePath);
                    StartupManager.SetEnabled(false, executablePath);
                }
                else if (previousMode == StartupRegistrationMode.CurrentUserRun)
                {
                    if (NetworkInfo.IsAdministrator() &&
                        StartupManager.IsProtectedInstallPath(executablePath))
                    {
                        ElevatedStartupManager.SetEnabled(false, executablePath);
                    }
                    StartupManager.SetEnabled(true, executablePath);
                }
                else if (previousMode == StartupRegistrationMode.None)
                {
                    if (NetworkInfo.IsAdministrator() &&
                        StartupManager.IsProtectedInstallPath(executablePath))
                    {
                        ElevatedStartupManager.SetEnabled(false, executablePath);
                    }
                    StartupManager.SetEnabled(false, executablePath);
                }
            }
            catch { }
        }

        private void ShowStartupFailure(string detail)
        {
            string message = L("開機自動啟動設定失敗：") + detail;
            AppendLog(message, true);
            MessageBox.Show(this, message, "NetOptimizer",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            TryStartMonitoring(true);
        }

        private bool TryStartMonitoring(bool showError)
        {
            if (failoverEnabledBox != null && failoverEnabledBox.Checked &&
                !NetworkInfo.IsAdministrator())
            {
                string message =
                    L("A/B 自動切換需要系統管理員權限，已阻止啟動。") +
                    L("請按「重新以管理員啟動」後再開始監測。");
                AppendLog(L("A/B 啟動已阻止：目前不是系統管理員。"), true);
                if (adminButton != null)
                {
                    adminButton.Visible = true;
                    adminButton.Enabled = true;
                    adminButton.Focus();
                }
                if (showError)
                {
                    MessageBox.Show(this, message, L("需要管理員權限"),
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }

            if (!TrySaveSettings(showError)) { return false; }
            if (settings.FailoverEnabled)
            {
                InterfaceSnapshot primarySnapshot =
                    NetworkInfo.GetInterfaceSnapshot(settings.PrimaryInterface);
                InterfaceSnapshot backupSnapshot =
                    NetworkInfo.GetInterfaceSnapshot(settings.BackupInterface);
                if (!FailoverManager.IsReadyPair(primarySnapshot, backupSnapshot))
                {
                    string detail = primarySnapshot == null || !primarySnapshot.IsReady
                        ? L("主要網路尚未就緒")
                        : (backupSnapshot == null || !backupSnapshot.IsReady
                            ? L("備援網路尚未就緒")
                            : L("主線與備援不能是同一張網卡"));
                    if (backupSnapshot == null || !backupSnapshot.IsReady)
                    {
                        DisableFailoverWithoutReadyBackup();
                        TrySaveSettings(false);
                    }
                    string message = L("A/B 自動切換已阻止：") + detail + Environment.NewLine +
                                     L("請重新偵測網路，確認 A 與 B 都有 IPv4 與 gateway 後再開始。 ");
                    AppendLog(L("A/B 啟動已阻止：") + detail + "。", true);
                    if (showError)
                    {
                        MessageBox.Show(this, message, L("網路尚未就緒"),
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return false;
                }
            }
            engine.Start(settings);
            monitoringEverStarted = true;
            statusValue.Text = L("狀態：") + L("監測中");
            statusValue.ForeColor = GetMonitorStatusColor();
            UpdateBeginnerStatus();
            UpdateButtons();
            return true;
        }

        private void StopMonitoring()
        {
            engine.Stop();
            if (statusValue != null)
            {
                statusValue.Text = L("狀態：") + L("已停止");
                statusValue.ForeColor = MutedText;
            }
            UpdateBeginnerStatus();
            UpdateButtons();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (!TrySaveSettings(true)) { return; }
            engine.RefreshNow(settings);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            TrySaveSettings(true);
        }

        private async void ExportButton_Click(object sender, EventArgs e)
        {
            if (exportingDiagnostics) { return; }
            if (!TrySaveSettings(true)) { return; }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = L("匯出 NetOptimizer 診斷報告");
                dialog.Filter = L("文字報告 (*.txt)|*.txt|所有檔案 (*.*)|*.*");
                dialog.FileName = "NetOptimizer-diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (dialog.ShowDialog(this) != DialogResult.OK) { return; }

                exportingDiagnostics = true;
                UpdateButtons();
                try
                {
                    await DiagnosticsExporter.ExportAsync(
                        dialog.FileName,
                        settings,
                        visibleLog.ToArray(),
                        latestFailoverStatus,
                        CancellationToken.None);
                    AppendLog(L("診斷報告已匯出：") + dialog.FileName, false);
                    MessageBox.Show(this, L("診斷報告已匯出。報告包含網卡、IP、route 與近期 log，請確認內容後再分享。"),
                                    "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AppendLog(L("匯出診斷失敗：") + ex.Message, true);
                    MessageBox.Show(this, ex.Message, "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    exportingDiagnostics = false;
                    UpdateButtons();
                }
            }
        }

        private void Engine_LogRaised(object sender, EngineLogEventArgs e)
        {
            RunOnUi(delegate { AppendLog(e.Message, e.Warning); });
        }

        private void Engine_ProbeCompleted(object sender, ProbeEventArgs e)
        {
            RunOnUi(delegate
            {
                ProbeResult result = e.Result;
                hasProbeResult = result != null;
                if (result != null)
                {
                    lastProbeTarget = result.Target ?? string.Empty;
                    lastProbePort = result.Port;
                    lastProbeLatencyMs = result.LatencyMs;
                    lastProbeState = result.State;
                    lastProbeIntervalMs = e.IntervalMs;
                    lastProbeHealthy = result.IsHealthy(settings.ThresholdMs);
                }
                string resultText;
                if (result.State == ProbeState.Success)
                {
                    resultText = result.LatencyMs + " ms";
                }
                else
                {
                    resultText = result.State == ProbeState.Timeout ? "timeout" : L("失敗");
                }
                lastProbeSummary = FormatLastProbeSummary();
                lastProbeValue.Text = L("最近探測：") + lastProbeSummary;
                statusValue.Text = L("狀態：") + L("監測中");
                statusValue.ForeColor = GetMonitorStatusColor();
                UpdateBeginnerStatus();
                if (uiToolTip != null)
                {
                    uiToolTip.SetToolTip(lastProbeValue, L("完整結果：") + lastProbeSummary);
                }
                if (!result.IsHealthy(settings.ThresholdMs) && result.State != ProbeState.Cancelled)
                {
                    AppendLog(L("探測異常：") + result.Target + " → " + resultText +
                              (string.IsNullOrWhiteSpace(result.Error) ? string.Empty : "（" + result.Error + "）"), true);
                }
            });
        }

        private void Engine_FailoverStatusChanged(object sender, FailoverStatusEventArgs e)
        {
            latestFailoverStatus = e.Status == null ? null : e.Status.Clone();
            RunOnUi(delegate
            {
                UpdateFailoverStatusDisplay();
                UpdateBeginnerStatus();
            });
        }

        private void UpdateFailoverStatusDisplay()
        {
            FailoverStatus status = latestFailoverStatus;
            if (status == null || !status.Ready)
            {
                failoverStatusValue.Text = L("A/B：未啟用或尚未就緒");
                failoverStatusValue.ForeColor = MutedText;
                failoverHealthValue.Text = L("健康度：尚未測試");
                failoverHealthValue.ForeColor = MutedText;
                if (uiToolTip != null)
                {
                    uiToolTip.SetToolTip(failoverStatusValue, failoverStatusValue.Text);
                    uiToolTip.SetToolTip(failoverHealthValue, failoverHealthValue.Text);
                }
                return;
            }
            string mode = status.InFailover ? L("故障切換中") :
                          (status.SmartSelection ? L("智慧選路") : L("主線優先"));
            failoverStatusValue.Text = L("目前 ") + status.ActiveInterface +
                                       L("／備援 ") + status.StandbyInterface + " · " + mode +
                                       L(" · 切換 ") + status.SwitchCount + " " + L("次");
            failoverStatusValue.ForeColor = status.InFailover ? Warning : Accent;
            failoverHealthValue.Text = L("A/B 健康度：") + status.ActiveHealth +
                                       " | " + status.StandbyHealth;
            failoverHealthValue.ForeColor = status.InFailover ? Warning : MutedText;
            if (uiToolTip != null)
            {
                uiToolTip.SetToolTip(failoverStatusValue, failoverStatusValue.Text);
                uiToolTip.SetToolTip(failoverHealthValue, failoverHealthValue.Text);
            }
        }

        private void UpdatePermissionText()
        {
            string permissionTip;
            if (NetworkInfo.IsAdministrator())
            {
                permissionValue.Text = L("權限：管理員");
                permissionValue.ForeColor = Accent;
                permissionTip = L("系統管理員；ARP／MTU／A-B metric 動作可正常嘗試。");
                if (adminButton != null)
                {
                    adminButton.Visible = false;
                    adminButton.Text = L("重新以管理員啟動");
                }
            }
            else
            {
                permissionValue.Text = L("權限：一般使用者");
                permissionValue.ForeColor = Warning;
                permissionTip = L("一般使用者；DNS 通常可執行，ARP／MTU／A-B metric 可能需要系統管理員。");
                if (adminButton != null)
                {
                    adminButton.Visible = true;
                    adminButton.Text = L("重新以管理員啟動");
                }
            }
            if (uiToolTip != null) { uiToolTip.SetToolTip(permissionValue, permissionTip); }
        }

        private void ToggleUiMode()
        {
            OpenAdvancedSettingsDialog();
        }

        private void OpenAdvancedSettingsDialog()
        {
            if (settingsDialog != null && !settingsDialog.IsDisposed)
            {
                settingsDialog.Activate();
                return;
            }
            if (advancedDrawer == null || contentViewport == null)
            {
                return;
            }

            MonitorSettings originalSettings = settings.Clone();
            SyncBeginnerSettingsToAdvanced(true);
            MoveAdvancedControlsToDrawer();
            advancedDrawerOpen = true;
            settingsDialogOpen = true;
            if (monitorGroup != null) { monitorGroup.Visible = true; }
            if (actionsGroup != null) { actionsGroup.Visible = true; }
            if (failoverGroup != null) { failoverGroup.Visible = true; }

            AdvancedSettingsDialog dialog = new AdvancedSettingsDialog();
            settingsDialog = dialog;
            dialog.UpdateTexts(L("進階設定"), L("儲存設定"), L("取消"));
            dialog.SaveRequested = delegate { return TrySaveSettings(true); };
            DialogResult result = DialogResult.None;
            try
            {
                if (advancedDrawer.Parent != null)
                {
                    advancedDrawer.Parent.Controls.Remove(advancedDrawer);
                }
                dialog.AttachContent(advancedDrawer);
                result = dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                AppendLog(L("進階設定視窗開啟失敗：") + ex.Message, true);
            }
            finally
            {
                dialog.DetachContent();
                if (advancedDrawer.Parent != contentViewport)
                {
                    contentViewport.Controls.Add(advancedDrawer);
                }
                advancedDrawer.Dock = DockStyle.Right;
                advancedDrawer.Width = 520;
                advancedDrawer.Visible = false;
                settingsDialog = null;
                settingsDialogOpen = false;
                advancedDrawerOpen = false;
                UpdateUiMode();
                dialog.Dispose();
            }

            if (result == DialogResult.OK)
            {
                SyncAdvancedSettingsToBeginner();
                UpdateBeginnerStatus();
            }
            else
            {
                settings = originalSettings;
                ApplySettingsToUi();
            }
        }

        private void UpdateUiMode()
        {
            if (modeButton != null)
            {
                modeButton.Text = L("顯示進階設定 ▸");
                if (uiToolTip != null)
                {
                    string modeTooltip = L("開啟進階設定視窗。");
                    uiToolTip.SetToolTip(modeButton, modeTooltip);
                    modeButton.AccessibleName = modeButton.Text;
                    modeButton.AccessibleDescription = modeTooltip;
                }
            }
            if (beginnerPanel != null) { beginnerPanel.Visible = true; }
            // Advanced controls remain outside the compact home layout and are
            // attached to the settings window only while it is open.
            MoveAdvancedControlsToDrawer();
            if (monitorGroup != null) { monitorGroup.Visible = false; }
            if (actionsGroup != null) { actionsGroup.Visible = false; }
            if (failoverGroup != null) { failoverGroup.Visible = false; }
            if (logGroup != null) { logGroup.Visible = beginnerLogExpanded; }
            if (lastProbeValue != null) { lastProbeValue.Visible = true; }
            UpdateUpdateUi();
            if (footerLayout != null) { footerLayout.Visible = true; }
            if (footerActionFlow != null) { footerActionFlow.Visible = false; }
            if (layoutShell != null && layoutShell.RowStyles.Count > 1)
            {
                RowStyle footerRow = layoutShell.RowStyles[1];
                footerRow.SizeType = SizeType.Absolute;
                footerRow.Height = 60F;
            }
            if (beginnerLogButton != null)
            {
                beginnerLogButton.Text = beginnerLogExpanded ? L("隱藏執行紀錄 ▴") : L("查看執行紀錄 ▸");
            }
            UpdateBeginnerLogLayout();
            if (layoutRoot != null)
            {
                layoutRoot.AutoSize = true;
                layoutRoot.Dock = DockStyle.Top;
                if (layoutRoot.RowStyles.Count > 1)
                {
                    RowStyle beginnerRow = layoutRoot.RowStyles[1];
                    beginnerRow.SizeType = SizeType.AutoSize;
                    beginnerRow.Height = 0F;
                }
                layoutRoot.PerformLayout();
                PerformLayout();
            }
            if (advancedDrawer != null && !settingsDialogOpen)
            {
                advancedDrawer.Visible = false;
            }
            ApplyResponsiveLayout();
            UpdateButtons();
        }

        private void UpdateBeginnerLogLayout()
        {
            if (beginnerDashboard == null || beginnerDashboard.RowStyles.Count < 4 ||
                logGroup == null)
            {
                return;
            }
            int rowHeight = beginnerLogExpanded ? BeginnerInlineLogRowHeight : 0;
            logGroup.Visible = beginnerLogExpanded;
            logGroup.Height = BeginnerInlineLogHeight;
            beginnerDashboard.RowStyles[3].SizeType = SizeType.Absolute;
            beginnerDashboard.RowStyles[3].Height = rowHeight;
            beginnerDashboard.Height = BeginnerDashboardBaseHeight + rowHeight;
            if (beginnerPanel != null)
            {
                beginnerPanel.Height = BeginnerPanelBaseHeight + rowHeight;
            }
            if (!IsHandleCreated || WindowState != FormWindowState.Normal)
            {
                return;
            }
            if (beginnerLogExpanded && !windowHeightExpandedForLog)
            {
                int maximumHeight = Math.Max(ClientSize.Height,
                    Screen.FromControl(this).WorkingArea.Height - 24);
                int desiredHeight = Math.Min(maximumHeight,
                    ClientSize.Height + BeginnerInlineLogRowHeight);
                if (desiredHeight > ClientSize.Height)
                {
                    windowHeightBeforeInlineLog = ClientSize.Height;
                    windowHeightExpandedForLog = true;
                    ClientSize = new Size(ClientSize.Width, desiredHeight);
                }
            }
            else if (!beginnerLogExpanded && windowHeightExpandedForLog)
            {
                int restoreHeight = Math.Max(400, windowHeightBeforeInlineLog);
                windowHeightExpandedForLog = false;
                ClientSize = new Size(ClientSize.Width, restoreHeight);
            }
        }

        private void MoveAdvancedControlsToDrawer()
        {
            if (advancedDrawerLayout == null || layoutRoot == null ||
                monitorGroup == null || actionsGroup == null || failoverGroup == null)
            {
                return;
            }
            if (monitorGroup.Parent == advancedDrawerLayout &&
                actionsGroup.Parent == advancedDrawerLayout &&
                failoverGroup.Parent == advancedDrawerLayout)
            {
                return;
            }

            Control[] groups = { monitorGroup, actionsGroup, failoverGroup };
            foreach (Control group in groups)
            {
                if (group.Parent != null) { group.Parent.Controls.Remove(group); }
                group.Dock = DockStyle.Top;
                group.Margin = new Padding(0, 0, 0, 8);
            }
            advancedDrawerLayout.Controls.Clear();
            advancedDrawerLayout.RowStyles.Clear();
            advancedDrawerLayout.RowCount = 0;
            foreach (Control group in groups)
            {
                int row = advancedDrawerLayout.RowCount++;
                advancedDrawerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                advancedDrawerLayout.Controls.Add(group, 0, row);
            }
            advancedDrawerLayout.PerformLayout();
        }

        private Color GetMonitorStatusColor()
        {
            if (!engine.IsRunning) { return MutedText; }
            if (!hasProbeResult) { return Warning; }
            return lastProbeHealthy ? Accent : Error;
        }

        private void UpdateBeginnerStatus()
        {
            string primary = beginnerPrimaryInterfaceBox == null ? string.Empty :
                             beginnerPrimaryInterfaceBox.Text.Trim();
            string backup = beginnerBackupInterfaceBox == null ? string.Empty :
                            beginnerBackupInterfaceBox.Text.Trim();
            bool failoverEnabled = beginnerFailoverBox != null && beginnerFailoverBox.Checked;

            bool failoverReady = latestFailoverStatus != null && latestFailoverStatus.Ready;
            bool failoverActive = latestFailoverStatus != null &&
                                  !string.IsNullOrWhiteSpace(latestFailoverStatus.ActiveInterface);
            string activeInterface = failoverActive
                ? (latestFailoverStatus.ActiveInterface ?? string.Empty).Trim()
                : string.Empty;
            List<InterfaceSnapshot> snapshots = NetworkInfo.GetInterfaceSnapshots();
            bool backupLineReady = backup.Length > 0 && snapshots.Any(delegate(InterfaceSnapshot item)
            {
                return item != null && item.IsReady &&
                       string.Equals(item.Name, backup, StringComparison.OrdinalIgnoreCase);
            });
            bool hasReadyBackupOption = snapshots.Any(delegate(InterfaceSnapshot item)
            {
                return item != null && item.IsReady &&
                       !string.Equals(item.Name, primary, StringComparison.OrdinalIgnoreCase);
            });
            bool backupHasReadyPath = backupLineReady || hasReadyBackupOption;
            bool backupIsActive = failoverActive && backupLineReady &&
                                  string.Equals(activeInterface, backup, StringComparison.OrdinalIgnoreCase);
            bool primaryHealthy = engine.IsRunning && hasProbeResult && lastProbeHealthy;
            bool primaryFailed = engine.IsRunning && hasProbeResult && !lastProbeHealthy;
            Color primaryColor = primaryFailed ? Error : (primaryHealthy ? Accent : MutedText);

            if (statusValue != null)
            {
                statusValue.ForeColor = GetMonitorStatusColor();
            }

            if (beginnerPrimaryGlyph != null)
            {
                beginnerPrimaryGlyph.GlyphKind = GetNetworkGlyphKind(primary);
                beginnerPrimaryGlyph.GlyphColor = primaryColor == MutedText ? Accent : primaryColor;
            }
            if (beginnerBackupGlyph != null)
            {
                beginnerBackupGlyph.GlyphKind = GetNetworkGlyphKind(backup);
                beginnerBackupGlyph.GlyphColor = !backupHasReadyPath ? DisabledGlyph :
                    (backupIsActive && latestFailoverStatus != null && latestFailoverStatus.InFailover
                        ? Warning : Color.FromArgb(60, 170, 255));
            }
            if (beginnerPrimarySignal != null)
            {
                beginnerPrimarySignal.Level = !hasProbeResult ? 0 :
                    (lastProbeHealthy ? 4 : 1);
                beginnerPrimarySignal.ActiveColor = primaryColor == MutedText ? Accent : primaryColor;
            }
            if (beginnerPrimaryStatePill != null)
            {
                beginnerPrimaryStatePill.Text = !engine.IsRunning ? L("未啟動") :
                    (!hasProbeResult ? L("等待監測") :
                     (lastProbeHealthy ? L("正常") : L("異常")));
                beginnerPrimaryStatePill.StateColor = primaryColor;
            }
            if (beginnerPrimaryValue != null)
            {
                beginnerPrimaryValue.Text = activeInterface.Length > 0
                    ? L("目前使用：") + activeInterface
                    : (primary.Length == 0 ? L("尚未選擇") : L("已選擇：") + primary);
            }
            if (beginnerBackupValue != null)
            {
                beginnerBackupValue.Text = !failoverEnabled ? L("未啟用") :
                    (!backupLineReady ? L("未設定") :
                     (backupIsActive ? L("目前使用：") + backup : L("待命：") + backup));
            }

            Color backupColor = MutedText;
            string backupState = !backupLineReady ? (!failoverEnabled ? L("未啟用") : L("未設定")) :
                (backupIsActive ? L("使用中") : L("待命"));
            if (backupIsActive)
            {
                backupColor = latestFailoverStatus != null && latestFailoverStatus.InFailover
                    ? Warning : Accent;
            }
            else if (failoverEnabled && backupLineReady)
            {
                backupColor = Warning;
            }
            if (beginnerBackupCard != null)
            {
                beginnerBackupCard.Enabled = backupHasReadyPath;
            }
            if (beginnerFailoverBox != null)
            {
                beginnerFailoverBox.Enabled = backupHasReadyPath && !restoringNetwork;
            }
            if (beginnerBackupInterfaceBox != null)
            {
                beginnerBackupInterfaceBox.Enabled = backupHasReadyPath;
            }
            if (beginnerBackupSignal != null)
            {
                beginnerBackupSignal.Level = failoverActive && backupLineReady ? 3 : 0;
                beginnerBackupSignal.ActiveColor = backupHasReadyPath && backupColor != MutedText
                    ? backupColor : DisabledGlyph;
            }
            if (beginnerBackupStatePill != null)
            {
                beginnerBackupStatePill.Text = backupState;
                beginnerBackupStatePill.StateColor = backupHasReadyPath ? backupColor : MutedText;
            }
            if (beginnerPrimaryCard != null)
            {
                beginnerPrimaryCard.BorderColor = primaryFailed ? Error :
                    (primaryHealthy ? Color.FromArgb(20, 166, 157) : Color.FromArgb(54, 91, 111));
            }
            if (beginnerBackupCard != null)
            {
                beginnerBackupCard.BorderColor = !backupHasReadyPath
                    ? Color.FromArgb(45, 59, 68)
                    : (backupIsActive ? Color.FromArgb(20, 166, 157) :
                       (failoverEnabled ? Color.FromArgb(178, 131, 35) :
                        Color.FromArgb(54, 91, 111)));
            }

            if (beginnerPrimaryHealthValue != null)
            {
                if (!hasProbeResult)
                {
                    beginnerPrimaryHealthValue.Text = L("尚未測試");
                }
                else if (lastProbeState == ProbeState.Success)
                {
                    beginnerPrimaryHealthValue.Text = L("最新延遲") + "  " + lastProbeLatencyMs + " ms";
                }
                else
                {
                    beginnerPrimaryHealthValue.Text = lastProbeState == ProbeState.Timeout
                        ? "timeout" : L("失敗");
                }
                beginnerPrimaryHealthValue.ForeColor = primaryColor;
            }

            if (beginnerBackupHealthValue != null)
            {
                string backupHealth = !backupHasReadyPath ?
                    (failoverEnabled ? L("未設定") : L("未啟用")) :
                    (!failoverEnabled ? L("未啟用") : L("等待監測"));
                if (failoverActive && backupLineReady)
                {
                    if (backupIsActive)
                    {
                        backupHealth = L("健康度：") + (latestFailoverStatus.ActiveHealth ?? L("尚未測試")) + L(" · 目前使用中");
                    }
                    else if (string.Equals(latestFailoverStatus.StandbyInterface, backup,
                                           StringComparison.OrdinalIgnoreCase))
                    {
                        backupHealth = L("健康度：") + (latestFailoverStatus.StandbyHealth ?? L("尚未測試")) + L(" · 待命");
                    }
                    else
                    {
                        backupHealth = L("健康度：尚未對應");
                    }
                }
                beginnerBackupHealthValue.Text = backupHealth;
                beginnerBackupHealthValue.ForeColor = backupHasReadyPath && backupColor != MutedText
                    ? backupColor : MutedText;
            }
        }

        private static NetworkGlyphKind GetNetworkGlyphKind(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                return NetworkGlyphKind.Unknown;
            }

            NetworkGlyphKind nameKind = ClassifyNetworkGlyph(
                NetworkInterfaceType.Unknown, interfaceName);
            if (nameKind != NetworkGlyphKind.Unknown)
            {
                return nameKind;
            }

            InterfaceSnapshot snapshot = NetworkInfo.GetInterfaceSnapshot(interfaceName);
            string snapshotName = snapshot == null ? string.Empty : snapshot.Name;
            string snapshotDescription = snapshot == null ? string.Empty : snapshot.Description;
            NetworkInterfaceType type = snapshot == null ? NetworkInterfaceType.Unknown : snapshot.Type;
            return ClassifyNetworkGlyph(type, interfaceName + " " + snapshotName + " " + snapshotDescription);
        }

        private static NetworkGlyphKind ClassifyNetworkGlyph(
            NetworkInterfaceType type,
            string interfaceText)
        {
            string typeName = type.ToString();
            string text = interfaceText ?? string.Empty;
            if (typeName.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("藍牙", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return NetworkGlyphKind.Bluetooth;
            }

            if (typeName.IndexOf("Ethernet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Ethernet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("乙太", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("以太", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("LAN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return NetworkGlyphKind.Ethernet;
            }

            if (type == NetworkInterfaceType.Wireless80211 ||
                text.IndexOf("Wi-Fi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("WiFi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Wireless", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("WLAN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("無線", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return NetworkGlyphKind.WiFi;
            }

            return NetworkGlyphKind.Unknown;
        }

        private void UpdateActionEnabled()
        {
            bool enabled = enableRefreshBox != null && enableRefreshBox.Checked;
            if (dnsBox != null) { dnsBox.Enabled = enabled; }
            if (arpBox != null) { arpBox.Enabled = enabled; }
            if (mtuBox != null) { mtuBox.Enabled = enabled; }
        }

        private void UpdateFailoverAdvanced()
        {
            if (failoverAdvancedPanel == null || failoverAdvancedButton == null) { return; }
            failoverAdvancedPanel.Visible = advancedExpanded;
            failoverAdvancedButton.Text = advancedExpanded ? L("進階設定 ▾") : L("進階設定 ▸");
        }

        private void UpdateFailoverEnabled()
        {
            bool enabled = failoverEnabledBox != null && failoverEnabledBox.Checked;
            if (primaryInterfaceBox != null) { primaryInterfaceBox.Enabled = enabled; }
            if (backupInterfaceBox != null) { backupInterfaceBox.Enabled = enabled; }
            if (failoverTargetBox != null) { failoverTargetBox.Enabled = enabled; }
            if (failoverThresholdBox != null) { failoverThresholdBox.Enabled = enabled; }
            if (failoverTimeoutBox != null) { failoverTimeoutBox.Enabled = enabled; }
            if (failoverBadSamplesBox != null) { failoverBadSamplesBox.Enabled = enabled; }
            if (failoverRecoveryBox != null) { failoverRecoveryBox.Enabled = enabled; }
            if (failoverSwitchCooldownBox != null) { failoverSwitchCooldownBox.Enabled = enabled; }
            if (failoverPrimaryMetricBox != null) { failoverPrimaryMetricBox.Enabled = enabled; }
            if (failoverBackupMetricBox != null) { failoverBackupMetricBox.Enabled = enabled; }
            if (smartSelectionBox != null) { smartSelectionBox.Enabled = enabled; }
            if (suppressRefreshBox != null) { suppressRefreshBox.Enabled = enabled; }
            if (smartDecisionIntervalBox != null) { smartDecisionIntervalBox.Enabled = enabled; }
            if (smartMinDwellBox != null) { smartMinDwellBox.Enabled = enabled; }
            if (smartSwitchHoldBox != null) { smartSwitchHoldBox.Enabled = enabled; }
            if (smartMarginBox != null) { smartMarginBox.Enabled = enabled; }
        }

        private void UpdateButtons()
        {
            bool running = engine.IsRunning;
            if (startButton != null)
            {
                startButton.Visible = advancedDrawerOpen;
                startButton.Enabled = !running;
            }
            if (stopButton != null)
            {
                stopButton.Visible = advancedDrawerOpen;
                stopButton.Enabled = running;
            }
            if (refreshButton != null) { refreshButton.Visible = advancedDrawerOpen; }
            if (saveButton != null) { saveButton.Visible = advancedDrawerOpen; }
            if (exportButton != null) { exportButton.Visible = advancedDrawerOpen; }
            if (beginnerStartButton != null)
            {
                beginnerStartButton.Visible = true;
                beginnerStartButton.Text = running ? L("停止自動保護") : L("開始自動保護");
                beginnerStartButton.Enabled = !restoringNetwork && !exportingDiagnostics;
                ModernButton modernBeginnerStart = beginnerStartButton as ModernButton;
                if (modernBeginnerStart != null)
                {
                    modernBeginnerStart.Glyph = "shield";
                }
            }
            if (beginnerDetectButton != null)
            {
                beginnerDetectButton.Enabled = !restoringNetwork;
            }
            if (beginnerRestoreButton != null)
            {
                beginnerRestoreButton.Enabled = !running && !restoringNetwork;
            }
            if (trayStart != null) { trayStart.Enabled = !running; }
            if (trayStop != null) { trayStop.Enabled = running; }
            if (trayAdmin != null) { trayAdmin.Enabled = !NetworkInfo.IsAdministrator(); }
            if (trayStartup != null && startWithWindowsBox != null &&
                !applyingStartupPreference)
            {
                trayStartup.Checked = startWithWindowsBox.Checked;
            }
            if (startWithWindowsBox != null) { startWithWindowsBox.Enabled = !startupConfigurationInProgress; }
            if (trayStartup != null) { trayStartup.Enabled = !startupConfigurationInProgress; }
            if (adminButton != null) { adminButton.Enabled = !NetworkInfo.IsAdministrator(); }
            if (exportButton != null) { exportButton.Enabled = !exportingDiagnostics; }
            UpdateUpdateUi();
        }

        private void AppendLog(string message, bool warning)
        {
            if (logBox == null || logBox.IsDisposed) { return; }
            while (visibleLog.Count >= 240)
            {
                visibleLog.Dequeue();
            }
            visibleLog.Enqueue(message);
            logBox.Lines = visibleLog.ToArray();
            if (logEmptyLabel != null) { logEmptyLabel.Visible = false; }
            if (followLog) { ScrollLogToEnd(); }
        }

        private void RunOnUi(Action action)
        {
            if (closing || IsDisposed || !IsHandleCreated) { return; }
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void MainForm_CheckUpdatesOnShown(object sender, EventArgs e)
        {
            if (!updateChecksEnabled || closing || IsDisposed) { return; }
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (!closing && !IsDisposed)
                    {
                        CheckForUpdates(false);
                    }
                });
            }
            catch (InvalidOperationException) { }
        }

        private async void CheckForUpdates(bool force)
        {
            if (!updateChecksEnabled || closing || IsDisposed || updateCheckInProgress)
            {
                return;
            }
            if (!UpdateChecker.ShouldCheck(updateState, force, DateTime.UtcNow))
            {
                return;
            }

            updateCheckInProgress = true;
            UpdateUpdateUi();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            updateCheckCancellation = cancellation;
            UpdateCheckResult result = null;
            try
            {
                result = await UpdateChecker.CheckAsync(
                    typeof(MainForm).Assembly.GetName().Version,
                    cancellation.Token);
            }
            catch (Exception ex)
            {
                result = UpdateCheckResult.Failure(ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                updateCheckInProgress = false;
                if (ReferenceEquals(updateCheckCancellation, cancellation))
                {
                    updateCheckCancellation = null;
                }
                cancellation.Dispose();
            }

            if (closing || IsDisposed || result == null)
            {
                return;
            }
            if (!result.Succeeded)
            {
                if (force && !string.Equals(result.Error, "Update check cancelled.", StringComparison.Ordinal))
                {
                    AppendLog(L("更新檢查失敗：") + result.Error, true);
                    if (tray != null)
                    {
                        tray.ShowBalloonTip(
                            3500,
                            L("檢查更新失敗"),
                            L("請稍後再試。"),
                            ToolTipIcon.Warning);
                    }
                }
                UpdateUpdateUi();
                return;
            }

            updateState.LastSuccessfulCheckUtc = DateTime.UtcNow;
            availableUpdate = result.IsUpdateAvailable && result.Update != null
                ? result.Update
                : null;
            if (availableUpdate != null &&
                string.Equals(updateState.IgnoredVersion, availableUpdate.VersionKey, StringComparison.OrdinalIgnoreCase))
            {
                availableUpdate = null;
            }

            bool notify = availableUpdate != null &&
                          !string.Equals(
                              updateState.LastNotifiedVersion,
                              availableUpdate.VersionKey,
                              StringComparison.OrdinalIgnoreCase);
            if (notify)
            {
                updateState.LastNotifiedVersion = availableUpdate.VersionKey;
            }
            SaveUpdateState(true);
            UpdateUpdateUi();

            if (notify && availableUpdate != null && tray != null)
            {
                tray.ShowBalloonTip(
                    5000,
                    L("有新版本"),
                    L("發現 ") + availableUpdate.DisplayVersion + L("，點擊查看更新。"),
                    ToolTipIcon.Info);
            }
            else if (force && availableUpdate == null)
            {
                AppendLog(L("目前已是最新版本。"), false);
                if (tray != null)
                {
                    tray.ShowBalloonTip(
                        2500,
                        L("檢查更新"),
                        L("目前已是最新版本。"),
                        ToolTipIcon.Info);
                }
            }
        }

        private void UpdateUpdateUi()
        {
            if (updateLink != null)
            {
                bool visible = availableUpdate != null;
                bool infoVisible = visible || (lastProbeValue != null && lastProbeValue.Visible);
                if (headerInfoLayout != null && headerInfoLayout.ColumnStyles.Count > 1)
                {
                    headerInfoLayout.ColumnStyles[1].Width = visible ? 92F : 0F;
                    headerInfoLayout.Visible = infoVisible;
                    if (headerStatusLayout != null && headerStatusLayout.RowStyles.Count > 1)
                    {
                        headerStatusLayout.RowStyles[1].Height = infoVisible ? 20F : 0F;
                    }
                }
                updateLink.Visible = visible;
                updateLink.Enabled = visible;
                updateLink.Text = visible
                    ? L("更新") + " " + availableUpdate.DisplayVersion
                    : L("檢查更新");
            }
            if (trayCheckUpdates != null)
            {
                trayCheckUpdates.Text = updateCheckInProgress
                    ? L("檢查更新中…")
                    : L("檢查更新");
                trayCheckUpdates.Enabled = !updateCheckInProgress;
            }
            if (trayUpdate != null)
            {
                trayUpdate.Visible = availableUpdate != null;
                trayUpdate.Enabled = availableUpdate != null;
                trayUpdate.Text = availableUpdate == null
                    ? L("查看更新")
                    : L("查看更新") + " " + availableUpdate.DisplayVersion;
            }
            if (trayIgnoreUpdate != null)
            {
                trayIgnoreUpdate.Visible = availableUpdate != null;
                trayIgnoreUpdate.Enabled = availableUpdate != null;
            }
        }

        private bool SaveUpdateState(bool logFailure)
        {
            try
            {
                UpdateStateStore.Save(updateState);
                return true;
            }
            catch (Exception ex)
            {
                if (logFailure)
                {
                    AppendLog(L("更新狀態儲存失敗：") + ex.Message, true);
                }
                return false;
            }
        }

        private void OpenAvailableUpdate()
        {
            if (availableUpdate == null || string.IsNullOrWhiteSpace(availableUpdate.ReleaseUrl))
            {
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(availableUpdate.ReleaseUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppendLog(L("無法開啟更新頁面：") + ex.Message, true);
            }
        }

        private void IgnoreAvailableUpdate()
        {
            if (availableUpdate == null) { return; }
            updateState.IgnoredVersion = availableUpdate.VersionKey;
            string version = availableUpdate.DisplayVersion;
            availableUpdate = null;
            SaveUpdateState(true);
            UpdateUpdateUi();
            AppendLog(L("已忽略版本：") + version, false);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            ApplyWindowShape();
            LayoutCustomChrome();
            ApplyResponsiveLayout();
            UpdateChromeButtons();
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                Hide();
                string notice = engine.IsRunning
                    ? L("監測仍在背景執行。")
                    : L("程式已縮到系統匣。");
                tray.ShowBalloonTip(1200, "NetOptimizer", notice, ToolTipIcon.Info);
            }
        }

        private void MainForm_StartupShown(object sender, EventArgs e)
        {
            if (closing || IsDisposed)
            {
                return;
            }

            BeginInvoke((MethodInvoker)delegate
            {
                if (!closing && !IsDisposed)
                {
                    if (autoStartMonitoring)
                    {
                        TryStartMonitoring(false);
                    }
                    ShowInTaskbar = false;
                    WindowState = FormWindowState.Minimized;
                    Hide();
                }
            });
        }

        private void ShowFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            closing = true;
            if (settingsDialog != null && !settingsDialog.IsDisposed)
            {
                settingsDialog.Close();
            }
            if (updateCheckCancellation != null)
            {
                try { updateCheckCancellation.Cancel(); } catch { }
            }
            TrySaveSettings(false);
            if (tray != null) { tray.Visible = false; }
            engine.Stop();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (tray != null)
            {
                tray.Dispose();
                tray = null;
            }
            engine.Dispose();
        }

        private static Panel BeginnerCard(Padding margin)
        {
            return new ModernCard
            {
                BackColor = PanelBackground,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Margin = margin,
                Padding = new Padding(12, 10, 12, 10),
                BorderColor = Color.FromArgb(54, 91, 111),
                CornerRadius = 10
            };
        }

        private static TableLayoutPanel CardHeaderGrid()
        {
            TableLayoutPanel grid = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = PanelBackground
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return grid;
        }

        private static TableLayoutPanel CardIdentityGrid()
        {
            TableLayoutPanel grid = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = PanelBackground
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            return grid;
        }

        private static TableLayoutPanel BeginnerHealthGrid(Control signal, Control value)
        {
            TableLayoutPanel grid = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 42,
                MinimumSize = new Size(0, 42),
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = PanelBackground
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            grid.Controls.Add(signal, 0, 0);
            grid.Controls.Add(new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(63, 91, 108),
                Margin = new Padding(0, 8, 0, 8)
            }, 1, 0);
            value.Margin = new Padding(12, 0, 0, 0);
            grid.Controls.Add(value, 2, 0);
            return grid;
        }

        private static Panel SeparatorPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(48, 76, 92),
                Margin = new Padding(0)
            };
        }

        private static StatePill StatePillOf(string text, Color color)
        {
            return new StatePill
            {
                Text = text,
                StateColor = color,
                Font = new Font("Microsoft JhengHei UI", 8.5F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 10, 0, 10)
            };
        }

        private static TableLayoutPanel BeginnerCardGrid(int columns)
        {
            TableLayoutPanel grid = new TableLayoutPanel
            {
                ColumnCount = columns,
                RowCount = 0,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = PanelBackground
            };
            float width = 100F / Math.Max(1, columns);
            for (int i = 0; i < columns; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
            }
            return grid;
        }

        private static Label BeginnerCardTitle(string text)
        {
            Label label = LabelOf(text, 0, 0, 0, 20, 9.5F, MutedText,
                                  FontStyle.Bold, ContentAlignment.MiddleLeft);
            label.Dock = DockStyle.Fill;
            label.AutoEllipsis = true;
            return label;
        }

        private static GroupBox AutoGroupOf(string text)
        {
            GroupBox group = new ModernGroupBox
            {
                Text = text,
                ForeColor = TextColor,
                BackColor = PanelBackground,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 28, 14, 12),
                Margin = new Padding(0, 0, 0, 8)
            };
            Localization.Mark(group, text);
            return group;
        }

        private static TableLayoutPanel GridOf(int columns)
        {
            TableLayoutPanel grid = new TableLayoutPanel
            {
                ColumnCount = columns,
                RowCount = 0,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            float width = 100F / Math.Max(1, columns);
            for (int i = 0; i < columns; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
            }
            return grid;
        }

        private static int AddGridRow(TableLayoutPanel grid)
        {
            int row = grid.RowCount;
            grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return row;
        }

        private static FlowLayoutPanel FlowOf()
        {
            return new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = PanelBackground,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private static TableLayoutPanel FieldOf(string caption, Control editor, string unit)
        {
            int columns = string.IsNullOrWhiteSpace(unit) ? 2 : 3;
            TableLayoutPanel field = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                ColumnCount = columns,
                RowCount = 1,
                Margin = new Padding(0, 0, 10, 6),
                Padding = new Padding(0)
            };
            field.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            if (columns == 3)
            {
                field.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }

            Label label = CaptionOf(caption);
            field.Controls.Add(label, 0, 0);
            editor.Dock = DockStyle.Fill;
            editor.Margin = new Padding(0, 0, 5, 0);
            field.Controls.Add(editor, 1, 0);
            if (columns == 3)
            {
                Label unitLabel = CaptionOf(unit);
                unitLabel.Margin = new Padding(0, 0, 4, 0);
                field.Controls.Add(unitLabel, 2, 0);
            }
            return field;
        }

        private static Label CaptionOf(string text)
        {
            Label label = new Label
            {
                Text = text,
                AutoSize = true,
                Height = 27,
                ForeColor = MutedText,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 6, 0)
            };
            Localization.Mark(label, text);
            return label;
        }

        private static TextBox TextBoxEditor()
        {
            return new TextBox
            {
                BackColor = Color.FromArgb(17, 18, 20),
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Height = 27,
                MinimumSize = new Size(0, 27)
            };
        }

        private static CheckBox CheckBoxText(string text, bool value)
        {
            CheckBox box = new ModernCheckBox
            {
                Text = text,
                Checked = value,
                AutoSize = true,
                Height = 27,
                ForeColor = TextColor,
                BackColor = PanelBackground,
                Margin = new Padding(0, 0, 18, 4)
            };
            Localization.Mark(box, text);
            return box;
        }

        private static NumericUpDown NumberBox(int minimum, int maximum, int value)
        {
            NumericUpDown box = new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = Math.Max(minimum, Math.Min(maximum, value)),
                Increment = 1,
                BackColor = Color.FromArgb(17, 18, 20),
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Right,
                Width = 76,
                Height = 27,
                MinimumSize = new Size(64, 27)
            };
            return box;
        }

        private static ComboBox InterfaceBox()
        {
            return new ComboBox
            {
                BackColor = Color.FromArgb(17, 18, 20),
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                DropDownWidth = 360,
                Height = 27,
                MinimumSize = new Size(0, 27)
            };
        }

        private static ComboBox BeginnerInterfaceBox()
        {
            return new ModernComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(17, 18, 20),
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                DropDownWidth = 360,
                Height = 27,
                MinimumSize = new Size(0, 27)
            };
        }

        private static Button ButtonOf(string text, int x, int y, int width, int height, bool accent)
        {
            Button button = new ModernButton
            {
                Text = text,
                BackColor = accent ? Color.FromArgb(19, 39, 35) : Color.FromArgb(20, 22, 25),
                ForeColor = accent ? Accent : TextColor,
            };
            ModernButton modernButton = button as ModernButton;
            if (modernButton != null)
            {
                modernButton.Accent = accent;
                modernButton.AccentBorder = accent;
            }
            button.SetBounds(x, y, width, height);
            Localization.Mark(button, text);
            return button;
        }

        private static Button ChromeButtonOf(string text, bool close)
        {
            Button button = new Button
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = Background,
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Symbol", close ? 15F : 11F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Padding = new Padding(0),
                TabStop = false,
                UseCompatibleTextRendering = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = close
                ? Color.FromArgb(150, 48, 58)
                : Color.FromArgb(25, 44, 55);
            button.FlatAppearance.MouseDownBackColor = close
                ? Color.FromArgb(185, 55, 64)
                : Color.FromArgb(31, 56, 68);
            return button;
        }

        private static Label LabelOf(
            string text,
            int x,
            int y,
            int width,
            int height,
            float size,
            Color color,
            FontStyle style = FontStyle.Regular,
            ContentAlignment alignment = ContentAlignment.MiddleLeft)
        {
            Label label = new Label
            {
                Text = text,
                ForeColor = color,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft JhengHei UI", size, style),
                AutoSize = false,
                TextAlign = alignment
            };
            label.SetBounds(x, y, width, height);
            Localization.Mark(label, text);
            return label;
        }

        private void OpenSupportDialog()
        {
            using (SupportDialog dialog = new SupportDialog(currentLanguage))
            {
                dialog.ShowDialog(this);
            }
        }

        private void RestartAsAdministrator()
        {
            try
            {
                using (Process process = Process.Start(new ProcessStartInfo(Application.ExecutablePath)
                {
                    Arguments = "--restart-as-admin",
                    UseShellExecute = true,
                    Verb = "runas"
                }))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("無法建立管理員重新啟動程序。");
                    }
                }
                Close();
            }
            catch (Win32Exception)
            {
                AppendLog(L("使用者取消系統管理員重新啟動。"), true);
            }
            catch (Exception ex)
            {
                AppendLog(L("無法以系統管理員重新啟動：") + ex.Message, true);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (updateCheckCancellation != null)
                {
                    try { updateCheckCancellation.Cancel(); } catch { }
                    updateCheckCancellation.Dispose();
                    updateCheckCancellation = null;
                }
                engine.LogRaised -= Engine_LogRaised;
                engine.ProbeCompleted -= Engine_ProbeCompleted;
                engine.FailoverStatusChanged -= Engine_FailoverStatusChanged;
                engine.Dispose();
                if (tray != null)
                {
                    tray.Visible = false;
                    tray.Icon = null;
                    tray.Dispose();
                    tray = null;
                }
                if (trayIcon != null)
                {
                    trayIcon.Dispose();
                    trayIcon = null;
                }
                if (trayMenu != null)
                {
                    trayMenu.Dispose();
                    trayMenu = null;
                }
                if (logMenu != null)
                {
                    logMenu.Dispose();
                    logMenu = null;
                }
                if (uiToolTip != null)
                {
                    uiToolTip.Dispose();
                    uiToolTip = null;
                }
                if (brandImage != null)
                {
                    brandImage.Image = null;
                    brandImage = null;
                }
                if (brandBitmap != null)
                {
                    brandBitmap.Dispose();
                    brandBitmap = null;
                }
                if (appIcon != null)
                {
                    Icon = null;
                    appIcon.Dispose();
                    appIcon = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
