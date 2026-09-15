using System;
using System.Drawing;
using System.Windows.Forms;

namespace NetOptimizerV2
{
    internal sealed class AdvancedSettingsDialog : Form
    {
        private static readonly Color Background = Color.FromArgb(10, 20, 28);
        private static readonly Color PanelBackground = Color.FromArgb(16, 29, 38);
        private static readonly Color TextColor = Color.FromArgb(241, 244, 247);
        private static readonly Color Accent = Color.FromArgb(20, 224, 205);

        private readonly Panel contentHost;
        private readonly FlowLayoutPanel actionFlow;
        private readonly ModernButton saveButton;
        private readonly ModernButton cancelButton;
        private Control attachedContent;

        internal Func<bool> SaveRequested { get; set; }

        internal AdvancedSettingsDialog()
        {
            Text = "NetOptimizer - 進階設定";
            BackColor = Background;
            ForeColor = TextColor;
            Font = new Font("Microsoft JhengHei UI", 9F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(720, 650);
            MinimumSize = new Size(620, 520);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(10, 8, 10, 8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            Controls.Add(root);

            contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PanelBackground,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            root.Controls.Add(contentHost, 0, 0);

            actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Background,
                Margin = new Padding(0),
                Padding = new Padding(0, 8, 0, 0)
            };
            root.Controls.Add(actionFlow, 0, 1);

            cancelButton = DialogButton("取消", false, 96);
            saveButton = DialogButton("儲存設定", true, 118);
            cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            saveButton.Click += SaveButton_Click;
            actionFlow.Controls.Add(cancelButton);
            actionFlow.Controls.Add(saveButton);
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            FormClosing += delegate { DetachContent(); };
        }

        internal void UpdateTexts(string title, string saveText, string cancelText)
        {
            Text = "NetOptimizer - " + (title ?? string.Empty);
            saveButton.Text = saveText ?? string.Empty;
            cancelButton.Text = cancelText ?? string.Empty;
        }

        internal void AttachContent(Control content)
        {
            DetachContent();
            if (content == null)
            {
                return;
            }
            if (content.Parent != null)
            {
                content.Parent.Controls.Remove(content);
            }
            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(0);
            content.Visible = true;
            contentHost.Controls.Add(content);
            content.BringToFront();
            attachedContent = content;
        }

        internal void DetachContent()
        {
            if (attachedContent == null)
            {
                return;
            }
            if (attachedContent.Parent == contentHost)
            {
                contentHost.Controls.Remove(attachedContent);
            }
            attachedContent = null;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (SaveRequested != null && !SaveRequested())
            {
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private static ModernButton DialogButton(string text, bool accent, int width)
        {
            ModernButton button = new ModernButton
            {
                Text = text,
                Width = width,
                Height = 32,
                MinimumSize = new Size(width, 32),
                MaximumSize = new Size(width, 32),
                BackColor = accent ? Color.FromArgb(19, 39, 35) : Color.FromArgb(20, 22, 25),
                ForeColor = accent ? Accent : TextColor,
                Accent = accent,
                AccentBorder = accent,
                Margin = new Padding(8, 0, 0, 0)
            };
            return button;
        }
    }
}
