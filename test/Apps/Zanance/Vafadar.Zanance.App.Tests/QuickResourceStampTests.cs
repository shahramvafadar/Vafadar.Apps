using System.Text.Json;
using Vafadar.Zanance.Core.Accounts;
using Vafadar.Zanance.Core.Categories;
using Vafadar.Zanance.Core.Ledger;
using Vafadar.Zanance.Core.Settings;
using Vafadar.Zanance.Data;
namespace Vafadar.Zanance.App.Tests;
/// <summary>Complete stamp equality and mutation coverage without a database or financial writer.</summary>
public sealed class QuickResourceStampTests
{
    private sealed record Inputs(IReadOnlyList<EntryTemplate> Templates, IReadOnlyList<SavedFilter> Filters,
        IReadOnlyList<Account> Accounts, IReadOnlyList<Category> Categories, ZananceSettings? Settings, LedgerEntry? Source);
    private static Inputs Fixture(int size, bool source) => new(
        [Make<EntryTemplate>(2), Make<EntryTemplate>(3)], [Make<SavedFilter>(2), Make<SavedFilter>(3)],
        Enumerable.Range(0, size).Select(Make<Account>).ToArray(), [Make<Category>(2), Make<Category>(3)],
        Make<ZananceSettings>(2), source ? Make<LedgerEntry>(3) : null);
    private static string Original(Inputs p) => JsonSerializer.Serialize(new
    { p.Templates, p.Filters, p.Accounts, p.Categories, p.Settings, p.Source });
    private static string Current(Inputs p) => QuickResourceReview.Capture(p.Templates,p.Filters,p.Accounts,p.Categories,p.Settings,p.Source);
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(200, false)]
    public void Complete_stamp_keeps_original_JSON(int size, bool source)
    {
        var p=Fixture(size,source); Assert.Equal(Original(p),Current(p));
        p=p with { Settings=null, Source=null }; Assert.Equal(Original(p),Current(p));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Every_original_mutable_property_still_changes_the_complete_stamp(int entityIndex)
    {
        var p=Fixture(2,true);
        object[] entities=[p.Templates[0],p.Filters[0],p.Accounts[0],p.Categories[0],p.Settings!,p.Source!];
        var entity=entities[entityIndex];
        foreach(var prop in entity.GetType().GetProperties().Where(p=>p.SetMethod?.IsPublic==true))
        {
            var before=Current(p); var prior=prop.GetValue(entity); var changed=Value(prop.PropertyType,8);
            if(Equals(prior,changed)) changed=Value(prop.PropertyType,9);
            prop.SetValue(entity,changed);
            Assert.Equal(Original(p),Current(p)); Assert.NotEqual(before,Current(p)); prop.SetValue(entity,prior);
            if(Nullable.GetUnderlyingType(prop.PropertyType)!=null || !prop.PropertyType.IsValueType)
            { prop.SetValue(entity,null); Assert.Equal(Original(p),Current(p)); prop.SetValue(entity,prior); }
        }
    }
    [Fact]
    public void Order_duplicates_and_in_place_nested_mutation_remain_significant()
    {
        var p=Fixture(2,true); var original=Current(p);
        var reversed=p with { Accounts=p.Accounts.Reverse().ToArray() };
        Assert.Equal(Original(reversed),Current(reversed)); Assert.NotEqual(original,Current(reversed));
        p.Source!.Tags.Add("duplicate"); p.Source.Tags.Add("duplicate"); p.Filters[0].CategoryIds.Add(Guid.Empty);
        Assert.Equal(Original(p),Current(p)); Assert.NotEqual(original,Current(p));
    }
private static object Value(Type type, int seed)
{
    var t = Nullable.GetUnderlyingType(type) ?? type;
    if (t == typeof(string)) return seed % 2 == 0 ? "فارسیé<\"\\\n" : "z‏😀";
    if (t == typeof(Guid)) return new Guid(seed, 2, 3, new byte[8]);
    if (t == typeof(bool)) return seed % 2 == 0;
    if (t == typeof(int)) return seed % 5;
    if (t == typeof(long)) return seed % 2 == 0 ? long.MaxValue : long.MinValue;
    if (t == typeof(decimal)) return seed % 2 == 0 ? 123.4500m : -0.0001m;
    if (t == typeof(double)) return 123.45;
    if (t == typeof(DateOnly)) return seed % 2 == 0 ? DateOnly.MinValue : DateOnly.MaxValue;
    if (t == typeof(TimeOnly)) return seed % 2 == 0 ? TimeOnly.MinValue : TimeOnly.MaxValue;
    if (t == typeof(DateTimeOffset)) return seed % 2 == 0 ? DateTimeOffset.MinValue : DateTimeOffset.MaxValue;
    if (t.IsEnum) { var values=Enum.GetValues(t); return values.GetValue(seed % values.Length)!; }
    if (t == typeof(List<Guid>)) return new List<Guid> { Guid.Empty, new Guid(seed, 2, 3, new byte[8]), Guid.Empty };
    if (t == typeof(List<string>)) return new List<string> { "x"+seed, "فارسی", "x"+seed };
    throw new InvalidOperationException("Uncovered property type " + t.FullName);
}
private static T Make<T>(int seed) where T : class
{
    var value=Activator.CreateInstance<T>();
    foreach(var p in typeof(T).GetProperties().Where(p=>p.SetMethod?.IsPublic==true)) p.SetValue(value,Value(p.PropertyType,seed));
    return value;
}
}
