using System;

namespace Shiny.WebApi.Caching
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public TimeSpan? LifeSpan { get; } = null;
        public CacheMode Mode { get; } = CacheMode.GetOrFetch;
        public bool ShouldInvalidateOnError { get; } = false;

        public CacheAttribute()
        {
        }

        public CacheAttribute(CacheMode mode)
        {
            this.Mode = mode;
        }

        public CacheAttribute(TimeSpan lifeSpan)
        {
            this.LifeSpan = lifeSpan;
        }

        public CacheAttribute(bool shouldInvalidateOnError)
        {
            this.ShouldInvalidateOnError = shouldInvalidateOnError;
        }

        public CacheAttribute(CacheMode mode, TimeSpan lifeSpan)
        {
            this.Mode = mode;
            this.LifeSpan = lifeSpan;
        }

        public CacheAttribute(CacheMode mode, bool shouldInvalidateOnError)
        {
            this.Mode = mode;
            this.ShouldInvalidateOnError = shouldInvalidateOnError;
        }

        public CacheAttribute(TimeSpan lifeSpan, bool shouldInvalidateOnError)
        {
            this.LifeSpan = lifeSpan;
            this.ShouldInvalidateOnError = shouldInvalidateOnError;
        }

        public CacheAttribute(CacheMode mode, TimeSpan lifeSpan, bool shouldInvalidateOnError)
        {
            this.Mode = mode;
            this.LifeSpan = lifeSpan;
            this.ShouldInvalidateOnError = shouldInvalidateOnError;
        }
    }
}
