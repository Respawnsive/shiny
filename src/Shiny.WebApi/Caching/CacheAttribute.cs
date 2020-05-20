using System;

namespace Shiny.WebApi.Caching
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public TimeSpan? LifeSpan { get; }

        public CacheAttribute()
        {

        }

        public CacheAttribute(TimeSpan lifeSpan)
        {
            this.LifeSpan = lifeSpan;
        }
    }
}
