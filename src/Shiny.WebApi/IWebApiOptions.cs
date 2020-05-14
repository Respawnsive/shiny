using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using HttpTracer;
using Polly;
using Refit;

namespace Shiny.WebApi
{
    public interface IWebApiOptions
    {
        Type WebApiType { get; }
        Uri BaseAddress { get; }
        DecompressionMethods DecompressionMethods { get; }
        RefitSettings RefitSettings { get; }
        IList<Func<IServiceProvider, DelegatingHandler>> DelegatingHandlerFactories { get; }
        HttpMessageParts HttpTracerVerbosity { get; }
    }
}
