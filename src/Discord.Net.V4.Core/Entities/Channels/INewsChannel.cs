using Discord.Models;

namespace Discord;

/// <summary>
///     Represents a generic news channel in a guild that can send and receive messages.
/// </summary>
public interface INewsChannel :
    ITextChannel,
    INewsChannelActor,
    IUpdatable<IGuildNewsChannelModel>
{
    new IGuildNewsChannelModel GetModel();
}
