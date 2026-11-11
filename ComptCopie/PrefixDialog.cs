using System.Text.RegularExpressions;

namespace ComptCopie;

/// <summary>Petite fenêtre de saisie d'un nouveau préfixe (ex. L1SN, M2INFO…).</summary>
public sealed class PrefixDialog : Form
{
    private readonly TextBox _input = new()
    {
        Width = 240,
        CharacterCasing = CharacterCasing.Upper,
        MaxLength = 20,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        Margin = new Padding(0, 6, 0, 6),
    };

    private PrefixDialog()
    {
        Text = "Nouveau préfixe";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 10);
        Padding = new Padding(16);

        var ok = new Button { Text = "Ajouter", AutoSize = true, MinimumSize = new Size(100, 32) };
        var cancel = new Button { Text = "Annuler", AutoSize = true, MinimumSize = new Size(100, 32), DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => Validate(ok);

        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = "Préfixe (lettres et chiffres, ex. L1SN) :", AutoSize = true });
        layout.Controls.Add(_input);
        layout.Controls.Add(new Label { Text = "Exemple de numéro : L1SN-001", AutoSize = true, ForeColor = Color.DimGray, Name = "example" });
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        AcceptButton = ok;
        CancelButton = cancel;

        var example = (Label)layout.Controls["example"]!;
        _input.TextChanged += (_, _) =>
            example.Text = "Exemple de numéro : " + (_input.Text.Trim().Length > 0 ? _input.Text.Trim() : "L1SN") + "-001";
    }

    private void Validate(Button ok)
    {
        var text = _input.Text.Trim();
        if (!Regex.IsMatch(text, "^[A-Z0-9]+$"))
        {
            MessageBox.Show(this, "Le préfixe doit contenir uniquement des lettres et des chiffres, sans espace ni tiret.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _input.Focus();
            return;
        }
        DialogResult = DialogResult.OK;
    }

    /// <summary>Affiche la fenêtre et renvoie le préfixe saisi, ou null si annulé.</summary>
    public static string? Ask(IWin32Window owner)
    {
        using var dialog = new PrefixDialog();
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._input.Text.Trim() : null;
    }
}
