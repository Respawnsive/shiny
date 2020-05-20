using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using HttpTracer;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Refit;

namespace Shiny.WebApi
{
    public interface IWebApiOptions
    {
        Type WebApiType { get; }
        Uri? BaseAddress { get; }
        DecompressionMethods DecompressionMethods { get; }
        HttpMessageParts HttpTracerVerbosity { get; }
        string[] PolicyRegistryKeys { get; }
        Func<IServiceProvider, RefitSettings> RefitSettingsFactory { get; }
        Func<IHttpClientBuilder, IHttpClientBuilder>? HttpClientBuilder { get; }
    }
}
