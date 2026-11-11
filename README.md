# AnonymCopie

**Compostage des copies d'examen** : application Windows qui imprime des numéros d'anonymat sur des feuilles A4. Chaque feuille porte le même numéro deux fois, en en-tête et dans le corps de la page, par exemple `L1SN-001`.

## Installation

Aucune installation n'est nécessaire : `AnonymCopie.exe` contient tout ce qu'il faut, y compris .NET.

1. Copiez `AnonymCopie.exe` sur votre PC (sur le bureau, par exemple). Vous le trouverez dans le dossier partagé du service.
2. Double-cliquez dessus pour le lancer.
3. Si Windows affiche « Windows a protégé votre ordinateur », cliquez sur **Informations complémentaires**, puis sur **Exécuter quand même**. Ce message vient de ce que l'application n'est pas signée.

Mieux vaut lancer une copie locale que le fichier du partage. Tant que l'application est ouverte depuis le partage, elle ne peut pas y être mise à jour.

## Utilisation

1. **Préfixe** : choisissez le préfixe dans la liste, par exemple `L1SN`.
   - **＋** ajoute un préfixe. Il ne doit contenir que des lettres et des chiffres.
   - **－** retire le préfixe sélectionné de la liste. Son compteur est conservé.
2. **Nombre de chiffres** : longueur du numéro. Avec 3 chiffres, on obtient `L1SN-001`.
3. **Début de plage** : premier numéro à imprimer. L'application propose automatiquement le numéro qui suit le dernier imprimé pour ce préfixe.
4. **Nombre de copies** : nombre de feuilles à imprimer. La plage et le total s'affichent en rouge.
5. **Imprimante…** : choisissez l'imprimante. Elle est mémorisée pour les fois suivantes.
6. **Aperçu avant impression** : vérifiez les pages avant de lancer l'impression.
7. Cliquez sur **Impression Centrée**, **Impression à Gauche** ou **Impression à Droite** selon la position voulue pour le numéro. L'application demande une confirmation avant d'imprimer.
8. Une fois l'impression terminée, la plage imprimée reste affichée. Cliquez sur **Nouvelle impression** pour préparer la série suivante.

### Compteurs

- Chaque préfixe a son propre compteur. Par exemple, `L1SN` peut en être à 120 pendant que `L2SN` en est à 45.
- Le compteur ne change qu'après une impression réelle. Un aperçu ne le modifie pas.
- Si vous réimprimez une ancienne plage, le compteur ne recule pas.
- Les réglages sont enregistrés sur chaque PC, dans `%AppData%\AnonymCopie\settings.json`. Si vous imprimez depuis deux PC différents, chacun a donc ses propres compteurs.

## Pour les développeurs

L'application est écrite en C# avec WinForms et .NET 8. Le code source se trouve dans le dossier `ComptCopie/`.

Pour compiler, il faut le SDK .NET 8 ou une version plus récente. La compilation fonctionne aussi sous Linux.

```bash
# Compilation
dotnet build ComptCopie/ComptCopie.csproj

# Exécutable Windows autonome → publish/AnonymCopie.exe
dotnet publish ComptCopie/ComptCopie.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Si .NET 8 Desktop Runtime est installé sur les PC, vous pouvez générer un exécutable léger (environ 200 Ko) en remplaçant `--self-contained true` par `--self-contained false`.

| Fichier | Rôle |
|---|---|
| `MainForm.cs` | Fenêtre principale (interface construite dans le code) |
| `NumberPrinter.cs` | Mise en page et impression des feuilles |
| `AppSettings.cs` | Réglages et compteurs (fichier JSON) |
| `PrefixDialog.cs` | Fenêtre d'ajout d'un préfixe |
| `SplashForm.cs` | Écran d'accueil animé |
| `Assets/` | Logo et icône de l'application |
