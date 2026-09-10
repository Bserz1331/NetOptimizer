using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NetOptimizerV2
{
    internal sealed class SupportDialog : Form
    {
        private const string KoFiUrl = "https://ko-fi.com/minz_space_cat";
        private const string Bep20Address = "0x7a4E3D8D9684196E4F96a6a28c49D3a1a785A0b5";
        private const string Trc20Address = "TNm7kRfeFo2wa1TVtz5EoNNBS8LFahoe7j";
        private readonly ToolTip toolTip;
        private Button kofiButton;
        private Button bep20CopyButton;
        private Button trc20CopyButton;
        private TextBox bep20AddressBox;
        private TextBox trc20AddressBox;
        private readonly AppLanguage language;

        public SupportDialog(AppLanguage language = AppLanguage.TraditionalChinese)
        {
            this.language = Localization.Normalize(language);
            Text = L("支持開發");
            BackColor = Color.FromArgb(20, 21, 23);
            ForeColor = Color.FromArgb(238, 240, 242);
            Font = new Font("Microsoft JhengHei UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 330);
            AutoScaleMode = AutoScaleMode.Dpi;

            toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 350,
                ReshowDelay = 100,
                ShowAlways = true
            };

            Controls.Add(LabelOf(L("自願支持"), 14, 12, 150, 20, 9F,
                                Color.FromArgb(82, 201, 151), FontStyle.Bold));
            Controls.Add(LabelOf(L("支持開發"), 14, 34, 300, 34, 19F,
                                ForeColor, FontStyle.Bold));

            Panel kofi = Card(Color.FromArgb(19, 39, 35), Color.FromArgb(39, 111, 91));
            kofi.SetBounds(14, 80, 612, 62);
            kofi.Controls.Add(LabelOf("☕", 16, 13, 34, 34, 17F, ForeColor));
            kofi.Controls.Add(LabelOf(L("透過 Ko-fi 支持"), 58, 8, 250, 24, 11F,
                                     ForeColor, FontStyle.Bold));
            kofi.Controls.Add(LabelOf(L("支持後續維護與改善"), 58, 33, 250, 18, 8.5F,
                                     Color.FromArgb(155, 161, 168)));
            kofiButton = ButtonOf(L("開啟 ↗"), 520, 15, 80, 30, true);
            kofiButton.Click += delegate { OpenUrl(KoFiUrl); };
            kofi.Controls.Add(kofiButton);
            Controls.Add(kofi);

            Controls.Add(CreateWalletCard("USDT | BEP20", "BNB Smart Chain",
                                          Bep20Address, out bep20AddressBox,
                                          out bep20CopyButton, 14, 154));
            Controls.Add(CreateWalletCard("USDT | TRC20", "TRON",
                                          Trc20Address, out trc20AddressBox,
                                          out trc20CopyButton, 328, 154));

            Panel warning = Card(Color.FromArgb(42, 36, 20), Color.FromArgb(144, 103, 26));
            warning.SetBounds(14, 270, 612, 44);
            warning.Controls.Add(LabelOf(L("轉帳前請確認網路：BEP20／TRC20。建議先小額測試。"),
                                         12, 7, 585, 28, 8.5F,
                                         Color.FromArgb(238, 181, 43), FontStyle.Bold));
            Controls.Add(warning);
        }

        private Control CreateWalletCard(string title, string network, string address,
                                         out TextBox addressBox, out Button copyButton,
                                         int x, int y)
        {
            Panel card = Card(Color.FromArgb(15, 16, 18), Color.FromArgb(49, 53, 59));
            card.SetBounds(x, y, 298, 100);
            card.Controls.Add(LabelOf(title, 12, 8, 175, 22, 10F,
                                      Color.FromArgb(235, 237, 240), FontStyle.Bold));
            card.Controls.Add(LabelOf(network, 12, 30, 175, 18, 8F,
                                      Color.FromArgb(146, 151, 159)));

            addressBox = new TextBox
            {
                Text = address,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(22, 24, 27),
                ForeColor = Color.FromArgb(202, 205, 210),
                Font = new Font("Consolas", 7F),
                Location = new Point(12, 54),
                Size = new Size(180, 25),
                Margin = new Padding(0),
                TabStop = true
            };
            toolTip.SetToolTip(addressBox, address);
            card.Controls.Add(addressBox);

            Button localCopyButton = ButtonOf(L("複製地址"), 199, 52, 87, 28, false);
            copyButton = localCopyButton;
            localCopyButton.Click += delegate { CopyAddress(address, localCopyButton); };
            card.Controls.Add(localCopyButton);
            return card;
        }

        private void CopyAddress(string address, Button button)
        {
            try
            {
                Clipboard.SetText(address);
                string oldText = button.Text;
                button.Text = L("已複製 ✓");
                Timer reset = new Timer { Interval = 1400 };
                reset.Tick += delegate
                {
                    reset.Stop();
                    reset.Dispose();
                    if (!button.IsDisposed) { button.Text = oldText; }
                };
                reset.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L("無法複製地址，請稍後再試。") + Environment.NewLine + ex.Message,
                                L("支持開發"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static Panel Card(Color fill, Color border)
        {
            Panel panel = new Panel
            {
                BackColor = fill,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            panel.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
            return panel;
        }

        private static Label LabelOf(string text, int x, int y, int width, int height,
                                     float size, Color color,
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

        private static Button ButtonOf(string text, int x, int y, int width, int height, bool accent)
        {
            Button button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = accent ? Color.FromArgb(19, 39, 35) : Color.FromArgb(20, 22, 25),
                ForeColor = accent ? Color.FromArgb(79, 214, 163) : Color.FromArgb(231, 233, 236),
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true
            };
            button.FlatAppearance.BorderColor = accent ? Color.FromArgb(39, 111, 91) :
                                                 Color.FromArgb(59, 64, 71);
            button.SetBounds(x, y, width, height);
            return button;
        }

        private string L(string source)
        {
            return Localization.Get(language, source);
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(L("無法開啟瀏覽器。") + Environment.NewLine + url +
                                Environment.NewLine + ex.Message,
                                L("支持開發"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        internal static void RunUiSelfTest()
        {
            using (SupportDialog dialog = new SupportDialog())
            {
                dialog.ShowInTaskbar = false;
                dialog.Opacity = 0.0;
                dialog.Show();
                Application.DoEvents();
                if (dialog.ClientSize.Width < 600 || dialog.ClientSize.Height < 300 ||
                    dialog.kofiButton == null || dialog.bep20CopyButton == null ||
                    dialog.trc20CopyButton == null || dialog.bep20AddressBox == null ||
                    dialog.trc20AddressBox == null ||
                    dialog.bep20AddressBox.Text != Bep20Address ||
                    dialog.trc20AddressBox.Text != Trc20Address)
                {
                    throw new InvalidOperationException("支持開發視窗控制項初始化失敗。");
                }
                Control[] controls =
                {
                    dialog.kofiButton,
                    dialog.bep20CopyButton,
                    dialog.trc20CopyButton,
                    dialog.bep20AddressBox,
                    dialog.trc20AddressBox
                };
                foreach (Control control in controls)
                {
                    if (!control.Visible || control.Width <= 0 || control.Height <= 0 ||
                        control.Right > dialog.ClientSize.Width || control.Bottom > dialog.ClientSize.Height)
                    {
                        throw new InvalidOperationException("支持開發視窗有控制項遭到裁切。");
                    }
                }
                dialog.Close();
            }
            using (SupportDialog english = new SupportDialog(AppLanguage.English))
            {
                english.ShowInTaskbar = false;
                english.Opacity = 0.0;
                english.Show();
                Application.DoEvents();
                if (english.Text != "Support development" ||
                    english.kofiButton.Text != "Open ↗" ||
                    english.bep20CopyButton.Text != "Copy" ||
                    english.trc20CopyButton.Text != "Copy")
                {
                    throw new InvalidOperationException("English support dialog localization failed.");
                }
                english.Close();
            }
        }

        internal static void SaveUiSnapshot(string outputPath)
        {
            SaveUiSnapshot(outputPath, AppLanguage.TraditionalChinese);
        }

        internal static void SaveUiSnapshot(string outputPath, AppLanguage language)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Snapshot output path is required.", "outputPath");
            }
            string fullPath = Path.GetFullPath(outputPath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory)) { Directory.CreateDirectory(directory); }
            using (SupportDialog dialog = new SupportDialog(language))
            {
                dialog.ShowInTaskbar = false;
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = new Point(20, 20);
                dialog.Show();
                Application.DoEvents();
                using (Bitmap snapshot = new Bitmap(dialog.Width, dialog.Height))
                {
                    dialog.DrawToBitmap(snapshot, new Rectangle(Point.Empty, dialog.Size));
                    snapshot.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                dialog.Close();
            }
            Console.WriteLine("NetOptimizer support snapshot: PASS " + fullPath);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && toolTip != null) { toolTip.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
