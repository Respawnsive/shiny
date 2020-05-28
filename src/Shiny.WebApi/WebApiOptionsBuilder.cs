using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Shiny.WebApi.Authenticating;

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

        public WebApiOptionsBuilder WithAuthenticationHandler<TAuthenticationHandler>(Func<IServiceProvider, TAuthenticationHandler> authenticationHandler) where TAuthenticationHandler : AuthenticationHandlerBase
        {
            this.WebApiOptions.AuthenticationHandlerFactory = authenticationHandler;

            return this;
        }

        public WebApiOptionsBuilder WithAuthenticationHandler(Func<HttpRequestMessage, Task<string?>> refreshToken)
        {
            this.WebApiOptions.AuthenticationHandlerFactory = provider =>
                new AuthenticationHandler(refreshToken);

            return this;
        }

        public WebApiOptionsBuilder WithAuthenticationHandler<TSettingsService>(Expression<Func<TSettingsService, string?>> tokenProperty, Func<HttpRequestMessage, Task<string?>> refreshToken)
        {
            this.WebApiOptions.AuthenticationHandlerFactory = provider =>
                new AuthenticationHandler<TSettingsService>(
                    provider.GetRequiredService<TSettingsService>(), tokenProperty, refreshToken);

            return this;
        }

        public WebApiOptionsBuilder WithAuthenticationHandler<TSettingsService, TTokenService>(Expression<Func<TSettingsService, string?>> tokenProperty, Expression<Func<TTokenService, HttpRequestMessage, Task<string?>>> refreshTokenMethod)
        {
            this.WebApiOptions.AuthenticationHandlerFactory = provider =>
                new AuthenticationHandler<TSettingsService, TTokenService>(
                    provider.GetRequiredService<TSettingsService>(), tokenProperty,
                    provider.GetRequiredService<TTokenService>(), refreshTokenMethod);

            return this;
        }
    }
}
