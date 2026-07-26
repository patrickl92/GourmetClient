using System.Text.Json.Serialization;

namespace GourmetClient.Network.GourmetApi;

internal class BillingResponse
{
    [JsonPropertyName("Billings")]
    public required Bill[] Bills { get; set; }
}