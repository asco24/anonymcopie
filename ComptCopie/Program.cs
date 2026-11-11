namespace ComptCopie;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Écran d'accueil animé, puis fenêtre principale.
        using (var splash = new SplashForm())
            Application.Run(splash);

        Application.Run(new MainForm());
    }
}
