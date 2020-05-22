using System;

namespace Shiny.WebApi.Caching
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public TimeSpan? LifeSpan { get; }
        public CacheMode Mode { get; }
        public bool ShouldInvalidateOnError { get; }

        public CacheAttribute(CacheMode mode = CacheMode.GetOrFetch, TimeSpan? lifeSpan = null, bool shouldInvalidateOnError = false)
        {
            this.Mode = mode;
            this.LifeSpan = lifeSpan;
            this.ShouldInvalidateOnError = shouldInvalidateOnError;
        }
    }
}
