using System;
using System.Collections.Generic;
using System.Net;
using HttpTracer;

namespace Shiny.WebApi
{
    public class WebApiOptions : IWebApiOptions
    {
        public WebApiOptions(Type webApiType, Uri baseAddress, DecompressionMethods decompressionMethods)
        {
            this.WebApiType = webApiType;
            this.BaseAddress = baseAddress;
            this.DecompressionMethods = decompressionMethods;
            this.HttpMessageHandlerTypes = new List<Type>{ typeof(WebApiHttpHandler) };
        }

        public Type WebApiType { get; }
        public Uri BaseAddress { get; }
        public DecompressionMethods DecompressionMethods { get; }
        public IList<Type> HttpMessageHandlerTypes { get; }
        public HttpMessageParts HttpTracerVerbosity { get; internal set; } = HttpMessageParts.None;
    }
}