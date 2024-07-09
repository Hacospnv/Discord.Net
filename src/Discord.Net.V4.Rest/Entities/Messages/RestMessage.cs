using Discord.Models;
using Discord.Models.Json;
using Discord.Rest.Channels;
using Discord.Rest.Guilds;
using Discord.Rest.Stickers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Discord.Rest.Messages;

public partial class RestLoadableMessageActor(
    DiscordRestClient client,
    MessageChannelIdentity channel,
    MessageIdentity message,
    GuildIdentity? guild = null
) :
    RestMessageActor(client, channel, message, guild),
    ILoadableMessageActor
{
    [ProxyInterface(typeof(ILoadableEntity<IMessage>))]
    internal RestLoadable<ulong, RestMessage, IMessage, IMessageModel> Loadable { get; } =
        RestLoadable<ulong, RestMessage, IMessage, IMessageModel>
            .FromContextConstructable<RestMessage, GuildIdentity?>(
                client,
                message,
                Routes.GetChannelMessage(channel.Id, message.Id),
                guild
            );
}

public partial class RestMessageActor(
    DiscordRestClient client,
    MessageChannelIdentity channel,
    MessageIdentity message,
    GuildIdentity? guild = null
) :
    RestActor<ulong, RestMessage, MessageIdentity>(client, message),
    IMessageActor
{
    public RestLoadableMessageChannelActor Channel { get; } = new(client, channel, guild);

    ILoadableMessageChannelActor IMessageChannelRelationship.Channel => Channel;

    public IMessage CreateEntity(IMessageModel model)
        => RestMessage.Construct(Client, model, guild);
}

