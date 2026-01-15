using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;
using Newtonsoft.Json;
using NUnit.Framework;

namespace GreenChainz.Revit.Tests
{
    [TestFixture]
    public class ApiClientJsonTests
    {
        private const string TestBaseUrl = "https://test.greenchainz.com/api";
        private const string TestToken = "test-jwt-token-123";

        private MockHttpMessageHandler _mockHandler;
        private HttpClient _httpClient;
        private MockLogger _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockHandler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHandler);
            _mockLogger = new MockLogger();
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        [Test]
        public void SendRequestAsync_InvalidJson_ThrowsApiException_AndLogsError()
        {
            // Arrange
            // Return invalid JSON to trigger JsonException during deserialization
            _mockHandler.SetupResponse(HttpStatusCode.OK, "Invalid JSON");

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient, _mockLogger))
            {
                var request = new AuditResult { ProjectName = "Test" };

                // Act
                // We assert that the call returns an error result (because SubmitAuditAsync catches ApiException)
                // Wait, SubmitAuditAsync catches ApiException and returns an object.
                // So it won't throw externally.
                // But internally it catches JsonException, logs it, throws ApiException, which is then caught.
                var result = client.SubmitAuditAsync(request).Result;

                // Assert
                Assert.AreEqual(-1, result.OverallScore);
                Assert.That(result.Summary, Does.Contain("API Error"));

                // Verify Logger was called
                Assert.IsTrue(_mockLogger.LogErrorCalled, "Logger.LogError should have been called.");
                Assert.IsInstanceOf<JsonReaderException>(_mockLogger.LastException); // Newtonsoft throws JsonReaderException for "Invalid JSON"
                Assert.AreEqual("Failed to deserialize response", _mockLogger.LastMessage);
            }
        }
    }

    public class MockLogger : ILogger
    {
        public bool LogErrorCalled { get; private set; }
        public Exception LastException { get; private set; }
        public string LastMessage { get; private set; }

        public void LogError(Exception ex, string message)
        {
            LogErrorCalled = true;
            LastException = ex;
            LastMessage = message;
        }

        public void LogInfo(string message)
        {
            // No-op
        }
    }

    // Simple Mock Handler for testing
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage LastRequest { get; private set; }
        private HttpResponseMessage _response;
        private Exception _exception;

        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
            _exception = null;
        }

        public void SetupException(Exception ex)
        {
            _exception = ex;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (_exception != null)
                throw _exception;

            return Task.FromResult(_response ?? new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
