using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using HttpTracer;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Shiny.WebApi
{
    public class WebApiModule : ShinyModule
    {
        readonly WebApiOptions webApiOptions;

        public WebApiModule(Type webApiType, string baseUrl, DecompressionMethods decompressionMethods, Action<WebApiOptionsBuilder>? optionsAction = null)
        {
            if(baseUrl.IsEmpty() || !Uri.TryCreate(baseUrl, UriKind.RelativeOrAbsolute, out var baseAddress))
                throw new ArgumentException("baseUrl parameter should be a valid url");

            this.webApiOptions = this.CreateWebApiOptions(webApiType, baseAddress, decompressionMethods, optionsAction);
        }

        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<IWebApiOptions>(this.webApiOptions);

            services.AddRefitClient(this.webApiOptions.WebApiType)
                .ConfigureHttpClient(x => x.BaseAddress = this.webApiOptions.BaseAddress)
                .AddHttpMessageHandler(serviceProvider =>
                {
                    var httpHandlerBuilder = new HttpHandlerBuilder();
                    foreach (var httpMessageHandlerType in this.webApiOptions.HttpMessageHandlerTypes)
                    {
                        httpHandlerBuilder.AddHandler((DelegatingHandler)serviceProvider.GetRequiredService(httpMessageHandlerType));
                    }

                    var tracer = (HttpTracerHandler)httpHandlerBuilder.Build();
                    tracer.Verbosity = this.webApiOptions.HttpTracerVerbosity;

                    return tracer;
                });
        }

        WebApiOptions CreateWebApiOptions(Type webApiType, Uri baseAddress, DecompressionMethods decompressionMethods, Action<WebApiOptionsBuilder>? optionsAction = null)
        {
            var optionsBuilder = new WebApiOptionsBuilder(new WebApiOptions(webApiType, baseAddress, decompressionMethods));

            optionsAction?.Invoke(optionsBuilder);

            return optionsBuilder.WebApiOptions;
        }
    }
}
