using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Vafadar.Zanance.Core.Holdings;

namespace Vafadar.Zanance.App.Features.Holdings;

public sealed partial class AssetTypeEditorViewModel
{
    private CultureInfo? _purityCulture, _weightCulture;
    /// <summary>Explains the original free-purity decimal format without changing the raw value.</summary>
    public string? PurityFormatNote => HasMetal && IsOtherPurity ? OriginalPhysicalFormat(PurityText, _purityCulture, 18.5m) : null;
    /// <summary>Explains the independently original gram-weight decimal format.</summary>
    public string? UnitWeightFormatNote => OriginalPhysicalFormat(UnitWeightText, _weightCulture, 1.234m);
    /// <summary>Uses the weight editor's actual original parser for its numeric example.</summary>
    public string UnitWeightExample => 8.133m.ToString("0.###", _weightCulture ?? _localization.CurrentCulture);
    /// <summary>Shows the name validation at its own field.</summary>
    [ObservableProperty] public partial string? NameError { get; set; }
    /// <summary>Shows free-purity validation without hiding the independent gram-weight error.</summary>
    [ObservableProperty] public partial string? PurityError { get; set; }
    /// <summary>Shows gram-weight validation beside the original raw value.</summary>
    [ObservableProperty] public partial string? UnitWeightError { get; set; }

    private CultureInfo CapturePhysicalCulture() => CultureInfo.ReadOnly((CultureInfo)_localization.CurrentCulture.Clone());
    private string? OriginalPhysicalFormat(string text, CultureInfo? original, decimal example) =>
        !string.IsNullOrWhiteSpace(text) && original is not null &&
        (original.NumberFormat.NumberDecimalSeparator != _localization.CurrentCulture.NumberFormat.NumberDecimalSeparator ||
         original.NumberFormat.NumberGroupSeparator != _localization.CurrentCulture.NumberFormat.NumberGroupSeparator)
        ? _translator.Format("Numeric_OriginalInputFormat", $"\u2066\u200E{example.ToString("0.###", original)}\u200E\u2069") : null;
    partial void OnPurityTextChanging(string value)
    {
        // D-437: partial typing after a display change retains the original parser; only full clearing renews it.
        if (!_publishing && (!_initialized || string.IsNullOrWhiteSpace(PurityText) || string.IsNullOrWhiteSpace(value))) _purityCulture = CapturePhysicalCulture();
    }
    partial void OnUnitWeightTextChanging(string value)
    {
        if (!_publishing && (!_initialized || string.IsNullOrWhiteSpace(UnitWeightText) || string.IsNullOrWhiteSpace(value))) _weightCulture = CapturePhysicalCulture();
    }
    partial void OnPurityTextChanged(string value) { DraftChanged(); PurityError = null; RefreshNumericPresentation(); }
    partial void OnUnitWeightTextChanged(string value) { DraftChanged(); UnitWeightError = null; RefreshNumericPresentation(); }
    partial void OnNameChanged(string value) { DraftChanged(); NameError = null; }
    partial void OnMetalIndexChanged(int value) { DraftChanged(); }
    partial void OnCountUnitNameChanged(string value) { DraftChanged(); }
    partial void OnDivisibleChanged(bool value) { DraftChanged(); }
    partial void OnCurrencyCodeChanged(string value) { DraftChanged(); }
    partial void OnNoteChanged(string value) { DraftChanged(); }

    /// <summary>Publishes only display metadata; raw values and original cultures stay unchanged.</summary>
    public void RefreshNumericPresentation()
    {
        if (_departed || !OwnsRoot) return;
        if (NameError is not null) NameError = _translator["AssetType_NameRequired"];
        if (PurityError is not null) PurityError = _translator["AssetType_PurityInvalid"];
        if (UnitWeightError is not null) UnitWeightError = _translator["AssetType_WeightInvalid"];
        OnPropertyChanged(nameof(PurityFormatNote)); OnPropertyChanged(nameof(UnitWeightFormatNote)); OnPropertyChanged(nameof(UnitWeightExample));
    }
    private void OnNumericDisplayChanged(object? sender, EventArgs args) => RefreshDisplay();
    /// <summary>Attaches display notifications only while the actual original native page is present.</summary>
    public void SetNumericPresentationActive(bool active)
    {
        _localization.Changed -= OnNumericDisplayChanged;
        if (active) _localization.Changed += OnNumericDisplayChanged;
        RefreshNumericPresentation();
    }
}
