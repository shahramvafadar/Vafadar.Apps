namespace Vafadar.Zanance.App.Features.Goals;

public partial class GoalEditorPage : ContentPage
{
    public GoalEditorPage(GoalEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}