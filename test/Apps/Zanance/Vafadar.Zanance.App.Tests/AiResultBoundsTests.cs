using System.Text;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Focused final hostile-output/null contract boundaries, without repeating completed financial source scenarios.</summary>
[Trait("AT", "AT-298")]
public sealed class AiResultBoundsTests
{
    [Theory][InlineData(4000, true)][InlineData(4001, false)]
    public void Plain_text_output_has_a_finite_private_journal_bound(int characters, bool accepted)
    {
        var value = new string('\u06F9', characters);
        if (accepted) AiProtocol.ValidateText(value); else Assert.Throws<ArgumentException>(() => AiProtocol.ValidateText(value));
    }
    [Fact]
    public void Invalid_UTF16_never_becomes_a_silently_replaced_model_result() => Assert.Throws<EncoderFallbackException>(() => AiProtocol.ValidateText("\uD800"));
    [Theory][InlineData(true)][InlineData(false)]
    public void Explicit_null_period_or_currency_row_is_refused_as_invalid_input(bool nullPeriod)
    {
        var payload = new AiPayload(1, AiPurpose.PeriodExplanation, "en", nullPeriod ? null! : new(new(2026, 10, 1), new(2026, 10, 17), 1, true, [null!], 0), null);
        Assert.ThrowsAny<ArgumentException>(() => AiProtocol.Validate(payload));
    }
}
