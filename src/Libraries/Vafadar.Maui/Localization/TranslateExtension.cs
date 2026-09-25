using System.Diagnostics.CodeAnalysis;
using Vafadar.Localization;

namespace Vafadar.Maui.Localization;

/// <summary>
/// XAML markup extension that shows a translated string and updates it when the language changes.
/// </summary>
/// <example>
/// <code>
/// &lt;ContentPage xmlns:v="http://vafadar.pro/schemas/maui" ...&gt;
///     &lt;Label Text="{v:Translate Settings_Language}" /&gt;
///     &lt;Label Text="{v:Translate Settings_Version, StringFormat='{0}!'}" /&gt;
/// </code>
/// </example>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class TranslateExtension : IMarkupExtension<BindingBase>
{
    /// <summary>Gets or sets the resource key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional format applied to the translated string.</summary>
    public string? StringFormat { get; set; }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Translator))]
    public BindingBase ProvideValue(IServiceProvider serviceProvider) => new Binding
    {
        Mode = BindingMode.OneWay,
        Path = $"[{Key}]",
        Source = Translator.Instance,
        StringFormat = StringFormat,
    };

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
