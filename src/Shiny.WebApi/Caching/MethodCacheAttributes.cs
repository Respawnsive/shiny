using System;

namespace Shiny.WebApi.Caching
{
    public class MethodCacheAttributes
    {
        public MethodCacheAttributes(CacheAttribute cacheAttribute, CacheKeyAttribute primaryKeyAttribute, string paramName,
            Type paramType, int paramOrder)
        {
            this.CacheAttribute = cacheAttribute;
            this.CachePrimaryKeyAttribute = primaryKeyAttribute;
            this.ParameterName = paramName;
            this.ParameterType = paramType;
            this.ParameterOrder = paramOrder;
        }

        public int ParameterOrder { get; }

        public CacheAttribute CacheAttribute { get; }

        public CacheKeyAttribute CachePrimaryKeyAttribute { get; }

        public string ParameterName { get; }

        public Type ParameterType { get; }
    }
}
