using System;
using System.Net.Http;

namespace Riz.XFramework
{
#if !net40

    /// <summary>
    /// An exception that allows for a given <see cref="T:System.Net.Http.HttpResponseMessage" /> to be returned to the client.
    /// </summary>
    public class XFrameworkHttpResponseException : Exception
    {
        /// <summary>
        /// Gets the HTTP response to return to the client.
        /// </summary>
        /// <returns>The <see cref="T:System.Net.Http.HttpResponseMessage" /> that represents the HTTP response.</returns>
        public HttpResponseMessage Response
        {
            get;
            private set;
        }

        /// <summary>I
        /// nitializes a new instance of the <see cref="T:System.Web.Http.HttpResponseException" /> class.
        /// </summary>
        /// <param name="response">The HTTP response to return to the client.</param>
        public XFrameworkHttpResponseException(HttpResponseMessage response)
            : base("Processing the HTTP request resulted in an exception. See the HTTP response returned by the \"Response\" property of this exception for details")
        {
            Response = response;
        }
    }

#endif
}
