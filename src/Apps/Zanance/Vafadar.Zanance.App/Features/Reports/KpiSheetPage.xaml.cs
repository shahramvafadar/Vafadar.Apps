namespace Vafadar.Zanance.App.Features.Reports;

public partial class KpiSheetPage : ContentPage
{
    public KpiSheetPage(KpiSheetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
