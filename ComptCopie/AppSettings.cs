using System.Text.Json;

namespace ComptCopie;

/// <summary>Réglages persistants, stockés dans %AppData%\ComptCopie\settings.json.</summary>
public sealed class AppSettings
{
    /// <summary>Ancien compteur unique (avant les préfixes), repris pour le préfixe vide.</summary>
    public long LastPrintedNumber { get; set; } = 0;
    /// <summary>Dernier numéro imprimé pour chaque préfixe (ex. "L1SN" → 45).</summary>
    public Dictionary<string, long> LastByPrefix { get; set; } = new();
    public string Prefix { get; set; } = "L1SN";
    /// <summary>Liste des préfixes proposés, gérée par l'utilisateur (boutons ＋ / －).</summary>
    public List<string>? PrefixList { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public List<string> Prefixes => PrefixList ??= new[] { "L1SN", "L2SN", "L3SN", "M1SN", "M2SN" }
        .Concat(LastByPrefix.Keys.Where(k => k.Length > 0))
        .Distinct().OrderBy(k => k, StringComparer.Ordinal).ToList();
    public int Digits { get; set; } = 3;
    public int LastQuantity { get; set; } = 1;
    public string? PrinterName { get; set; }
    /// <summary>Dernier alignement utilisé : Near (gauche), Center ou Far (droite).</summary>
    public StringAlignment Alignment { get; set; } = StringAlignment.Center;

    public long GetLast(string prefix) => LastByPrefix.TryGetValue(prefix, out var n) ? n : 0;

    /// <summary>Formate un numéro : "L1SN" + 1 sur 3 chiffres → "L1SN-001" (sans préfixe → "001").</summary>
    public static string Format(string prefix, long number, int digits)
    {
        var n = number.ToString().PadLeft(digits, '0');
        return string.IsNullOrEmpty(prefix) ? n : $"{prefix}-{n}";
    }

    private static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string FilePath = Path.Combine(AppData, "AnonymCopie", "settings.json");
    /// <summary>Réglages de l'ancienne version (avant le renommage en AnonymCopie).</summary>
    private static readonly string OldFilePath = Path.Combine(AppData, "ComptCopie", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            // Première ouverture d'AnonymCopie : on reprend les compteurs de l'ancienne version.
            if (!File.Exists(FilePath) && File.Exists(OldFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.Copy(OldFilePath, FilePath);
            }

            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                if (s.LastPrintedNumber > 0 && !s.LastByPrefix.ContainsKey(""))
                    s.LastByPrefix[""] = s.LastPrintedNumber;
                return s;
            }
        }
        catch
        {
            // Fichier corrompu : on repart sur des valeurs par défaut.
        }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
