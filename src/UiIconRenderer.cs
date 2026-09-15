using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace NetOptimizerV2
{
    internal static class UiIconRenderer
    {
        private const string ResourcePrefix = "NetOptimizerV2.UiIcons.";
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, string> ResourceNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "network-wifi", ResourcePrefix + "network-wifi" },
                { "network-bluetooth", ResourcePrefix + "network-bluetooth" },
                { "network-ethernet", ResourcePrefix + "network-ethernet" },
                { "signal", ResourcePrefix + "signal" },
                { "heartbeat", ResourcePrefix + "heartbeat" },
                { "shield", ResourcePrefix + "shield" },
                { "shield-stop", ResourcePrefix + "shield" },
                { "refresh", ResourcePrefix + "refresh" },
                { "undo", ResourcePrefix + "undo" },
                { "log", ResourcePrefix + "log" },
                { "gear", ResourcePrefix + "gear" },
                { "globe", ResourcePrefix + "globe" },
                { "heart", ResourcePrefix + "heart" },
                { "launch", ResourcePrefix + "launch" }
            };
        private static readonly Dictionary<string, SvgIconDocument> Documents =
            new Dictionary<string, SvgIconDocument>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> MissingResources =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static bool TryDraw(Graphics graphics, string iconName, Rectangle bounds,
                                     Color tint, Color background, out int iconWidth)
        {
            iconWidth = 0;
            if (graphics == null || bounds.Width <= 0 || bounds.Height <= 0 ||
                string.IsNullOrWhiteSpace(iconName))
            {
                return false;
            }

            string resourceName;
            if (!ResourceNames.TryGetValue(iconName.Trim(), out resourceName))
            {
                return false;
            }

            SvgIconDocument document = GetDocument(resourceName);
            if (document == null)
            {
                return false;
            }

            document.Draw(graphics, bounds, tint, background);
            iconWidth = Math.Max(24, Math.Min(32, bounds.Width));
            return true;
        }

        internal static void RunSelfTest()
        {
            int loaded = 0;
            HashSet<string> resourceNames = new HashSet<string>(
                ResourceNames.Values, StringComparer.OrdinalIgnoreCase);
            foreach (string resourceName in resourceNames)
            {
                if (GetDocument(resourceName) != null)
                {
                    loaded++;
                }
            }

            int expected = resourceNames.Count;
            if (loaded != expected)
            {
                throw new InvalidOperationException(
                    "UI icon resource test failed: loaded=" + loaded + ", expected=" + expected);
            }
            Console.WriteLine("NetOptimizer UI icon assets: PASS (" + expected + " resources)");
        }

        private static SvgIconDocument GetDocument(string resourceName)
        {
            lock (SyncRoot)
            {
                SvgIconDocument document;
                if (Documents.TryGetValue(resourceName, out document))
                {
                    return document;
                }
                if (MissingResources.Contains(resourceName))
                {
                    return null;
                }

                try
                {
                    Assembly assembly = typeof(UiIconRenderer).Assembly;
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            MissingResources.Add(resourceName);
                            return null;
                        }

                        XmlDocument xml = new XmlDocument();
                        xml.XmlResolver = null;
                        xml.Load(stream);
                        document = SvgIconDocument.Parse(xml);
                    }
                    Documents.Add(resourceName, document);
                    return document;
                }
                catch
                {
                    MissingResources.Add(resourceName);
                    return null;
                }
            }
        }

        private sealed class SvgIconDocument
        {
            private readonly XmlElement root;
            private readonly float viewBoxWidth;
            private readonly float viewBoxHeight;

            private SvgIconDocument(XmlElement root, float viewBoxWidth, float viewBoxHeight)
            {
                this.root = root;
                this.viewBoxWidth = viewBoxWidth <= 0 ? 64F : viewBoxWidth;
                this.viewBoxHeight = viewBoxHeight <= 0 ? 64F : viewBoxHeight;
            }

            internal static SvgIconDocument Parse(XmlDocument xml)
            {
                if (xml == null || xml.DocumentElement == null)
                {
                    throw new InvalidDataException("SVG document has no root element.");
                }

                float width = 64F;
                float height = 64F;
                string viewBox = xml.DocumentElement.GetAttribute("viewBox");
                if (!string.IsNullOrWhiteSpace(viewBox))
                {
                    List<float> values = SvgNumbers.Parse(viewBox);
                    if (values.Count >= 4)
                    {
                        width = values[2];
                        height = values[3];
                    }
                }
                return new SvgIconDocument(xml.DocumentElement, width, height);
            }

            internal void Draw(Graphics graphics, Rectangle bounds, Color tint, Color background)
            {
                float scale = Math.Min(bounds.Width / viewBoxWidth, bounds.Height / viewBoxHeight);
                if (scale <= 0F) { return; }

                float offsetX = bounds.Left + (bounds.Width - viewBoxWidth * scale) / 2F;
                float offsetY = bounds.Top + (bounds.Height - viewBoxHeight * scale) / 2F;
                GraphicsState saved = graphics.Save();
                try
                {
                    graphics.TranslateTransform(offsetX, offsetY);
                    graphics.ScaleTransform(scale, scale);
                    DrawElement(graphics, root, SvgStyle.Empty, tint, background);
                }
                finally
                {
                    graphics.Restore(saved);
                }
            }

            private static void DrawElement(Graphics graphics, XmlElement element,
                                            SvgStyle inherited, Color tint, Color background)
            {
                if (element == null) { return; }
                SvgStyle style = inherited.Merge(element);
                GraphicsState saved = graphics.Save();
                try
                {
                    string transformText = element.GetAttribute("transform");
                    if (!string.IsNullOrWhiteSpace(transformText))
                    {
                        using (Matrix transform = SvgTransform.Parse(transformText))
                        {
                            graphics.MultiplyTransform(transform, MatrixOrder.Append);
                        }
                    }

                    string name = element.LocalName.ToLowerInvariant();
                    if (name == "path")
                    {
                        DrawPath(graphics, element.GetAttribute("d"), style, tint, background);
                    }
                    else if (name == "rect")
                    {
                        DrawRectangle(graphics, element, style, tint, background);
                    }
                    else if (name == "circle")
                    {
                        DrawCircle(graphics, element, style, tint, background);
                    }
                    else if (name == "ellipse")
                    {
                        DrawEllipse(graphics, element, style, tint, background);
                    }

                    foreach (XmlNode node in element.ChildNodes)
                    {
                        XmlElement child = node as XmlElement;
                        if (child != null)
                        {
                            DrawElement(graphics, child, style, tint, background);
                        }
                    }
                }
                finally
                {
                    graphics.Restore(saved);
                }
            }

            private static void DrawPath(Graphics graphics, string data, SvgStyle style,
                                         Color tint, Color background)
            {
                if (string.IsNullOrWhiteSpace(data)) { return; }
                using (GraphicsPath path = SvgPathParser.Parse(data))
                {
                    DrawShape(graphics, path, style, tint, background);
                }
            }

            private static void DrawRectangle(Graphics graphics, XmlElement element,
                                              SvgStyle style, Color tint, Color background)
            {
                float x = SvgNumbers.Attribute(element, "x", 0F);
                float y = SvgNumbers.Attribute(element, "y", 0F);
                float width = SvgNumbers.Attribute(element, "width", 0F);
                float height = SvgNumbers.Attribute(element, "height", 0F);
                if (width <= 0F || height <= 0F) { return; }

                float radius = Math.Min(
                    SvgNumbers.Attribute(element, "rx", 0F),
                    SvgNumbers.Attribute(element, "ry", 0F));
                using (GraphicsPath path = new GraphicsPath())
                {
                    if (radius > 0F)
                    {
                        RectangleF rectangle = new RectangleF(x, y, width, height);
                        int diameter = (int)Math.Max(1F, radius * 2F);
                        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180F, 90F);
                        path.AddArc(rectangle.Right - diameter, rectangle.Y,
                                    diameter, diameter, 270F, 90F);
                        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter,
                                    diameter, diameter, 0F, 90F);
                        path.AddArc(rectangle.X, rectangle.Bottom - diameter,
                                    diameter, diameter, 90F, 90F);
                        path.CloseFigure();
                    }
                    else
                    {
                        path.AddRectangle(new RectangleF(x, y, width, height));
                    }
                    DrawShape(graphics, path, style, tint, background);
                }
            }

            private static void DrawCircle(Graphics graphics, XmlElement element,
                                           SvgStyle style, Color tint, Color background)
            {
                float cx = SvgNumbers.Attribute(element, "cx", 0F);
                float cy = SvgNumbers.Attribute(element, "cy", 0F);
                float radius = SvgNumbers.Attribute(element, "r", 0F);
                if (radius <= 0F) { return; }
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(cx - radius, cy - radius, radius * 2F, radius * 2F);
                    DrawShape(graphics, path, style, tint, background);
                }
            }

            private static void DrawEllipse(Graphics graphics, XmlElement element,
                                            SvgStyle style, Color tint, Color background)
            {
                float cx = SvgNumbers.Attribute(element, "cx", 0F);
                float cy = SvgNumbers.Attribute(element, "cy", 0F);
                float rx = SvgNumbers.Attribute(element, "rx", 0F);
                float ry = SvgNumbers.Attribute(element, "ry", 0F);
                if (rx <= 0F || ry <= 0F) { return; }
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(cx - rx, cy - ry, rx * 2F, ry * 2F);
                    DrawShape(graphics, path, style, tint, background);
                }
            }

            private static void DrawShape(Graphics graphics, GraphicsPath path, SvgStyle style,
                                          Color tint, Color background)
            {
                Color fill;
                if (TryResolveColor(style.Fill, tint, background, out fill))
                {
                    using (SolidBrush brush = new SolidBrush(fill))
                    {
                        graphics.FillPath(brush, path);
                    }
                }

                Color stroke;
                if (TryResolveColor(style.Stroke, tint, background, out stroke) &&
                    style.StrokeWidth > 0F)
                {
                    using (Pen pen = new Pen(stroke, style.StrokeWidth))
                    {
                        pen.StartCap = style.LineCap;
                        pen.EndCap = style.LineCap;
                        pen.LineJoin = style.LineJoin;
                        graphics.DrawPath(pen, path);
                    }
                }
            }

            private static bool TryResolveColor(string value, Color tint, Color background,
                                               out Color color)
            {
                color = Color.Empty;
                if (string.IsNullOrWhiteSpace(value) ||
                    string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string text = value.Trim();
                if (text.Equals("#14E0CD", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("#3CAAFF", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("#FFC72A", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("#EAF4FA", StringComparison.OrdinalIgnoreCase))
                {
                    color = tint;
                    return true;
                }
                if (text.Equals("#0A141C", StringComparison.OrdinalIgnoreCase))
                {
                    color = background;
                    return true;
                }
                if (text.StartsWith("#", StringComparison.Ordinal))
                {
                    try
                    {
                        string hex = text.Substring(1);
                        if (hex.Length == 3)
                        {
                            hex = string.Concat(
                                hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
                        }
                        if (hex.Length == 6)
                        {
                            color = Color.FromArgb(
                                int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber),
                                int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber),
                                int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber));
                            return true;
                        }
                    }
                    catch { }
                }
                return false;
            }
        }

        private sealed class SvgStyle
        {
            internal static readonly SvgStyle Empty = new SvgStyle
            {
                Fill = "none",
                Stroke = "none",
                StrokeWidth = 1F,
                LineCap = LineCap.Flat,
                LineJoin = LineJoin.Miter
            };

            internal string Fill { get; private set; }
            internal string Stroke { get; private set; }
            internal float StrokeWidth { get; private set; }
            internal LineCap LineCap { get; private set; }
            internal LineJoin LineJoin { get; private set; }

            internal SvgStyle Merge(XmlElement element)
            {
                SvgStyle result = new SvgStyle
                {
                    Fill = Fill,
                    Stroke = Stroke,
                    StrokeWidth = StrokeWidth,
                    LineCap = LineCap,
                    LineJoin = LineJoin
                };
                Apply(result, element, "fill");
                Apply(result, element, "stroke");
                Apply(result, element, "stroke-width");
                Apply(result, element, "stroke-linecap");
                Apply(result, element, "stroke-linejoin");

                string inlineStyle = element.GetAttribute("style");
                if (!string.IsNullOrWhiteSpace(inlineStyle))
                {
                    string[] declarations = inlineStyle.Split(';');
                    foreach (string declaration in declarations)
                    {
                        string[] parts = declaration.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            ApplyValue(result, parts[0].Trim(), parts[1].Trim());
                        }
                    }
                }
                return result;
            }

            private static void Apply(SvgStyle style, XmlElement element, string attribute)
            {
                if (element.HasAttribute(attribute))
                {
                    ApplyValue(style, attribute, element.GetAttribute(attribute));
                }
            }

            private static void ApplyValue(SvgStyle style, string attribute, string value)
            {
                if (attribute.Equals("fill", StringComparison.OrdinalIgnoreCase))
                {
                    style.Fill = value;
                }
                else if (attribute.Equals("stroke", StringComparison.OrdinalIgnoreCase))
                {
                    style.Stroke = value;
                }
                else if (attribute.Equals("stroke-width", StringComparison.OrdinalIgnoreCase))
                {
                    style.StrokeWidth = SvgNumbers.ParseSingle(value, style.StrokeWidth);
                }
                else if (attribute.Equals("stroke-linecap", StringComparison.OrdinalIgnoreCase))
                {
                    style.LineCap = value.Equals("round", StringComparison.OrdinalIgnoreCase)
                        ? LineCap.Round : LineCap.Flat;
                }
                else if (attribute.Equals("stroke-linejoin", StringComparison.OrdinalIgnoreCase))
                {
                    style.LineJoin = value.Equals("round", StringComparison.OrdinalIgnoreCase)
                        ? LineJoin.Round : LineJoin.Miter;
                }
            }
        }

        private static class SvgNumbers
        {
            internal static float Attribute(XmlElement element, string name, float fallback)
            {
                return element.HasAttribute(name)
                    ? ParseSingle(element.GetAttribute(name), fallback)
                    : fallback;
            }

            internal static float ParseSingle(string value, float fallback)
            {
                float parsed;
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                      out parsed) ? parsed : fallback;
            }

            internal static List<float> Parse(string value)
            {
                List<float> result = new List<float>();
                if (string.IsNullOrWhiteSpace(value)) { return result; }
                string[] parts = value.Replace(',', ' ').Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    float number;
                    if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture,
                                       out number))
                    {
                        result.Add(number);
                    }
                }
                return result;
            }
        }

        private static class SvgTransform
        {
            internal static Matrix Parse(string value)
            {
                Matrix matrix = new Matrix();
                if (string.IsNullOrWhiteSpace(value)) { return matrix; }
                int index = 0;
                while (index < value.Length)
                {
                    while (index < value.Length &&
                           (char.IsWhiteSpace(value[index]) || value[index] == ',')) { index++; }
                    int nameStart = index;
                    while (index < value.Length && char.IsLetter(value[index])) { index++; }
                    if (index == nameStart) { index++; continue; }
                    string name = value.Substring(nameStart, index - nameStart);
                    while (index < value.Length && value[index] != '(') { index++; }
                    if (index >= value.Length) { break; }
                    int end = value.IndexOf(')', index + 1);
                    if (end < 0) { break; }
                    List<float> numbers = SvgNumbers.Parse(
                        value.Substring(index + 1, end - index - 1));
                    if (name.Equals("translate", StringComparison.OrdinalIgnoreCase) &&
                        numbers.Count >= 1)
                    {
                        matrix.Translate(numbers[0], numbers.Count > 1 ? numbers[1] : 0F,
                                         MatrixOrder.Append);
                    }
                    else if (name.Equals("scale", StringComparison.OrdinalIgnoreCase) &&
                             numbers.Count >= 1)
                    {
                        matrix.Scale(numbers[0], numbers.Count > 1 ? numbers[1] : numbers[0],
                                     MatrixOrder.Append);
                    }
                    index = end + 1;
                }
                return matrix;
            }
        }

        private static class SvgPathParser
        {
            internal static GraphicsPath Parse(string data)
            {
                return new Reader(data).Read();
            }

            private sealed class Reader
            {
                private readonly string data;
                private int index;
                private char command;
                private char previousCommand;
                private PointF current;
                private PointF subPathStart;
                private PointF lastCubicControl;
                private PointF lastQuadraticControl;

                internal Reader(string data)
                {
                    this.data = data ?? string.Empty;
                }

                internal GraphicsPath Read()
                {
                    GraphicsPath path = new GraphicsPath();
                    while (true)
                    {
                        SkipSeparators();
                        if (index >= data.Length) { break; }
                        if (IsCommand(data[index]))
                        {
                            command = data[index++];
                            if (command == 'Z' || command == 'z')
                            {
                                path.CloseFigure();
                                current = subPathStart;
                                previousCommand = command;
                                command = '\0';
                                continue;
                            }
                        }
                        if (command == '\0') { index++; continue; }

                        if (command == 'M' || command == 'm')
                        {
                            PointF point;
                            if (!TryReadPoint(out point)) { command = '\0'; continue; }
                            if (command == 'm')
                            {
                                point = new PointF(current.X + point.X, current.Y + point.Y);
                            }
                            path.StartFigure();
                            current = point;
                            subPathStart = point;
                            previousCommand = command;
                            command = command == 'm' ? 'l' : 'L';
                            continue;
                        }
                        if (command == 'L' || command == 'l')
                        {
                            PointF point;
                            if (!TryReadPoint(out point)) { command = '\0'; continue; }
                            if (command == 'l')
                            {
                                point = new PointF(current.X + point.X, current.Y + point.Y);
                            }
                            path.AddLine(current, point);
                            current = point;
                            previousCommand = command;
                            continue;
                        }
                        if (command == 'H' || command == 'h')
                        {
                            float x;
                            if (!TryReadNumber(out x)) { command = '\0'; continue; }
                            if (command == 'h') { x += current.X; }
                            PointF point = new PointF(x, current.Y);
                            path.AddLine(current, point);
                            current = point;
                            previousCommand = command;
                            continue;
                        }
                        if (command == 'V' || command == 'v')
                        {
                            float y;
                            if (!TryReadNumber(out y)) { command = '\0'; continue; }
                            if (command == 'v') { y += current.Y; }
                            PointF point = new PointF(current.X, y);
                            path.AddLine(current, point);
                            current = point;
                            previousCommand = command;
                            continue;
                        }
                        if (command == 'C' || command == 'c')
                        {
                            PointF c1;
                            PointF c2;
                            PointF point;
                            if (!TryReadPoint(out c1) || !TryReadPoint(out c2) ||
                                !TryReadPoint(out point))
                            {
                                command = '\0';
                                continue;
                            }
                            if (command == 'c')
                            {
                                c1 = Add(current, c1);
                                c2 = Add(current, c2);
                                point = Add(current, point);
                            }
                            path.AddBezier(current, c1, c2, point);
                            current = point;
                            lastCubicControl = c2;
                            previousCommand = command;
                            continue;
                        }
                        if (command == 'S' || command == 's')
                        {
                            PointF c2;
                            PointF point;
                            if (!TryReadPoint(out c2) || !TryReadPoint(out point))
                            {
                                command = '\0';
                                continue;
                            }
                            PointF c1 = (previousCommand == 'C' || previousCommand == 'c' ||
                                         previousCommand == 'S' || previousCommand == 's')
                                ? Reflect(current, lastCubicControl)
                                : current;
                            if (command == 's')
                            {
                                c2 = Add(current, c2);
                                point = Add(current, point);
                            }
                            path.AddBezier(current, c1, c2, point);
                            current = point;
                            lastCubicControl = c2;
                            previousCommand = command;
                            continue;
                        }
                        if (command == 'Q' || command == 'q')
                        {
                            PointF control;
                            PointF point;
                            if (!TryReadPoint(out control) || !TryReadPoint(out point))
                            {
                                command = '\0';
                                continue;
                            }
                            if (command == 'q')
                            {
                                control = Add(current, control);
                                point = Add(current, point);
                            }
                            AddQuadratic(path, current, control, point);
                            current = point;
                            lastQuadraticControl = control;
                            previousCommand = command;
                            continue;
                        }
                        if (command == 'T' || command == 't')
                        {
                            PointF point;
                            if (!TryReadPoint(out point)) { command = '\0'; continue; }
                            PointF control = (previousCommand == 'Q' || previousCommand == 'q' ||
                                              previousCommand == 'T' || previousCommand == 't')
                                ? Reflect(current, lastQuadraticControl)
                                : current;
                            if (command == 't') { point = Add(current, point); }
                            AddQuadratic(path, current, control, point);
                            current = point;
                            lastQuadraticControl = control;
                            previousCommand = command;
                            continue;
                        }

                        command = '\0';
                    }
                    return path;
                }

                private bool TryReadPoint(out PointF point)
                {
                    float x;
                    float y;
                    if (!TryReadNumber(out x) || !TryReadNumber(out y))
                    {
                        point = PointF.Empty;
                        return false;
                    }
                    point = new PointF(x, y);
                    return true;
                }

                private bool TryReadNumber(out float value)
                {
                    SkipSeparators();
                    if (index >= data.Length || IsCommand(data[index]))
                    {
                        value = 0F;
                        return false;
                    }

                    int start = index;
                    if (data[index] == '+' || data[index] == '-') { index++; }
                    while (index < data.Length && char.IsDigit(data[index])) { index++; }
                    if (index < data.Length && data[index] == '.')
                    {
                        index++;
                        while (index < data.Length && char.IsDigit(data[index])) { index++; }
                    }
                    if (index < data.Length && (data[index] == 'e' || data[index] == 'E'))
                    {
                        index++;
                        if (index < data.Length && (data[index] == '+' || data[index] == '-'))
                        {
                            index++;
                        }
                        while (index < data.Length && char.IsDigit(data[index])) { index++; }
                    }

                    if (index == start)
                    {
                        value = 0F;
                        return false;
                    }
                    return float.TryParse(data.Substring(start, index - start),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                }

                private void SkipSeparators()
                {
                    while (index < data.Length &&
                           (char.IsWhiteSpace(data[index]) || data[index] == ',')) { index++; }
                }

                private static bool IsCommand(char value)
                {
                    return "MmLlHhVvCcSsQqTtZz".IndexOf(value) >= 0;
                }

                private static PointF Add(PointF first, PointF second)
                {
                    return new PointF(first.X + second.X, first.Y + second.Y);
                }

                private static PointF Reflect(PointF around, PointF point)
                {
                    return new PointF(around.X * 2F - point.X, around.Y * 2F - point.Y);
                }

                private static void AddQuadratic(GraphicsPath path, PointF start,
                                                 PointF control, PointF end)
                {
                    PointF c1 = new PointF(
                        start.X + (control.X - start.X) * 2F / 3F,
                        start.Y + (control.Y - start.Y) * 2F / 3F);
                    PointF c2 = new PointF(
                        end.X + (control.X - end.X) * 2F / 3F,
                        end.Y + (control.Y - end.Y) * 2F / 3F);
                    path.AddBezier(start, c1, c2, end);
                }
            }
        }
    }
}
