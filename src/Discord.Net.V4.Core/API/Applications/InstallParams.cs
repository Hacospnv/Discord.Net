using System.Text.Json.Serialization;

namespace Discord.API;

public sealed class InstallParams
{
    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; }

    [JsonPropertyName("permissions")]
    public ulong Permission { get; set; }
}
