using System.Drawing.Printing;

namespace ComptCopie;

/// <summary>
/// Imprime une feuille A4 par numéro : le numéro en en-tête (en haut)
/// et le même numéro en grand au centre du corps de la page.
/// </summary>
public sealed class NumberPrinter : IDisposable
{
    private readonly PrintDocument _document = new();
    private readonly Font _headerFont = new("Arial", 16, FontStyle.Bold);
    private readonly Font _bodyFont = new("Arial", 16, FontStyle.Bold);

    private long _start;
    private int _quantity;
    private long _current;
    private StringAlignment _alignment = StringAlignment.Center;
    private string _prefix = "";
    private int _digits = 3;

    /// <summary>Déclenché après une impression réelle (pas un aperçu), avec le dernier numéro imprimé.</summary>
    public event Action<long>? Printed;

    public PrintDocument Document => _document;

    public NumberPrinter()
    {
        _document.DocumentName = "AnonymCopie";
        _document.DefaultPageSettings.PaperSize = FindA4(_document.PrinterSettings);
        _document.DefaultPageSettings.Landscape = false;
        _document.BeginPrint += (_, _) => _current = _start;
        _document.PrintPage += OnPrintPage;
        _document.EndPrint += (_, e) =>
        {
            if (!e.Cancel && e.PrintAction == PrintAction.PrintToPrinter)
                Printed?.Invoke(_start + _quantity - 1);
        };
    }

    public void Configure(string prefix, int digits, long start, int quantity, string? printerName, StringAlignment alignment)
    {
        _prefix = prefix;
        _digits = digits;
        _start = start;
        _quantity = quantity;
        _alignment = alignment;
        if (!string.IsNullOrEmpty(printerName))
        {
            _document.PrinterSettings.PrinterName = printerName;
            _document.DefaultPageSettings.PaperSize = FindA4(_document.PrinterSettings);
        }
    }

    private static PaperSize FindA4(PrinterSettings settings)
    {
        try
        {
            foreach (PaperSize size in settings.PaperSizes)
                if (size.Kind == PaperKind.A4) return size;
        }
        catch
        {
            // Aucune imprimante disponible : on utilise la taille A4 théorique.
        }
        return new PaperSize("A4", 827, 1169); // centièmes de pouce
    }

    private void OnPrintPage(object? sender, PrintPageEventArgs e)
    {
        var g = e.Graphics!;
        var text = AppSettings.Format(_prefix, _current, _digits);
        var m = e.MarginBounds;

        // En-tête et corps : même numéro, alignés à gauche, au centre ou à droite.
        using var headerFormat = new StringFormat { Alignment = _alignment, LineAlignment = StringAlignment.Near };
        var headerHeight = g.MeasureString(text, _headerFont).Height;
        g.DrawString(text, _headerFont, Brushes.Black, new RectangleF(m.Left, m.Top, m.Width, headerHeight), headerFormat);
        var headerBottom = m.Top + headerHeight + 8;

        // Corps : centré verticalement dans l'espace restant.
        var bodyRect = new RectangleF(m.Left, headerBottom, m.Width, m.Bottom - headerBottom);
        using var bodyFormat = new StringFormat { Alignment = _alignment, LineAlignment = StringAlignment.Center };
        using var bodyFont = FitFont(g, text, bodyRect.Width);
        g.DrawString(text, bodyFont, Brushes.Black, bodyRect, bodyFormat);

        _current++;
        e.HasMorePages = _current < _start + _quantity;
    }

    /// <summary>Réduit la police du corps si le numéro est trop long pour la largeur de la page.</summary>
    private Font FitFont(Graphics g, string text, float maxWidth)
    {
        var size = _bodyFont.Size;
        while (size > 12)
        {
            var font = new Font(_bodyFont.FontFamily, size, _bodyFont.Style);
            if (g.MeasureString(text, font).Width <= maxWidth) return font;
            font.Dispose();
            size -= 4;
        }
        return new Font(_bodyFont.FontFamily, 12, _bodyFont.Style);
    }

    public void Dispose()
    {
        _document.Dispose();
        _headerFont.Dispose();
        _bodyFont.Dispose();
    }
}
