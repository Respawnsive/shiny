using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HttpTracer;
using Refit;

namespace Shiny.WebApi
{
    public interface IWebApiOptions
    {
        Type WebApiType { get; }
        Uri BaseAddress { get; }
        DecompressionMethods DecompressionMethods { get; }
        RefitSettings RefitSettings { get; }
        IList<Type> DelegatingHandlerTypes { get; }
        HttpMessageParts HttpTracerVerbosity { get; }
    }
}
