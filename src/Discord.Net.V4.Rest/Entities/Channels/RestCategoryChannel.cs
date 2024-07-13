using Discord.Models;

namespace Discord.Rest.Channels;

public sealed partial class RestLoadableCategoryChannelActor(
    DiscordRestClient client,
    GuildIdentity guild,
    CategoryChannelIdentity channel
):
    RestCategoryChannelActor(client, guild, channel),
    ILoadableCategoryChannelActor
{
    [ProxyInterface(typeof(ILoadableEntity<ICategoryChannel>))]
    internal RestLoadable<ulong, RestCategoryChannel, ICategoryChannel, IChannelModel> Loadable { get; }
        = new(
            client,
            channel,
            Routes.GetChannel(channel.Id),
            EntityUtils.FactoryOfDescendantModel<ulong, IChannelModel, RestCategoryChannel, IGuildCategoryChannelModel>(
                (_, model) => RestCategoryChannel.Construct(client, model, guild)
            ).Invoke
        );
}

public partial class RestCategoryChannelActor(
    DiscordRestClient client,
    GuildIdentity guild,
    CategoryChannelIdentity channel
) :
    RestGuildChannelActor(client, guild, channel),
    ICategoryChannelActor;

public sealed partial class RestCategoryChannel :
    RestGuildChannel,
    ICategoryChannel,
    IContextConstructable<RestCategoryChannel, IGuildCategoryChannelModel, GuildIdentity, DiscordRestClient>
{
    internal override RestCategoryChannelActor Actor { get; }

    internal override IGuildCategoryChannelModel Model => _model;

    private IGuildCategoryChannelModel _model;

    internal RestCategoryChannel(
        DiscordRestClient client,
        GuildIdentity guild,
        IGuildCategoryChannelModel model,
        RestCategoryChannelActor? actor = null
    ) : base(client, guild, model)
    {
        _model = model;
        Actor = actor ?? new RestCategoryChannelActor(client, guild, CategoryChannelIdentity.Of(this));
    }

    public static RestCategoryChannel Construct(
        DiscordRestClient client,
        IGuildCategoryChannelModel model,
        GuildIdentity guild
    ) => new(client, guild, model);

    [CovariantOverride]
    public ValueTask UpdateAsync(IGuildCategoryChannelModel model, CancellationToken token = default)
    {
        _model = model;
        return ValueTask.CompletedTask;
    }

    public override IGuildCategoryChannelModel GetModel() => Model;


}
