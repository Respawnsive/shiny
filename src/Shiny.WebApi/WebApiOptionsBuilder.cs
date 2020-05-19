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

        #region General

        public WebApiOptionsBuilder WithDecompressionMethods(DecompressionMethods decompressionMethods)
        {
            this.WebApiOptions.DecompressionMethods = decompressionMethods;

            return this;
        }

        #endregion

        #region Refit

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

        #endregion

        #region HttpTracer

        public WebApiOptionsBuilder WithHttpTracerVerbosity(HttpMessageParts verbosity)
        {
            this.WebApiOptions.HttpTracerVerbosity = verbosity;

            return this;
        }

        #endregion

        //#region Polly

        //public WebApiOptionsBuilder AddPolicyHandler(IAsyncPolicy<HttpResponseMessage> policy)
        //{
        //    if (policy == null)
        //        throw new ArgumentNullException(nameof(policy));

        //    this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(policy));

        //    return this;
        //}

        //public WebApiOptionsBuilder AddPolicyHandler(Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        //{
        //    if(policySelector == null)
        //        throw new ArgumentNullException(nameof(policySelector));

        //    this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(policySelector));

        //    return this;
        //}

        //public WebApiOptionsBuilder AddPolicyHandler(Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        //{
        //    if (policySelector == null)
        //        throw new ArgumentNullException(nameof(policySelector));

        //    this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(request => policySelector(serviceProvider, request)));

        //    return this;
        //}

        //public WebApiOptionsBuilder AddPolicyHandlerFromRegistry(string policyKey)
        //{
        //    if (policyKey == null)
        //        throw new ArgumentNullException(nameof(policyKey));

        //    this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider =>
        //    {
        //        var registry = serviceProvider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

        //        var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>(policyKey);

        //        return new PolicyHttpMessageHandler(policy);
        //    });

        //    return this;
        //}

        //public WebApiOptionsBuilder AddPolicyHandlerFromRegistry(Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        //{
        //    if (policySelector == null)
        //        throw new ArgumentNullException(nameof(policySelector));

        //    this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider =>
        //    {
        //        var registry = serviceProvider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

        //        return new PolicyHttpMessageHandler(request => policySelector(registry, request));
        //    });

        //    return this;
        //}

        //public WebApiOptionsBuilder AddTransientHttpErrorPolicy(Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy)
        //{
        //    if (configurePolicy == null)
        //        throw new ArgumentNullException(nameof(configurePolicy));

        //    var policyBuilder = HttpPolicyExtensions.HandleTransientHttpError();

        //    // Important - cache policy instances so that they are singletons per handler.
        //    var policy = configurePolicy(policyBuilder);

        //    this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(policy));

        //    return this;
        //}

        //public WebApiOptionsBuilder AddPolicyHandler(Func<IServiceProvider, HttpRequestMessage, string, IAsyncPolicy<HttpResponseMessage>> policyFactory, Func<HttpRequestMessage, string> keySelector)
        //{
        //    if (keySelector == null)
        //        throw new ArgumentNullException(nameof(keySelector));

        //    if (policyFactory == null)
        //        throw new ArgumentNullException(nameof(policyFactory));

        //    this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider =>
        //    {
        //        var registry = serviceProvider.GetRequiredService<IPolicyRegistry<string>>();

        //        return new PolicyHttpMessageHandler((request) =>
        //        {
        //            var key = keySelector(request);

        //            if (registry.TryGet<IAsyncPolicy<HttpResponseMessage>>(key, out var policy))
        //            {
        //                return policy;
        //            }

        //            var newPolicy = policyFactory(serviceProvider, request, key);
        //            registry[key] = newPolicy;
        //            return newPolicy;
        //        });
        //    });

        //    return this;
        //}

        //#endregion
    }
}
