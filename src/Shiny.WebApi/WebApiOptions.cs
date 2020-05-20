using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using HttpTracer;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Refit;

namespace Shiny.WebApi
{
    public class WebApiOptions : IWebApiOptions
    {
        public WebApiOptions(Type webApiType, Uri? baseAddress)
        {
            this.WebApiType = webApiType;
            this.BaseAddress = baseAddress;
            this.RefitSettingsFactory = provider => new RefitSettings();
        }

        public Type WebApiType { get; }
        public Uri? BaseAddress { get; }
        public DecompressionMethods DecompressionMethods { get; internal set; } = DecompressionMethods.None;
        public Func<IServiceProvider, RefitSettings> RefitSettingsFactory { get; internal set; }
        public Func<IHttpClientBuilder, IHttpClientBuilder>? HttpClientBuilder { get; internal set; }
        public HttpMessageParts HttpTracerVerbosity { get; internal set; } = HttpMessageParts.None;
    }
}