public partial class RestMessage :
    RestEntity<ulong>,
    IMessage,
    IConstructable<RestMessage, IMessageModel, DiscordRestClient>,
    IContextConstructable<RestMessage, IMessageModel, GuildIdentity?, DiscordRestClient>
{
    internal RestMessage(
        DiscordRestClient client,
        IMessageModel model,
        RestMessageActor? actor = null,
        GuildIdentity? guild = null,
        MessageChannelIdentity? channel = null
    ) : base(client, model.Id)
    {
        Model = model;

        Actor = actor ?? new(
            client,
            channel ?? MessageChannelIdentity.Of(model.ChannelId),
            MessageIdentity.Of(this),
            guild
        );
        Author = new(
            client,
            UserIdentity.OfNullable(
                model.GetReferencedEntityModel<ulong, IUserModel>(model.AuthorId),
                model => RestUser.Construct(client, model)
            ) ?? UserIdentity.Of(model.AuthorId)
        );
        Thread = model is {ThreadId: not null, ThreadGuildId: not null}
            ? CreateThreadLoadable(client, model, guild)
            : null;
        Attachments = model.Attachments.Select(x => Attachment.Construct(client, x)).ToImmutableArray();
        Embeds = model.Embeds.Select(x => Embed.Construct(client, x)).ToImmutableArray();
        Activity = model.Activity is not null
            ? MessageActivity.Construct(client, model.Activity)
            : null;
        Application = model.Application is not null
            ? MessageApplication.Construct(client, model.Application)
            : null;
        Reference = model.MessageReference is not null
            ? MessageReference.Construct(client, model.MessageReference)
            : null;
        Reactions = MessageReactions.Construct(client, model.Reactions);
        Components = model.Components.Select(x => IMessageComponent.Construct(client, x)).ToImmutableArray();
        Stickers = model.Stickers.Select(x => RestStickerItem.Construct(client, x)).ToImmutableArray();
        InteractionMetadata = model.InteractionMetadata is not null
            ? RestMessageInteractionMetadata.Construct(client, model.InteractionMetadata, new RestMessageInteractionMetadata.Context(
                channel ?? MessageChannelIdentity.Of(model.ChannelId),
                guild
            ))
            : null;
        RoleSubscriptionData = model.RoleSubscriptionData is not null
            ? MessageRoleSubscriptionData.Construct(client, model.RoleSubscriptionData)
            : null;
    }

    internal IMessageModel Model { get; set; }

    [ProxyInterface(
        typeof(IMessageActor),
        typeof(IMessageChannelRelationship),
        typeof(IEntityProvider<IMessage, IMessageModel>)
    )]
    internal virtual RestMessageActor Actor { get; }

    public static RestMessage Construct(DiscordRestClient client, IMessageModel model)
    {
        return model.IsWebhook
            ? RestWebhookMessage.Construct(client, model)
            : new RestMessage(client, model);
    }

    public static RestMessage Construct(DiscordRestClient client, IMessageModel model, GuildIdentity? guild)
    {
        return model.IsWebhook
            ? RestWebhookMessage.Construct(client, model, guild)
            : new RestMessage(client, model, guild: guild);
    }

    private static RestLoadableThreadChannelActor? CreateThreadLoadable(
        DiscordRestClient client,
        IMessageModel model,
        GuildIdentity? guild)
    {
        var threadIdentity = CreateThreadIdentity(
            client,
            model,
            guild,
            out var guildIdentity,
            out var threadChannelModel
        );

        if (threadIdentity is null)
            return null;

        return new(
            client,
            guildIdentity!,
            threadIdentity,
            RestThreadChannel.GetCurrentThreadMemberIdentity(client, guildIdentity!, threadIdentity, threadChannelModel)
        );
    }

    private static ThreadIdentity? CreateThreadIdentity(
        DiscordRestClient client,
        IMessageModel model,
        GuildIdentity? guild,
        out GuildIdentity? guildIdentity,
        out IThreadChannelModel? threadModel)
    {
        if (model.ThreadId is null || model.ThreadGuildId is null)
        {
            guildIdentity = null;
            threadModel = null;
            return null;
        }

        var guildIdentityLocal = guildIdentity = guild ?? GuildIdentity.Of(model.ThreadGuildId.Value);

        threadModel = model.GetReferencedEntityModel<ulong, IThreadChannelModel>(model.ThreadId.Value);


        return ThreadIdentity.OfNullable(
            threadModel,
            model => RestThreadChannel.Construct(client, model, new RestThreadChannel.Context(
                guildIdentityLocal
            ))
        ) ?? ThreadIdentity.Of(model.ThreadId.Value);
    }

    public async ValueTask UpdateAsync(IMessageModel model, CancellationToken token = default)
    {
        if (Thread?.Id != Model.ThreadId)
        {
            if (model is {ThreadId: not null, ThreadGuildId: not null})
            {
                if (Thread is null)
                {
                    Thread = CreateThreadLoadable(Client, model, Actor.Channel.GuildIdentity);
                }
                else
                {
                    Thread.Loadable.Identity =
                        CreateThreadIdentity(Client, model, Actor.Channel.GuildIdentity, out _, out _)!;
                }
            }
            else
                Thread = null;
        }

        if (!Model.Attachments.SequenceEqual(model.Attachments))
            Attachments = model.Attachments.Select(x => Attachment.Construct(Client, x)).ToImmutableArray();

        if (!Model.Embeds.SequenceEqual(model.Embeds))
            Embeds = model.Embeds.Select(x => Embed.Construct(Client, x)).ToImmutableArray();

        if (!Model.Activity?.Equals(model.Activity) ?? model.Activity is not null)
            Activity = model.Activity is not null
                ? MessageActivity.Construct(Client, model.Activity)
                : null;

        if (!Model.Reactions.SequenceEqual(model.Reactions))
            Reactions = MessageReactions.Construct(Client, model.Reactions);

        if (!Model.Components.SequenceEqual(model.Components))
            Components = model.Components.Select(x => IMessageComponent.Construct(Client, x)).ToImmutableArray();

        if (!Model.Stickers.SequenceEqual(model.Stickers))
            Stickers = model.Stickers.Select(x => RestStickerItem.Construct(Client, x)).ToImmutableArray();

        if (!Model.Application?.Equals(model.Application) ?? model.Application is not null)
            Application = model.Application is not null
                ? MessageApplication.Construct(Client, model.Application)
                : null;

        if (!Model.MessageReference?.Equals(model.MessageReference) ?? model.MessageReference is not null)
            Reference = model.MessageReference is not null
                ? MessageReference.Construct(Client, model.MessageReference)
                : null;

        if (!Model.InteractionMetadata?.Equals(model.InteractionMetadata) ?? model.InteractionMetadata is not null)
        {
            if (model.InteractionMetadata is not null)
            {
                InteractionMetadata ??= RestMessageInteractionMetadata.Construct(
                    Client,
                    model.InteractionMetadata,
                    new RestMessageInteractionMetadata.Context(
                        Actor.Channel.Loadable.Identity,
                        Actor.Channel.GuildIdentity
                    )
                );

                await InteractionMetadata.UpdateAsync(model.InteractionMetadata, token);
            }
            else
            {
                InteractionMetadata = null;
            }
        }

        if (!Model.RoleSubscriptionData?.Equals(model.RoleSubscriptionData) ?? model.RoleSubscriptionData is not null)
            RoleSubscriptionData = model.RoleSubscriptionData is not null
                ? MessageRoleSubscriptionData.Construct(Client, model.RoleSubscriptionData)
                : null;
    }

    public IMessageModel GetModel() => Model;


    #region Loadables

    public RestLoadableUserActor Author { get; }

    public RestLoadableThreadChannelActor? Thread { get; private set; }

    //public ILoadableThreadActor? Thread => throw new NotImplementedException();

    public IDefinedLoadableEntityEnumerable<ulong, IChannel> MentionedChannels => throw new NotImplementedException();

    public IDefinedLoadableEntityEnumerable<ulong, IRole> MentionedRoles => throw new NotImplementedException();

    public IDefinedLoadableEntityEnumerable<ulong, IUser> MentionedUsers => throw new NotImplementedException();
    public ILoadableWebhookActor Webhook => throw new NotImplementedException();

    #endregion

    #region Properties

    public MessageType Type => (MessageType)Model.Type;

    public MessageFlags Flags => (MessageFlags)Model.Flags;

    public bool IsTTS => Model.IsTTS;

    public bool IsPinned => Model.IsPinned;

    public bool MentionedEveryone => Model.MentionsEveryone;

    public string? Content => Model.Content;

    public DateTimeOffset Timestamp => Model.Timestamp;

    public DateTimeOffset? EditedTimestamp => Model.EditedTimestamp;

    public IReadOnlyCollection<Attachment> Attachments { get; private set; }

    public IReadOnlyCollection<Embed> Embeds { get; private set; }

    public MessageActivity? Activity { get; private set; }

    public MessageApplication? Application { get; private set; }

    public MessageReference? Reference { get; private set; }

    public MessageReactions Reactions { get; private set; }

    public IReadOnlyCollection<IMessageComponent> Components { get; private set; }

    public IReadOnlyCollection<RestStickerItem> Stickers { get; private set; }

    public RestMessageInteractionMetadata? InteractionMetadata { get; private set; }

    public MessageRoleSubscriptionData? RoleSubscriptionData { get; private set; }

    #endregion

    IReadOnlyCollection<IStickerItem> IMessage.Stickers => Stickers;
    ILoadableUserActor IMessage.Author => Author;
    ILoadableThreadChannelActor? IMessage.Thread => Thread;
    IMessageInteractionMetadata? IMessage.InteractionMetadata => InteractionMetadata;
}
