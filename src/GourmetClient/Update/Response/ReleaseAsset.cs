using System.Text.Json.Serialization;

namespace GourmetClient.Update.Response
{
    internal class ReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
