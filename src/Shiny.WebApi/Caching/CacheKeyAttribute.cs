using System;

namespace Shiny.WebApi.Caching
{
    public class CacheKeyAttribute : Attribute
    {
        public CacheKeyAttribute()
        {

        }

        public string? PropertyName { get; }

        /// <summary>
        /// If you use non primitive type (like your ModelClass object) as Cache Primary key you should provide 
        /// property name of primitive primary Id, otherwise ToString() method will be used.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        public CacheKeyAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }
    }
}
