using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpTracer;
using Refit;

namespace Shiny.WebApi
{
    public class WebApiOptionsBuilder
    {
        readonly WebApiOptions webApiOptions;

        public WebApiOptionsBuilder(WebApiOptions webApiOptions)
        {
            this.webApiOptions = webApiOptions;
        }

        internal WebApiOptions WebApiOptions => this.webApiOptions;

        public WebApiOptionsBuilder WithDecompressionMethods(DecompressionMethods decompressionMethods)
        {
            this.webApiOptions.DecompressionMethods = decompressionMethods;

            return this;
        }

        /// <summary>
        /// The <see cref="IContentSerializer"/> instance to use.
        /// </summary>
        public WebApiOptionsBuilder WithContentSerializer(IContentSerializer contentSerializer)
        {
            this.webApiOptions.RefitSettings.ContentSerializer = contentSerializer;

            return this;
        }

        /// <summary>
        /// The <see cref="IUrlParameterFormatter"/> instance to use (defaults to <see cref="DefaultUrlParameterFormatter"/>).
        /// </summary>
        public WebApiOptionsBuilder WithUrlParameterFormatter(IUrlParameterFormatter urlParameterFormatter)
        {
            this.webApiOptions.RefitSettings.UrlParameterFormatter = urlParameterFormatter;

            return this;
        }

        /// <summary>
        /// The <see cref="IFormUrlEncodedParameterFormatter"/> instance to use (defaults to <see cref="DefaultFormUrlEncodedParameterFormatter"/>).
        /// </summary>
        public WebApiOptionsBuilder WithFormUrlEncodedParameterFormatter(IFormUrlEncodedParameterFormatter formUrlEncodedParameterFormatter)
        {
            this.webApiOptions.RefitSettings.FormUrlEncodedParameterFormatter = formUrlEncodedParameterFormatter;

            return this;
        }

        /// <summary>
        /// Supply a function to provide the Authorization header. Does not work if you supply an HttpClient instance.
        /// </summary>
        public WebApiOptionsBuilder WithAuthorizationHeaderFactory(Func<HttpRequestMessage, Task<string>> authorizationHeaderFactory)
        {
            this.webApiOptions.RefitSettings.AuthorizationHeaderValueWithParamGetter = authorizationHeaderFactory;

            return this;
        }

        public WebApiOptionsBuilder WithHttpTracerVerbosity(HttpMessageParts verbosity)
        {
            this.webApiOptions.HttpTracerVerbosity = verbosity;

            return this;
        }

        public WebApiOptionsBuilder AddDelegatingHandler<THandler>() where THandler : DelegatingHandler =>
            this.AddDelegatingHandler(typeof(THandler));

        public WebApiOptionsBuilder AddDelegatingHandler(Type delegatingHandlerType)
        {
            if (!typeof(DelegatingHandler).IsAssignableFrom(delegatingHandlerType))
                throw new ArgumentException($"Your handler class must inherit from {nameof(DelegatingHandler)} or derived");

            this.webApiOptions.DelegatingHandlerTypes.Add(delegatingHandlerType);

            return this;
        }
    }
}
