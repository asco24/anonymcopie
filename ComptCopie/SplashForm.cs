using System.Drawing.Drawing2D;

namespace ComptCopie;

/// <summary>
/// Écran d'accueil : apparition en fondu, barre de chargement verte qui se remplit,
/// puis disparition en fondu (≈ 2,5 s au total). Un clic le ferme immédiatement.
/// </summary>
public sealed class SplashForm : Form
{
    private static readonly Color Green = Color.FromArgb(0, 150, 40);
    private static readonly Color Red = Color.FromArgb(200, 20, 30);

    private const int FadeInMs = 500;
    private const int LoadMs = 1500;
    private const int FadeOutMs = 450;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private readonly System.Diagnostics.Stopwatch _clock = new();
    private float _progress; // 0 → 1

    // Dessin sur une surface logique de 480 × 360, agrandie selon la mise à l'échelle de Windows.
    private const int BaseWidth = 480;
    private const int BaseHeight = 360;
    private new float Scale => DeviceDpi / 96f;

    /// <summary>Police en pixels logiques (la mise à l'échelle est appliquée par ScaleTransform).</summary>
    private static Font PxFont(float points, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI", points * 96f / 72f, style, GraphicsUnit.Pixel);

    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.White;
        ClientSize = new Size((int)(BaseWidth * Scale), (int)(BaseHeight * Scale));
        Opacity = 0;
        DoubleBuffered = true;
        Icon = AppAssets.Icon;

        _timer.Tick += (_, _) => Animate();
        Click += (_, _) => Close();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _clock.Start();
        _timer.Start();
    }

    private void Animate()
    {
        var t = _clock.ElapsedMilliseconds;

        if (t < FadeInMs)
            Opacity = Ease(t / (double)FadeInMs);
        else
            Opacity = 1;

        // La barre commence à se remplir pendant le fondu d'entrée.
        _progress = (float)Ease(Math.Clamp((t - FadeInMs / 2.0) / LoadMs, 0, 1));
        Invalidate();

        var fadeOutStart = FadeInMs / 2 + LoadMs + 150;
        if (t > fadeOutStart)
        {
            var k = (t - fadeOutStart) / (double)FadeOutMs;
            Opacity = 1 - Ease(Math.Min(k, 1));
            if (k >= 1)
            {
                _timer.Stop();
                Close();
            }
        }
    }

    /// <summary>Courbe douce (ease-in-out) pour des mouvements moins mécaniques.</summary>
    private static double Ease(double x) => x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.ScaleTransform(Scale, Scale);
        const int w = BaseWidth;
        const int h = BaseHeight;

        // Cadre vert
        using (var border = new Pen(Green, 4))
            g.DrawRectangle(border, 2, 2, w - 4, h - 4);

        // Logo
        const int logoSize = 140;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(AppAssets.Logo, (w - logoSize) / 2, 34, logoSize, logoSize);

        // Titre et sous-titre
        using var center = new StringFormat { Alignment = StringAlignment.Center };
        using (var nameFont = PxFont(24, FontStyle.Bold))
        using (var nameBrush = new SolidBrush(Green))
            g.DrawString("AnonymCopie", nameFont, nameBrush, new RectangleF(0, 184, w, 48), center);
        using (var subFont = PxFont(11, FontStyle.Bold | FontStyle.Italic))
            g.DrawString("COMPOSTAGE DES COPIES D'EXAMEN", subFont, Brushes.DimGray, new RectangleF(0, 236, w, 26), center);

        // Barre de chargement
        var bar = new Rectangle(60, 290, w - 120, 10);
        using (var track = RoundedRect(bar, 5))
        using (var trackBrush = new SolidBrush(Color.FromArgb(225, 232, 227)))
            g.FillPath(trackBrush, track);
        if (_progress > 0.01f)
        {
            var fill = new Rectangle(bar.X, bar.Y, Math.Max(10, (int)(bar.Width * _progress)), bar.Height);
            using var path = RoundedRect(fill, 5);
            using var brush = new LinearGradientBrush(fill, Green, Color.FromArgb(60, 190, 80), LinearGradientMode.Horizontal);
            g.FillPath(brush, path);
        }

        using (var smallFont = PxFont(9))
        using (var redBrush = new SolidBrush(Red))
            g.DrawString(_progress < 1 ? "Chargement…" : "Prêt", smallFont, _progress < 1 ? Brushes.Gray : redBrush,
                new RectangleF(0, 308, w, 20), center);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
