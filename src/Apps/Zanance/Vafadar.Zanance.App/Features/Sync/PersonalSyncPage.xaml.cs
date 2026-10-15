using System.ComponentModel;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>The original optional personal sync screen; native lifetime and secrets stay in explicit ports.</summary>
public partial class PersonalSyncPage : ContentPage
{
    private readonly PersonalSyncViewModel viewModel;

    /// <summary>Builds controls only; no service session or financial snapshot is opened during construction.</summary>
    public PersonalSyncPage(PersonalSyncViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
        AttachmentImageViewport.SizeChanged += (_, _) => viewModel.SetAttachmentViewport(AttachmentImageViewport.Width);
    }
    /// <summary>Keeps first-run navigation explicit while using this same original personal sync flow.</summary>
    public void BeginOnboarding(Func<Task> close, Func<Task> finish) => viewModel.BeginOnboarding(close, finish);

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        viewModel.PropertyChanged -= OnPreviewChanged; viewModel.PropertyChanged += OnPreviewChanged;
        // The flow translates failures without sending credentials or response payloads to general exception logs.
        await viewModel.ActivateAsync();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        viewModel.PropertyChanged -= OnPreviewChanged; viewModel.Deactivate();
        base.OnDisappearing();
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !viewModel.CanLeave || base.OnBackButtonPressed();

    private async void OnPreviewChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(PersonalSyncViewModel.AttachmentPreview) || !viewModel.HasAttachmentPreview || Handler is null || Window is null) return;
        await Task.Yield();
        if (Handler is null || Window is null || !viewModel.HasAttachmentPreview) return;
        try { await ((ScrollView)((Grid)Content!).Children[0]).ScrollToAsync(AttachmentPreviewPanel, ScrollToPosition.Start, false); }
        catch (InvalidOperationException) when (Handler is null || Window is null)
        {
            // A native tree retired during layout must not be revived by a completed preview.
        }
    }
}

/// <summary>Creates an in-memory image source from the one current native-rendered preview, never an original file path.</summary>
public sealed class PersonalSyncPreviewImageConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        value is PersonalSyncRenderedPage page ? ImageSource.FromStream(() => new MemoryStream(page.Data, writable: false)) : null;
    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        throw new NotSupportedException();
}
