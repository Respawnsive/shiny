using System;
using System.Reflection;

namespace Shiny.WebApi.Caching
{
    class MethodCacheDetails
    {
        public MethodCacheDetails(Type apiInterfaceType, MethodInfo methodInfo)
        {
            this.ApiInterfaceType = apiInterfaceType;
            this.MethodInfo = methodInfo;
        }

        public Type ApiInterfaceType { get; }

        public MethodInfo MethodInfo { get; }

        public CacheAttribute? CacheAttribute { get; internal set; }

        public override int GetHashCode() => this.ApiInterfaceType.GetHashCode() * 23 * this.MethodInfo.GetHashCode() * 23 * 29;

        public override bool Equals(object obj)
        {
            var other = obj as MethodCacheDetails;
            return other != null &&
                   other.ApiInterfaceType.Equals(this.ApiInterfaceType) &&
                   other.MethodInfo.Equals(this.MethodInfo);
        }
    }
}
