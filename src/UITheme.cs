using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OpenPrestige
{
    public enum ThemePreset
    {
        CyberIndigo,
        EmeraldMatrix,
        ObsidianCrimson,
        SapphireIce,
        AmethystSunset
    }

    public static class UITheme
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(byte[] pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);
        private const uint FR_PRIVATE = 0x10;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int LOGPIXELSX = 88;

        // Scaling
        public static float DpiScale = 1.0f;
        public static int Scale(int val) { return (int)Math.Round(val * DpiScale); }

        // Active Theme Colors
        public static ThemePreset CurrentTheme = ThemePreset.CyberIndigo;
        public static event Action ThemeChanged;

        public static Color BgDark = Color.FromArgb(10, 11, 16);
        public static Color BgGradientTop = Color.FromArgb(17, 19, 30);
        public static Color BgGradientBottom = Color.FromArgb(10, 11, 16);

        public static Color GlassSurface = Color.FromArgb(140, 18, 22, 34);
        public static Color GlassBorderTop = Color.FromArgb(75, 255, 255, 255);
        public static Color GlassBorderBottom = Color.FromArgb(18, 255, 255, 255);

        // Solid opaque card backgrounds for clean artifact-free rendering
        public static Color CardBgSolid = Color.FromArgb(17, 21, 32);
        public static Color CardBgLight = Color.FromArgb(22, 27, 42);

        public static Color TextPrimary = Color.FromArgb(243, 244, 246);
        public static Color TextMuted = Color.FromArgb(148, 163, 184);
        public static Color TextFaint = Color.FromArgb(100, 116, 139);

        // Theme Accent Dynamic Colors
        public static Color Accent = Color.FromArgb(99, 102, 241);
        public static Color AccentHover = Color.FromArgb(129, 140, 248);
        public static Color AccentEnd = Color.FromArgb(79, 70, 229);
        public static Color GlowColor1 = Color.FromArgb(45, 99, 102, 241);
        public static Color GlowColor2 = Color.FromArgb(35, 56, 189, 248);
        public static Color HighlightTag = Color.FromArgb(56, 189, 248);

        public static readonly Color StatusGreen = Color.FromArgb(52, 211, 153);
        public static readonly Color StatusAmber = Color.FromArgb(251, 191, 36);
        public static readonly Color StatusRed = Color.FromArgb(248, 113, 113);
        public static readonly Color ConsoleBg = Color.FromArgb(8, 10, 15);

        // Font system
        public static PrivateFontCollection FontCollection = new PrivateFontCollection();
        public static FontFamily InterFamily;
        private static IntPtr _pinnedFontMemory = IntPtr.Zero;

        public static Font FontDisplay;
        public static Font FontTitle;
        public static Font FontSubtitle;
        public static Font FontHeader;
        public static Font FontBody;
        public static Font FontBodyBold;
        public static Font FontSmall;
        public static Font FontSmallBold;
        public static Font FontMono;

        public static void Initialize()
        {
            DetectDpiScale();
            InitializeFonts();
            CreateCachedFonts();
            ApplyTheme(ThemePreset.CyberIndigo);
        }

        private static void DetectDpiScale()
        {
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                    ReleaseDC(IntPtr.Zero, hdc);
                    if (dpiX > 0)
                    {
                        DpiScale = dpiX / 96.0f;
                        if (DpiScale < 1.0f) DpiScale = 1.0f;
                    }
                }
            }
            catch { DpiScale = 1.0f; }
        }

        public static void ApplyTheme(ThemePreset preset)
        {
            CurrentTheme = preset;

            switch (preset)
            {
                case ThemePreset.EmeraldMatrix:
                    BgDark = Color.FromArgb(8, 14, 12);
                    BgGradientTop = Color.FromArgb(12, 24, 20);
                    BgGradientBottom = Color.FromArgb(8, 14, 12);
                    GlassSurface = Color.FromArgb(140, 14, 28, 22);
                    CardBgSolid = Color.FromArgb(13, 23, 19);
                    CardBgLight = Color.FromArgb(18, 30, 25);
                    Accent = Color.FromArgb(16, 185, 129);
                    AccentHover = Color.FromArgb(52, 211, 153);
                    AccentEnd = Color.FromArgb(5, 150, 105);
                    GlowColor1 = Color.FromArgb(45, 16, 185, 129);
                    GlowColor2 = Color.FromArgb(35, 132, 204, 22);
                    HighlightTag = Color.FromArgb(52, 211, 153);
                    break;

                case ThemePreset.ObsidianCrimson:
                    BgDark = Color.FromArgb(14, 9, 11);
                    BgGradientTop = Color.FromArgb(26, 14, 18);
                    BgGradientBottom = Color.FromArgb(14, 9, 11);
                    GlassSurface = Color.FromArgb(140, 32, 16, 22);
                    CardBgSolid = Color.FromArgb(23, 16, 20);
                    CardBgLight = Color.FromArgb(30, 21, 26);
                    Accent = Color.FromArgb(225, 29, 72);
                    AccentHover = Color.FromArgb(244, 63, 94);
                    AccentEnd = Color.FromArgb(190, 18, 60);
                    GlowColor1 = Color.FromArgb(50, 225, 29, 72);
                    GlowColor2 = Color.FromArgb(35, 234, 88, 12);
                    HighlightTag = Color.FromArgb(251, 113, 133);
                    break;

                case ThemePreset.SapphireIce:
                    BgDark = Color.FromArgb(8, 12, 18);
                    BgGradientTop = Color.FromArgb(14, 22, 36);
                    BgGradientBottom = Color.FromArgb(8, 12, 18);
                    GlassSurface = Color.FromArgb(140, 16, 26, 42);
                    CardBgSolid = Color.FromArgb(14, 20, 32);
                    CardBgLight = Color.FromArgb(19, 27, 42);
                    Accent = Color.FromArgb(2, 132, 199);
                    AccentHover = Color.FromArgb(56, 189, 248);
                    AccentEnd = Color.FromArgb(3, 105, 161);
                    GlowColor1 = Color.FromArgb(50, 2, 132, 199);
                    GlowColor2 = Color.FromArgb(40, 6, 182, 212);
                    HighlightTag = Color.FromArgb(56, 189, 248);
                    break;

                case ThemePreset.AmethystSunset:
                    BgDark = Color.FromArgb(13, 9, 18);
                    BgGradientTop = Color.FromArgb(24, 15, 34);
                    BgGradientBottom = Color.FromArgb(13, 9, 18);
                    GlassSurface = Color.FromArgb(140, 30, 18, 44);
                    CardBgSolid = Color.FromArgb(23, 16, 32);
                    CardBgLight = Color.FromArgb(30, 21, 42);
                    Accent = Color.FromArgb(168, 85, 247);
                    AccentHover = Color.FromArgb(192, 132, 252);
                    AccentEnd = Color.FromArgb(147, 51, 234);
                    GlowColor1 = Color.FromArgb(50, 168, 85, 247);
                    GlowColor2 = Color.FromArgb(35, 249, 115, 22);
                    HighlightTag = Color.FromArgb(232, 121, 249);
                    break;

                case ThemePreset.CyberIndigo:
                default:
                    BgDark = Color.FromArgb(10, 11, 16);
                    BgGradientTop = Color.FromArgb(17, 19, 30);
                    BgGradientBottom = Color.FromArgb(10, 11, 16);
                    GlassSurface = Color.FromArgb(140, 18, 22, 34);
                    CardBgSolid = Color.FromArgb(17, 21, 32);
                    CardBgLight = Color.FromArgb(22, 27, 42);
                    Accent = Color.FromArgb(99, 102, 241);
                    AccentHover = Color.FromArgb(129, 140, 248);
                    AccentEnd = Color.FromArgb(79, 70, 229);
                    GlowColor1 = Color.FromArgb(45, 99, 102, 241);
                    GlowColor2 = Color.FromArgb(35, 56, 189, 248);
                    HighlightTag = Color.FromArgb(56, 189, 248);
                    break;
            }

            if (ThemeChanged != null)
            {
                try { ThemeChanged(); } catch { }
            }
        }

        public static void EnableDarkMode(IntPtr handle)
        {
            try
            {
                int trueVal = 1;
                if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueVal, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref trueVal, sizeof(int));
                }
            }
            catch { }
        }

        private static void InitializeFonts()
        {
            try
            {
                string localAppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenPrestige");
                string fontPath = Path.Combine(localAppDir, "Inter.ttf");

                if (!File.Exists(fontPath))
                {
                    string localDirFont = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Inter.ttf");
                    if (File.Exists(localDirFont))
                    {
                        fontPath = localDirFont;
                    }
                    else
                    {
                        ResourceHelper.ExtractResourceToFile("Inter.ttf", fontPath);
                    }
                }

                byte[] fontBytes = null;
                if (File.Exists(fontPath))
                {
                    fontBytes = File.ReadAllBytes(fontPath);
                }
                else
                {
                    fontBytes = ResourceHelper.GetResourceBytes("Inter.ttf");
                }

                if (fontBytes != null && fontBytes.Length > 0)
                {
                    uint cFonts = 0;
                    AddFontMemResourceEx(fontBytes, (uint)fontBytes.Length, IntPtr.Zero, ref cFonts);

                    if (_pinnedFontMemory == IntPtr.Zero)
                    {
                        _pinnedFontMemory = Marshal.AllocCoTaskMem(fontBytes.Length);
                        Marshal.Copy(fontBytes, 0, _pinnedFontMemory, fontBytes.Length);
                        FontCollection.AddMemoryFont(_pinnedFontMemory, fontBytes.Length);
                    }
                }

                if (File.Exists(fontPath))
                {
                    try { AddFontResourceEx(fontPath, FR_PRIVATE, IntPtr.Zero); } catch { }
                    try { FontCollection.AddFontFile(fontPath); } catch { }
                }

                foreach (var family in FontCollection.Families)
                {
                    if (family.Name.IndexOf("Inter", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        InterFamily = family;
                        break;
                    }
                }
            }
            catch { }

            if (InterFamily == null)
            {
                try { InterFamily = new FontFamily("Segoe UI"); }
                catch { InterFamily = FontFamily.GenericSansSerif; }
            }
        }

        private static void CreateCachedFonts()
        {
            // Create fonts with DPI-scaled sizes
            FontDisplay = CreateFont(15f, FontStyle.Bold);
            FontTitle = CreateFont(13f, FontStyle.Bold);
            FontSubtitle = CreateFont(9f, FontStyle.Regular);
            FontHeader = CreateFont(9.5f, FontStyle.Bold);
            FontBody = CreateFont(9.5f, FontStyle.Regular);
            FontBodyBold = CreateFont(9.5f, FontStyle.Bold);
            FontSmall = CreateFont(8f, FontStyle.Regular);
            FontSmallBold = CreateFont(8f, FontStyle.Bold);
            FontMono = CreateFont(9f, FontStyle.Regular);
        }

        public static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
        {
            try
            {
                if (InterFamily != null)
                {
                    return new Font(InterFamily, size, style, GraphicsUnit.Point);
                }
                return new Font("Segoe UI", size, style, GraphicsUnit.Point);
            }
            catch
            {
                return SystemFonts.DefaultFont;
            }
        }

        public static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;

            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void PaintGlassBackground(Graphics g, Rectangle bounds)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (LinearGradientBrush brush = new LinearGradientBrush(bounds, BgGradientTop, BgGradientBottom, 90f))
            {
                g.FillRectangle(brush, bounds);
            }

            // Radial ambient glowing orbs based on current theme
            using (GraphicsPath orb1 = new GraphicsPath())
            {
                Rectangle orbRect = new Rectangle(-Scale(50), -Scale(50), Scale(360), Scale(260));
                orb1.AddEllipse(orbRect);
                using (PathGradientBrush pgb = new PathGradientBrush(orb1))
                {
                    pgb.CenterColor = GlowColor1;
                    pgb.SurroundColors = new[] { Color.FromArgb(0, GlowColor1) };
                    g.FillPath(pgb, orb1);
                }
            }

            using (GraphicsPath orb2 = new GraphicsPath())
            {
                Rectangle orbRect = new Rectangle(bounds.Width - Scale(280), -Scale(40), Scale(340), Scale(240));
                orb2.AddEllipse(orbRect);
                using (PathGradientBrush pgb = new PathGradientBrush(orb2))
                {
                    pgb.CenterColor = GlowColor2;
                    pgb.SurroundColors = new[] { Color.FromArgb(0, GlowColor2) };
                    g.FillPath(pgb, orb2);
                }
            }
        }
    }

    public class GlassCard : Panel
    {
        public int BorderRadius { get; set; }
        public bool DrawShineLine { get; set; }

        public GlassCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BorderRadius = 12;
            DrawShineLine = true;
            BackColor = UITheme.CardBgSolid;
            Padding = new Padding(14);
            UITheme.ThemeChanged += () =>
            {
                BackColor = UITheme.CardBgSolid;
                Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(UITheme.BgDark);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            int r = UITheme.Scale(BorderRadius);
            using (GraphicsPath path = UITheme.CreateRoundedRectanglePath(rect, r))
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, UITheme.CardBgLight, UITheme.CardBgSolid, 90f))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (LinearGradientBrush borderBrush = new LinearGradientBrush(rect, UITheme.GlassBorderTop, UITheme.GlassBorderBottom, 90f))
                using (Pen pen = new Pen(borderBrush, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                if (DrawShineLine && Width > 30)
                {
                    using (Pen shinePen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                    {
                        e.Graphics.DrawLine(shinePen, r + 4, 2, Width - r - 4, 2);
                    }
                }
            }
        }
    }

    public class GlassButton : Button
    {
        public bool IsAccent { get; set; }
        public int BorderRadius { get; set; }
        private bool _isHovered;
        private bool _isPressed;

        public GlassButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BorderRadius = 8;
            Font = UITheme.FontBodyBold;
            ForeColor = UITheme.TextPrimary;
            IsAccent = false;
            BackColor = UITheme.CardBgSolid;
            UITheme.ThemeChanged += () =>
            {
                BackColor = Parent != null ? Parent.BackColor : UITheme.CardBgSolid;
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color parentBg = Parent != null ? Parent.BackColor : UITheme.CardBgSolid;
            e.Graphics.Clear(parentBg);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fillStart, fillEnd, borderStart, borderEnd;

            if (IsAccent)
            {
                if (!Enabled)
                {
                    fillStart = Color.FromArgb(45, 48, 62);
                    fillEnd = Color.FromArgb(35, 38, 50);
                    borderStart = Color.FromArgb(60, 255, 255, 255);
                    borderEnd = Color.FromArgb(20, 255, 255, 255);
                }
                else if (_isPressed)
                {
                    fillStart = UITheme.AccentEnd;
                    fillEnd = UITheme.Accent;
                    borderStart = Color.FromArgb(180, 255, 255, 255);
                    borderEnd = Color.FromArgb(80, 255, 255, 255);
                }
                else if (_isHovered)
                {
                    fillStart = UITheme.AccentHover;
                    fillEnd = UITheme.Accent;
                    borderStart = Color.FromArgb(240, 255, 255, 255);
                    borderEnd = Color.FromArgb(120, 255, 255, 255);
                }
                else
                {
                    fillStart = UITheme.Accent;
                    fillEnd = UITheme.AccentEnd;
                    borderStart = Color.FromArgb(160, 255, 255, 255);
                    borderEnd = Color.FromArgb(60, 255, 255, 255);
                }
            }
            else
            {
                if (!Enabled)
                {
                    fillStart = Color.FromArgb(24, 28, 38);
                    fillEnd = Color.FromArgb(20, 24, 32);
                    borderStart = Color.FromArgb(30, 255, 255, 255);
                    borderEnd = Color.FromArgb(15, 255, 255, 255);
                }
                else if (_isPressed)
                {
                    fillStart = Color.FromArgb(22, 26, 38);
                    fillEnd = Color.FromArgb(18, 22, 32);
                    borderStart = Color.FromArgb(70, 255, 255, 255);
                    borderEnd = Color.FromArgb(30, 255, 255, 255);
                }
                else if (_isHovered)
                {
                    fillStart = Color.FromArgb(44, 52, 74);
                    fillEnd = Color.FromArgb(32, 38, 54);
                    borderStart = Color.FromArgb(140, 255, 255, 255);
                    borderEnd = Color.FromArgb(60, 255, 255, 255);
                }
                else
                {
                    fillStart = Color.FromArgb(32, 38, 54);
                    fillEnd = Color.FromArgb(24, 28, 42);
                    borderStart = Color.FromArgb(70, 255, 255, 255);
                    borderEnd = Color.FromArgb(30, 255, 255, 255);
                }
            }

            int r = UITheme.Scale(BorderRadius);
            using (GraphicsPath path = UITheme.CreateRoundedRectanglePath(rect, r))
            {
                using (LinearGradientBrush fillBrush = new LinearGradientBrush(rect, fillStart, fillEnd, 90f))
                {
                    e.Graphics.FillPath(fillBrush, path);
                }

                using (LinearGradientBrush borderBrush = new LinearGradientBrush(rect, borderStart, borderEnd, 90f))
                using (Pen pen = new Pen(borderBrush, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            Color textCol = Enabled ? (IsAccent ? Color.White : UITheme.TextPrimary) : UITheme.TextFaint;
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                rect,
                textCol,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    public class GlassComboBox : ComboBox
    {
        private bool _isHovered;

        public GlassComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            BackColor = UITheme.CardBgSolid;
            ForeColor = UITheme.TextPrimary;
            Font = UITheme.FontSmall;
            ItemHeight = UITheme.Scale(28);
            FlatStyle = FlatStyle.Flat;
            Cursor = Cursors.Hand;
            UITheme.ThemeChanged += () =>
            {
                BackColor = Parent != null ? Parent.BackColor : UITheme.CardBgSolid;
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color parentBg = Parent != null ? Parent.BackColor : UITheme.CardBgSolid;
            e.Graphics.Clear(parentBg);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            Color bg = _isHovered ? Color.FromArgb(36, 44, 62) : Color.FromArgb(24, 30, 44);
            Color borderTop = _isHovered ? Color.FromArgb(140, 255, 255, 255) : Color.FromArgb(70, 255, 255, 255);
            Color borderBottom = _isHovered ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(25, 255, 255, 255);

            int r = UITheme.Scale(8);
            using (GraphicsPath path = UITheme.CreateRoundedRectanglePath(rect, r))
            {
                using (SolidBrush brush = new SolidBrush(bg))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (LinearGradientBrush borderBrush = new LinearGradientBrush(rect, borderTop, borderBottom, 90f))
                using (Pen pen = new Pen(borderBrush, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            string text = SelectedItem != null ? SelectedItem.ToString() : (Items.Count > 0 ? Items[0].ToString() : Variables.NoProcessSelected);
            Rectangle textRect = new Rectangle(UITheme.Scale(10), 0, Width - UITheme.Scale(34), Height);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                Font,
                textRect,
                UITheme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            int arrowX = Width - UITheme.Scale(18);
            int arrowY = Height / 2 - 1;
            Point[] chevron = new[]
            {
                new Point(arrowX - 4, arrowY - 2),
                new Point(arrowX, arrowY + 2),
                new Point(arrowX + 4, arrowY - 2)
            };
            using (Pen arrowPen = new Pen(UITheme.TextMuted, 1.8f))
            {
                arrowPen.StartCap = LineCap.Round;
                arrowPen.EndCap = LineCap.Round;
                e.Graphics.DrawLines(arrowPen, chevron);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bgColor = isSelected ? UITheme.Accent : Color.FromArgb(24, 30, 44);
            Color textColor = isSelected ? Color.White : UITheme.TextPrimary;

            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            string text = Items[e.Index].ToString();
            Rectangle textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
