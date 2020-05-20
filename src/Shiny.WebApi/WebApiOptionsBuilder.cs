using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpTracer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using Refit;

namespace Shiny.WebApi
{
    public class WebApiOptionsBuilder
    {
        public WebApiOptionsBuilder(WebApiOptions webApiOptions)
        {
            this.WebApiOptions = webApiOptions;
        }

        internal WebApiOptions WebApiOptions { get; }

        public WebApiOptionsBuilder ConfigureHttpClientBuilder(Func<IHttpClientBuilder, IHttpClientBuilder> httpClientBuilder)
        {
            this.WebApiOptions.HttpClientBuilder = httpClientBuilder;

            return this;
        }

        public WebApiOptionsBuilder WithRefitSettings(Func<IServiceProvider, RefitSettings> refitSettingsFactory)
        {
            this.WebApiOptions.RefitSettingsFactory = refitSettingsFactory;

            return this;
        }

        public WebApiOptionsBuilder WithDecompressionMethods(DecompressionMethods decompressionMethods)
        {
            this.WebApiOptions.DecompressionMethods = decompressionMethods;

            return this;
        }

        public WebApiOptionsBuilder WithHttpTracerVerbosity(HttpMessageParts verbosity)
        {
            this.WebApiOptions.HttpTracerVerbosity = verbosity;

            return this;
        }
    }
}
