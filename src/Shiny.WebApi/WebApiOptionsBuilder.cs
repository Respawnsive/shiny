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

        public WebApiOptionsBuilder AddDelegatingHandler<THandler>(Func<IServiceProvider, THandler> handlerFactory)
            where THandler : DelegatingHandler
        {
            if(handlerFactory == null)
                throw new ArgumentNullException(nameof(handlerFactory));

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => serviceProvider.GetRequiredService<THandler>());

            return this;
        }

        public WebApiOptionsBuilder AddDelegatingHandler(DelegatingHandler delegatingHandlerType)
        {
            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => delegatingHandlerType);

            return this;
        } 

        #endregion

        #region Refit

        /// <summary>
        /// The <see cref="IContentSerializer"/> instance to use.
        /// </summary>
        public WebApiOptionsBuilder WithContentSerializer(IContentSerializer contentSerializer)
        {
            this.WebApiOptions.RefitSettings.ContentSerializer = contentSerializer;

            return this;
        }

        /// <summary>
        /// The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).
        /// </summary>
        public WebApiOptionsBuilder WithUrlParameterFormatter(IUrlParameterFormatter urlParameterFormatter)
        {
            this.WebApiOptions.RefitSettings.UrlParameterFormatter = urlParameterFormatter;

            return this;
        }

        /// <summary>
        /// The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>).
        /// </summary>
        public WebApiOptionsBuilder WithFormUrlEncodedParameterFormatter(IFormUrlEncodedParameterFormatter formUrlEncodedParameterFormatter)
        {
            this.WebApiOptions.RefitSettings.FormUrlEncodedParameterFormatter = formUrlEncodedParameterFormatter;

            return this;
        }

        /// <summary>
        /// Supply a function to provide the Authorization header. Does not work if you supply an HttpClient instance.
        /// </summary>
        public WebApiOptionsBuilder WithAuthorizationHeaderFactory(Func<HttpRequestMessage, Task<string>> authorizationHeaderFactory)
        {
            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new AuthenticatedParameterizedHttpClientHandler(authorizationHeaderFactory));

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

        #region Polly

        public WebApiOptionsBuilder AddPolicyHandler(IAsyncPolicy<HttpResponseMessage> policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(policy));

            return this;
        }

        public WebApiOptionsBuilder AddPolicyHandler(Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            if(policySelector == null)
                throw new ArgumentNullException(nameof(policySelector));

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(policySelector));

            return this;
        }

        public WebApiOptionsBuilder AddPolicyHandler(Func<IServiceProvider, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            if (policySelector == null)
                throw new ArgumentNullException(nameof(policySelector));

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(request => policySelector(serviceProvider, request)));

            return this;
        }

        public WebApiOptionsBuilder AddPolicyHandlerFromRegistry(string policyKey)
        {
            if (policyKey == null)
                throw new ArgumentNullException(nameof(policyKey));

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider =>
            {
                var registry = serviceProvider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

                var policy = registry.Get<IAsyncPolicy<HttpResponseMessage>>(policyKey);

                return new PolicyHttpMessageHandler(policy);
            });

            return this;
        }

        public WebApiOptionsBuilder AddPolicyHandlerFromRegistry(Func<IReadOnlyPolicyRegistry<string>, HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
        {
            if (policySelector == null)
                throw new ArgumentNullException(nameof(policySelector));

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider =>
            {
                var registry = serviceProvider.GetRequiredService<IReadOnlyPolicyRegistry<string>>();

                return new PolicyHttpMessageHandler(request => policySelector(registry, request));
            });

            return this;
        }

        public WebApiOptionsBuilder AddTransientHttpErrorPolicy(Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy)
        {
            if (configurePolicy == null)
                throw new ArgumentNullException(nameof(configurePolicy));

            var policyBuilder = HttpPolicyExtensions.HandleTransientHttpError();

            // Important - cache policy instances so that they are singletons per handler.
            var policy = configurePolicy(policyBuilder);

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider => new PolicyHttpMessageHandler(policy));

            return this;
        }

        public WebApiOptionsBuilder AddPolicyHandler(Func<IServiceProvider, HttpRequestMessage, string, IAsyncPolicy<HttpResponseMessage>> policyFactory, Func<HttpRequestMessage, string> keySelector)
        {
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (policyFactory == null)
                throw new ArgumentNullException(nameof(policyFactory));

            this.WebApiOptions.DelegatingHandlerFactories.Add(serviceProvider =>
            {
                var registry = serviceProvider.GetRequiredService<IPolicyRegistry<string>>();

                return new PolicyHttpMessageHandler((request) =>
                {
                    var key = keySelector(request);

                    if (registry.TryGet<IAsyncPolicy<HttpResponseMessage>>(key, out var policy))
                    {
                        return policy;
                    }

                    var newPolicy = policyFactory(serviceProvider, request, key);
                    registry[key] = newPolicy;
                    return newPolicy;
                });
            });

            return this;
        }

        #endregion
    }
}
