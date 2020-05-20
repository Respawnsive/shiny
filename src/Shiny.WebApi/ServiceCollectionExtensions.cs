using System;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Shiny.WebApi;

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseWebApi<TWebApi>(this IServiceCollection services, Action<WebApiOptionsBuilder>? builder = null)
            => services.UseWebApi(typeof(TWebApi), builder);

        public static bool UseWebApi(this IServiceCollection services, Type webApiType, Action<WebApiOptionsBuilder>? builder = null)
        {
            services.RegisterModule(new WebApiModule(webApiType, builder));

            return true;
        }
    }
}
