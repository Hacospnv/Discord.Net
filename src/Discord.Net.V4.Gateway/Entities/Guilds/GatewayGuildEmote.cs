using Discord.Gateway.Cache;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Discord.Gateway
{
    public sealed class GatewayGuildEmote : GatewayCacheableEntity<ulong, IGuildEmoteModel>, IEmote
    {
        public GuildRolesCacheable Roles { get; }
        public GuildMemberCacheable Author { get; }

        public IReadOnlyCollection<ulong> RoleIds
            => _source.Roles.ToImmutableArray();

        public string Name
            => _source.Name;

        public bool IsAnimated
            => _source.IsAnimated;

        public bool RequiresColons
            => _source.RequireColons;

        public bool IsManaged
            => _source.IsManaged;

        public bool IsAvailable
            => _source.IsAvailable;

        public DateTimeOffset CreatedAt
            => SnowflakeUtils.FromSnowflake(Id);

        protected override IGuildEmoteModel Model
            => _source;

        private IGuildEmoteModel _source;

        public GatewayGuildEmote(DiscordGatewayClient discord, ulong guildId, IGuildEmoteModel model)
            : base(discord, model.Id)
        {
            Update(model);
            Roles = new(guildId, () => _source.Roles, id => new GuildRoleCacheable(id, discord, discord.State.GuildRoles.ProvideSpecific(id)));
            Author = new(guildId, model.UserId, discord, discord.State.Members.ProvideSpecific(model.UserId, guildId));
        }

        [MemberNotNull(nameof(_source))]
        internal override void Update(IGuildEmoteModel model)
        {
            _source = model;
        }

        internal override object Clone() => throw new NotImplementedException();
        internal override void DisposeClone() => throw new NotImplementedException();
    }
}

