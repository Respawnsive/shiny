using System;
using System.Net;

namespace Shiny.WebApi
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class WebApiAttribute : Attribute
    {
        public WebApiAttribute(string baseUri)
        {
            this.BaseUri = baseUri;
        }
        public WebApiAttribute(string baseUri, DecompressionMethods decompressionMethods)
        {
            this.BaseUri = baseUri;
            this.DecompressionMethods = decompressionMethods;
        }

        public string BaseUri { get; }
        public DecompressionMethods? DecompressionMethods { get; }
    }
}
