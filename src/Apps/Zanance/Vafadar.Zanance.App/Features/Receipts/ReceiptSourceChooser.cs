using Vafadar.Localization;
using Vafadar.Zanance.App.Interaction;

namespace Vafadar.Zanance.App.Features.Receipts;

/// <summary>The user's explicit receipt source; help and cancellation never open a native source.</summary>
internal enum ReceiptSourceChoice
{
    /// <summary>The user closed source selection without opening a source.</summary>
    Cancelled,
    /// <summary>The user explicitly selected the camera.</summary>
    Photo,
    /// <summary>The user explicitly selected the file picker.</summary>
    File,
}

/// <summary>Offers source-specific reading help without adding a confirmation to the ordinary camera/file route.</summary>
internal static class ReceiptSourceChooser
{
    /// <summary>Returns only a chosen source; the native adapter retains camera, file and permission ownership.</summary>
    internal static async Task<ReceiptSourceChoice> ChooseAsync(Translator translator, IAppInteraction interaction,
        bool canTakePhoto, bool showLatinImageHelp)
    {
        if (!canTakePhoto && !showLatinImageHelp) return ReceiptSourceChoice.File;
        while (true)
        {
            var photo = translator["Receipt_TakePhoto"];
            var file = translator["Receipt_ChooseFile"];
            var help = translator["Receipt_ImageHelp"];
            var actions = new List<string>();
            if (canTakePhoto) actions.Add(photo);
            actions.Add(file);
            if (showLatinImageHelp) actions.Add(help);
            var choice = await interaction.ChooseAsync(translator["Receipt_SourceTitle"],
                translator["Common_Cancel"], [.. actions]);
            if (showLatinImageHelp && choice == help)
            {
                // Android's bundled Latin model does not recognize Persian image text. Digital
                // text-layer PDFs follow a separate original reader and must not be ruled out.
                await interaction.AlertAsync(help, translator["Receipt_ImageHelpText"], translator["Common_Ok"]);
                continue;
            }
            if (canTakePhoto && choice == photo) return ReceiptSourceChoice.Photo;
            return choice == file ? ReceiptSourceChoice.File : ReceiptSourceChoice.Cancelled;
        }
    }
}
