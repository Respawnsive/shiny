using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using HttpTracer;
using Polly;
using Refit;

namespace Shiny.WebApi
{
    public class WebApiOptions : IWebApiOptions
    {
        public WebApiOptions(Type webApiType, Uri baseAddress)
        {
            this.WebApiType = webApiType;
            this.BaseAddress = baseAddress;
            this.RefitSettings = new RefitSettings();
            this.DelegatingHandlerFactories = new List<Func<IServiceProvider, DelegatingHandler>>();
        }

        public Type WebApiType { get; }
        public Uri BaseAddress { get; }
        public DecompressionMethods DecompressionMethods { get; internal set; } = DecompressionMethods.None;
        public RefitSettings RefitSettings { get; } 
        public IList<Func<IServiceProvider, DelegatingHandler>> DelegatingHandlerFactories { get; }
        public HttpMessageParts HttpTracerVerbosity { get; internal set; } = HttpMessageParts.None;
    }
}