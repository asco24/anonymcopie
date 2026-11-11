using System.Drawing.Printing;

namespace ComptCopie;

public sealed class MainForm : Form
{
    private static readonly Color TitleGreen = Color.FromArgb(0, 150, 40);
    private static readonly Color NumberRed = Color.FromArgb(200, 20, 30);

    private readonly AppSettings _settings = AppSettings.Load();
    private readonly NumberPrinter _printer = new();

    private readonly ComboBox _prefixInput = new()
    {
        Width = 220,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = NumberRed,
        Anchor = AnchorStyles.Top | AnchorStyles.Left,
        Margin = new Padding(0, 6, 0, 6),
    };
    private readonly NumericUpDown _digitsInput = new()
    {
        Minimum = 1, Maximum = 9, Value = 3, Width = 80,
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 6, 0, 6),
    };
    private readonly NumericUpDown _startInput = MakeBigInput(1, 999_999_999);
    private readonly NumericUpDown _quantityInput = MakeBigInput(1, 10_000);
    private readonly Label _endValue = MakeRedValue();
    private readonly Label _totalValue = MakeRedValue();
    private readonly Label _lastLabel = new() { AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(0, 10, 0, 0) };
    private readonly Label _printerLabel = new() { AutoSize = true, MaximumSize = new Size(300, 0), Anchor = AnchorStyles.Left };
    private readonly Button _previewButton = MakeButton("Aperçu avant impression");

    public MainForm()
    {
        Text = "AnonymCopie — Impression rapide";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.WhiteSmoke;
        Padding = new Padding(20);
        Icon = AppAssets.Icon;

        // Grille à 3 colonnes : libellé | champ | boutons annexes. Chaque champ a sa propre cellule
        // et chaque ligne prend la hauteur de son contenu (aucun chevauchement possible).
        var root = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 9,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
        };
        for (var i = 0; i < root.ColumnCount; i++) root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (var i = 0; i < root.RowCount; i++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Titre vert
        var title = new Label
        {
            Text = "COMPOSTAGE DES COPIES D'EXAMEN",
            AutoSize = true,
            ForeColor = TitleGreen,
            Font = new Font("Segoe UI", 18, FontStyle.Bold | FontStyle.Italic),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(10, 0, 0, 0),
        };
        var logo = new PictureBox
        {
            Image = AppAssets.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(56, 56),
            Anchor = AnchorStyles.Left,
            Margin = Padding.Empty,
        };
        var header = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 16),
        };
        header.Controls.Add(logo);
        header.Controls.Add(title);
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 3);

        // Préfixe + boutons Ajouter / Retirer
        var addButton = MakeSmallButton("＋", "Ajouter un préfixe");
        addButton.Click += (_, _) => AddPrefix();
        var removeButton = MakeSmallButton("－", "Retirer le préfixe sélectionné de la liste");
        removeButton.Click += (_, _) => RemovePrefix();
        var prefixButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = new Padding(8, 6, 0, 6),
            Padding = Padding.Empty,
        };
        prefixButtons.Controls.Add(addButton);
        prefixButtons.Controls.Add(removeButton);

        root.Controls.Add(MakeFieldLabel("Préfixe"), 0, 1);
        root.Controls.Add(_prefixInput, 1, 1);
        root.Controls.Add(prefixButtons, 2, 1);
        root.Controls.Add(MakeFieldLabel("Nombre de chiffres"), 0, 2);
        root.Controls.Add(_digitsInput, 1, 2);
        root.Controls.Add(MakeFieldLabel("Début de plage"), 0, 3);
        root.Controls.Add(_startInput, 1, 3);
        root.Controls.Add(MakeFieldLabel("Nombre de copies"), 0, 4);
        root.Controls.Add(_quantityInput, 1, 4);

        // Séparateur
        var separator = new Label { BorderStyle = BorderStyle.Fixed3D, Height = 2, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 8) };
        root.Controls.Add(separator, 0, 5);
        root.SetColumnSpan(separator, 3);

        // Résultats en rouge + aperçu
        var results = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty };
        results.Controls.Add(MakeCaption("PLAGE"));
        results.Controls.Add(_endValue);
        results.Controls.Add(MakeCaption("TOTAL DES COPIES"));
        results.Controls.Add(_totalValue);
        results.Controls.Add(_lastLabel);
        root.Controls.Add(results, 0, 6);
        root.SetColumnSpan(results, 2);

        _previewButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        _previewButton.Click += (_, _) => Preview();
        root.Controls.Add(_previewButton, 2, 6);

        // Imprimante
        var printerRow = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0, 12, 0, 0) };
        var printerButton = new Button { Text = "Imprimante…", AutoSize = true };
        printerButton.Click += (_, _) => ChoosePrinter();
        printerRow.Controls.Add(printerButton);
        printerRow.Controls.Add(_printerLabel);
        root.Controls.Add(printerRow, 0, 7);
        root.SetColumnSpan(printerRow, 3);

        // Boutons d'impression : nouvelle, centrée, à gauche, à droite
        var printButtons = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Anchor = AnchorStyles.None, Margin = new Padding(0, 18, 0, 0) };
        var newButton = MakeButton("Nouvelle\nimpression");
        newButton.ForeColor = TitleGreen;
        newButton.Font = new Font(Font, FontStyle.Bold);
        newButton.Margin = new Padding(3, 3, 24, 3);
        newButton.Click += (_, _) => NewPrint();
        printButtons.Controls.Add(newButton);
        printButtons.Controls.Add(MakePrintButton("Impression\nCentrée", StringAlignment.Center));
        printButtons.Controls.Add(MakePrintButton("Impression\nà Gauche", StringAlignment.Near));
        printButtons.Controls.Add(MakePrintButton("Impression\nà Droite", StringAlignment.Far));
        root.Controls.Add(printButtons, 0, 8);
        root.SetColumnSpan(printButtons, 3);

        Controls.Add(root);

        // Les boutons + / - sont collés en haut de la ligne comme le combo, avec la même marge,
        // et prennent sa hauteur réelle (connue seulement une fois la fenêtre affichée et mise à l'échelle).
        void MatchComboHeight()
        {
            var h = _prefixInput.Height;
            addButton.Size = removeButton.Size = new Size(h, h);
        }
        Shown += (_, _) => MatchComboHeight();

        // Les textes variables (plage, dernier numéro) ont une largeur fixe : ils ne peuvent plus
        // agrandir ou rétrécir la fenêtre quand on change de préfixe. On leur donne toute la largeur
        // des deux premières colonnes, mesurée une fois la fenêtre mise en page.
        foreach (var label in new[] { _endValue, _lastLabel })
        {
            label.AutoSize = false;
            label.AutoEllipsis = true; // « … » si un texte est exceptionnellement long
            label.Size = new Size(10, label.PreferredHeight);
        }
        Shown += (_, _) =>
        {
            var widths = root.GetColumnWidths();
            var available = widths[0] + widths[1] - 4;
            foreach (var label in new[] { _endValue, _lastLabel })
                label.Size = new Size(available, label.PreferredHeight);
        };
        _prefixInput.SizeChanged += (_, _) => MatchComboHeight();

        // Liste des préfixes (gérée par l'utilisateur).
        foreach (var prefix in _settings.Prefixes) _prefixInput.Items.Add(prefix);
        SelectPrefix(_settings.Prefix);
        _digitsInput.Value = Math.Clamp(_settings.Digits, 1, 9);

        // Au lancement, on prépare une nouvelle impression à partir du dernier numéro imprimé.
        NewPrint();

        // Changer de préfixe : on reprend le compteur propre à ce préfixe.
        _prefixInput.SelectedIndexChanged += (_, _) => OnPrefixChanged();
        _digitsInput.ValueChanged += (_, _) =>
        {
            _settings.Digits = Digits;
            _settings.Save();
            UpdateRange();
            UpdateLastLabel();
        };
        _startInput.ValueChanged += (_, _) => UpdateRange();
        _quantityInput.ValueChanged += (_, _) => UpdateRange();

        _printer.Printed += OnPrinted;
        UpdateRange();
        UpdateLastLabel();
        UpdatePrinterLabel();
        UpdatePreviewButton();
    }

    // ---------- Fabrication des contrôles ----------

    private static NumericUpDown MakeBigInput(decimal min, decimal max) => new()
    {
        Minimum = min,
        Maximum = max,
        Width = 220,
        Font = new Font("Segoe UI", 14, FontStyle.Bold),
        ForeColor = NumberRed,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 6, 0, 6),
    };

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Font = new Font("Segoe UI", 14, FontStyle.Bold | FontStyle.Italic),
        Margin = new Padding(0, 6, 20, 6),
    };

    private static Label MakeCaption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Margin = new Padding(0, 6, 0, 0),
    };

    private static Label MakeRedValue() => new()
    {
        AutoSize = true,
        ForeColor = NumberRed,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
    };

    private static Button MakeButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MinimumSize = new Size(130, 46),
    };

    private static Button MakeSmallButton(string text, string tooltip)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(40, 40),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Padding = Padding.Empty,
            Margin = new Padding(0, 0, 4, 0),
        };
        new ToolTip().SetToolTip(button, tooltip);
        return button;
    }

    private Button MakePrintButton(string text, StringAlignment alignment)
    {
        var button = MakeButton(text);
        button.Click += (_, _) => Print(alignment);
        return button;
    }

    // ---------- Affichage ----------

    private string Prefix => _prefixInput.SelectedItem as string ?? "";
    private int Digits => (int)_digitsInput.Value;
    private string Fmt(long n) => AppSettings.Format(Prefix, n, Digits);
    private long Start => (long)_startInput.Value;
    private int Quantity => (int)_quantityInput.Value;
    private long End => Start + Quantity - 1;

    private void UpdateRange()
    {
        _endValue.Text = Quantity == 1 ? Fmt(Start) : $"{Fmt(Start)}  →  {Fmt(End)}";
        _totalValue.Text = Quantity.ToString();
    }

    private void UpdateLastLabel() =>
        _lastLabel.Text = _settings.GetLast(Prefix) > 0
            ? $"Dernier numéro imprimé : {Fmt(_settings.GetLast(Prefix))}"
            : Prefix.Length > 0 ? $"Aucune impression enregistrée pour « {Prefix} »" : "Ajoutez un préfixe avec ＋";

    private void UpdatePrinterLabel()
    {
        var name = _settings.PrinterName;
        if (string.IsNullOrEmpty(name))
            name = new PrinterSettings().PrinterName + " (par défaut)";
        _printerLabel.Text = name;
    }

    private static string AlignmentName(StringAlignment a) => a switch
    {
        StringAlignment.Near => "à gauche",
        StringAlignment.Far => "à droite",
        _ => "centrée",
    };

    /// <summary>L'aperçu utilise le dernier alignement choisi.</summary>
    private void UpdatePreviewButton() =>
        _previewButton.Text = $"Aperçu avant impression\n({AlignmentName(_settings.Alignment)})";

    // ---------- Actions ----------

    /// <summary>Prépare la série suivante : départ = dernier numéro imprimé + 1, nombre de copies à ressaisir.</summary>
    private string _appliedPrefix = "";

    /// <summary>Si le préfixe a vraiment changé, on repart du compteur de ce préfixe.</summary>
    private void OnPrefixChanged()
    {
        if (Prefix == _appliedPrefix) return;
        _settings.Prefix = Prefix;
        _settings.Save();
        NewPrint();
    }

    private void SelectPrefix(string prefix)
    {
        var index = _prefixInput.Items.IndexOf(prefix);
        _prefixInput.SelectedIndex = index >= 0 ? index : (_prefixInput.Items.Count > 0 ? 0 : -1);
    }

    private void AddPrefix()
    {
        var prefix = PrefixDialog.Ask(this);
        if (prefix is null) return;

        if (!_settings.Prefixes.Contains(prefix))
        {
            _settings.Prefixes.Add(prefix);
            _settings.Prefixes.Sort(StringComparer.Ordinal);
            _settings.Save();
            _prefixInput.Items.Clear();
            foreach (var p in _settings.Prefixes) _prefixInput.Items.Add(p);
        }
        SelectPrefix(prefix);
    }

    private void RemovePrefix()
    {
        var prefix = Prefix;
        if (prefix.Length == 0) return;
        var answer = MessageBox.Show(this,
            $"Retirer « {prefix} » de la liste ?\n\nSon compteur est conservé : si vous l'ajoutez à nouveau, la numérotation reprendra où elle s'était arrêtée.",
            Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        if (answer != DialogResult.OK) return;

        _settings.Prefixes.Remove(prefix);
        _settings.Save();
        var index = _prefixInput.SelectedIndex;
        _prefixInput.Items.Remove(prefix);
        if (_prefixInput.Items.Count > 0)
            _prefixInput.SelectedIndex = Math.Min(index, _prefixInput.Items.Count - 1);
        else
            OnPrefixChanged();
    }

    private void NewPrint()
    {
        _appliedPrefix = Prefix;
        _startInput.Value = Math.Min(_settings.GetLast(Prefix) + 1, _startInput.Maximum);
        UpdateLastLabel();
        _quantityInput.Value = 1;
        _quantityInput.Select();
        _quantityInput.Select(0, _quantityInput.Text.Length);
    }

    private void ChoosePrinter()
    {
        using var dialog = new PrintDialog { UseEXDialog = true, AllowSomePages = false };
        if (!string.IsNullOrEmpty(_settings.PrinterName))
            dialog.PrinterSettings.PrinterName = _settings.PrinterName;
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _settings.PrinterName = dialog.PrinterSettings.PrinterName;
        _settings.Save();
        UpdatePrinterLabel();
    }

    private bool Prepare(StringAlignment alignment)
    {
        if (Prefix.Length == 0)
        {
            MessageBox.Show(this, "Choisissez ou ajoutez d'abord un préfixe.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        _printer.Configure(Prefix, Digits, Start, Quantity, _settings.PrinterName, alignment);
        if (_printer.Document.PrinterSettings.IsValid) return true;

        MessageBox.Show(this, "Aucune imprimante valide n'est disponible.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private void Preview()
    {
        if (!Prepare(_settings.Alignment)) return;
        using var dialog = new PrintPreviewDialog
        {
            Document = _printer.Document,
            WindowState = FormWindowState.Maximized,
            UseAntiAlias = true,
        };
        dialog.ShowDialog(this);
    }

    private void Print(StringAlignment alignment)
    {
        // On mémorise l'alignement choisi pour les prochains aperçus.
        _settings.Alignment = alignment;
        _settings.Save();
        UpdatePreviewButton();

        if (!Prepare(alignment)) return;
        var confirm = MessageBox.Show(this,
            $"Impression {AlignmentName(alignment)} de {Quantity} copie(s), du n° {Fmt(Start)} au n° {Fmt(End)} ?",
            Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        if (confirm != DialogResult.OK) return;

        try
        {
            _printer.Document.Print();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erreur d'impression : " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>Après une vraie impression : on mémorise le dernier numéro, la plage imprimée reste affichée.</summary>
    private void OnPrinted(long lastNumber)
    {
        // On ne fait jamais reculer le compteur d'un préfixe (en cas de réimpression).
        _settings.LastByPrefix[Prefix] = Math.Max(lastNumber, _settings.GetLast(Prefix));
        _settings.Prefix = Prefix;
        _settings.Digits = Digits;
        _settings.LastQuantity = Quantity;
        _settings.Save();
        UpdateLastLabel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _printer.Dispose();
        base.Dispose(disposing);
    }
}
