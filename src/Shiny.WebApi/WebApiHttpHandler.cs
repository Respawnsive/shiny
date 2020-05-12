using System.Net.Http;

namespace Shiny.WebApi
{
    public class WebApiHttpHandler : DelegatingHandler
    {
        public WebApiHttpHandler(IWebApiOptions webApiOptions)
        {
            this.InnerHandler = new HttpClientHandler
            {
                AutomaticDecompression = webApiOptions.DecompressionMethods
            };
        }
    }
}
