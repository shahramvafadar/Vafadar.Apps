using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vafadar.Core.Hosting;
using Vafadar.Localization;
using Vafadar.Maui.Mvvm;

namespace Vafadar.Zanance.App.Features.About;

/// <summary>A component in the notices: its name, and its licence with the copyright holder.</summary>
public sealed record NoticeComponent(string Name, string Licence);

/// <summary>
/// About Zanance (D-39): version, what happens with the user's data, and the third-party notices the licences of the
/// included software and fonts require (<c>Resources/Raw/ThirdPartyNotices.txt</c>, shown in full on its own page).
/// </summary>
public sealed partial class AboutViewModel(Translator translator, IAppEnvironment app) : ViewModelBase
{
    /// <summary>The file with the notices; the first block lists the components, the licence texts follow.</summary>
    public const string NoticesFile = "ThirdPartyNotices.txt";

    private const string Separator = "------";

    // Lines of the licence files are wrapped at about 80 characters; shorter lines end a heading or an entry on purpose.
    private const int WrappedLineLength = 60;

    [ObservableProperty]
    public partial string VersionText { get; set; } = string.Empty;

    /// <summary>Gets the components with their licences, one row each.</summary>
    public ObservableCollection<NoticeComponent> Components { get; } = [];

    /// <summary>Gets the full notices, re-flowed so that they read well at any width.</summary>
    [ObservableProperty]
    public partial string NoticesText { get; set; } = string.Empty;

    public async Task LoadAsync()
    {
        VersionText = translator.Format("Settings_Version", app.Version);
        if (NoticesText.Length > 0)
        {
            return;
        }

        string text;
        await using (var stream = await FileSystem.OpenAppPackageFileAsync(NoticesFile))
        using (var reader = new StreamReader(stream))
        {
            text = (await reader.ReadToEndAsync()).Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        // The component lines ("name – licence – copyright"): after the title and the introduction, before the texts.
        var head = text[..Math.Max(0, text.IndexOf(Separator, StringComparison.Ordinal))];
        foreach (var line in head.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Skip(3))
        {
            var dash = line.IndexOf(" – ", StringComparison.Ordinal);
            Components.Add(dash < 0
                ? new NoticeComponent(line, string.Empty)
                : new NoticeComponent(line[..dash], line[(dash + 3)..].Replace(" – ", " · ", StringComparison.Ordinal)));
        }

        NoticesText = Reflow(text);
    }

    // Joins hard-wrapped lines into paragraphs and turns the separator lines into a short rule.
    private static string Reflow(string text)
    {
        var result = new StringBuilder();
        var lines = text.Split('\n');
        var inLicences = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            if (line.StartsWith(Separator, StringComparison.Ordinal))
            {
                // The component list above the first rule keeps one line per component.
                inLicences = true;
                result.Append("\n――――――――\n");
                continue;
            }

            result.Append(line.Trim());
            var next = i + 1 < lines.Length ? lines[i + 1].Trim() : string.Empty;
            var continues = inLicences && line.Length >= WrappedLineLength && next.Length > 0 && !next.StartsWith(Separator, StringComparison.Ordinal);
            result.Append(continues ? ' ' : '\n');
        }

        return result.ToString().Trim();
    }

    [RelayCommand]
    private Task ShowNoticesAsync() => Shell.Current.GoToAsync(AppShell.NoticesRoute);
}
