using Discord.Converters;
using System.Text.Json.Serialization;

namespace Discord.Models.Json;

[ComponentType(ComponentTypes.ActionRow)]
public sealed class ActionRow : MessageComponent
{
    [JsonPropertyName("components")]
    public required MessageComponent[] Components { get; set; }
}
