using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GakumasSmartLauncher
{
    internal static class Palette
    {
        public static readonly Color Background = Color.FromArgb(249, 247, 255);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceSoft = Color.FromArgb(253, 251, 255);
        public static readonly Color Text = Color.FromArgb(55, 51, 83);
        public static readonly Color TextMuted = Color.FromArgb(126, 118, 153);
        public static readonly Color Primary = Color.FromArgb(240, 91, 143);
        public static readonly Color PrimaryHover = Color.FromArgb(224, 72, 126);
        public static readonly Color PrimaryPressed = Color.FromArgb(198, 56, 108);
        public static readonly Color PrimarySoft = Color.FromArgb(255, 235, 244);
        public static readonly Color Success = Color.FromArgb(44, 166, 139);
        public static readonly Color SuccessSoft = Color.FromArgb(232, 249, 244);
        public static readonly Color Warning = Color.FromArgb(218, 135, 50);
        public static readonly Color WarningSoft = Color.FromArgb(255, 246, 232);
        public static readonly Color Danger = Color.FromArgb(202, 68, 107);
        public static readonly Color Border = Color.FromArgb(231, 224, 243);
        public static readonly Color Indigo = Color.FromArgb(80, 82, 157);
        public static readonly Color Gold = Color.FromArgb(245, 188, 70);
        public static readonly Color Sky = Color.FromArgb(91, 178, 242);
        public static readonly Color Lavender = Color.FromArgb(151, 127, 224);
    }

    internal static class RoundedGeometry
    {
        public static GraphicsPath Create(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            var diameter = Math.Max(2, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal class RoundedPanel : Panel
    {
        private int _cornerRadius = 16;
        private Color _borderColor = Color.Transparent;
        private int _borderWidth;

        public RoundedPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
        }

        public int CornerRadius
        {
            get { return _cornerRadius; }
            set { _cornerRadius = Math.Max(0, value); Invalidate(); UpdateRegion(); }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public int BorderWidth
        {
            get { return _borderWidth; }
            set { _borderWidth = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var path = RoundedGeometry.Create(bounds, _cornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                eventArgs.Graphics.FillPath(brush, path);
                if (_borderWidth > 0 && _borderColor != Color.Transparent)
                {
                    using (var pen = new Pen(_borderColor, _borderWidth))
                    {
                        eventArgs.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            if (Parent != null)
            {
                eventArgs.Graphics.Clear(Parent.BackColor);
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            using (var path = RoundedGeometry.Create(new Rectangle(0, 0, Width, Height), _cornerRadius))
            {
                var oldRegion = Region;
                Region = new Region(path);
                if (oldRegion != null)
                {
                    oldRegion.Dispose();
                }
            }
        }
    }

    internal sealed class IdolHeroPanel : RoundedPanel
    {
        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            {
                using (var previousClip = eventArgs.Graphics.Clip)
                try
                {
                    eventArgs.Graphics.SetClip(new Rectangle(1, 1, Math.Max(1, Width - 2), Math.Max(1, Height - 2)));
                    var bounds = new Rectangle(1, 1, Math.Max(1, Width - 2), Math.Max(1, Height - 2));
                using (var path = RoundedGeometry.Create(bounds, CornerRadius))
                using (var gradient = new LinearGradientBrush(
                    bounds,
                    Color.FromArgb(255, 244, 250),
                    Color.FromArgb(241, 248, 255),
                    12F))
                {
                    eventArgs.Graphics.FillPath(gradient, path);
                }

                using (var ribbon = new Pen(Color.FromArgb(22, Palette.Primary), 12F))
                {
                    eventArgs.Graphics.DrawLine(ribbon, Width - 280, -20, Width + 10, 166);
                }

                using (var ribbon = new Pen(Color.FromArgb(20, Palette.Sky), 8F))
                {
                    eventArgs.Graphics.DrawLine(ribbon, Width - 215, -10, Width + 8, 132);
                }

                DrawBokeh(eventArgs.Graphics, Width - 160, 34, 54, Palette.Primary, 0.10F);
                DrawBokeh(eventArgs.Graphics, Width - 84, 112, 42, Palette.Sky, 0.12F);
                DrawBokeh(eventArgs.Graphics, Width - 268, 126, 30, Palette.Gold, 0.15F);
                DrawStar(eventArgs.Graphics, Width - 94, 36, 12F, Palette.Gold, 0.75F);
                DrawStar(eventArgs.Graphics, Width - 228, 74, 8F, Palette.Primary, 0.48F);
                DrawStar(eventArgs.Graphics, 38, Height - 30, 7F, Palette.Sky, 0.38F);

                if (BorderWidth > 0)
                {
                    using (var path = RoundedGeometry.Create(bounds, CornerRadius))
                    using (var border = new Pen(BorderColor, BorderWidth))
                    {
                        eventArgs.Graphics.DrawPath(border, path);
                    }
                }
                }
                finally
                {
                    eventArgs.Graphics.Clip = previousClip;
                }
            }
        }

        private static void DrawBokeh(Graphics graphics, float x, float y, float size, Color color, float opacity)
        {
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * opacity), color)))
            {
                graphics.FillEllipse(brush, x - (size / 2F), y - (size / 2F), size, size);
            }
        }

        private static void DrawStar(Graphics graphics, float centerX, float centerY, float radius, Color color, float opacity)
        {
            var points = CreateStarPoints(centerX, centerY, radius, radius * 0.43F);
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * opacity), color)))
            {
                graphics.FillPolygon(brush, points);
            }
        }

        internal static PointF[] CreateStarPoints(float centerX, float centerY, float outerRadius, float innerRadius)
        {
            var points = new PointF[10];
            for (var index = 0; index < points.Length; index++)
            {
                var radius = index % 2 == 0 ? outerRadius : innerRadius;
                var angle = (-Math.PI / 2D) + (index * Math.PI / 5D);
                points[index] = new PointF(
                    centerX + ((float)Math.Cos(angle) * radius),
                    centerY + ((float)Math.Sin(angle) * radius));
            }

            return points;
        }
    }

    internal sealed class ModernButton : Button
    {
        private bool _hovered;
        private bool _pressed;

        public ModernButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            CornerRadius = 13;
            NormalBackColor = Palette.Primary;
            HoverBackColor = Palette.PrimaryHover;
            PressedBackColor = Palette.PrimaryPressed;
            DisabledBackColor = Color.FromArgb(218, 222, 232);
            BorderColor = Color.Transparent;
            BorderWidth = 0;
        }

        public int CornerRadius { get; set; }
        public Color NormalBackColor { get; set; }
        public Color HoverBackColor { get; set; }
        public Color PressedBackColor { get; set; }
        public Color DisabledBackColor { get; set; }
        public Color BorderColor { get; set; }
        public int BorderWidth { get; set; }

        protected override void OnMouseEnter(EventArgs eventArgs)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(eventArgs);
        }

        protected override void OnMouseLeave(EventArgs eventArgs)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(eventArgs);
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(eventArgs);
        }

        protected override void OnMouseUp(MouseEventArgs eventArgs)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(eventArgs);
        }

        protected override void OnEnabledChanged(EventArgs eventArgs)
        {
            Invalidate();
            base.OnEnabledChanged(eventArgs);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var backColor = !Enabled
                ? DisabledBackColor
                : (_pressed ? PressedBackColor : (_hovered ? HoverBackColor : NormalBackColor));
            var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var path = RoundedGeometry.Create(bounds, CornerRadius))
            using (var brush = new SolidBrush(backColor))
            {
                eventArgs.Graphics.FillPath(brush, path);
                if (BorderWidth > 0 && BorderColor != Color.Transparent)
                {
                    using (var pen = new Pen(BorderColor, BorderWidth))
                    {
                        eventArgs.Graphics.DrawPath(pen, path);
                    }
                }
            }

            var textColor = Enabled ? ForeColor : Color.FromArgb(142, 149, 166);
            TextRenderer.DrawText(
                eventArgs.Graphics,
                Text,
                Font,
                new Rectangle(12, 0, Math.Max(1, Width - 24), Height),
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);

            if (Focused && ShowFocusCues)
            {
                var focus = Rectangle.Inflate(bounds, -5, -5);
                ControlPaint.DrawFocusRectangle(eventArgs.Graphics, focus, textColor, backColor);
            }
        }
    }

    internal sealed class SlimProgressBar : Control
    {
        private int _minimum;
        private int _maximum = 100;
        private int _value;

        public SlimProgressBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            TrackColor = Color.FromArgb(225, 229, 242);
            ProgressColor = Palette.Primary;
            Height = 8;
        }

        public int Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Value = _value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = Math.Max(value, _minimum + 1); Value = _value; Invalidate(); }
        }

        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(_minimum, Math.Min(_maximum, value)); Invalidate(); }
        }

        public Color TrackColor { get; set; }
        public Color ProgressColor { get; set; }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var trackPath = RoundedGeometry.Create(bounds, Height / 2))
            using (var trackBrush = new SolidBrush(TrackColor))
            {
                eventArgs.Graphics.FillPath(trackBrush, trackPath);
            }

            var ratio = (double)(_value - _minimum) / (_maximum - _minimum);
            var width = Math.Max(0, (int)Math.Round(bounds.Width * ratio));
            if (width > 0)
            {
                var progressBounds = new Rectangle(bounds.X, bounds.Y, width, bounds.Height);
                using (var progressPath = RoundedGeometry.Create(progressBounds, Height / 2))
                using (var progressBrush = new SolidBrush(ProgressColor))
                {
                    eventArgs.Graphics.FillPath(progressBrush, progressPath);
                }
            }
        }
    }

    internal static class AppIconFactory
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon Create()
        {
            using (var bitmap = new Bitmap(64, 64))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                using (var gradient = new LinearGradientBrush(
                    new Rectangle(4, 4, 56, 56),
                    Palette.Sky,
                    Palette.Primary,
                    35F))
                {
                    graphics.FillEllipse(gradient, 4, 4, 56, 56);
                }

                using (var star = new SolidBrush(Color.White))
                {
                    graphics.FillPolygon(star, IdolHeroPanel.CreateStarPoints(32, 31, 19F, 8.5F));
                }

                using (var sparkle = new SolidBrush(Palette.Gold))
                {
                    graphics.FillEllipse(sparkle, 45, 10, 7, 7);
                }

                var handle = bitmap.GetHicon();
                try
                {
                    using (var temporary = Icon.FromHandle(handle))
                    {
                        return (Icon)temporary.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }
    }
}
