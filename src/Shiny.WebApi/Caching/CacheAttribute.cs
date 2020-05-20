using System;

namespace Shiny.WebApi.Caching
{
    public class CacheAttribute : Attribute
    {
        /// <summary>
        /// Cache Response Cache Time To Live Duration
        /// </summary>
        public TimeSpan? CacheTtl { get; }

        public CacheAttribute()
        {

        }

        public CacheAttribute(int ttlInSeconds) : this(TimeSpan.FromSeconds(ttlInSeconds))
        {

        }

        public CacheAttribute(int ttlHours, int ttlSeconds) : this(TimeSpan.FromHours(ttlHours).Add(TimeSpan.FromSeconds(ttlSeconds)))
        {

        }

        public CacheAttribute(TimeSpan cacheTtl)
        {
            this.CacheTtl = cacheTtl;
        }
    }
}
