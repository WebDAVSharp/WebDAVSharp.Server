using System;
using System.Runtime.Caching;

namespace WebDAVSharp.Server.Utilities
{
    /// <summary>
    /// </summary>
    internal class WebDavCacheBase
    {
        private readonly MemoryCache _cache;
        private readonly object _padlock = new object();
        private readonly CacheItemPolicy _policy;
        private readonly string name;

        /// <summary>
        /// </summary>
        public WebDavCacheBase()
        {
            _policy = new CacheItemPolicy
            {
                Priority = CacheItemPriority.Default,
                AbsoluteExpiration = DateTimeOffset.Now.AddHours(1.00),
                RemovedCallback = RemovedCallback
            };
            name = GetType().Name + "_Cache";
            _cache = new MemoryCache(name);
        }

        /// <summary>
        /// </summary>
        /// <param name="policy"></param>
        protected WebDavCacheBase(CacheItemPolicy policy)
        {
            _policy = policy;
            name = GetType().Name + "_Cache";
            _cache = new MemoryCache(name);
        }

        /// <summary>
        /// </summary>
        public MemoryCache Cache => _cache;

        private void RemovedCallback(CacheEntryRemovedArguments arguments)
        {
#if DEBUG
            String strLog = String.Concat("Reason: ", arguments.RemovedReason.ToString(), " | Key-Name: ", arguments.CacheItem.Key, " | Value-Object: ", arguments.CacheItem.Value.ToString());

            WebDavServer.Log.Info("Cache: " + name + " - " + strLog);
#endif
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="remove"></param>
        /// <returns></returns>
        public virtual object GetCachedObject(string key, bool remove = false)
        {
            lock (_padlock)
            {
                object res = _cache[key];
                if (res != null && remove)
                    _cache.Remove(key);
                return res;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public virtual void AddCacheObject(string key, object value)
        {
            lock (_padlock)
            {
                _cache.Set(key, value, _policy);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeToExpire"></param>
        public virtual void AddCacheObject(string key, object value, TimeSpan timeToExpire)
        {
            lock (_padlock)
            {
                CacheItemPolicy itempolicy = new CacheItemPolicy
                {
                    Priority = CacheItemPriority.Default,
                    AbsoluteExpiration = DateTimeOffset.Now.Add(timeToExpire),
                    RemovedCallback = RemovedCallback
                };

                _cache.Set(key, value, _policy);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        public virtual void RemoveCacheObject(string key)
        {
            lock (_padlock)
            {
                _cache.Remove(key);
            }
        }
    }
}