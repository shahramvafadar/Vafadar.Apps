using System.ComponentModel;

namespace Vafadar.Zanance.App.Features.Sync;

/// <summary>The original optional personal sync screen; native lifetime and secrets stay in explicit ports.</summary>
public partial class PersonalSyncPage : ContentPage
{
    private readonly PersonalSyncViewModel viewModel;
    private AppShell? ownerShell;
    private WeakReference<Page>? firstRunRoot;
    private bool rootRetired;
    /// <summary>Retires accepted departure while keeping literal non-secret inputs for the original return.</summary>
    internal void RetirePublication() => viewModel.Deactivate();
    /// <summary>Permanently retires a replaced root and its original native consent.</summary>
    internal void RetireShellPublication() { rootRetired = true; viewModel.RetireRoot(); }
    /// <summary>Coalesces original appearance and retained-tab return under the actual native owner.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (rootRetired) return Task.CompletedTask;
        if (!viewModel.IsOnboardingRestore && Shell.Current is AppShell shell)
        {
            if (ownerShell is not null && !ReferenceEquals(ownerShell, shell)) { RetireShellPublication(); return Task.CompletedTask; }
            ownerShell = shell; shell.RegisterPersonalSync(this); var root = new WeakReference<AppShell>(shell);
            viewModel.BindRootOwner(() => root.TryGetTarget(out var original) && original.OwnsPersonalSyncPublication
                && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                && (ReferenceEquals(original.CurrentPage, this) || Security.NativeSensitivePresentation.CoversOriginalPage(this)));
        }
        else if (viewModel.IsOnboardingRestore)
        {
            firstRunRoot ??= Application.Current?.Windows.FirstOrDefault()?.Page is { } root ? new(root) : null;
            if (firstRunRoot is { } firstRun)
                viewModel.BindRootOwner(() => firstRun.TryGetTarget(out var original)
                    && ReferenceEquals(Application.Current?.Windows.FirstOrDefault()?.Page, original)
                    && original.Navigation.ModalStack.Contains(this));
        }
        return viewModel.ActivateAsync();
    }

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
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        viewModel.PropertyChanged -= OnPreviewChanged;
        if (!Security.NativeSensitivePresentation.CoversOriginalPage(this)) RetirePublication();
        base.OnDisappearing();
    }

    /// <inheritdoc />
    protected override bool OnBackButtonPressed() => !viewModel.CanLeave || base.OnBackButtonPressed();

    private async void OnPreviewChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(PersonalSyncViewModel.AttachmentPreview) || viewModel.AttachmentPreview is not { } frame
            || !viewModel.OwnsPreview(frame) || Handler is null || Window is null) return;
        await Task.Yield();
        if (Handler is null || Window is null || !viewModel.OwnsPreview(frame)) return;
        try { await ((ScrollView)((Grid)Content!).Children[0]).ScrollToAsync(AttachmentPreviewPanel, ScrollToPosition.Start, false); }
        catch (InvalidOperationException) when (Handler is null || Window is null || !viewModel.OwnsPreview(frame))
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
