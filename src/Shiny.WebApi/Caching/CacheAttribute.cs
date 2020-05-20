using System;

namespace Shiny.WebApi.Caching
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public TimeSpan? LifeSpan { get; }

        public CacheAttribute()
        {

        }

        public CacheAttribute(int lifeInSeconds) : this(TimeSpan.FromSeconds(lifeInSeconds))
        {

        }

        public CacheAttribute(int lifeInHours, int lifeInSeconds) : this(TimeSpan.FromHours(lifeInHours).Add(TimeSpan.FromSeconds(lifeInSeconds)))
        {

        }

        public CacheAttribute(TimeSpan lifeSpan)
        {
            this.LifeSpan = lifeSpan;
        }
    }
}
