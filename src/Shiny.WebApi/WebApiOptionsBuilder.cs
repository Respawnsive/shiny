using System;
using Microsoft.Extensions.DependencyInjection;
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
    }
}
