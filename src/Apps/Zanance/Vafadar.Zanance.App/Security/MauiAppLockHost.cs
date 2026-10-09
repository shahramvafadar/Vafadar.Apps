using Vafadar.Localization;

namespace Vafadar.Zanance.App.Security;

/// <summary>MAUI modal pages for the platform-independent access policy.</summary>
internal sealed class MauiAppLockHost(Translator translator) : IAppLockHost
{
    private static Page? Root => Application.Current?.Windows.FirstOrDefault()?.Page;

    /// <inheritdoc />
    public void ApplyProtection() => ScreenProtection.Apply();

    /// <inheritdoc />
    public ILockCover? CreateCover(AppLockService service, bool prompt) => Root is { } root
        ? new Cover(root, new LockPage(service, translator, prompt)) : null;

    /// <inheritdoc />
    public async Task<bool> ConfirmPinAsync(AppLockService service, string reason)
    {
        if (Root is not { } root) { return false; }
        var completion = new TaskCompletionSource<bool>();
        await root.Navigation.PushModalAsync(new LockPage(service, translator, false, completion, reason), animated: false);
        return await completion.Task;
    }

    /// <inheritdoc />
    public Task<bool> ConfirmRecoveryAsync()
    {
        var page = Root?.Navigation.ModalStack.LastOrDefault();
        return page is LockPage
            ? page.DisplayAlertAsync(translator["Pin_Setting"], translator["Pin_ResetConfirm"], translator["Pin_Remove"], translator["Common_Cancel"])
            : Task.FromResult(false);
    }

    /// <summary>Retains the actual modal page rather than assuming that the current root never changes.</summary>
    private sealed class Cover(Page root, LockPage page) : ILockCover
    {
        /// <inheritdoc />
        public bool IsOnScreen => Root?.Navigation.ModalStack.Contains(page) == true;
        /// <inheritdoc />
        public bool WasLockedBeforeSleep { get => page.WasLockedBeforeSleep; set => page.WasLockedBeforeSleep = value; }
        /// <inheritdoc />
        public Task ShowAsync() => root.Navigation.PushModalAsync(page, animated: false);
        /// <inheritdoc />
        public Task PromptAsync() => page.PromptAsync();
        /// <inheritdoc />
        public async Task CloseAsync()
        {
            // A sensitive prompt may still exist below this cover; never pop an unrelated modal.
            if (Root?.Navigation.ModalStack.LastOrDefault() != page)
            {
                throw new InvalidOperationException("The access cover is not the current modal page.");
            }
            await page.Navigation.PopModalAsync(animated: false);
        }
    }
}
