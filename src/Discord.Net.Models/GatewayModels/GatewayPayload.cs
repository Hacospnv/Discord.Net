using System;
using System.Text.Json.Serialization;

namespace Discord.API.Gateway
{
    public sealed class GatewayPayload
    {
        [JsonPropertyName("op")]
        public GatewayOpCode Operation { get; set; }
        [JsonPropertyName("t")]
        public string? Type { get; set; }
        [JsonPropertyName("s")]
        public int? Sequence { get; set; }
        [JsonPropertyName("d")]
        public object? Payload { get; set; }
    }
}

