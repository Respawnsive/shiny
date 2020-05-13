using System;
using System.Collections.Generic;
using System.Net;
using HttpTracer;
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
            this.HttpHandlerTypes = new List<Type>();//{typeof(WebApiHttpHandler)});
        }

        #region Refit

        public Type WebApiType { get; }
        public Uri BaseAddress { get; }
        public DecompressionMethods DecompressionMethods { get; internal set; } = DecompressionMethods.None;
        public RefitSettings RefitSettings { get; } 

        #endregion

        public IList<Type> HttpHandlerTypes { get; }
        public HttpMessageParts HttpTracerVerbosity { get; internal set; } = HttpMessageParts.None;
    }
}