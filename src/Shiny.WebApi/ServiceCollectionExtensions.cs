using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Shiny.WebApi;

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseWebApi<TWebApi>(this IServiceCollection services, string baseUrl, DecompressionMethods decompressionMethods, Action<WebApiOptionsBuilder>? optionsAction = null)
            => services.UseWebApi(typeof(TWebApi), baseUrl, decompressionMethods, optionsAction);

        public static bool UseWebApi(this IServiceCollection services, Type webApiType, string baseUrl, DecompressionMethods decompressionMethods, Action<WebApiOptionsBuilder>? optionsAction = null)
        {
            services.RegisterModule(new WebApiModule(webApiType, baseUrl, decompressionMethods, optionsAction));

            return true;
        }
    }
}
