﻿using System;
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

        public WebApiModule(Type webApiType, string baseUrl, Action<WebApiOptionsBuilder>? optionsAction = null)
        {
            if(baseUrl.IsEmpty() || !Uri.TryCreate(baseUrl, UriKind.RelativeOrAbsolute, out var baseAddress))
                throw new ArgumentException("baseUrl parameter should be a valid url");

            this.webApiOptions = this.CreateWebApiOptions(webApiType, baseAddress, optionsAction);
        }

        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<IWebApiOptions>(this.webApiOptions);

            services.AddHttpClient(ForType(this.webApiOptions.WebApiType))
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                {
                    var httpHandlerBuilder = new HttpHandlerBuilder();
                    foreach (var httpMessageHandlerType in this.webApiOptions.HttpHandlerTypes)
                    {
                        httpHandlerBuilder.AddHandler((DelegatingHandler)serviceProvider.GetRequiredService(httpMessageHandlerType));
                    }

                    var httpTracerHandler = (HttpTracerHandler)httpHandlerBuilder.Build();
                    httpTracerHandler.Verbosity = this.webApiOptions.HttpTracerVerbosity;
                    ((HttpClientHandler)httpTracerHandler.InnerHandler).AutomaticDecompression = this.webApiOptions.DecompressionMethods;

                    return httpTracerHandler;
                })
                .AddTypedClient(this.webApiOptions.WebApiType, (client, serviceProvider) => RestService.For(this.webApiOptions.WebApiType, client, this.webApiOptions.RefitSettings))
                .ConfigureHttpClient(x => x.BaseAddress = this.webApiOptions.BaseAddress);
        }

        /// <summary>
        /// Refit private method
        /// </summary>
        static string ForType(Type refitInterfaceType)
        {
            string typeName;

            if (refitInterfaceType.IsNested)
            {
                var className = "AutoGenerated" + refitInterfaceType.DeclaringType.Name + refitInterfaceType.Name;
                typeName = refitInterfaceType.AssemblyQualifiedName.Replace(refitInterfaceType.DeclaringType.FullName + "+" + refitInterfaceType.Name, refitInterfaceType.Namespace + "." + className);
            }
            else
            {
                var className = "AutoGenerated" + refitInterfaceType.Name;

                if (refitInterfaceType.Namespace == null)
                {
                    className = $"{className}.{className}";
                }

                typeName = refitInterfaceType.AssemblyQualifiedName.Replace(refitInterfaceType.Name, className);
            }

            return typeName;
        }

        WebApiOptions CreateWebApiOptions(Type webApiType, Uri baseAddress, Action<WebApiOptionsBuilder>? optionsAction = null)
        {
            var optionsBuilder = new WebApiOptionsBuilder(new WebApiOptions(webApiType, baseAddress));

            optionsAction?.Invoke(optionsBuilder);

            return optionsBuilder.WebApiOptions;
        }
    }
}