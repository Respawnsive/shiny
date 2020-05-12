using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using HttpTracer;

namespace Shiny.WebApi
{
    public class WebApiOptionsBuilder
    {
        private readonly WebApiOptions webApiOptions;

        public WebApiOptionsBuilder(WebApiOptions webApiOptions)
        {
            this.webApiOptions = webApiOptions;
        }

        internal WebApiOptions WebApiOptions => this.webApiOptions;

        public WebApiOptionsBuilder AddHttpMessageHandler<THandler>() where THandler : DelegatingHandler =>
            this.AddHttpMessageHandler(typeof(THandler));

        public WebApiOptionsBuilder AddHttpMessageHandler(Type httpMessageHandlerType)
        {
            if(!typeof(DelegatingHandler).IsAssignableFrom(httpMessageHandlerType))
                throw new ArgumentException($"Your handler class must inherit from {nameof(DelegatingHandler)} or derived");

            this.webApiOptions.HttpMessageHandlerTypes.Add(httpMessageHandlerType);
            return this;
        }

        public WebApiOptionsBuilder SetHttpTracerVerbosity(HttpMessageParts verbosity)
        {
            this.webApiOptions.HttpTracerVerbosity = verbosity;

            return this;
        }
    }
}
