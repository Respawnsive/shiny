using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Shiny.WebApi;

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseWebApi<TWebApi>(this IServiceCollection services, string baseUrl, Action<WebApiOptionsBuilder>? optionsAction = null)
            => services.UseWebApi(typeof(TWebApi), baseUrl, optionsAction);

        public static bool UseWebApi(this IServiceCollection services, Type webApiType, string baseUrl, Action<WebApiOptionsBuilder>? optionsAction = null)
        {
            services.RegisterModule(new WebApiModule(webApiType, baseUrl, optionsAction));

            return true;
        }
    }
}
