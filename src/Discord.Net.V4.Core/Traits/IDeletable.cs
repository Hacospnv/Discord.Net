namespace Discord;


public interface IDeletable<TId, out TSelf> : IEntity<TId>, IPathable
    where TSelf : IDeletable<TId, TSelf>, IEntity<TId>, IPathable
    where TId : IEquatable<TId>
{
    Task DeleteAsync(RequestOptions? options = null, CancellationToken token = default)
        => DeleteAsync(Client, this, Id, options, token);

    internal static Task DeleteAsync(IDiscordClient client, IPathable path, TId id, RequestOptions? options = null, CancellationToken token = default)
        => client.RestApiClient.ExecuteAsync(TSelf.DeleteRoute(path, id), options ?? client.DefaultRequestOptions, token);

    internal static abstract BasicApiRoute DeleteRoute(IPathable path, TId id);
}
