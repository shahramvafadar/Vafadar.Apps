namespace Vafadar.Zanance.App.Features.Categories;

public partial class CategoryEditorPage : ContentPage, IQueryAttributable
{
    private readonly CategoryEditorViewModel _viewModel;
    private AppShell? _ownerShell;
    private bool _shellRetired;
    private bool _publicationActive;
    /// <summary>Shares original appearance/navigation publication for one actual page presence.</summary>
    internal Task PendingPublication { get; private set; } = Task.CompletedTask;

    public CategoryEditorPage(CategoryEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Captures the native route; actual appearance/navigation share the first covered publication.</summary>
    public async void ApplyQueryAttributes(IDictionary<string, object> query) =>
        await Presentation.Failures.GuardAsync(() => _viewModel.ApplyQueryAsync(query, load: false));

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_shellRetired) return;
        if (Shell.Current is AppShell shell)
        {
            if (_ownerShell is not null && !ReferenceEquals(_ownerShell, shell)) { RetireShellPublication(); return; }
            _ownerShell = shell; shell.RegisterCategoryEditor(this);
        }
        await Presentation.Failures.GuardAsync(RefreshForNavigationAsync);
    }

    /// <summary>Refreshes a retained actual tab without a second automatic Retry for the same presence.</summary>
    internal Task RefreshForNavigationAsync()
    {
        if (_shellRetired) return Task.CompletedTask;
        if (_publicationActive) return PendingPublication;
        _publicationActive = true; _viewModel.SetActive(true);
        return PendingPublication = PublishAfterRouteAsync();
    }
    private async Task PublishAfterRouteAsync()
    {
        // Shell applies the route while building its original native navigation; keep the first frame covered.
        await Task.Yield();
        if (!_shellRetired && _publicationActive) await _viewModel.LoadAsync();
    }
    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () => await Presentation.Failures.GuardAsync(() => _viewModel.CancelCommand.ExecuteAsync(null)));
        return true;
    }
    /// <summary>Retires original rows, reads and native confirmations on actual departure.</summary>
    internal void RetirePublication() { _publicationActive = false; _viewModel.SetActive(false); }
    /// <summary>Prevents an old native root from publishing or accepting original category-form commands.</summary>
    internal void RetireShellPublication() { _shellRetired = true; RetirePublication(); }
    protected override void OnDisappearing() { RetirePublication(); base.OnDisappearing(); }
}
