namespace Discord;

/// <summary>
///     Represents a generic direct-message channel.
/// </summary>
public interface IDMChannel : IMessageChannel, IPrivateChannel
{
    /// <summary>
    ///     Gets the recipient of all messages in this channel.
    /// </summary>
    /// <returns>
    ///     A user object that represents the other user in this channel.
    /// </returns>
    IEntitySource<ulong, IUser> Recipient { get; }

    /// <summary>
    ///     Closes this private channel, removing it from your channel list.
    /// </summary>
    /// <param name="options">The options to be used when sending the request.</param>
    /// <param name="token">A <see cref="CancellationToken" /> used to cancel the asynchronous operation.</param>
    /// <returns>
    ///     A task that represents the asynchronous close operation.
    /// </returns>
    Task CloseAsync(RequestOptions? options = null, CancellationToken token = default);
}
