using Discord.Gateway.Cache;
using Discord.Gateway.State;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Gateway
{
    public class Cacheable<TId, TGateway, TRest, TCommon> : Cacheable<TId, TGateway>, ILoadableEntity<TCommon, TId>
        where TGateway : class, ICacheableEntity<TId>, TCommon
        where TRest : class, IEntity<TId>, TCommon // TODO: RestEntity<TId>
        where TCommon : class, IEntity<TId>
        where TId : IEquatable<TId>
    {

        internal Cacheable(TId id, DiscordGatewayClient client, IEntityProvider<TId, TGateway> source)
            : base(id, client, source)
        {
        }

        internal Cacheable(TId id, Optional<TId> parent, DiscordGatewayClient client, IEntityProvider<TId, TGateway> source)
            : base(id, parent, client, source)
        {

        }

        internal Cacheable(Func<TId> idFunc, DiscordGatewayClient client, CacheableSourceFactory source)
            : base(idFunc, () => default, client, source)
        {
        }

        internal Cacheable(Func<TId> idFunc, Func<Optional<TId>> parentIdFunc, DiscordGatewayClient client, CacheableSourceFactory source)
            : base(idFunc, parentIdFunc, client, source)
        {
        }

        public ValueTask<TRest?> FetchAsync(RequestOptions? options = null, CancellationToken token = default)
        {
            // TODO: fetch from rest
            return default!;
        }

        public async ValueTask<TCommon?> GetOrFetchAsync(RequestOptions? options = null, CancellationToken token = default)
        {
            TCommon? entity = await GetAsync(token);

            if (entity is not null)
                return entity;

            return await FetchAsync(options, token);
        }

        TCommon? ILoadableEntity<TCommon, TId>.Value => Value;

        ValueTask<TCommon?> ILoadableEntity<TCommon, TId>.LoadAsync(RequestOptions? options, CancellationToken token)
            => GetOrFetchAsync(options, token);
    }

    public class Cacheable<TId, TGateway> : ILoadableEntity<TGateway, TId>
        where TGateway : class, ICacheableEntity<TId>
        where TId : IEquatable<TId>
    {
        internal delegate IEntityProvider<TId, TGateway>? CacheableSourceFactory(TId id, Optional<TId> parent);

        public TId Id
            => _idFunc();

        public TGateway? Value
        {
            get
            {
                var source = _sourceFactory(_idFunc(), _parentIdFunc());

                if (source is null)
                    return null;

                return source.TryGetReferenced(out var value) ? value : null;
            }
        }

        protected readonly DiscordGatewayClient Client;

        private readonly Func<TId> _idFunc;
        private readonly Func<Optional<TId>> _parentIdFunc;
        private readonly CacheableSourceFactory _sourceFactory;

        internal Cacheable(TId id, DiscordGatewayClient client, IEntityProvider<TId, TGateway> source)
            : this(id, default, client, source)
        { }

        internal Cacheable(TId id, Optional<TId> parent, DiscordGatewayClient client, IEntityProvider<TId, TGateway> source)
            : this(() => id, () => parent, client, (_, __) => source)
        { }

        internal Cacheable(Func<TId> idFunc, Func<Optional<TId>> parentFunc, DiscordGatewayClient client, CacheableSourceFactory cacheableSource)
        {
            _idFunc = idFunc;
            _parentIdFunc = parentFunc;
            Client = client;
            _sourceFactory = cacheableSource;
        }

        public virtual ValueTask<IEntityHandle<TId, TGateway>?> GetHandleAsync(CancellationToken token = default)
        {
            return _sourceFactory(_idFunc(), _parentIdFunc())?.GetHandleAsync(token) ?? ValueTask.FromResult<IEntityHandle<TId, TGateway>?>(null);
        }

        public virtual async ValueTask<TGateway?> GetAsync(CancellationToken token = default)
        {
            // lifetime for the entity extends to the callee
            using var handle = await GetHandleAsync(token);

            if (handle is not null)
            {
                return handle.Entity;
            }

            return null;
        }

        ValueTask<TGateway?> ILoadableEntity<TGateway, TId>.LoadAsync(RequestOptions? options, CancellationToken token)
            => GetAsync(token);

        public static implicit operator TGateway?(Cacheable<TId, TGateway> cacheable) => cacheable.Value;
    }
}
