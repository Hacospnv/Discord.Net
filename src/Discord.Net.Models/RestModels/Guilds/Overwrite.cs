using System.Text.Json.Serialization;

namespace Discord.Models.Json;

public sealed class Overwrite
{
    [JsonPropertyName("id")]
    public ulong TargetId { get; set; }

    [JsonPropertyName("type")]
    public int TargetType { get; set; }

    [JsonPropertyName("deny")]
    public ulong Deny { get; set; }

    [JsonPropertyName("allow")]
    public ulong Allow { get; set; }
}
