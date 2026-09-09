using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetOptimizerV2
{
    internal sealed class MainForm : Form
    {
        private static readonly Color Background = Color.FromArgb(20, 21, 23);
        private static readonly Color PanelBackground = Color.FromArgb(27, 29, 33);
        private static readonly Color TextColor = Color.FromArgb(238, 240, 242);
        private static readonly Color MutedText = Color.FromArgb(164, 169, 178);
        private static readonly Color Accent = Color.FromArgb(79, 214, 163);
        private static readonly Color Warning = Color.FromArgb(238, 181, 43);

        private readonly MonitorEngine engine = new MonitorEngine();
        private readonly Queue<string> visibleLog = new Queue<string>();
        private MonitorSettings settings;
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
        private Label permissionValue;
        private Label failoverStatusValue;
        private Label failoverHealthValue;
        private Label beginnerPrimaryValue;
        private Label beginnerPrimaryHealthValue;
        private Label beginnerBackupValue;
        private Label beginnerBackupHealthValue;
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
        private Button failoverAdvancedButton;
        private Panel failoverAdvancedPanel;
        private GroupBox beginnerPanel;
        private GroupBox monitorGroup;
        private GroupBox actionsGroup;
        private GroupBox failoverGroup;
        private TableLayoutPanel permissionPanel;
        private GroupBox logGroup;
        private TableLayoutPanel layoutRoot;
        private TableLayoutPanel layoutShell;
        private Panel contentViewport;
        private TableLayoutPanel headerLayout;
        private TableLayoutPanel headerStatusLayout;
        private TableLayoutPanel footerLayout;
        private FlowLayoutPanel permissionActions;
        private TableLayoutPanel beginnerDashboard;
        private ContextMenuStrip logMenu;
        private ToolStripMenuItem followLogItem;
        private ToolTip uiToolTip;
        private NotifyIcon tray;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem trayStart;
        private ToolStripMenuItem trayStop;
        private ToolStripMenuItem trayAdmin;
        private FailoverStatus latestFailoverStatus;
        private bool exportingDiagnostics;
        private bool followLog = true;
        private bool advancedExpanded;
        private bool beginnerMode = true;
        private bool beginnerLogExpanded;
        private bool restoringNetwork;
        private ComboBox beginnerPrimaryInterfaceBox;
        private ComboBox beginnerBackupInterfaceBox;
        private CheckBox beginnerFailoverBox;
        private CheckBox beginnerAutoRepairBox;

        private sealed class AutoDetectSelection
        {
            public List<InterfaceSnapshot> Ready;
            public InterfaceSnapshot Primary;
            public InterfaceSnapshot Backup;
        }

        public MainForm()
        {
            string warning;
            settings = SettingsStore.Load(out warning);
            RecoveryReport recovery = null;
            try
            {
                recovery = FailoverManager.RecoverPendingAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                recovery = new RecoveryReport();
                recovery.Messages.Add("A/B recovery 檢查失敗：" + ex.Message);
            }
            BuildUi();
            PopulateInterfaces();
            ApplySettingsToUi();
            engine.LogRaised += Engine_LogRaised;
            engine.ProbeCompleted += Engine_ProbeCompleted;
            engine.FailoverStatusChanged += Engine_FailoverStatusChanged;

            if (!string.IsNullOrWhiteSpace(warning))
            {
                AppendLog(warning, true);
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
        }

        private void BuildUi()
        {
            Text = "NetOptimizer";
            BackColor = Background;
            ForeColor = TextColor;
            Font = new Font("Microsoft JhengHei UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            ShowIcon = true;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 440);
            MinimumSize = new Size(760, 420);
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
                Dock = DockStyle.Fill,
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
                Padding = new Padding(12),
                Margin = new Padding(0)
            };
            layoutShell.Controls.Add(contentViewport, 0, 0);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Background,
                ColumnCount = 1,
                RowCount = 7,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            layoutRoot = mainLayout;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentViewport.Controls.Add(mainLayout);

            headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 8)
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290F));
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

            titleLabel = LabelOf("NetOptimizer", 0, 0, 0, 0, 21F, TextColor, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Margin = new Padding(4, 0, 0, 0);
            headerLayout.Controls.Add(titleLabel, 1, 0);

            headerStatusLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            headerStatusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            headerStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            headerStatusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));

            statusValue = LabelOf("狀態：未啟動", 0, 0, 0, 0, 11F, MutedText,
                                 FontStyle.Bold, ContentAlignment.MiddleRight);
            statusValue.Dock = DockStyle.Fill;
            statusValue.AutoEllipsis = true;
            lastProbeValue = LabelOf("最近探測：尚未測試", 0, 0, 0, 0, 8.5F, MutedText,
                                     FontStyle.Regular, ContentAlignment.MiddleRight);
            lastProbeValue.Dock = DockStyle.Fill;
            lastProbeValue.AutoEllipsis = true;
            headerStatusLayout.Controls.Add(statusValue, 0, 0);
            headerStatusLayout.Controls.Add(lastProbeValue, 0, 1);
            modeButton = ButtonOf("進階設定 ▸", 0, 0, 128, 24, false);
            modeButton.Dock = DockStyle.Fill;
            modeButton.Margin = new Padding(0, 2, 0, 0);
            modeButton.Click += delegate { ToggleUiMode(); };
            headerStatusLayout.Controls.Add(modeButton, 0, 2);
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

            failoverAdvancedButton = new Button
            {
                Text = "進階設定 ▸",
                FlatStyle = FlatStyle.Flat,
                BackColor = PanelBackground,
                ForeColor = MutedText,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 0)
            };
            failoverAdvancedButton.FlatAppearance.BorderColor = Color.FromArgb(59, 64, 71);
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
            supportButton = ButtonOf("☕ 支持開發", 0, 0, 128, 28, true);
            supportButton.Dock = DockStyle.None;
            supportButton.Margin = new Padding(8, 0, 0, 0);
            supportButton.Click += delegate { OpenSupportDialog(); };
            permissionActions.Controls.Add(supportButton);
            adminButton = ButtonOf("重新以管理員啟動", 0, 0, 180, 28, false);
            adminButton.Dock = DockStyle.None;
            adminButton.Margin = new Padding(8, 0, 0, 0);
            adminButton.Click += delegate { RestartAsAdministrator(); };
            permissionActions.Controls.Add(adminButton);
            permissionPanel.Controls.Add(permissionActions, 1, row);
            mainLayout.Controls.Add(permissionPanel, 0, 5);
            uiToolTip.SetToolTip(adminButton, "重新啟動並要求系統管理員權限；不會自動提權。");
            uiToolTip.SetToolTip(supportButton, "開啟支持開發選項：Ko-fi 與加密貨幣地址。");

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
            logSurface.Controls.Add(logBox);
            logSurface.Controls.Add(logEmptyLabel);
            logGroup.Controls.Add(logSurface);
            mainLayout.Controls.Add(logGroup, 0, 6);
            BuildLogMenu();

            TableLayoutPanel footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ColumnCount = 1,
                RowCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(12, 6, 12, 10),
                BackColor = Background
            };
            footerLayout = footer;
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            FlowLayoutPanel buttonFlow = FlowOf();
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
            footer.Controls.Add(buttonFlow, 0, 0);
            layoutShell.Controls.Add(footer, 0, 1);

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
            trayStart.Click += StartButton_Click;
            trayStop.Click += delegate { StopMonitoring(); };
            trayMenu.Items.Add("顯示主視窗", null, delegate { ShowFromTray(); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayStart);
            trayMenu.Items.Add(trayStop);
            trayMenu.Items.Add("立即刷新", null, delegate { RefreshButton_Click(this, EventArgs.Empty); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayAdmin = new ToolStripMenuItem("以系統管理員重新啟動", null, delegate { RestartAsAdministrator(); });
            trayMenu.Items.Add(trayAdmin);
            trayMenu.Items.Add("支持開發", null, delegate { OpenSupportDialog(); });
            trayMenu.Items.Add("結束", null, delegate { Close(); });
            tray.ContextMenuStrip = trayMenu;
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

        private GroupBox BuildBeginnerPanel()
        {
            GroupBox group = AutoGroupOf("快速開始");
            group.AutoSize = true;
            group.Dock = DockStyle.Top;
            group.MinimumSize = new Size(0, 0);

            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = PanelBackground,
                ColumnCount = 2,
                RowCount = 3,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            beginnerDashboard = content;

            Panel primaryCard = BeginnerCard(new Padding(0, 0, 4, 8));
            TableLayoutPanel primaryGrid = BeginnerCardGrid(1);
            primaryGrid.RowCount = 4;
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            primaryGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            primaryGrid.Controls.Add(BeginnerCardTitle("目前連線"), 0, 0);
            beginnerPrimaryValue = LabelOf("尚未選擇", 0, 0, 0, 34, 12F, TextColor,
                                          FontStyle.Bold, ContentAlignment.MiddleLeft);
            beginnerPrimaryValue.Dock = DockStyle.Fill;
            beginnerPrimaryValue.AutoEllipsis = true;
            beginnerPrimaryInterfaceBox = InterfaceBox();
            beginnerPrimaryInterfaceBox.Dock = DockStyle.Fill;
            beginnerPrimaryInterfaceBox.Margin = new Padding(0);
            beginnerPrimaryHealthValue = LabelOf("尚未測試", 0, 0, 0, 24, 8.5F, MutedText,
                                                 FontStyle.Regular, ContentAlignment.MiddleLeft);
            beginnerPrimaryHealthValue.Dock = DockStyle.Fill;
            beginnerPrimaryHealthValue.AutoEllipsis = true;
            primaryGrid.Controls.Add(beginnerPrimaryValue, 0, 1);
            primaryGrid.Controls.Add(beginnerPrimaryInterfaceBox, 0, 2);
            primaryGrid.Controls.Add(beginnerPrimaryHealthValue, 0, 3);
            primaryCard.Controls.Add(primaryGrid);
            content.Controls.Add(primaryCard, 0, 0);

            Panel backupCard = BeginnerCard(new Padding(4, 0, 0, 8));
            TableLayoutPanel backupGrid = BeginnerCardGrid(1);
            backupGrid.RowCount = 4;
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            backupGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            backupGrid.Controls.Add(BeginnerCardTitle("備援網路"), 0, 0);
            beginnerBackupValue = LabelOf("未啟用", 0, 0, 0, 34, 12F, TextColor,
                                         FontStyle.Bold, ContentAlignment.MiddleLeft);
            beginnerBackupValue.Dock = DockStyle.Fill;
            beginnerBackupValue.AutoEllipsis = true;
            beginnerBackupInterfaceBox = InterfaceBox();
            beginnerBackupInterfaceBox.Dock = DockStyle.Fill;
            beginnerBackupInterfaceBox.Margin = new Padding(0);
            beginnerBackupHealthValue = LabelOf("未啟用", 0, 0, 0, 24, 8.5F, MutedText,
                                                FontStyle.Regular, ContentAlignment.MiddleLeft);
            beginnerBackupHealthValue.Dock = DockStyle.Fill;
            beginnerBackupHealthValue.AutoEllipsis = true;
            backupGrid.Controls.Add(beginnerBackupValue, 0, 1);
            backupGrid.Controls.Add(beginnerBackupInterfaceBox, 0, 2);
            backupGrid.Controls.Add(beginnerBackupHealthValue, 0, 3);
            backupCard.Controls.Add(backupGrid);
            content.Controls.Add(backupCard, 1, 0);

            FlowLayoutPanel options = FlowOf();
            options.AutoSize = false;
            options.WrapContents = true;
            options.Padding = new Padding(0, 4, 0, 0);
            beginnerFailoverBox = CheckBoxText("網路中斷時自動切換備援", false);
            beginnerAutoRepairBox = CheckBoxText("遇到問題時自動修復", true);
            options.Controls.Add(beginnerFailoverBox);
            options.Controls.Add(beginnerAutoRepairBox);
            content.Controls.Add(options, 0, 1);
            content.SetColumnSpan(options, 2);

            FlowLayoutPanel tools = FlowOf();
            tools.AutoSize = false;
            tools.WrapContents = false;
            tools.Padding = new Padding(0, 4, 0, 0);
            beginnerStartButton = ButtonOf("開始自動保護", 0, 0, 180, 36, true);
            beginnerStartButton.Margin = new Padding(0, 0, 8, 0);
            beginnerStartButton.Click += delegate
            {
                if (engine.IsRunning) { StopMonitoring(); }
                else { StartButton_Click(this, EventArgs.Empty); }
            };
            tools.Controls.Add(beginnerStartButton);
            beginnerDetectButton = ButtonOf("重新偵測網路", 0, 0, 132, 28, false);
            beginnerDetectButton.Margin = new Padding(0, 2, 8, 0);
            beginnerDetectButton.Click += delegate { AutoDetectBeginnerInterfaces(); };
            tools.Controls.Add(beginnerDetectButton);
            beginnerRestoreButton = ButtonOf("復原上一筆變更", 0, 0, 145, 28, false);
            beginnerRestoreButton.Margin = new Padding(0, 2, 8, 0);
            beginnerRestoreButton.Click += BeginnerRestoreButton_Click;
            tools.Controls.Add(beginnerRestoreButton);
            beginnerLogButton = ButtonOf("查看執行紀錄 ▸", 0, 0, 140, 28, false);
            beginnerLogButton.Margin = new Padding(0, 2, 0, 0);
            tools.Controls.Add(beginnerLogButton);
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
                    PopulateInterfaceBox(beginnerPrimaryInterfaceBox, readyNames, string.Empty);
                    PopulateInterfaceBox(beginnerBackupInterfaceBox, readyNames, string.Empty);
                    beginnerPrimaryInterfaceBox.Text = string.Empty;
                    beginnerBackupInterfaceBox.Text = string.Empty;
                    if (interfaceBox != null) { interfaceBox.Text = string.Empty; }
                    if (beginnerMode) { SyncBeginnerSettingsToAdvanced(); }
                    UpdateBeginnerStatus();
                    string noReadyMessage =
                        "自動偵測未找到任何就緒網路；主要與備援已留空。" + Environment.NewLine +
                        "請先連線 Wi‑Fi 或藍牙網路，再按「重新偵測網路」。";
                    AppendLog("自動偵測：沒有找到具 IPv4 與 gateway 的就緒網路。", true);
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
                if (interfaceBox != null) { interfaceBox.Text = selection.Primary.Name; }
                if (beginnerMode)
                {
                    SyncBeginnerSettingsToAdvanced();
                }
                UpdateBeginnerStatus();

                if (selection.Backup == null)
                {
                    string oneReadyMessage =
                        "已找到主要網路「" + selection.Primary.Name + "」，但沒有第二條就緒線路。" +
                        Environment.NewLine +
                        "備援已留空；請再連線另一條具 IPv4 與 gateway 的網路。";
                    AppendLog("已重新偵測網路：主要「" + selection.Primary.Name + "」；備援未找到，已留空。", true);
                    if (showPrompt)
                    {
                        MessageBox.Show(this, oneReadyMessage, "NetOptimizer",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    AppendLog("已重新偵測網路：主要「" + selection.Primary.Name + "」；備援「" +
                              selection.Backup.Name + "」。", false);
                }
            }
            catch (Exception ex)
            {
                AppendLog("自動偵測網路失敗：" + ex.Message, true);
                if (showPrompt)
                {
                    MessageBox.Show(this, "無法完成網路偵測。" + Environment.NewLine + ex.Message,
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

        private async void BeginnerRestoreButton_Click(object sender, EventArgs e)
        {
            if (restoringNetwork) { return; }
            if (engine.IsRunning)
            {
                MessageBox.Show(this, "請先停止自動保護，再執行復原。",
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
                    AppendLog("復原：" + message, !report.Restored);
                }

                string summary;
                MessageBoxIcon icon;
                if (!report.FoundJournal)
                {
                    summary = "目前沒有待復原的 A/B 網路變更。";
                    icon = MessageBoxIcon.Information;
                }
                else if (report.Restored)
                {
                    summary = "上一筆 A/B 網路變更已完成復原。";
                    icon = MessageBoxIcon.Information;
                }
                else
                {
                    summary = "目前無法完整復原上一筆變更。請以系統管理員身分重試，或查看執行紀錄。";
                    icon = MessageBoxIcon.Warning;
                }
                MessageBox.Show(this, summary, "NetOptimizer", MessageBoxButtons.OK, icon);
            }
            catch (Exception ex)
            {
                AppendLog("手動復原失敗：" + ex.Message, true);
                MessageBox.Show(this, "復原失敗。" + Environment.NewLine + ex.Message,
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
            using (MainForm form = new MainForm())
            {
                form.CreateControl();
                form.ShowInTaskbar = false;
                form.Opacity = 0.0;
                form.Show();
                Application.DoEvents();
                form.PerformLayout();

                if (form.layoutRoot == null || form.layoutRoot.Controls.Count != 7 ||
                     form.layoutShell == null || form.footerLayout == null ||
                     form.beginnerPanel == null || !form.beginnerMode || !form.beginnerPanel.Visible ||
                     form.beginnerDashboard == null ||
                     form.beginnerPrimaryValue == null || form.beginnerPrimaryHealthValue == null ||
                     form.beginnerBackupValue == null || form.beginnerBackupHealthValue == null ||
                     form.beginnerStartButton == null || !form.beginnerStartButton.Visible ||
                    form.beginnerDetectButton == null || !form.beginnerDetectButton.Visible ||
                    form.beginnerRestoreButton == null || !form.beginnerRestoreButton.Visible ||
                    form.monitorGroup.Visible || form.actionsGroup.Visible ||
                    form.failoverGroup.Visible || form.logGroup.Visible)
                {
                    throw new InvalidOperationException("新手模式或主布局控制項數量不正確。");
                }
                AssertBeginnerDashboard(form, "初始");
                AssertHeaderVisible(form, "初始");
                if (form.brandImage == null || form.brandImage.Image == null)
                {
                    throw new InvalidOperationException("品牌圖示未載入。");
                }
                AssertNoNotPresentInterfaceItems(form, "初始");
                SupportDialog.RunUiSelfTest();
                if (form.FormBorderStyle != FormBorderStyle.Sizable || !form.MaximizeBox ||
                     form.MinimumSize.Width < 760 || form.MinimumSize.Height < 420)
                {
                    throw new InvalidOperationException("視窗縮放或最小尺寸設定不正確。");
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
                    form.logEmptyLabel == null)
                {
                    throw new InvalidOperationException("進階設定或紀錄空狀態未正確初始化。");
                }
                form.beginnerLogButton.PerformClick();
                form.ClearLogContent();
                if (!form.logEmptyLabel.Visible)
                {
                    throw new InvalidOperationException("清除紀錄後的空狀態未正確顯示。");
                }
                form.beginnerLogButton.PerformClick();

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
                if (!form.logGroup.Visible)
                {
                    throw new InvalidOperationException("新手模式無法展開執行紀錄。");
                }
                form.beginnerLogButton.PerformClick();
                if (form.logGroup.Visible)
                {
                    throw new InvalidOperationException("新手模式無法收合執行紀錄。");
                }

                form.modeButton.PerformClick();
                form.PerformLayout();
                if (form.beginnerMode || !form.monitorGroup.Visible || !form.actionsGroup.Visible ||
                    !form.failoverGroup.Visible || !form.logGroup.Visible)
                {
                    throw new InvalidOperationException("進階模式切換布局失敗。");
                }
                AssertHeaderVisible(form, "進階模式");
                AssertLayoutHasWidth(form, "進階模式");
                AssertFooterVisible(form, "進階模式");

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

                form.modeButton.PerformClick();
                form.PerformLayout();
                if (!form.beginnerMode || !form.beginnerPanel.Visible || form.monitorGroup.Visible ||
                    form.actionsGroup.Visible || form.failoverGroup.Visible || form.logGroup.Visible)
                {
                    throw new InvalidOperationException("新手模式切換布局失敗。");
                }
                AssertFooterVisible(form, "回到新手模式");

                Console.WriteLine("NetOptimizer UI layout test: PASS");
            }
        }

        internal static void RunGuiStartupSelfTest()
        {
            string failure = null;
            using (MainForm form = new MainForm())
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

                        form.WindowState = FormWindowState.Minimized;
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

            using (MainForm form = new MainForm())
            {
                form.ShowInTaskbar = false;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(20, 20);
                form.Show();
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

        private static void AssertLayoutHasWidth(MainForm form, string stage)
        {
            Control monitor = form.layoutRoot.GetControlFromPosition(0, 2);
            Control failover = form.layoutRoot.GetControlFromPosition(0, 4);
            Control log = form.layoutRoot.GetControlFromPosition(0, 6);
            if (monitor == null || failover == null || log == null ||
                monitor.Width <= 0 || failover.Width <= 0 || log.Width <= 0)
            {
                throw new InvalidOperationException(stage + "布局沒有取得有效寬度。");
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
        }

        private static void AssertBeginnerDashboard(MainForm form, string stage)
        {
            if (form.beginnerPanel == null || form.beginnerDashboard == null ||
                form.beginnerPanel.Height <= 0 || form.beginnerPanel.Height >= 400 ||
                form.beginnerDashboard.Width <= 0 || form.beginnerDashboard.Height <= 0 ||
                form.beginnerDashboard.Height >= 330)
            {
                throw new InvalidOperationException(stage + "新手介面沒有收合至內容高度。 ");
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
                form.titleLabel == null || form.statusValue == null ||
                form.lastProbeValue == null || form.modeButton == null)
            {
                throw new InvalidOperationException(stage + "標題區控制項未完整建立。");
            }

            Rectangle headerBounds = new Rectangle(
                form.headerLayout.PointToScreen(Point.Empty), form.headerLayout.ClientSize);
            Rectangle beginnerBounds = new Rectangle(
                form.beginnerPanel.PointToScreen(Point.Empty), form.beginnerPanel.ClientSize);
            Control[] controls =
            {
                form.brandImage,
                form.titleLabel,
                form.statusValue,
                form.lastProbeValue,
                form.modeButton
            };
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
                throw new InvalidOperationException(stage + "標題區與快速開始區重疊或切換按鈕高度不足。");
            }
        }

        private static void AssertFooterVisible(MainForm form, string stage)
        {
            if (form.beginnerMode)
            {
                int footerRowHeight = form.layoutShell.GetRowHeights()[1];
                if (form.footerLayout.Visible || footerRowHeight > 1 ||
                    form.supportButton == null || !form.supportButton.Visible ||
                    form.permissionActions == null || form.supportButton.Parent != form.permissionActions ||
                    form.supportButton.Width <= 0 || form.supportButton.Height <= 0 ||
                    form.supportButton.Right > form.permissionActions.ClientSize.Width ||
                    form.supportButton.Bottom > form.permissionActions.ClientSize.Height)
                {
                    throw new InvalidOperationException(stage + "新手模式的支持開發入口未完整顯示。");
                }
                return;
            }

            if (!form.footerLayout.Visible || form.footerLayout.Height < 40 ||
                form.footerLayout.Bottom > form.layoutShell.ClientSize.Height)
            {
                throw new InvalidOperationException(stage + "頁尾按鈕沒有固定在可視區域。");
            }

            Button[] buttons = new[] { form.startButton, form.stopButton, form.refreshButton,
                                      form.saveButton, form.exportButton };
            foreach (Button button in buttons)
            {
                if (button == null || !button.Visible || button.Width <= 0 || button.Height <= 0 ||
                    button.Right > form.footerLayout.ClientSize.Width ||
                    button.Bottom > form.footerLayout.ClientSize.Height)
                {
                    throw new InvalidOperationException(stage + "頁尾操作按鈕未完整顯示。");
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
            copyAllItem.Click += delegate { CopyLog(); };
            logMenu.Items.Add(copyAllItem);

            ToolStripMenuItem clearItem = new ToolStripMenuItem("清除紀錄");
            clearItem.Click += delegate { ClearLog(); };
            logMenu.Items.Add(clearItem);
            logMenu.Items.Add(new ToolStripSeparator());

            followLogItem = new ToolStripMenuItem("自動捲到最新")
            {
                CheckOnClick = true,
                Checked = followLog
            };
            followLogItem.CheckedChanged += delegate
            {
                followLog = followLogItem.Checked;
                if (followLog) { ScrollLogToEnd(); }
            };
            logMenu.Items.Add(followLogItem);
            logBox.ContextMenuStrip = logMenu;
            uiToolTip.SetToolTip(logBox, "右鍵可複製或清除紀錄，也可暫停自動捲動。");
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
                MessageBox.Show(this, "無法複製紀錄。" + Environment.NewLine + ex.Message,
                                "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearLog()
        {
            if (logBox == null || visibleLog.Count == 0) { return; }
            DialogResult result = MessageBox.Show(
                this,
                "確定要清除目前執行紀錄嗎？清除後仍可重新匯出之後的新紀錄。",
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
                uiToolTip.SetToolTip(box, "可直接輸入 Windows 網路介面名稱。");
                return;
            }

            InterfaceSnapshot snapshot = NetworkInfo.GetInterfaceSnapshot(name);
            if (snapshot == null)
            {
                uiToolTip.SetToolTip(box, "尚未取得這張介面的 IPv4 與 gateway。");
                return;
            }

            uiToolTip.SetToolTip(
                box,
                "狀態：" + snapshot.Status + Environment.NewLine +
                "IPv4：" + (string.IsNullOrWhiteSpace(snapshot.IPv4) ? "無" : snapshot.IPv4) + Environment.NewLine +
                "Gateway：" + (string.IsNullOrWhiteSpace(snapshot.Gateway) ? "無" : snapshot.Gateway) + Environment.NewLine +
                "就緒：" + (snapshot.IsReady ? "是" : "否"));
        }

        private void ApplySettingsToUi()
        {
            beginnerMode = !settings.BeginnerMode.HasValue || settings.BeginnerMode.Value;
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
            failoverEnabledBox.Checked = settings.FailoverEnabled;
            smartSelectionBox.Checked = settings.SmartSelectionEnabled;
            suppressRefreshBox.Checked = settings.SuppressRefreshDuringFailover;
            dnsBox.Checked = settings.FlushDns;
            arpBox.Checked = settings.ClearArp;
            mtuBox.Checked = settings.PulseMtu;
            beginnerFailoverBox.Checked = settings.FailoverEnabled;
            beginnerAutoRepairBox.Checked = settings.EnableRefresh;
            UpdateActionEnabled();
            UpdateFailoverEnabled();
            UpdateBeginnerStatus();
            UpdateUiMode();
        }

        private void SyncBeginnerSettingsToAdvanced()
        {
            if (!beginnerMode || beginnerPrimaryInterfaceBox == null) { return; }
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
            SyncBeginnerSettingsToAdvanced();
            MonitorSettings next = settings.Clone();
            next.BeginnerMode = beginnerMode;
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
                throw new InvalidOperationException("請先選擇網卡。 ");
            }
            if (next.Targets.Count == 0)
            {
                throw new InvalidOperationException("請至少填寫一個測試目標。 ");
            }
            if (next.FailoverEnabled)
            {
                if (next.PrimaryInterface.Length == 0 || next.BackupInterface.Length == 0)
                {
                    throw new InvalidOperationException("啟用 A/B 切換時，請同時指定主線 A 與備援 B。 ");
                }
                if (string.Equals(next.PrimaryInterface, next.BackupInterface, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("主線 A 與備援 B 不能是同一張網卡。 ");
                }
                if (next.FailoverTargets.Count == 0)
                {
                    throw new InvalidOperationException("啟用 A/B 切換時，請至少填寫一個故障切換測試目標。 ");
                }
                if (next.FailoverPrimaryMetric >= next.FailoverBackupMetric)
                {
                    throw new InvalidOperationException("A metric 必須小於 B metric，才能讓 A/B 優先順序明確。 ");
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
                AppendLog("設定已儲存：" + SettingsStore.SettingsPath, false);
                return true;
            }
            catch (Exception ex)
            {
                AppendLog("儲存設定失敗：" + ex.Message, true);
                if (showError)
                {
                    MessageBox.Show(this, ex.Message, "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
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
                const string message =
                    "A/B 自動切換需要系統管理員權限，已阻止啟動。" +
                    "請按「重新以管理員啟動」後再開始監測。";
                AppendLog("A/B 啟動已阻止：目前不是系統管理員。", true);
                if (adminButton != null)
                {
                    adminButton.Visible = true;
                    adminButton.Enabled = true;
                    adminButton.Focus();
                }
                if (showError)
                {
                    MessageBox.Show(this, message, "需要管理員權限",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }

            if (!TrySaveSettings(showError)) { return false; }
            engine.Start(settings);
            statusValue.Text = "狀態：監測中";
            statusValue.ForeColor = Accent;
            UpdateBeginnerStatus();
            UpdateButtons();
            return true;
        }

        private void StopMonitoring()
        {
            engine.Stop();
            if (statusValue != null)
            {
                statusValue.Text = "狀態：已停止";
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
                dialog.Title = "匯出 NetOptimizer 診斷報告";
                dialog.Filter = "文字報告 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
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
                    AppendLog("診斷報告已匯出：" + dialog.FileName, false);
                    MessageBox.Show(this, "診斷報告已匯出。報告包含網卡、IP、route 與近期 log，請確認內容後再分享。",
                                    "NetOptimizer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    AppendLog("匯出診斷失敗：" + ex.Message, true);
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
                string resultText;
                if (result.State == ProbeState.Success)
                {
                    resultText = result.LatencyMs + " ms";
                }
                else
                {
                    resultText = result.State == ProbeState.Timeout ? "timeout" : "失敗";
                }
                lastProbeValue.Text = "最近探測：" + result.Target + ":" + result.Port + " → " + resultText;
                statusValue.Text = "狀態：監測中 · 下次約 " + e.IntervalMs + " ms";
                statusValue.ForeColor = result.IsHealthy(settings.ThresholdMs) ? Accent : Warning;
                UpdateBeginnerStatus();
                if (uiToolTip != null)
                {
                    uiToolTip.SetToolTip(lastProbeValue, "完整結果：" + result.Target + ":" + result.Port + " → " + resultText);
                }
                if (!result.IsHealthy(settings.ThresholdMs) && result.State != ProbeState.Cancelled)
                {
                    AppendLog("探測異常：" + result.Target + " → " + resultText +
                              (string.IsNullOrWhiteSpace(result.Error) ? string.Empty : "（" + result.Error + "）"), true);
                }
            });
        }

        private void Engine_FailoverStatusChanged(object sender, FailoverStatusEventArgs e)
        {
            FailoverStatus status = e.Status;
            latestFailoverStatus = status == null ? null : status.Clone();
            RunOnUi(delegate
            {
                UpdateBeginnerStatus();
                if (status == null || !status.Ready)
                {
                    failoverStatusValue.Text = "A/B：未啟用或尚未就緒";
                    failoverStatusValue.ForeColor = MutedText;
                    failoverHealthValue.Text = "健康度：尚未測試";
                    if (uiToolTip != null)
                    {
                        uiToolTip.SetToolTip(failoverStatusValue, failoverStatusValue.Text);
                        uiToolTip.SetToolTip(failoverHealthValue, failoverHealthValue.Text);
                    }
                    UpdateBeginnerStatus();
                    return;
                }
                string mode = status.InFailover ? "故障切換中" :
                              (status.SmartSelection ? "智慧選路" : "主線優先");
                failoverStatusValue.Text = "目前 " + status.ActiveInterface +
                                           "／備援 " + status.StandbyInterface + " · " + mode +
                                           " · 切換 " + status.SwitchCount + " 次";
                failoverStatusValue.ForeColor = status.InFailover ? Warning : Accent;
                failoverHealthValue.Text = "A/B 健康度：" + status.ActiveHealth +
                                           " | " + status.StandbyHealth;
                failoverHealthValue.ForeColor = status.InFailover ? Warning : MutedText;
                if (uiToolTip != null)
                {
                    uiToolTip.SetToolTip(failoverStatusValue, failoverStatusValue.Text);
                    uiToolTip.SetToolTip(failoverHealthValue, failoverHealthValue.Text);
                }
                UpdateBeginnerStatus();
            });
        }

        private void UpdatePermissionText()
        {
            string permissionTip;
            if (NetworkInfo.IsAdministrator())
            {
                permissionValue.Text = "權限：管理員";
                permissionValue.ForeColor = Accent;
                permissionTip = "系統管理員；ARP／MTU／A-B metric 動作可正常嘗試。";
                if (adminButton != null)
                {
                    adminButton.Visible = false;
                    adminButton.Text = "重新以管理員啟動";
                }
            }
            else
            {
                permissionValue.Text = "權限：一般使用者";
                permissionValue.ForeColor = Warning;
                permissionTip = "一般使用者；DNS 通常可執行，ARP／MTU／A-B metric 可能需要系統管理員。";
                if (adminButton != null)
                {
                    adminButton.Visible = true;
                    adminButton.Text = "重新以管理員啟動";
                }
            }
            if (uiToolTip != null) { uiToolTip.SetToolTip(permissionValue, permissionTip); }
        }

        private void ToggleUiMode()
        {
            if (beginnerMode)
            {
                SyncBeginnerSettingsToAdvanced();
                beginnerMode = false;
            }
            else
            {
                SyncAdvancedSettingsToBeginner();
                beginnerMode = true;
            }
            UpdateBeginnerStatus();
            UpdateUiMode();
        }

        private void UpdateUiMode()
        {
            if (modeButton != null)
            {
                modeButton.Text = beginnerMode ? "顯示進階設定 ▸" : "切換新手模式";
                if (uiToolTip != null)
                {
                    uiToolTip.SetToolTip(
                        modeButton,
                        beginnerMode ? "顯示完整監測、刷新、A/B 與 EWMA 設定。" :
                                        "回到簡化畫面，只保留一般使用者需要的選項。");
                }
            }
            if (beginnerPanel != null) { beginnerPanel.Visible = beginnerMode; }
            if (monitorGroup != null) { monitorGroup.Visible = !beginnerMode; }
            if (actionsGroup != null) { actionsGroup.Visible = !beginnerMode; }
            if (failoverGroup != null) { failoverGroup.Visible = !beginnerMode; }
            if (permissionPanel != null) { permissionPanel.Visible = true; }
            if (logGroup != null) { logGroup.Visible = !beginnerMode || beginnerLogExpanded; }
            if (footerLayout != null) { footerLayout.Visible = !beginnerMode; }
            if (layoutShell != null && layoutShell.RowStyles.Count > 1)
            {
                RowStyle footerRow = layoutShell.RowStyles[1];
                footerRow.SizeType = SizeType.Absolute;
                footerRow.Height = beginnerMode ? 0F : 54F;
            }
            if (beginnerLogButton != null)
            {
                beginnerLogButton.Text = beginnerLogExpanded ? "隱藏執行紀錄 ▴" : "查看執行紀錄 ▸";
            }
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
            UpdateButtons();
        }

        private void UpdateBeginnerStatus()
        {
            string primary = beginnerPrimaryInterfaceBox == null ? string.Empty :
                             beginnerPrimaryInterfaceBox.Text.Trim();
            string backup = beginnerBackupInterfaceBox == null ? string.Empty :
                            beginnerBackupInterfaceBox.Text.Trim();
            bool failoverEnabled = beginnerFailoverBox != null && beginnerFailoverBox.Checked;

            bool failoverReady = latestFailoverStatus != null && latestFailoverStatus.Ready;
            string activeInterface = failoverReady ? (latestFailoverStatus.ActiveInterface ?? string.Empty).Trim() : string.Empty;
            bool backupIsActive = failoverReady && backup.Length > 0 &&
                                  string.Equals(activeInterface, backup, StringComparison.OrdinalIgnoreCase);
            if (beginnerPrimaryValue != null)
            {
                beginnerPrimaryValue.Text = activeInterface.Length > 0
                    ? "目前使用：" + activeInterface
                    : (primary.Length == 0 ? "尚未選擇" : "已選擇：" + primary);
            }
            if (beginnerBackupValue != null)
            {
                beginnerBackupValue.Text = !failoverEnabled ? "未啟用" :
                    (backup.Length == 0 ? "請選擇備援" :
                     (backupIsActive ? "目前使用：" + backup : "待命：" + backup));
            }

            string probeSummary = lastProbeValue == null ? string.Empty : lastProbeValue.Text;
            const string probePrefix = "最近探測：";
            if (probeSummary.StartsWith(probePrefix, StringComparison.Ordinal))
            {
                probeSummary = probeSummary.Substring(probePrefix.Length).Trim();
            }
            if (beginnerPrimaryHealthValue != null)
            {
                beginnerPrimaryHealthValue.Text = probeSummary.Length == 0 ||
                                                  string.Equals(probeSummary, "尚未測試", StringComparison.OrdinalIgnoreCase)
                    ? "尚未測試"
                    : "最近：" + probeSummary;
                beginnerPrimaryHealthValue.ForeColor = statusValue == null ? MutedText : statusValue.ForeColor;
            }

            if (beginnerBackupHealthValue != null)
            {
                string backupHealth = !failoverEnabled ? "未啟用" : "等待監測";
                Color backupColor = MutedText;
                if (failoverReady && backup.Length > 0)
                {
                    if (backupIsActive)
                    {
                        backupHealth = "健康度：" + (latestFailoverStatus.ActiveHealth ?? "尚未測試") + " · 目前使用中";
                    }
                    else if (string.Equals(latestFailoverStatus.StandbyInterface, backup,
                                           StringComparison.OrdinalIgnoreCase))
                    {
                        backupHealth = "健康度：" + (latestFailoverStatus.StandbyHealth ?? "尚未測試") + " · 待命";
                    }
                    else
                    {
                        backupHealth = "健康度：尚未對應";
                    }
                    backupColor = latestFailoverStatus.InFailover ? Warning : MutedText;
                }
                else if (failoverEnabled && backup.Length == 0)
                {
                    backupHealth = "先選擇備援網路";
                    backupColor = Warning;
                }
                beginnerBackupHealthValue.Text = backupHealth;
                beginnerBackupHealthValue.ForeColor = backupColor;
            }
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
            failoverAdvancedButton.Text = advancedExpanded ? "進階設定 ▾" : "進階設定 ▸";
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
                startButton.Visible = !beginnerMode;
                startButton.Enabled = !running;
            }
            if (stopButton != null)
            {
                stopButton.Visible = !beginnerMode;
                stopButton.Enabled = running;
            }
            if (refreshButton != null) { refreshButton.Visible = !beginnerMode; }
            if (saveButton != null) { saveButton.Visible = !beginnerMode; }
            if (exportButton != null) { exportButton.Visible = !beginnerMode; }
            if (beginnerStartButton != null)
            {
                beginnerStartButton.Visible = beginnerMode;
                beginnerStartButton.Text = running ? "停止自動保護" : "開始自動保護";
                beginnerStartButton.Enabled = !restoringNetwork && !exportingDiagnostics;
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
            if (adminButton != null) { adminButton.Enabled = !NetworkInfo.IsAdministrator(); }
            if (exportButton != null) { exportButton.Enabled = !exportingDiagnostics; }
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

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                Hide();
                tray.ShowBalloonTip(1200, "NetOptimizer", "監測仍在背景執行。", ToolTipIcon.Info);
            }
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
            return new Panel
            {
                BackColor = PanelBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = margin,
                Padding = new Padding(12, 8, 12, 8)
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
            Label label = LabelOf(text, 0, 0, 0, 20, 9F, MutedText,
                                  FontStyle.Bold, ContentAlignment.MiddleLeft);
            label.Dock = DockStyle.Fill;
            label.AutoEllipsis = true;
            return label;
        }

        private static GroupBox AutoGroupOf(string text)
        {
            return new GroupBox
            {
                Text = text,
                ForeColor = TextColor,
                BackColor = PanelBackground,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 24, 10, 10),
                Margin = new Padding(0, 0, 0, 8)
            };
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
            return new Label
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
            return new CheckBox
            {
                Text = text,
                Checked = value,
                AutoSize = true,
                Height = 27,
                ForeColor = TextColor,
                BackColor = PanelBackground,
                Margin = new Padding(0, 0, 18, 4)
            };
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
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Color.FromArgb(17, 18, 20),
                ForeColor = TextColor,
                FlatStyle = FlatStyle.Flat,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                DropDownWidth = 360,
                Height = 27,
                MinimumSize = new Size(0, 27)
            };
        }

        private static Button ButtonOf(string text, int x, int y, int width, int height, bool accent)
        {
            Button button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = accent ? Color.FromArgb(19, 39, 35) : Color.FromArgb(20, 22, 25),
                ForeColor = accent ? Accent : TextColor,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true
            };
            button.FlatAppearance.BorderColor = accent ? Color.FromArgb(39, 111, 91) : Color.FromArgb(59, 64, 71);
            button.SetBounds(x, y, width, height);
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
            return label;
        }

        private void OpenSupportDialog()
        {
            using (SupportDialog dialog = new SupportDialog())
            {
                dialog.ShowDialog(this);
            }
        }

        private void RestartAsAdministrator()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Application.ExecutablePath)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                });
                Close();
            }
            catch (Win32Exception)
            {
                AppendLog("使用者取消系統管理員重新啟動。", true);
            }
            catch (Exception ex)
            {
                AppendLog("無法以系統管理員重新啟動：" + ex.Message, true);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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
