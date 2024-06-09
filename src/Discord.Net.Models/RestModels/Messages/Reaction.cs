using System.Text.Json.Serialization;

namespace Discord.Models.Json;

public sealed class Reaction : IReactionModel
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("me")]
    public bool Me { get; set; }

    [JsonPropertyName("me_burst")]
    public bool IsMeBurst { get; set; }

    [JsonPropertyName("emoji")]
    public required IEmote Emoji { get; set; }

    [JsonPropertyName("count_details")]
    public required ReactionCountDetails CountDetails { get; set; }

    [JsonPropertyName("burst_colors")]
    public required string[] Colors { get; set; }

    ulong? IReactionModel.EmojiId => (Emoji as GuildEmote)?.Id;

    string? IReactionModel.EmojiName => Emoji.Name;
}
