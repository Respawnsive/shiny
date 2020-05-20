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
        public WebApiOptions(Type webApiType, Uri? baseAddress, DecompressionMethods? decompressionMethods, HttpMessageParts? httpTracerVerbosity)
        {
            this.WebApiType = webApiType;
            this.BaseAddress = baseAddress;
            this.DecompressionMethods = decompressionMethods ?? DecompressionMethods.None;
            this.HttpTracerVerbosity = httpTracerVerbosity ?? HttpMessageParts.None;
            this.RefitSettingsFactory = provider => new RefitSettings();
        }

        public Type WebApiType { get; }
        public Uri? BaseAddress { get; }
        public DecompressionMethods DecompressionMethods { get; }
        public HttpMessageParts HttpTracerVerbosity { get; }
        public Func<IServiceProvider, RefitSettings> RefitSettingsFactory { get; internal set; }
        public Func<IHttpClientBuilder, IHttpClientBuilder>? HttpClientBuilder { get; internal set; }
    }
}