using Vafadar.Localization;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.Core.Commerce;
using Vafadar.Zanance.Data.Commerce;

namespace Vafadar.Zanance.App.Presentation;

/// <summary>Translates structured original write rejections without granting rights, exposing identities or guessing purchase state.</summary>
internal static class CommercialFailureMessage
{
    /// <summary>Describes only a structured rejection; unrelated failures retain their established reporting.</summary>
    internal static string? Describe(Exception exception, Translator translator, bool draftKept = false)
    {
        ArgumentNullException.ThrowIfNull(exception); ArgumentNullException.ThrowIfNull(translator);
        if (exception is not CommercialWriteRejectedException rejected) return null;
        string? reason;
        if (rejected.Maximum is { } maximum && rejected.Current is { } current && rejected.Requested is { } requested
            && rejected.Quota is { } quota && maximum >= 0 && current >= 0 && requested >= 0 && ResourceKey(quota) is { } resource)
            reason = translator.Format("Commercial_QuotaReached", translator[resource], Count(maximum), Count((long)current + requested));
        else if (rejected.ResourceId is not null && rejected.Quota is { } kind && ResourceKey(kind) is { } selectedResource)
            reason = translator.Format("Commercial_ResourceSelection", translator[selectedResource]);
        else reason = rejected.Permission switch
        {
            FeaturePermission.RequiresPlus => translator["Commercial_RequiresPlus"],
            FeaturePermission.RequiresPro => translator["Commercial_RequiresPro"],
            FeaturePermission.RequiresMembership => translator["Commercial_RequiresMembership"],
            FeaturePermission.RequiresAddOn => translator["Commercial_RequiresAddOn"],
            FeaturePermission.OutsideScope => translator["Commercial_OutsideScope"],
            FeaturePermission.RequiresProfileSelection => translator["Commercial_ProfileSelection"],
            _ => null,
        };
        // A shared reporter can also be called after a committed operation. Only its pre-write caller may promise retained drafts.
        return reason is null ? null : draftKept ? reason + "\n\n" + translator["Commercial_DraftKept"] : reason;

        string Count(long value) => "\u2066\u200E" + NativeDigits.Apply(value.ToString("N0", translator.Culture)) + "\u200E\u2069";
    }

    /// <summary>Uses a translated original pre-write reason when available, preserving unrelated or post-commit feedback.</summary>
    internal static string OrFallback(Exception exception, Translator translator, string fallback, bool draftKept = false) =>
        Describe(exception, translator, draftKept) ?? translator[fallback];

    /// <summary>Explains a captured pre-Save affordance without a false no-save promise or an authorization grant.</summary>
    internal static string? Describe(WorkRestriction restriction, Translator translator)
    {
        ArgumentNullException.ThrowIfNull(restriction); ArgumentNullException.ThrowIfNull(translator);
        if (restriction.InvalidConfiguration) return null;
        if (restriction.RequiresSelection && restriction.Quota is { } kind && ResourceKey(kind) is { } resource)
            return translator.Format("Commercial_SelectionRequired", translator[resource]);
        if (restriction.Quota is { } quota && restriction.Maximum is { } maximum && restriction.Current is { } current
            && restriction.Requested is { } requested)
            return Describe(new CommercialWriteRejectedException(restriction.Feature, quota, maximum, current, requested), translator);
        return Describe(new CommercialWriteRejectedException(restriction.Feature, restriction.Permission), translator);
    }

    /// <summary>Maps original quota kinds to captions without displaying stored identifiers or enum names.</summary>
    private static string? ResourceKey(QuotaKind kind) => kind switch
    {
        QuotaKind.FinancialAccounts => "Accounts_Title",
        QuotaKind.BudgetDefinitions => "Commercial_BudgetDefinitions",
        QuotaKind.Goals => "Goals_Title",
        QuotaKind.RecurringPlans => "Tab_Plans",
        QuotaKind.QuickTemplates => "Templates_Title",
        QuotaKind.SavedFilters => "Filter_Title",
        QuotaKind.LocalProfiles => "Profile_Title",
        QuotaKind.HostedSpaces => "Commercial_HostedSpaces",
        QuotaKind.SharedMembers => "Commercial_SharedMembers",
        _ => null,
    };
}
