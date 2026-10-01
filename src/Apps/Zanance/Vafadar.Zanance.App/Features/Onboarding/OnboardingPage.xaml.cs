namespace Vafadar.Zanance.App.Features.Onboarding;

public partial class OnboardingPage : ContentPage
{
    // A readable column on wide windows (desktop, tablets); the full width less the margins on phones.
    private const double MaxContentWidth = 560;
    private const double SideMargin = 24;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        SizeChanged += (_, _) => UpdateWidth();
    }

    private void UpdateWidth()
    {
        if (Width <= 0)
        {
            return;
        }

        var width = Math.Min(MaxContentWidth, Width - (2 * SideMargin));
        Body.WidthRequest = width;
        Footer.WidthRequest = width;
        Progress.WidthRequest = Math.Min(240, width);
    }
}
