namespace ComptCopie;

/// <summary>Logo et icône embarqués dans l'exécutable (dossier Assets).</summary>
internal static class AppAssets
{
    private static Stream Open(string name) =>
        typeof(AppAssets).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Ressource introuvable : {name}");

    private static Image? _logo;
    private static Icon? _icon;

    public static Image Logo => _logo ??= LoadImage("logo.png");
    public static Icon Icon => _icon ??= new Icon(Open("logo.ico"));

    // Copie en mémoire : GDI+ exige que le flux reste ouvert tant que l'image vit.
    private static Image LoadImage(string name)
    {
        using var stream = Open(name);
        var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;
        return Image.FromStream(memory);
    }
}
