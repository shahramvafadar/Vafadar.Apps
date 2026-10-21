using Microsoft.Extensions.DependencyInjection;
using Vafadar.Localization.Formatting;
using Vafadar.Zanance.App.Features.Plans;
using Vafadar.Zanance.App.Reminders;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Money;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-430: original monetary interpretation across actual plan form display and account changes.</summary>
[Trait("AT", "AT-430")]
public sealed class PlanMoneyContextTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static PlanStore Plans(FlowFixture f) => f.Services.GetRequiredService<PlanStore>();
    private static ReminderService Reminders(FlowFixture f) => new(new NoReminderScheduler(), f.Store, Plans(f),
        f.Services.GetRequiredService<GoalStore>(), f.Services.GetRequiredService<HoldingStore>(), f.Preferences, f.Translator,
        new DateFormatter(f.Localization), f.Localization, f.Time);
    private static PlanEditorViewModel Editor(FlowFixture f, ReminderService reminders) =>
        new(f.Store, Plans(f), f.Translator, f.Localization, new DateFormatter(f.Localization), f.Time, reminders, f.Platform);
    private static async Task<Account> AccountAsync(FlowFixture f, string currency)
    {
        var result = new Account { Name = "Original " + currency, CurrencyCode = currency, OpeningDate = new(2020, 1, 1) };
        await f.Store.SaveAccountAsync(result, Ct); return result;
    }

    /// <summary>Keeps the original grouped amount interpretation when only the formatting culture changes.</summary>
    [Fact]
    public async Task Grouped_original_amount_keeps_its_input_culture_after_format_change()
    {
        using var f = new FlowFixture(); await AccountAsync(f, "EUR"); f.Localization.SetFormattingCulture("en-US");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Name = "Original grouped amount"; vm.AmountText = "1,234";
        f.Localization.SetFormattingCulture("de-DE"); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("1,234", vm.AmountText); Assert.Equal(123400, Assert.Single(await Plans(f).GetSchedulesAsync(Ct)).Amount);
    }

    /// <summary>Refuses a changed display unit instead of multiplying an already entered amount silently.</summary>
    [Fact]
    public async Task Display_unit_change_cannot_reinterpret_existing_raw_amount()
    {
        var originalUnits = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await AccountAsync(f, "EUR");
            await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Name = "Original unit amount"; vm.AmountText = "12.34";
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); await vm.SaveCommand.ExecuteAsync(null);
            Assert.Equal("12.34", vm.AmountText); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct));
            Assert.False(vm.CanSave); Assert.NotNull(vm.AmountError);
        }
        finally { DisplayUnits.Set(originalUnits); }
    }

    /// <summary>Requires an explicit new monetary entry when the selected account changes currency.</summary>
    [Fact]
    public async Task Different_currency_account_cannot_relabel_existing_raw_amount()
    {
        using var f = new FlowFixture(); var euros = await AccountAsync(f, "EUR"); var dollars = await AccountAsync(f, "USD");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Account = vm.Accounts.Single(a => a.Id == euros.Id); vm.Name = "Original euro amount"; vm.AmountText = "12.34";
        vm.Account = vm.Accounts.Single(a => a.Id == dollars.Id); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("12.34", vm.AmountText); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct));
        Assert.False(vm.CanSave); Assert.NotNull(vm.AmountError);
    }

    /// <summary>A partial edit cannot acknowledge another currency; only clearing and entering a fresh amount can.</summary>
    [Fact]
    public async Task Currency_change_requires_complete_reentry_and_retains_original_label()
    {
        using var f = new FlowFixture(); var euros = await AccountAsync(f, "EUR"); var dollars = await AccountAsync(f, "USD");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Account = vm.Accounts.Single(a => a.Id == euros.Id); vm.Name = "Explicit dollar amount"; vm.AmountText = "12.34";
        vm.Account = vm.Accounts.Single(a => a.Id == dollars.Id); vm.AmountText += "0";
        Assert.Equal("EUR", vm.AmountUnitName); Assert.False(vm.CanSave);
        vm.AmountText = string.Empty; vm.AmountText = "56.78";
        Assert.Equal("USD", vm.AmountUnitName); Assert.Null(vm.AmountError); await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await Plans(f).GetSchedulesAsync(Ct)); Assert.Equal(dollars.Id, saved.AccountId); Assert.Equal(5678, saved.Amount);
    }

    /// <summary>Moving to another account in the same currency does not change monetary intent.</summary>
    [Fact]
    public async Task Same_currency_account_preserves_raw_amount_without_reentry()
    {
        using var f = new FlowFixture(); var first = await AccountAsync(f, "EUR"); var second = await AccountAsync(f, "EUR");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Account = vm.Accounts.Single(a => a.Id == first.Id); vm.Name = "Same currency"; vm.AmountText = "12.34";
        vm.Account = vm.Accounts.Single(a => a.Id == second.Id); Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await Plans(f).GetSchedulesAsync(Ct)); Assert.Equal(second.Id, saved.AccountId); Assert.Equal(1234, saved.Amount);
    }

    /// <summary>Each transfer side retains the format in which it was actually entered.</summary>
    [Fact]
    public async Task Transfer_amounts_keep_independent_original_formats()
    {
        using var f = new FlowFixture(); var euros = await AccountAsync(f, "EUR"); var dollars = await AccountAsync(f, "USD");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.KindIndex = 2;
        vm.Account = vm.Accounts.Single(a => a.Id == euros.Id); vm.ToAccount = vm.Accounts.Single(a => a.Id == dollars.Id);
        vm.Name = "Independent formats"; f.Localization.SetFormattingCulture("en-US"); vm.AmountText = "1,234";
        f.Localization.SetFormattingCulture("de-DE"); vm.ToAmountText = "1.234";
        Assert.NotNull(vm.AmountFormatNote); Assert.Null(vm.DestinationAmountFormatNote);
        f.Localization.SetFormattingCulture("fr-FR"); await vm.SaveCommand.ExecuteAsync(null);
        var saved = Assert.Single(await Plans(f).GetSchedulesAsync(Ct)); Assert.Equal(123400, saved.Amount); Assert.Equal(123400, saved.ToAmount);
    }

    /// <summary>Unknown amounts never post hidden stale money or demand irrelevant monetary reentry.</summary>
    [Fact]
    public async Task Unknown_amount_ignores_hidden_old_currency_input()
    {
        using var f = new FlowFixture(); var euros = await AccountAsync(f, "EUR"); var dollars = await AccountAsync(f, "USD");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Account = vm.Accounts.Single(a => a.Id == euros.Id); vm.Name = "Unknown amount"; vm.AmountText = "12.34";
        vm.Account = vm.Accounts.Single(a => a.Id == dollars.Id); vm.AmountModeIndex = (int)Vafadar.Zanance.Core.Plans.AmountMode.Unknown;
        f.Localization.SetFormattingCulture("de-DE"); Assert.Null(vm.AmountFormatNote); Assert.Null(vm.DestinationAmountFormatNote);
        Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Null(Assert.Single(await Plans(f).GetSchedulesAsync(Ct)).Amount); Assert.Equal("12.34", vm.AmountText);
    }

    /// <summary>Changing a display unit blocks the raw value until actual clearing and retains the old visible label.</summary>
    [Fact]
    public async Task Unit_reentry_captures_new_scaling_only_after_clear()
    {
        var original = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await AccountAsync(f, "EUR");
            await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Name = "New display unit"; vm.AmountText = "12.34"; DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            await ((Vafadar.Zanance.App.Presentation.IThemeAware)vm).RefreshThemeAsync();
            Assert.False(vm.CanSave); Assert.Equal("EUR", vm.AmountUnitName); Assert.NotNull(vm.AmountError);
            vm.AmountText = string.Empty; vm.AmountText = "12.34"; Assert.Equal("Hundred euros", vm.AmountUnitName);
            await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(123400, Assert.Single(await Plans(f).GetSchedulesAsync(Ct)).Amount);
        }
        finally { DisplayUnits.Set(original); }
    }

    /// <summary>A read-only readiness publication never recaptures the existing text in the new display context.</summary>
    [Fact]
    public async Task Readonly_resume_keeps_original_parser_and_raw_input()
    {
        using var f = new FlowFixture(); await AccountAsync(f, "EUR"); f.Localization.SetFormattingCulture("en-US");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Name = "Read-only refresh"; vm.AmountText = "1,234"; f.Localization.SetFormattingCulture("de-DE");
        await vm.ResumeFormAsync(); Assert.Equal("1,234", vm.AmountText); Assert.NotNull(vm.AmountFormatNote);
        await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(123400, Assert.Single(await Plans(f).GetSchedulesAsync(Ct)).Amount);
    }

    /// <summary>Existing plan values are initialized and parsed in one captured original display unit.</summary>
    [Fact]
    public async Task Existing_plan_initializes_exact_original_unit_then_keeps_it_after_rename()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([new("EUR", "Hundred euros", 2)]); using var f = new FlowFixture(); var account = await AccountAsync(f, "EUR");
            var plan = new Vafadar.Zanance.Core.Plans.Schedule { Name = "Stored original", AccountId = account.Id, Amount = 123456,
                Rule = new() { Start = new(2026, 10, 9), Frequency = Vafadar.Zanance.Core.Plans.Frequency.Once } };
            await Plans(f).SaveScheduleAsync(plan, Ct);
            await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object> { ["id"] = plan.Id });
            Assert.Equal("12.3456", vm.AmountText); Assert.Equal("Hundred euros", vm.AmountUnitName);
            DisplayUnits.Set([new("EUR", "Renamed unit", 2)]); await vm.ResumeFormAsync();
            Assert.Equal("12.3456", vm.AmountText); Assert.Equal("Hundred euros", vm.AmountUnitName); Assert.False(vm.CanSave);
            Assert.Equal(123456, Assert.Single(await Plans(f).GetSchedulesAsync(Ct)).Amount);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>A unit change during the real writer rolls back before acceptance instead of committing stale interpretation.</summary>
    [Fact]
    public async Task Unit_change_under_writer_rolls_back_original_submission()
    {
        var units = DisplayUnits.All;
        try
        {
            DisplayUnits.Set([]); using var f = new FlowFixture(); await AccountAsync(f, "EUR");
            await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
            vm.Name = "Pending monetary writer"; vm.AmountText = "12.34";
            var reachedWriter = false;
            f.Time.BeforeRead = () =>
            {
                if (!new System.Diagnostics.StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType == typeof(Vafadar.Data.Auditing.AuditingSaveChangesInterceptor))) return;
                reachedWriter = true; f.Time.BeforeRead = null; DisplayUnits.Set([new("EUR", "Hundred euros", 2)]);
            };
            await vm.SaveCommand.ExecuteAsync(null); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct));
            Assert.True(reachedWriter);
            Assert.False(vm.IsBusy); Assert.False(vm.CanSave); Assert.NotNull(vm.AmountError); Assert.Equal("12.34", vm.AmountText);
        }
        finally { DisplayUnits.Set(units); }
    }

    /// <summary>Returning to identical text still retires an older in-flight monetary submission.</summary>
    [Fact]
    public async Task Edit_and_return_to_same_text_cannot_revive_pending_submission()
    {
        using var f = new FlowFixture(); await AccountAsync(f, "EUR");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.Name = "Monetary revision"; vm.AmountText = "12.34";
        var reachedWriter = false;
        f.Time.BeforeRead = () =>
        {
            if (!new System.Diagnostics.StackTrace().GetFrames().Any(frame => frame.GetMethod()?.DeclaringType == typeof(Vafadar.Data.Auditing.AuditingSaveChangesInterceptor))) return;
            reachedWriter = true; f.Time.BeforeRead = null; vm.AmountText = "99"; vm.AmountText = "12.34";
        };
        await vm.SaveCommand.ExecuteAsync(null); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct)); Assert.False(vm.IsBusy);
        Assert.True(reachedWriter);
        Assert.True(vm.CanSave); await vm.SaveCommand.ExecuteAsync(null); Assert.Equal(1234, Assert.Single(await Plans(f).GetSchedulesAsync(Ct)).Amount);
    }

    /// <summary>Validation feedback cannot relabel a previously parsed amount as a newer raw draft.</summary>
    [Fact]
    public async Task Monetary_edit_during_validation_feedback_requires_a_new_explicit_save()
    {
        using var f = new FlowFixture(); await AccountAsync(f, "EUR");
        await using var reminders = Reminders(f); var vm = Editor(f, reminders); await vm.ApplyQueryAsync(new Dictionary<string, object>());
        vm.AmountText = "12.34"; await vm.SaveCommand.ExecuteAsync(null); Assert.NotNull(vm.NameError);
        vm.Name = "Validation boundary"; var changed = false;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.NameError) && vm.NameError is null && !changed)
            { changed = true; vm.AmountText = "99.00"; }
        };
        await vm.SaveCommand.ExecuteAsync(null); Assert.True(changed); Assert.Empty(await Plans(f).GetSchedulesAsync(Ct));
        Assert.Equal("99.00", vm.AmountText); await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal(9900, Assert.Single(await Plans(f).GetSchedulesAsync(Ct)).Amount);
    }
}
