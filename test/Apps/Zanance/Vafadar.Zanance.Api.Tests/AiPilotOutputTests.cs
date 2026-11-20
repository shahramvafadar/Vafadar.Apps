using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Vafadar.Zanance.Api.Analysis;
using Vafadar.Zanance.Core.Analysis;

namespace Vafadar.Zanance.Api.Tests;

/// <summary>Exercises output filtering through the frozen synthetic provider route.</summary>
public sealed class AiPilotOutputTests
{
    /// <summary>Retains usage and status when a refusal accompanies otherwise valid structured output.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refusal_suppresses_otherwise_valid_output(bool separateMessage)
    {
        const string model = "gpt-6.2-sol";
        const string project = "fictional-refusal-project";
        const string operation = "resp_refusal_fixture";
        var revision = Convert.ToHexStringLower(SHA256.HashData(
            Encoding.UTF8.GetBytes($"zanance-pilot-v1|openai|{model}|{project}")));
        var route = new AiPilotRouteRow { Provider = "openai", Purpose = AiPurpose.PeriodExplanation, Revision = revision };
        var offer = new AiOffer(Guid.NewGuid(), route.Provider, model, route.Purpose, 1, DateTimeOffset.UnixEpoch.AddDays(1));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analysis:ProviderPilot:openai:ApiKey"] = "fictional-output-only",
            ["Analysis:ProviderPilot:openai:Project"] = project,
        }).Build();
        var output = JsonSerializer.Serialize(new { net_expenses_minor = 38000, explanation = "Fictional expenses." });
        var answer = new { type = "output_text", text = output };
        var refusal = new { type = "refusal", refusal = "Fictional refusal." };
        object[] messages = separateMessage
            ? [new { type = "message", content = new[] { answer } }, new { type = "message", content = new[] { refusal } }]
            : [new { type = "message", content = new object[] { answer, refusal } }];
        var response = JsonSerializer.Serialize(new
        {
            id = operation, model, status = "completed", usage = new { input_tokens = 100, output_tokens = 40 }, output = messages,
        });
        using var transport = new RefusalReceipt(operation, response);
        var provider = new AiPilotProvider(config, transport, TimeProvider.System);

        var outcome = await provider.StatusAsync(route, offer, AiProtocol.Digest(AiPilotProvider.ReportFixture), operation,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, transport.Reads);
        Assert.Equal(AiProviderState.Unknown, outcome.State);
        Assert.Null(outcome.Text);
        Assert.Equal(AiPilotVendorStatus.Completed, outcome.VendorStatus);
        Assert.Equal(100, outcome.Metering?.Input);
        Assert.Equal(40, outcome.Metering?.Output);
    }

    private sealed class RefusalReceipt(string operation, string response) : HttpMessageHandler, IHttpClientFactory
    {
        internal int Reads { get; private set; }

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"https://api.openai.com/v1/responses/{operation}", request.RequestUri!.AbsoluteUri);
            Reads++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(response, Encoding.UTF8, "application/json") });
        }
    }
}
