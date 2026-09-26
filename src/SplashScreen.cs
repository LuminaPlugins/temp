using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace OpenPrestige
{
    public class SplashScreen : Form
    {
        private System.Windows.Forms.Timer _timer;
        private int _progress = 0;
        private string _statusText = Variables.SplashStatusInit;

        public SplashScreen()
        {
            UITheme.Initialize();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(UITheme.Scale(540), UITheme.Scale(320));
            BackColor = UITheme.BgDark;
            ShowInTaskbar = false;
            TopMost = true;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _timer = new System.Windows.Forms.Timer { Interval = 25 };
            _timer.Tick += OnTick;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UITheme.EnableDarkMode(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            _progress += 2;

            if (_progress == 18) _statusText = Variables.SplashStatusFonts;
            else if (_progress == 40) _statusText = Variables.SplashStatusDll;
            else if (_progress == 65) _statusText = Variables.SplashStatusCert;
            else if (_progress == 85) _statusText = Variables.SplashStatusEndpoints;
            else if (_progress >= 100)
            {
                _timer.Stop();
                _statusText = Variables.SplashStatusReady;
                Invalidate();
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            UITheme.PaintGlassBackground(g, ClientRectangle);

            int p = UITheme.Scale(12);
            Rectangle cardRect = new Rectangle(p, p, Width - p * 2, Height - p * 2);
            using (GraphicsPath path = UITheme.CreateRoundedRectanglePath(cardRect, UITheme.Scale(16)))
            {
                using (SolidBrush brush = new SolidBrush(UITheme.GlassSurface))
                {
                    g.FillPath(brush, path);
                }

                using (LinearGradientBrush borderBrush = new LinearGradientBrush(cardRect, Color.FromArgb(120, 255, 255, 255), Color.FromArgb(20, 255, 255, 255), 90f))
                using (Pen pen = new Pen(borderBrush, 1.4f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Top Badge: "✦ MADE BY RACKELDEVS ✦"
            int bW = UITheme.Scale(210);
            int bH = UITheme.Scale(26);
            Rectangle badgeRect = new Rectangle(Width / 2 - bW / 2, UITheme.Scale(36), bW, bH);
            using (GraphicsPath bPath = UITheme.CreateRoundedRectanglePath(badgeRect, UITheme.Scale(13)))
            {
                using (SolidBrush bFill = new SolidBrush(Color.FromArgb(180, 26, 32, 50)))
                {
                    g.FillPath(bFill, bPath);
                }
                using (Pen bPen = new Pen(Color.FromArgb(120, UITheme.Accent), 1.2f))
                {
                    g.DrawPath(bPen, bPath);
                }
            }

            TextRenderer.DrawText(
                g,
                Variables.SplashBadge,
                UITheme.FontSmallBold,
                badgeRect,
                UITheme.HighlightTag,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Title
            Rectangle titleRect = new Rectangle(0, UITheme.Scale(80), Width, UITheme.Scale(42));
            TextRenderer.DrawText(
                g,
                Variables.SplashTitle,
                UITheme.FontDisplay,
                titleRect,
                UITheme.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Subtitle
            Rectangle subRect = new Rectangle(0, UITheme.Scale(126), Width, UITheme.Scale(22));
            TextRenderer.DrawText(
                g,
                Variables.SplashSubtitle,
                UITheme.FontSmallBold,
                subRect,
                UITheme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Progress Bar Track
            int barWidth = Width - UITheme.Scale(100);
            int barHeight = UITheme.Scale(6);
            int barX = UITheme.Scale(50);
            int barY = UITheme.Scale(200);

            Rectangle trackRect = new Rectangle(barX, barY, barWidth, barHeight);
            using (GraphicsPath trackPath = UITheme.CreateRoundedRectanglePath(trackRect, 3))
            {
                using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(100, 20, 24, 38)))
                {
                    g.FillPath(trackBrush, trackPath);
                }
            }

            // Glowing Progress Fill
            int fillWidth = (int)(barWidth * (_progress / 100.0f));
            if (fillWidth > 4)
            {
                Rectangle fillRect = new Rectangle(barX, barY, fillWidth, barHeight);
                using (GraphicsPath fillPath = UITheme.CreateRoundedRectanglePath(fillRect, 3))
                {
                    using (LinearGradientBrush fillBrush = new LinearGradientBrush(fillRect, UITheme.Accent, UITheme.HighlightTag, 0f))
                    {
                        g.FillPath(fillBrush, fillPath);
                    }
                }
            }

            // Status label
            Rectangle statusRect = new Rectangle(barX, UITheme.Scale(218), barWidth, UITheme.Scale(24));
            TextRenderer.DrawText(
                g,
                _statusText,
                UITheme.FontSmall,
                statusRect,
                UITheme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // Percentage
            TextRenderer.DrawText(
                g,
                _progress + "%",
                UITheme.FontSmallBold,
                statusRect,
                UITheme.HighlightTag,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

            // Version info footer
            Rectangle footerRect = new Rectangle(0, Height - UITheme.Scale(36), Width, UITheme.Scale(20));
            TextRenderer.DrawText(
                g,
                Variables.VersionTag + " • " + Variables.AuthorCredit,
                UITheme.FontSmall,
                footerRect,
                UITheme.TextFaint,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
