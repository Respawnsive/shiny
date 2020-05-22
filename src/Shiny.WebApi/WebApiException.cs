using System;

namespace Shiny.WebApi
{

    public class WebApiException : Exception
    {
        public WebApiException(Exception innerException, object cachedResult) : base(innerException.Message, innerException)
        {
            this.CachedResult = cachedResult;
        }

        public object CachedResult { get; }
    }

    public class WebApiException<TResult> : WebApiException
    {
        public WebApiException(Exception innerException, TResult cachedResult) : base(innerException, cachedResult)
        {
            this.CachedResult = cachedResult;
        }

        public new TResult CachedResult { get; }
    }
}
