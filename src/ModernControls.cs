using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NetOptimizerV2
{
    internal static class ModernDrawing
    {
        internal static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            int safeRadius = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
            int diameter = safeRadius * 2;
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180F, 90F);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270F, 90F);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0F, 90F);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90F, 90F);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class ModernCard : Panel
    {
        private Color borderColor = Color.FromArgb(54, 91, 111);
        private int cornerRadius = 10;

        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = Math.Max(2, value); Invalidate(); }
        }

        public ModernCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(16, 29, 38);
            Padding = new Padding(12);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width < 4 || Height < 4) { return; }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color surface = Enabled ? BackColor : Color.FromArgb(12, 22, 29);
            Color outline = Enabled ? borderColor : Color.FromArgb(45, 59, 68);
            Rectangle bounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = ModernDrawing.RoundedRectangle(bounds, cornerRadius))
            using (Pen pen = new Pen(outline, 1F))
            using (SolidBrush brush = new SolidBrush(surface))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    internal sealed class ModernGroupBox : GroupBox
    {
        private int cornerRadius = 8;

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = Math.Max(2, value); Invalidate(); }
        }

        public ModernGroupBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(16, 29, 38);
            ForeColor = Color.FromArgb(238, 242, 246);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width < 4 || Height < 16) { return; }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(1, 10, Width - 3, Height - 12);
            using (GraphicsPath path = ModernDrawing.RoundedRectangle(bounds, cornerRadius))
            using (Pen pen = new Pen(Color.FromArgb(54, 81, 98), 1F))
            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            if (!string.IsNullOrWhiteSpace(Text))
            {
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(18, 31, 40)))
                using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(20, 224, 205)))
                {
                    e.Graphics.FillRectangle(titleBrush, 14, 0, Math.Min(210, Width - 28), 22);
                    e.Graphics.FillRectangle(accentBrush, 14, 4, 3, 14);
                }
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    new Rectangle(25, 0, Math.Max(1, Width - 34), 22),
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }

    internal sealed class ModernButton : Button
    {
        private bool hovered;
        private bool pressed;
        private bool accent;
        private bool accentBorder;
        private bool iconOnly;
        private string glyph = string.Empty;

        public bool Accent
        {
            get { return accent; }
            set { accent = value; Invalidate(); }
        }

        public bool AccentBorder
        {
            get { return accentBorder; }
            set { accentBorder = value; Invalidate(); }
        }

        public string Glyph
        {
            get { return glyph; }
            set { glyph = value ?? string.Empty; Invalidate(); }
        }

        public bool IconOnly
        {
            get { return iconOnly; }
            set { iconOnly = value; Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.FromArgb(20, 38, 49);
            ForeColor = Color.FromArgb(238, 242, 246);
            Cursor = Cursors.Hand;
            UseCompatibleTextRendering = false;
            TextAlign = ContentAlignment.MiddleCenter;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width < 4 || Height < 4) { return; }

            Color border = accent || accentBorder
                ? Color.FromArgb(16, 225, 205)
                : Color.FromArgb(63, 91, 108);
            Color fill = accent
                ? Color.FromArgb(0, pressed ? 116 : (hovered ? 165 : 142), pressed ? 108 : 147)
                : Color.FromArgb(19, hovered ? 48 : 37, hovered ? 62 : 49);
            Color textColor = Enabled
                ? (accent ? Color.FromArgb(245, 250, 250) : ForeColor)
                : Color.FromArgb(104, 121, 132);
            if (!Enabled)
            {
                border = Color.FromArgb(45, 59, 68);
                fill = Color.FromArgb(18, 27, 33);
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = ModernDrawing.RoundedRectangle(bounds, 8))
            using (Pen pen = new Pen(border, 1F))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            if (iconOnly)
            {
                int iconSize = Math.Min(32, Math.Max(20, Math.Min(Width - 8, Height - 8)));
                Rectangle iconBounds = new Rectangle(
                    Math.Max(0, (Width - iconSize) / 2),
                    Math.Max(0, (Height - iconSize) / 2),
                    iconSize,
                    iconSize);
                int iconWidth;
                DrawVectorGlyph(e.Graphics, glyph, iconBounds, textColor, fill, out iconWidth);
                if (Focused && ShowFocusCues && Enabled)
                {
                    Rectangle focus = new Rectangle(4, 4, Math.Max(1, Width - 9), Math.Max(1, Height - 9));
                    ControlPaint.DrawFocusRectangle(e.Graphics, focus, textColor, fill);
                }
                return;
            }

            Rectangle content = new Rectangle(8, 2, Math.Max(1, Width - 16), Math.Max(1, Height - 4));
            Font compactFont = null;
            try
            {
                Font textFont = Font;
                if (!string.IsNullOrWhiteSpace(glyph))
                {
                    int glyphWidth;
                    Rectangle glyphBounds = new Rectangle(content.Left, content.Top,
                                                          Math.Min(32, Math.Max(1, content.Width)),
                                                          content.Height);
                    if (DrawVectorGlyph(e.Graphics, glyph, glyphBounds, textColor, fill, out glyphWidth))
                    {
                        content = new Rectangle(
                            content.Left + glyphWidth + 6,
                            content.Top,
                            Math.Max(1, content.Width - glyphWidth - 6),
                            content.Height);
                        goto DrawButtonText;
                    }
                    if (string.Equals(glyph, "shield", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(glyph, "shield-stop", StringComparison.OrdinalIgnoreCase))
                    {
                        int left = content.Left + 2;
                        int top = content.Top + Math.Max(2, (content.Height - 21) / 2);
                        Point[] shield =
                        {
                            new Point(left + 10, top),
                            new Point(left + 20, top + 4),
                            new Point(left + 19, top + 13),
                            new Point(left + 10, top + 21),
                            new Point(left + 1, top + 13),
                            new Point(left, top + 4)
                        };
                        using (SolidBrush shieldBrush = new SolidBrush(textColor))
                        using (SolidBrush cutoutBrush = new SolidBrush(fill))
                        {
                            e.Graphics.FillPolygon(shieldBrush, shield);
                            e.Graphics.FillRectangle(cutoutBrush, left + 7, top + 9, 7, 6);
                        }
                        glyphWidth = 24;
                    }
                    else
                    {
                        using (Font glyphFont = new Font(Font.FontFamily, Math.Max(10F, Font.Size + 2F), FontStyle.Regular))
                        {
                            Size glyphSize = TextRenderer.MeasureText(glyph, glyphFont,
                                new Size(Int32.MaxValue, Int32.MaxValue), TextFormatFlags.NoPadding);
                            glyphWidth = Math.Max(18, glyphSize.Width);
                            int startX = content.Left;
                            Rectangle textGlyphBounds = new Rectangle(startX, content.Top, glyphWidth, content.Height);
                            TextRenderer.DrawText(e.Graphics, glyph, glyphFont, textGlyphBounds, textColor,
                                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                        }
                    }
                    content = new Rectangle(
                        content.Left + glyphWidth + 6,
                        content.Top,
                        Math.Max(1, content.Width - glyphWidth - 6),
                        content.Height);
                }

                DrawButtonText:
                Size availableText = TextRenderer.MeasureText(Text ?? string.Empty, textFont,
                    new Size(Int32.MaxValue, Int32.MaxValue), TextFormatFlags.NoPadding);
                if (availableText.Width > content.Width && Font.Size > 8F)
                {
                    compactFont = new Font(Font.FontFamily, Math.Max(8F, Font.Size - 1F), Font.Style);
                    textFont = compactFont;
                }
                TextRenderer.DrawText(e.Graphics, Text ?? string.Empty, textFont, content, textColor,
                                      TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                      TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
            finally
            {
                if (compactFont != null) { compactFont.Dispose(); }
            }

            if (Focused && ShowFocusCues && Enabled)
            {
                Rectangle focus = new Rectangle(4, 4, Math.Max(1, Width - 9), Math.Max(1, Height - 9));
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, textColor, fill);
            }
        }

        private static bool DrawVectorGlyph(Graphics graphics, string value, Rectangle bounds,
                                            Color color, Color fill, out int glyphWidth)
        {
            glyphWidth = 0;
            if (string.IsNullOrWhiteSpace(value)) { return false; }

            string key = value.Trim().ToLowerInvariant();
            int assetWidth;
            if (UiIconRenderer.TryDraw(graphics, key, bounds, color, fill, out assetWidth))
            {
                glyphWidth = assetWidth;
                return true;
            }

            int size = Math.Min(28, Math.Max(18, Math.Min(bounds.Width, bounds.Height)));
            int left = bounds.Left + Math.Max(0, (bounds.Width - size) / 2);
            int top = bounds.Top + Math.Max(0, (bounds.Height - size) / 2);
            float centerX = left + size / 2F;
            float centerY = top + size / 2F;
            using (Pen pen = new Pen(color, Math.Max(1.4F, size / 12F)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (key == "refresh")
                {
                    Rectangle arc = new Rectangle(left + 3, top + 3, size - 6, size - 6);
                    graphics.DrawArc(pen, arc, 42F, 275F);
                    Point arrowTip = new Point(left + size - 3, top + 6);
                    graphics.DrawLine(pen, arrowTip, new Point(arrowTip.X - 7, arrowTip.Y - 1));
                    graphics.DrawLine(pen, arrowTip, new Point(arrowTip.X - 1, arrowTip.Y + 6));
                    glyphWidth = 30;
                    return true;
                }
                if (key == "undo")
                {
                    Rectangle arc = new Rectangle(left + 3, top + 3, size - 6, size - 6);
                    graphics.DrawArc(pen, arc, 215F, 245F);
                    Point arrowTip = new Point(left + 4, top + size / 2);
                    graphics.DrawLine(pen, arrowTip, new Point(arrowTip.X + 7, arrowTip.Y - 6));
                    graphics.DrawLine(pen, arrowTip, new Point(arrowTip.X + 7, arrowTip.Y + 6));
                    glyphWidth = 30;
                    return true;
                }
                if (key == "log")
                {
                    Rectangle page = new Rectangle(left + 5, top + 3, size - 9, size - 6);
                    using (GraphicsPath pagePath = ModernDrawing.RoundedRectangle(page, 2))
                    {
                        graphics.DrawPath(pen, pagePath);
                    }
                    graphics.DrawLine(pen, left + 10, top + 10, left + size - 8, top + 10);
                    graphics.DrawLine(pen, left + 10, top + 15, left + size - 8, top + 15);
                    graphics.DrawLine(pen, left + 10, top + 20, left + size - 12, top + 20);
                    glyphWidth = 30;
                    return true;
                }
                if (key == "launch")
                {
                    graphics.DrawLine(pen, left + 7, top + size - 7, left + size - 6, top + 6);
                    graphics.DrawLine(pen, left + size - 6, top + 6, left + size - 7, top + 14);
                    graphics.DrawLine(pen, left + size - 6, top + 6, left + size - 14, top + 7);
                    graphics.DrawLine(pen, left + 6, top + size - 12, left + 6, top + size - 6);
                    graphics.DrawLine(pen, left + 6, top + size - 6, left + 12, top + size - 6);
                    glyphWidth = 30;
                    return true;
                }
                if (key == "gear")
                {
                    graphics.DrawEllipse(pen, new Rectangle(left + 7, top + 7, size - 14, size - 14));
                    graphics.DrawEllipse(pen, new Rectangle(left + 11, top + 11, size - 22, size - 22));
                    for (int index = 0; index < 8; index++)
                    {
                        double angle = index * Math.PI / 4D;
                        float x1 = centerX + (float)Math.Cos(angle) * (size / 2F - 2F);
                        float y1 = centerY + (float)Math.Sin(angle) * (size / 2F - 2F);
                        float x2 = centerX + (float)Math.Cos(angle) * (size / 2F - 6F);
                        float y2 = centerY + (float)Math.Sin(angle) * (size / 2F - 6F);
                        graphics.DrawLine(pen, x1, y1, x2, y2);
                    }
                    glyphWidth = 30;
                    return true;
                }
                if (key == "heart")
                {
                    using (GraphicsPath heart = new GraphicsPath())
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        heart.AddBezier(left + size / 2F, top + size - 4,
                                        left + 2, top + size * 0.58F,
                                        left + 2, top + 5,
                                        left + size / 2F, top + 10);
                        heart.AddBezier(left + size / 2F, top + 10,
                                        left + size - 2, top + 5,
                                        left + size - 2, top + size * 0.58F,
                                        left + size / 2F, top + size - 4);
                        graphics.FillPath(brush, heart);
                    }
                    glyphWidth = 30;
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class ModernComboBox : ComboBox
    {
        private bool hovered;
        private Panel arrowOverlay;
        private Panel leadingOverlay;
        private bool iconOnly;
        private bool dropdownOpen;
        private bool showGlobe;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        public ModernComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 25;
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(17, 18, 20);
            ForeColor = Color.FromArgb(241, 244, 247);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public bool ShowGlobe
        {
            get { return showGlobe; }
            set
            {
                showGlobe = value;
                if (IsHandleCreated)
                {
                    CreateLeadingOverlay();
                    UpdateLeadingOverlayLayout();
                }
                Invalidate();
            }
        }

        public bool IconOnly
        {
            get { return iconOnly; }
            set
            {
                iconOnly = value;
                if (IsHandleCreated)
                {
                    CreateArrowOverlay();
                    CreateLeadingOverlay();
                    if (arrowOverlay != null) { arrowOverlay.Visible = !iconOnly; }
                    UpdateLeadingOverlayLayout();
                }
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnDropDown(EventArgs e)
        {
            dropdownOpen = true;
            base.OnDropDown(e);
            Invalidate();
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            dropdownOpen = false;
            base.OnDropDownClosed(e);
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                SetWindowTheme(Handle, string.Empty, string.Empty);
            }
            catch
            {
                // The control remains functional when the theme API is unavailable.
            }
            CreateArrowOverlay();
            CreateLeadingOverlay();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (arrowOverlay != null)
            {
                Controls.Remove(arrowOverlay);
                arrowOverlay.Dispose();
                arrowOverlay = null;
            }
            if (leadingOverlay != null)
            {
                Controls.Remove(leadingOverlay);
                leadingOverlay.Dispose();
                leadingOverlay = null;
            }
            base.OnHandleDestroyed(e);
        }

        private void CreateArrowOverlay()
        {
            if (arrowOverlay != null) { return; }
            arrowOverlay = new Panel
            {
                Dock = DockStyle.Right,
                Width = 24,
                BackColor = Color.FromArgb(22, 39, 48),
                Cursor = Cursors.Hand,
                TabStop = false,
                Margin = new Padding(0),
                Visible = !iconOnly
            };
            arrowOverlay.Paint += ArrowOverlay_Paint;
            arrowOverlay.Click += delegate
            {
                if (Enabled) { DroppedDown = !DroppedDown; }
            };
            Controls.Add(arrowOverlay);
            arrowOverlay.BringToFront();
        }

        private void CreateLeadingOverlay()
        {
            if (!showGlobe || leadingOverlay != null) { return; }
            leadingOverlay = new Panel
            {
                Dock = DockStyle.Left,
                Width = 28,
                BackColor = Color.FromArgb(17, 18, 20),
                Cursor = Cursors.Hand,
                TabStop = false,
                Margin = new Padding(0),
                Visible = showGlobe
            };
            leadingOverlay.Paint += LeadingOverlay_Paint;
            leadingOverlay.Resize += delegate { UpdateLeadingOverlayRegion(); };
            leadingOverlay.Click += delegate
            {
                if (Enabled) { DroppedDown = !DroppedDown; }
            };
            Controls.Add(leadingOverlay);
            UpdateLeadingOverlayLayout();
        }

        private void UpdateLeadingOverlayLayout()
        {
            if (leadingOverlay == null) { return; }

            leadingOverlay.Dock = iconOnly ? DockStyle.Fill : DockStyle.Left;
            leadingOverlay.Width = iconOnly ? 0 : 28;
            leadingOverlay.BackColor = iconOnly
                ? GetIconOnlyBackground()
                : Color.FromArgb(17, 18, 20);
            leadingOverlay.Visible = showGlobe;
            UpdateLeadingOverlayRegion();
            leadingOverlay.BringToFront();
        }

        private Color GetIconOnlyBackground()
        {
            if (Parent != null && Parent.Parent != null)
            {
                return Parent.Parent.BackColor;
            }
            return Color.FromArgb(10, 20, 28);
        }

        private void UpdateLeadingOverlayRegion()
        {
            if (leadingOverlay == null) { return; }

            Region previous = leadingOverlay.Region;
            leadingOverlay.Region = null;
            if (previous != null) { previous.Dispose(); }
        }

        private void LeadingOverlay_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (iconOnly)
            {
                Color fill = Enabled
                    ? Color.FromArgb(19, hovered ? 48 : 37, hovered ? 62 : 49)
                    : Color.FromArgb(18, 27, 33);
                Color border = Enabled ? Color.FromArgb(63, 91, 108) : Color.FromArgb(45, 59, 68);
                Color textColor = Enabled ? ForeColor : Color.FromArgb(104, 121, 132);
                Rectangle bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
                using (GraphicsPath path = ModernDrawing.RoundedRectangle(bounds, 8))
                using (SolidBrush brush = new SolidBrush(fill))
                using (Pen pen = new Pen(border, 1F))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }

                int iconSize = Math.Min(28, Math.Max(20, Math.Min(Width - 8, Height - 8)));
                Rectangle iconBounds = new Rectangle(
                    Math.Max(0, (Width - iconSize) / 2),
                    Math.Max(0, (Height - iconSize) / 2),
                    iconSize,
                    iconSize);
                int globeWidth;
                if (UiIconRenderer.TryDraw(e.Graphics, "globe", iconBounds,
                                           textColor,
                                           fill, out globeWidth))
                {
                    if (Focused && ShowFocusCues && Enabled)
                    {
                        Rectangle focus = new Rectangle(4, 4, Math.Max(1, Width - 9), Math.Max(1, Height - 9));
                        ControlPaint.DrawFocusRectangle(e.Graphics, focus, textColor, fill);
                    }
                    return;
                }
                using (Pen pen = new Pen(textColor, 1.4F))
                {
                    Rectangle globe = new Rectangle(iconBounds.X + 3, iconBounds.Y + 3,
                                                    Math.Max(1, iconBounds.Width - 6),
                                                    Math.Max(1, iconBounds.Height - 6));
                    e.Graphics.DrawEllipse(pen, globe);
                    e.Graphics.DrawArc(pen, new Rectangle(globe.X + globe.Width / 4, globe.Y,
                                                          Math.Max(1, globe.Width / 2), globe.Height),
                                      90F, 180F);
                    e.Graphics.DrawArc(pen, new Rectangle(globe.X + globe.Width / 4, globe.Y,
                                                          Math.Max(1, globe.Width / 2), globe.Height),
                                      270F, 180F);
                    e.Graphics.DrawLine(pen, globe.Left, globe.Y + globe.Height / 2F,
                                        globe.Right, globe.Y + globe.Height / 2F);
                }
                if (Focused && ShowFocusCues && Enabled)
                {
                    Rectangle focus = new Rectangle(4, 4, Math.Max(1, Width - 9), Math.Max(1, Height - 9));
                    ControlPaint.DrawFocusRectangle(e.Graphics, focus, textColor, fill);
                }
                return;
            }

            Color color = Enabled ? Color.FromArgb(164, 190, 204) : Color.FromArgb(95, 111, 119);
            int fallbackGlobeWidth;
            if (UiIconRenderer.TryDraw(e.Graphics, "globe", new Rectangle(2, 2, 24, 24),
                                       color, BackColor, out fallbackGlobeWidth))
            {
                return;
            }
            using (Pen pen = new Pen(color, 1.2F))
            {
                Rectangle globe = new Rectangle(6, 5, 16, 16);
                e.Graphics.DrawEllipse(pen, globe);
                e.Graphics.DrawArc(pen, new Rectangle(10, 5, 8, 16), 90F, 180F);
                e.Graphics.DrawArc(pen, new Rectangle(10, 5, 8, 16), 270F, 180F);
                e.Graphics.DrawLine(pen, 7, 13, 21, 13);
            }
        }

        private void ArrowOverlay_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen separatorPen = new Pen(Color.FromArgb(55, 84, 99), 1F))
            using (Pen chevronPen = new Pen(Enabled ? Color.FromArgb(164, 190, 204) : Color.FromArgb(95, 111, 119), 1.6F))
            {
                e.Graphics.DrawLine(separatorPen, 0, 1, 0, Math.Max(1, arrowOverlay.Height - 2));
                chevronPen.StartCap = LineCap.Round;
                chevronPen.EndCap = LineCap.Round;
                float centerX = arrowOverlay.Width / 2F;
                float centerY = arrowOverlay.Height / 2F;
                e.Graphics.DrawLine(chevronPen, centerX - 4, centerY - 2, centerX, centerY + 2);
                e.Graphics.DrawLine(chevronPen, centerX, centerY + 2, centerX + 4, centerY - 2);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) { return; }

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color background = selected ? Color.FromArgb(24, 67, 77) : BackColor;
            Color foreground = Enabled ? ForeColor : Color.FromArgb(105, 121, 132);
            using (SolidBrush brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            if (!iconOnly || dropdownOpen)
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    GetItemText(Items[e.Index]),
                    Font,
                    new Rectangle(e.Bounds.Left + 6, e.Bounds.Top, Math.Max(1, e.Bounds.Width - 12), e.Bounds.Height),
                    foreground,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            {
                using (Pen pen = new Pen(Color.FromArgb(20, 224, 205)))
                {
                    Rectangle focus = new Rectangle(e.Bounds.X, e.Bounds.Y,
                                                    Math.Max(1, e.Bounds.Width - 1), Math.Max(1, e.Bounds.Height - 1));
                    e.Graphics.DrawRectangle(pen, focus);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x000F && !DroppedDown)
            {
                DrawClosedSurface();
            }
        }

        private void DrawClosedSurface()
        {
            if (Width < 8 || Height < 8 || IsDisposed) { return; }

            using (Graphics graphics = Graphics.FromHwnd(Handle))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color fill = Enabled
                    ? (hovered ? Color.FromArgb(22, 42, 52) : Color.FromArgb(17, 18, 20))
                    : Color.FromArgb(20, 27, 31);
                Color border = Enabled ? Color.FromArgb(79, 109, 124) : Color.FromArgb(49, 65, 73);
                if (iconOnly)
                {
                    Rectangle bounds = new Rectangle(1, 1, Width - 3, Height - 3);
                    using (GraphicsPath path = ModernDrawing.RoundedRectangle(bounds, 8))
                    using (SolidBrush fillBrush = new SolidBrush(fill))
                    using (Pen borderPen = new Pen(border, 1F))
                    {
                        graphics.FillPath(fillBrush, path);
                        graphics.DrawPath(borderPen, path);
                    }

                    int iconSize = Math.Min(28, Math.Max(20, Math.Min(Width - 8, Height - 8)));
                    Rectangle iconBounds = new Rectangle(
                        Math.Max(0, (Width - iconSize) / 2),
                        Math.Max(0, (Height - iconSize) / 2),
                        iconSize,
                        iconSize);
                    int iconWidth;
                    if (!UiIconRenderer.TryDraw(graphics, "globe", iconBounds,
                                                Enabled ? ForeColor : Color.FromArgb(95, 111, 119),
                                                fill, out iconWidth))
                    {
                        using (Pen pen = new Pen(Enabled ? ForeColor : Color.FromArgb(95, 111, 119), 1.4F))
                        {
                            Rectangle globe = new Rectangle(iconBounds.X + 3, iconBounds.Y + 3,
                                                            Math.Max(1, iconBounds.Width - 6),
                                                            Math.Max(1, iconBounds.Height - 6));
                            graphics.DrawEllipse(pen, globe);
                            graphics.DrawArc(pen, new Rectangle(globe.X + globe.Width / 4, globe.Y,
                                                                Math.Max(1, globe.Width / 2), globe.Height),
                                              90F, 180F);
                            graphics.DrawArc(pen, new Rectangle(globe.X + globe.Width / 4, globe.Y,
                                                                Math.Max(1, globe.Width / 2), globe.Height),
                                              270F, 180F);
                            graphics.DrawLine(pen, globe.Left, globe.Y + globe.Height / 2F,
                                              globe.Right, globe.Y + globe.Height / 2F);
                        }
                    }
                    return;
                }

                using (SolidBrush fillBrush = new SolidBrush(fill))
                using (Pen borderPen = new Pen(border, 1F))
                {
                    graphics.FillRectangle(fillBrush, 1, 1, Width - 2, Height - 2);
                    graphics.DrawRectangle(borderPen, 1, 1, Width - 3, Height - 3);
                }

                int arrowWidth = Math.Min(24, Math.Max(18, Width / 5));
                Rectangle arrow = new Rectangle(Width - arrowWidth - 1, 1, arrowWidth, Math.Max(1, Height - 2));
                using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(22, 39, 48)))
                using (Pen separatorPen = new Pen(Color.FromArgb(55, 84, 99), 1F))
                using (Pen chevronPen = new Pen(Enabled ? Color.FromArgb(164, 190, 204) : Color.FromArgb(95, 111, 119), 1.6F))
                {
                    graphics.FillRectangle(arrowBrush, arrow);
                    graphics.DrawLine(separatorPen, arrow.Left, arrow.Top + 1, arrow.Left, arrow.Bottom - 1);
                    chevronPen.StartCap = LineCap.Round;
                    chevronPen.EndCap = LineCap.Round;
                    float centerX = arrow.Left + arrow.Width / 2F;
                    float centerY = arrow.Top + arrow.Height / 2F;
                    graphics.DrawLine(chevronPen, centerX - 4, centerY - 2, centerX, centerY + 2);
                    graphics.DrawLine(chevronPen, centerX, centerY + 2, centerX + 4, centerY - 2);
                }

                TextRenderer.DrawText(
                    graphics,
                    SelectedIndex >= 0 ? GetItemText(SelectedItem) : (Text ?? string.Empty),
                    Font,
                    new Rectangle(7, 1, Math.Max(1, arrow.Left - 10), Math.Max(1, Height - 2)),
                    Enabled ? ForeColor : Color.FromArgb(105, 121, 132),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }
    }

    internal sealed class ModernCheckBox : CheckBox
    {
        public ModernCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            AutoSize = true;
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(238, 242, 246);
            Height = 30;
            FlatStyle = FlatStyle.Flat;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size textSize = TextRenderer.MeasureText(Text ?? string.Empty, Font);
            return new Size(textSize.Width + 36, Math.Max(30, textSize.Height + 8));
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
            if (Parent != null) { Parent.PerformLayout(); }
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BackColor == Color.Transparent && Parent != null)
            {
                e.Graphics.Clear(Parent.BackColor);
            }
            else
            {
                e.Graphics.Clear(BackColor);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int boxSize = Math.Min(22, Math.Max(18, Height - 8));
            Rectangle box = new Rectangle(1, Math.Max(3, (Height - boxSize) / 2), boxSize, boxSize);
            Color outline = Enabled ? Color.FromArgb(90, 116, 132) : Color.FromArgb(55, 71, 80);
            Color fill = Checked && Enabled ? Color.FromArgb(0, 126, 235) :
                         (Checked ? Color.FromArgb(44, 72, 82) : Color.FromArgb(20, 34, 43));
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(outline, 1F))
            {
                e.Graphics.FillRectangle(brush, box);
                e.Graphics.DrawRectangle(pen, box);
            }
            if (Checked)
            {
                using (Pen checkPen = new Pen(Enabled ? Color.White : Color.FromArgb(140, 155, 160), 2.2F))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    Point first = new Point(box.Left + 5, box.Top + box.Height / 2);
                    Point second = new Point(box.Left + 9, box.Bottom - 5);
                    Point third = new Point(box.Right - 4, box.Top + 5);
                    e.Graphics.DrawLines(checkPen, new[] { first, second, third });
                }
            }

            Rectangle textBounds = new Rectangle(32, 0, Math.Max(1, Width - 34), Height);
            TextRenderer.DrawText(e.Graphics, Text ?? string.Empty, Font, textBounds,
                                  Enabled ? ForeColor : Color.FromArgb(107, 123, 132),
                                  TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            if (Focused && ShowFocusCues)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics,
                    new Rectangle(30, 2, Math.Max(1, Width - 33), Math.Max(1, Height - 5)));
            }
        }
    }

    internal sealed class StatusBadge : Label
    {
        public StatusBadge()
        {
            AutoSize = false;
            BackColor = Color.Transparent;
            TextAlign = ContentAlignment.MiddleCenter;
            Padding = new Padding(48, 0, 10, 0);
            MinimumSize = new Size(170, 30);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null) { e.Graphics.Clear(Parent.BackColor); }
            else { e.Graphics.Clear(Color.Transparent); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width < 8 || Height < 8) { return; }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color border = Color.FromArgb(220, ForeColor);
            Color fill = Color.FromArgb(31, 48, 57);
            Rectangle bounds = new Rectangle(1, 2, Width - 3, Height - 5);
            using (GraphicsPath path = ModernDrawing.RoundedRectangle(bounds, Math.Max(6, Height / 2)))
            using (Pen pen = new Pen(border, 1.4F))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            int heartbeatWidth;
            if (!UiIconRenderer.TryDraw(e.Graphics, "heartbeat", new Rectangle(8, 4, 32,
                                                                                  Math.Max(18, Height - 8)),
                                         ForeColor, fill, out heartbeatWidth))
            {
                using (Pen pulsePen = new Pen(ForeColor, 2F))
                {
                    pulsePen.StartCap = LineCap.Round;
                    pulsePen.EndCap = LineCap.Round;
                    int mid = Height / 2;
                    int left = 14;
                    e.Graphics.DrawLines(pulsePen, new[]
                    {
                        new Point(left, mid),
                        new Point(left + 8, mid),
                        new Point(left + 13, mid - 8),
                        new Point(left + 18, mid + 7),
                        new Point(left + 23, mid),
                        new Point(left + 31, mid)
                    });
                }
            }

            Rectangle textBounds = new Rectangle(46, 2, Math.Max(1, Width - 52), Height - 5);
            TextRenderer.DrawText(e.Graphics, Text ?? string.Empty, Font, textBounds,
                                  Enabled ? ForeColor : Color.FromArgb(113, 126, 136),
                                  TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }

    internal sealed class StatePill : Label
    {
        private Color stateColor = Color.FromArgb(20, 224, 205);

        public Color StateColor
        {
            get { return stateColor; }
            set { stateColor = value; ForeColor = value; Invalidate(); }
        }

        public StatePill()
        {
            AutoSize = false;
            BackColor = Color.Transparent;
            ForeColor = stateColor;
            TextAlign = ContentAlignment.MiddleCenter;
            MinimumSize = new Size(94, 30);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null) { e.Graphics.Clear(Parent.BackColor); }
            else { e.Graphics.Clear(Color.Transparent); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width < 8 || Height < 8) { return; }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color effectiveColor = Enabled ? stateColor : Color.FromArgb(111, 126, 135);
            Color outline = Color.FromArgb(Enabled ? 235 : 180, effectiveColor);
            Color fill = Enabled
                ? Color.FromArgb(25, stateColor.R, stateColor.G, stateColor.B)
                : Color.FromArgb(18, 27, 33);
            Rectangle bounds = new Rectangle(1, 2, Width - 3, Height - 5);
            using (GraphicsPath path = ModernDrawing.RoundedRectangle(bounds, Math.Max(6, Height / 2)))
            using (Pen pen = new Pen(outline, 1.2F))
            using (SolidBrush brush = new SolidBrush(fill))
            using (SolidBrush dotBrush = new SolidBrush(effectiveColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
                int dot = Math.Min(10, Math.Max(7, Height / 3));
                e.Graphics.FillEllipse(dotBrush, 13, (Height - dot) / 2, dot, dot);
            }

            Rectangle textBounds = new Rectangle(29, 2, Math.Max(1, Width - 36), Height - 5);
            TextRenderer.DrawText(e.Graphics, Text ?? string.Empty, Font, textBounds,
                                  effectiveColor,
                                  TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                  TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }

    internal enum NetworkGlyphKind
    {
        Unknown,
        WiFi,
        Bluetooth,
        Ethernet
    }

    internal sealed class NetworkGlyph : Control
    {
        private NetworkGlyphKind glyphKind = NetworkGlyphKind.Unknown;
        private Color glyphColor = Color.FromArgb(20, 224, 205);

        public NetworkGlyphKind GlyphKind
        {
            get { return glyphKind; }
            set { glyphKind = value; Invalidate(); }
        }

        public bool Bluetooth
        {
            get { return glyphKind == NetworkGlyphKind.Bluetooth; }
            set
            {
                glyphKind = value ? NetworkGlyphKind.Bluetooth : NetworkGlyphKind.WiFi;
                Invalidate();
            }
        }

        public Color GlyphColor
        {
            get { return glyphColor; }
            set { glyphColor = value; Invalidate(); }
        }

        public NetworkGlyph()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            MinimumSize = new Size(38, 38);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null) { e.Graphics.Clear(Parent.BackColor); }
            else { e.Graphics.Clear(Color.Transparent); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color effectiveColor = Enabled ? glyphColor : Color.FromArgb(83, 104, 116);
            string iconName = glyphKind == NetworkGlyphKind.WiFi ? "network-wifi" :
                              glyphKind == NetworkGlyphKind.Bluetooth ? "network-bluetooth" :
                              glyphKind == NetworkGlyphKind.Ethernet ? "network-ethernet" :
                              string.Empty;
            int networkWidth;
            Color background = Parent == null ? Color.Transparent : Parent.BackColor;
            if (!string.IsNullOrWhiteSpace(iconName) &&
                UiIconRenderer.TryDraw(e.Graphics, iconName,
                                       new Rectangle(0, 0, Width, Height),
                                       effectiveColor, background, out networkWidth))
            {
                return;
            }
            using (Pen pen = new Pen(effectiveColor, 3F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float centerX = Width / 2F;
                float centerY = Height / 2F;
                if (glyphKind == NetworkGlyphKind.Bluetooth)
                {
                    DrawBluetooth(e.Graphics, pen, centerX, centerY);
                }
                else if (glyphKind == NetworkGlyphKind.Ethernet)
                {
                    DrawEthernet(e.Graphics, pen, centerX, centerY);
                }
                else if (glyphKind == NetworkGlyphKind.Unknown)
                {
                    DrawUnknown(e.Graphics, pen, centerX, centerY);
                }
                else
                {
                    DrawWiFi(e.Graphics, pen, centerX, centerY);
                }
            }
        }

        private void DrawWiFi(Graphics graphics, Pen pen, float centerX, float centerY)
        {
            float unit = Math.Max(0.7F, Math.Min(Width, Height) / 38F);
            RectangleF arc = new RectangleF(centerX - 17F * unit, centerY - 16F * unit,
                                             34F * unit, 34F * unit);
            graphics.DrawArc(pen, arc, 220F, 100F);
            arc.Inflate(-6F * unit, -6F * unit);
            graphics.DrawArc(pen, arc, 220F, 100F);
            arc.Inflate(-5F * unit, -5F * unit);
            graphics.DrawArc(pen, arc, 220F, 100F);
            using (SolidBrush dotBrush = new SolidBrush(Enabled ? glyphColor : Color.FromArgb(83, 104, 116)))
            {
                graphics.FillEllipse(dotBrush, centerX - 3F * unit,
                                     centerY + 10F * unit, 6F * unit, 6F * unit);
            }
        }

        private void DrawBluetooth(Graphics graphics, Pen pen, float centerX, float centerY)
        {
            float unit = Math.Max(0.7F, Math.Min(Width, Height) / 38F);
            graphics.DrawLine(pen, centerX, centerY - 16F * unit,
                              centerX, centerY + 16F * unit);
            graphics.DrawLine(pen, centerX, centerY - 16F * unit,
                              centerX + 10F * unit, centerY - 7F * unit);
            graphics.DrawLine(pen, centerX + 10F * unit, centerY - 7F * unit,
                              centerX - 10F * unit, centerY + 9F * unit);
            graphics.DrawLine(pen, centerX - 10F * unit, centerY + 9F * unit,
                              centerX + 10F * unit, centerY - 16F * unit);
            graphics.DrawLine(pen, centerX + 10F * unit, centerY + 16F * unit,
                              centerX - 10F * unit, centerY - 7F * unit);
        }

        private void DrawEthernet(Graphics graphics, Pen pen, float centerX, float centerY)
        {
            float unit = Math.Max(0.7F, Math.Min(Width, Height) / 38F);
            RectangleF port = new RectangleF(centerX - 11F * unit, centerY - 11F * unit,
                                              22F * unit, 17F * unit);
            using (GraphicsPath portPath = ModernDrawing.RoundedRectangle(
                Rectangle.Round(port), (int)Math.Max(2F, 3F * unit)))
            {
                graphics.DrawPath(pen, portPath);
            }

            for (int pin = 0; pin < 4; pin++)
            {
                float x = centerX - 7.5F * unit + pin * 5F * unit;
                graphics.DrawLine(pen, x, centerY + 1F * unit,
                                  x, centerY + 7F * unit);
            }
            graphics.DrawLine(pen, centerX, centerY + 7F * unit,
                              centerX, centerY + 16F * unit);
            graphics.DrawLine(pen, centerX - 5F * unit, centerY + 16F * unit,
                              centerX + 5F * unit, centerY + 16F * unit);
        }

        private void DrawUnknown(Graphics graphics, Pen pen, float centerX, float centerY)
        {
            float unit = Math.Max(0.7F, Math.Min(Width, Height) / 38F);
            graphics.DrawLine(pen, centerX, centerY - 10F * unit,
                              centerX, centerY + 1F * unit);
            graphics.DrawLine(pen, centerX, centerY + 1F * unit,
                              centerX - 10F * unit, centerY + 10F * unit);
            graphics.DrawLine(pen, centerX, centerY + 1F * unit,
                              centerX + 10F * unit, centerY + 10F * unit);
            using (SolidBrush brush = new SolidBrush(glyphColor))
            {
                graphics.FillEllipse(brush, centerX - 4F * unit,
                                     centerY - 14F * unit, 8F * unit, 8F * unit);
                graphics.FillEllipse(brush, centerX - 14F * unit,
                                     centerY + 7F * unit, 8F * unit, 8F * unit);
                graphics.FillEllipse(brush, centerX + 6F * unit,
                                     centerY + 7F * unit, 8F * unit, 8F * unit);
            }
        }
    }

    internal sealed class SignalGlyph : Control
    {
        private int level;
        private Color activeColor = Color.FromArgb(20, 224, 205);
        private Color inactiveColor = Color.FromArgb(78, 103, 119);

        public int Level
        {
            get { return level; }
            set { level = Math.Max(0, Math.Min(4, value)); Invalidate(); }
        }

        public Color ActiveColor
        {
            get { return activeColor; }
            set { activeColor = value; Invalidate(); }
        }

        public Color InactiveColor
        {
            get { return inactiveColor; }
            set { inactiveColor = value; Invalidate(); }
        }

        public SignalGlyph()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            MinimumSize = new Size(58, 34);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null) { e.Graphics.Clear(Parent.BackColor); }
            else { e.Graphics.Clear(Color.Transparent); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (Width < 8 || Height < 8) { return; }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color effectiveActive = Enabled ? activeColor : Color.FromArgb(83, 104, 116);
            Color effectiveInactive = Enabled ? inactiveColor : Color.FromArgb(47, 65, 76);
            if (level == 0 || level == 4)
            {
                int signalWidth;
                Color signalColor = level == 0 ? effectiveInactive : effectiveActive;
                if (UiIconRenderer.TryDraw(e.Graphics, "signal",
                                           new Rectangle(0, 0, Width, Height),
                                           signalColor,
                                           Parent == null ? Color.Transparent : Parent.BackColor,
                                           out signalWidth))
                {
                    return;
                }
            }
            int count = 4;
            int gap = Math.Max(3, Width / 22);
            int barWidth = Math.Max(5, (Width - 8 - gap * (count - 1)) / count);
            int baseY = Height - 5;
            using (SolidBrush activeBrush = new SolidBrush(effectiveActive))
            using (SolidBrush inactiveBrush = new SolidBrush(effectiveInactive))
            {
                for (int index = 0; index < count; index++)
                {
                    int height = Math.Max(9, (Height - 12) * (index + 1) / count);
                    int x = 4 + index * (barWidth + gap);
                    Rectangle bar = new Rectangle(x, baseY - height, barWidth, height);
                    e.Graphics.FillRectangle(index < level ? activeBrush : inactiveBrush, bar);
                }
            }
        }
    }
}
