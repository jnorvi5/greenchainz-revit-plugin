using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GreenChainz.Revit.Tests
{
    /// <summary>
    /// Mock HTTP message handler for testing HTTP requests without making actual network calls.
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode;
        private string _responseContent;
        private Exception _exception;

        /// <summary>
        /// Gets the last request that was sent.
        /// </summary>
        public HttpRequestMessage LastRequest { get; private set; }

        /// <summary>
        /// Gets the number of requests sent.
        /// </summary>
        public int RequestCount { get; private set; }

        /// <summary>
        /// Configures the mock to return a successful response with the specified content.
        /// </summary>
        /// <param name="statusCode">The HTTP status code to return.</param>
        /// <param name="content">The response content to return.</param>
        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _responseContent = content;
            _exception = null;
        }

        /// <summary>
        /// Configures the mock to throw an exception when a request is sent.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupException(Exception exception)
        {
            _exception = exception;
        }

        /// <summary>
        /// Sends an HTTP request (mocked).
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            RequestCount++;

            if (_exception != null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent ?? string.Empty, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
