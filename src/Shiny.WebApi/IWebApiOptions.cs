using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HttpTracer;

namespace Shiny.WebApi
{
    public interface IWebApiOptions
    {
        Type WebApiType { get; }
        Uri BaseAddress { get; }
        DecompressionMethods DecompressionMethods { get; }
        IList<Type> HttpMessageHandlerTypes { get; }
        HttpMessageParts HttpTracerVerbosity { get; }
    }
}
