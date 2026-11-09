namespace Vafadar.Zanance.Data.Tests;

/// <summary>Persisted and newly submitted original consent metadata share the original offer contract.</summary>
public sealed class AiConsentMetadataTests
{
    /// <summary>Each malformed field is checked independently at every original private/primary boundary.</summary>
    public static TheoryData<int, int> InvalidCases
    {
        get { var data = new TheoryData<int, int>(); for (var variant = 0; variant < 11; variant++) for (var operation = 0; operation < 4; operation++) data.Add(variant, operation); return data; }
    }
    /// <summary>Corrupt original consent cannot reach credentials, private publication or source revalidation.</summary>
    [Theory]
    [MemberData(nameof(InvalidCases))]
    public async Task Invalid_consent_metadata_refuses_before_each_original_effect(int variant, int operation)
    {
        var v = await AiConsentMetadataProbes.ObserveAsync(variant, operation, TestContext.Current.CancellationToken);
        Assert.True(v.Refused, $"variant{variant}/operation{operation}: {v.OtherError ?? "accepted"}");
        Assert.False(v.Accepted); Assert.Null(v.OtherError);
        Assert.Equal(operation == 3 ? 0 : 1, v.Reads); Assert.Equal(0, v.Writes);
        Assert.Equal(0, v.Credentials); Assert.Equal(0, v.Rechecks);
    }
    /// <summary>Historical expiry and exact metadata maxima cannot erase the original readable consent.</summary>
    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    public async Task Valid_literal_historical_consent_remains_readable(int variant)
    {
        var v = await AiConsentMetadataProbes.ObserveAsync(variant, 0, TestContext.Current.CancellationToken);
        Assert.True(v.Accepted); Assert.False(v.Refused); Assert.Null(v.OtherError);
        Assert.Equal(1, v.Reads); Assert.Equal(0, v.Writes); Assert.Equal(0, v.Credentials); Assert.Equal(0, v.Rechecks);
    }
    /// <summary>An unsupported selected purpose cannot begin metadata transport or credential lookup.</summary>
    [Fact]
    public async Task Unsupported_metadata_purpose_refuses_before_credentials()
    {
        var v = await AiConsentMetadataProbes.ObserveAsync(10, 4, TestContext.Current.CancellationToken);
        Assert.True(v.Refused); Assert.Null(v.OtherError); Assert.Equal(0, v.Credentials);
        Assert.Equal(0, v.Reads); Assert.Equal(0, v.Writes); Assert.Equal(0, v.Rechecks);
    }
}
