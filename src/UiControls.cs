using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AntigProxy
{
    public static class UiDrawing
    {
        public static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
            if (d <= 0.1f)
            {
                path.AddRectangle(rect);
                return path;
            }

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedCard : Panel
    {
        public int CornerRadius { get; set; }
        public Color CardBackColor { get; set; }
        public Color BorderColor { get; set; }

        public RoundedCard()
        {
            CornerRadius = 12;
            CardBackColor = Color.White;
            BorderColor = Color.FromArgb(226, 232, 240);
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath path = UiDrawing.CreateRoundedRect(rect, CornerRadius))
            using (SolidBrush brush = new SolidBrush(CardBackColor))
            using (Pen pen = new Pen(BorderColor, 1f))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    public class ModernButton : Control
    {
        private bool _hover;
        private bool _pressed;

        public int CornerRadius { get; set; }
        public bool IsPrimary { get; set; }
        public Color PrimaryStartColor { get; set; }
        public Color PrimaryEndColor { get; set; }
        public Color NormalBackColor { get; set; }
        public Color HoverBackColor { get; set; }
        public Color BorderColor { get; set; }
        public Color NormalForeColor { get; set; }

        public ModernButton()
        {
            CornerRadius = 9;
            IsPrimary = false;
            PrimaryStartColor = Color.FromArgb(37, 99, 235);
            PrimaryEndColor = Color.FromArgb(14, 165, 233);
            NormalBackColor = Color.White;
            HoverBackColor = Color.FromArgb(241, 245, 249);
            BorderColor = Color.FromArgb(203, 213, 225);
            NormalForeColor = Color.FromArgb(30, 41, 59);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath path = UiDrawing.CreateRoundedRect(rect, CornerRadius))
            {
                if (!Enabled)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(226, 232, 240)))
                    {
                        g.FillPath(b, path);
                    }
                }
                else if (IsPrimary)
                {
                    Color c1 = _pressed ? Color.FromArgb(29, 78, 216) : (_hover ? Color.FromArgb(59, 130, 246) : PrimaryStartColor);
                    Color c2 = _pressed ? Color.FromArgb(2, 132, 199) : (_hover ? Color.FromArgb(56, 189, 248) : PrimaryEndColor);
                    using (LinearGradientBrush lgb = new LinearGradientBrush(rect, c1, c2, 18f))
                    {
                        g.FillPath(lgb, path);
                    }
                }
                else
                {
                    Color fill = _pressed ? Color.FromArgb(226, 232, 240) : (_hover ? HoverBackColor : NormalBackColor);
                    using (SolidBrush b = new SolidBrush(fill))
                    using (Pen p = new Pen(BorderColor, 1f))
                    {
                        g.FillPath(b, path);
                        g.DrawPath(p, path);
                    }
                }
            }

            Color textColor = !Enabled
                ? Color.FromArgb(148, 163, 184)
                : (IsPrimary ? Color.White : NormalForeColor);

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor, flags);
        }
    }

    public class StatusPill : Control
    {
        private bool _hover;
        public Color PillBackColor { get; set; }
        public Color PillBorderColor { get; set; }
        public Color DotColor { get; set; }
        public Color PillTextColor { get; set; }

        public StatusPill()
        {
            PillBackColor = Color.FromArgb(241, 245, 249);
            PillBorderColor = Color.FromArgb(203, 213, 225);
            DotColor = Color.FromArgb(148, 163, 184);
            PillTextColor = Color.FromArgb(71, 85, 105);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetState(string text, Color backColor, Color borderColor, Color dotColor, Color textColor)
        {
            Text = text;
            PillBackColor = backColor;
            PillBorderColor = borderColor;
            DotColor = dotColor;
            PillTextColor = textColor;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            float radius = rect.Height / 2f;

            using (GraphicsPath path = UiDrawing.CreateRoundedRect(rect, radius))
            using (SolidBrush bg = new SolidBrush(PillBackColor))
            using (Pen border = new Pen(_hover ? DotColor : PillBorderColor, 1f))
            {
                g.FillPath(bg, path);
                g.DrawPath(border, path);
            }

            float dotSize = 8f;
            float dotX = 11f;
            float dotY = (Height - dotSize) / 2f;
            using (SolidBrush dotBrush = new SolidBrush(DotColor))
            {
                g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
            }

            Rectangle textRect = new Rectangle(22, 0, Width - 28, Height);
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
            TextRenderer.DrawText(g, Text, Font, textRect, PillTextColor, flags);
        }
    }

    public class ModernInputBox : Panel
    {
        private readonly TextBox _inner;
        private bool _focused;

        public Color BorderColor { get; set; }
        public Color FocusBorderColor { get; set; }

        public override string Text
        {
            get { return _inner.Text; }
            set { _inner.Text = value; }
        }

        public HorizontalAlignment TextAlign
        {
            get { return _inner.TextAlign; }
            set { _inner.TextAlign = value; }
        }

        public bool ReadOnly
        {
            get { return _inner.ReadOnly; }
            set { _inner.ReadOnly = value; }
        }

        public ModernInputBox()
        {
            BorderColor = Color.FromArgb(203, 213, 225);
            FocusBorderColor = Color.FromArgb(37, 99, 235);
            BackColor = Color.Transparent;
            Padding = new Padding(10, 7, 10, 6);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);

            _inner = new TextBox();
            _inner.BorderStyle = BorderStyle.None;
            _inner.BackColor = Color.FromArgb(248, 250, 252);
            _inner.ForeColor = Color.FromArgb(15, 23, 42);
            _inner.Dock = DockStyle.Fill;
            _inner.GotFocus += delegate { _focused = true; _inner.BackColor = Color.White; Invalidate(); };
            _inner.LostFocus += delegate { _focused = false; _inner.BackColor = Color.FromArgb(248, 250, 252); Invalidate(); };

            Controls.Add(_inner);
            Click += delegate { _inner.Focus(); };
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_inner != null)
            {
                _inner.Font = Font;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color bg = _focused ? Color.White : Color.FromArgb(248, 250, 252);
            Color bd = _focused ? FocusBorderColor : BorderColor;
            float penWidth = _focused ? 1.5f : 1f;

            using (GraphicsPath path = UiDrawing.CreateRoundedRect(rect, 8))
            using (SolidBrush brush = new SolidBrush(bg))
            using (Pen pen = new Pen(bd, penWidth))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }
    }
}
