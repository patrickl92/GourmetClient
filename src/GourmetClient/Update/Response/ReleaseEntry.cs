using System;
using System.Text.Json.Serialization;

namespace GourmetClient.Update.Response
{
    internal class ReleaseEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("draft")]
        public bool IsDraft { get; set; }

        [JsonPropertyName("assets")]
        public ReleaseAsset[] Assets { get; set; } = Array.Empty<ReleaseAsset>();
    }
}
