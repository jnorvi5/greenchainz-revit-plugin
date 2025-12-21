using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.Services;
using NUnit.Framework;

namespace GreenChainz.Revit.Tests
{
    /// <summary>
    /// Unit tests for the ApiClient class.
    /// </summary>
    [TestFixture]
    public class ApiClientTests
    {
        private const string TestBaseUrl = "https://test.greenchainz.com/api";
        private const string TestToken = "test-jwt-token-123";

        private MockHttpMessageHandler _mockHandler;
        private HttpClient _httpClient;

        [SetUp]
        public void SetUp()
        {
            _mockHandler = new MockHttpMessageHandler();
            _httpClient = new HttpClient(_mockHandler);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        #region GetMaterialsAsync Tests

        [Test]
        public async Task GetMaterialsAsync_Success_ReturnsValidResponse()
        {
            // Arrange
            var expectedResponse = @"{
                ""Materials"": [
                    {
                        ""Id"": ""mat-001"",
                        ""Name"": ""Low Carbon Concrete"",
                        ""Category"": ""Concrete"",
                        ""Description"": ""Sustainable concrete with reduced embodied carbon"",
                        ""Manufacturer"": ""EcoMaterials Inc"",
                        ""EmbodiedCarbon"": 150.5,
                        ""Unit"": ""m3"",
                        ""Certifications"": [""LEED"", ""EPD""],
                        ""IsVerified"": true,
                        ""ImageUrl"": ""https://example.com/image.jpg"",
                        ""PricePerUnit"": 250.00
                    }
                ],
                ""TotalCount"": 1,
                ""Page"": 1,
                ""PageSize"": 10
            }";

            _mockHandler.SetupResponse(HttpStatusCode.OK, expectedResponse);

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act
                var result = await client.GetMaterialsAsync();

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(1, result.TotalCount);
                Assert.AreEqual(1, result.Materials.Count);
                Assert.AreEqual("mat-001", result.Materials[0].Id);
                Assert.AreEqual("Low Carbon Concrete", result.Materials[0].Name);
                Assert.AreEqual("Concrete", result.Materials[0].Category);
                Assert.AreEqual(150.5, result.Materials[0].EmbodiedCarbon);
                Assert.IsTrue(result.Materials[0].IsVerified);
                Assert.AreEqual(2, result.Materials[0].Certifications.Count);
            }
        }

        [Test]
        public async Task GetMaterialsAsync_WithFilters_IncludesQueryParameters()
        {
            // Arrange
            var expectedResponse = @"{
                ""Materials"": [],
                ""TotalCount"": 0,
                ""Page"": 1,
                ""PageSize"": 10
            }";

            _mockHandler.SetupResponse(HttpStatusCode.OK, expectedResponse);

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act
                await client.GetMaterialsAsync(category: "Concrete", search: "low carbon");

                // Assert
                var requestUri = _mockHandler.LastRequest.RequestUri.ToString();
                Assert.That(requestUri, Does.Contain("category=Concrete"));
                Assert.That(requestUri, Does.Contain("search=low%20carbon"));
            }
        }

        [Test]
        public void GetMaterialsAsync_UnauthorizedToken_ThrowsApiException()
        {
            // Arrange
            var errorResponse = @"{""error"": ""Invalid token""}";
            _mockHandler.SetupResponse(HttpStatusCode.Unauthorized, errorResponse);

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.GetMaterialsAsync());
                Assert.AreEqual(401, ex.StatusCode);
                Assert.That(ex.Message, Does.Contain("Authentication failed"));
            }
        }

        [Test]
        public void GetMaterialsAsync_ServerError_ThrowsApiException()
        {
            // Arrange
            var errorResponse = @"{""error"": ""Internal server error""}";
            _mockHandler.SetupResponse(HttpStatusCode.InternalServerError, errorResponse);

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.GetMaterialsAsync());
                Assert.AreEqual(500, ex.StatusCode);
                Assert.That(ex.Message, Does.Contain("Server error"));
            }
        }

        [Test]
        public void GetMaterialsAsync_NotFound_ThrowsApiException()
        {
            // Arrange
            _mockHandler.SetupResponse(HttpStatusCode.NotFound, "Not found");

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.GetMaterialsAsync());
                Assert.AreEqual(404, ex.StatusCode);
                Assert.That(ex.Message, Does.Contain("not found"));
            }
        }

        #endregion

        #region SubmitAuditAsync Tests

        [Test]
        public async Task SubmitAuditAsync_ValidRequest_ReturnsAuditResponse()
        {
            // Arrange
            var expectedResponse = @"{
                ""AuditId"": ""audit-123"",
                ""TotalEmbodiedCarbon"": 5000.75,
                ""Results"": [
                    {
                        ""MaterialName"": ""Concrete"",
                        ""Quantity"": 100.5,
                        ""EmbodiedCarbon"": 3000.25,
                        ""Unit"": ""m3"",
                        ""SustainableAlternatives"": [""Low Carbon Concrete""]
                    }
                ],
                ""Recommendations"": [""Consider using low carbon alternatives""],
                ""ProcessedAt"": ""2024-01-15T10:30:00Z""
            }";

            _mockHandler.SetupResponse(HttpStatusCode.OK, expectedResponse);

            var request = new AuditRequest
            {
                ProjectName = "Test Project",
                ProjectId = "proj-001",
                Materials = new List<MaterialUsage>
                {
                    new MaterialUsage
                    {
                        ElementId = "elem-001",
                        ElementType = "Wall",
                        MaterialName = "Concrete",
                        MaterialCategory = "Structural",
                        Volume = 100.5,
                        Unit = "m3"
                    }
                },
                AuditDate = DateTime.UtcNow,
                RevitVersion = "2024"
            };

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act
                var result = await client.SubmitAuditAsync(request);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("audit-123", result.AuditId);
                Assert.AreEqual(5000.75, result.TotalEmbodiedCarbon);
                Assert.AreEqual(1, result.Results.Count);
                Assert.AreEqual("Concrete", result.Results[0].MaterialName);
                Assert.AreEqual(1, result.Recommendations.Count);
            }
        }

        [Test]
        public void SubmitAuditAsync_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                Assert.ThrowsAsync<ArgumentNullException>(async () => await client.SubmitAuditAsync(null));
            }
        }

        [Test]
        public void SubmitAuditAsync_InvalidRequest_ThrowsApiException()
        {
            // Arrange
            var errorResponse = @"{""error"": ""Invalid request data""}";
            _mockHandler.SetupResponse(HttpStatusCode.BadRequest, errorResponse);

            var request = new AuditRequest
            {
                ProjectName = "Test",
                Materials = new List<MaterialUsage>()
            };

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.SubmitAuditAsync(request));
                Assert.AreEqual(400, ex.StatusCode);
                Assert.That(ex.Message, Does.Contain("Bad request"));
            }
        }

        #endregion

        #region SubmitRfqAsync Tests

        [Test]
        public async Task SubmitRfqAsync_ValidRequest_ReturnsRfqResponse()
        {
            // Arrange
            var expectedResponse = @"{
                ""RfqId"": ""rfq-456"",
                ""Status"": ""submitted"",
                ""SubmittedAt"": ""2024-01-15T10:30:00Z"",
                ""SuppliersNotified"": 5,
                ""Message"": ""Your RFQ has been sent to 5 suppliers""
            }";

            _mockHandler.SetupResponse(HttpStatusCode.OK, expectedResponse);

            var request = new RfqRequest
            {
                ProjectName = "Test Project",
                ContactEmail = "test@example.com",
                ContactName = "John Doe",
                Items = new List<RfqItem>
                {
                    new RfqItem
                    {
                        MaterialName = "Low Carbon Concrete",
                        MaterialCategory = "Concrete",
                        Quantity = 100,
                        Unit = "m3",
                        Specifications = "Must meet LEED standards"
                    }
                },
                Notes = "Urgent order",
                RequiredDate = DateTime.UtcNow.AddDays(30)
            };

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act
                var result = await client.SubmitRfqAsync(request);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual("rfq-456", result.RfqId);
                Assert.AreEqual("submitted", result.Status);
                Assert.AreEqual(5, result.SuppliersNotified);
                Assert.That(result.Message, Does.Contain("5 suppliers"));
            }
        }

        [Test]
        public void SubmitRfqAsync_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                Assert.ThrowsAsync<ArgumentNullException>(async () => await client.SubmitRfqAsync(null));
            }
        }

        [Test]
        public void SubmitRfqAsync_NetworkFailure_ThrowsApiException()
        {
            // Arrange
            _mockHandler.SetupException(new HttpRequestException("Network error"));

            var request = new RfqRequest
            {
                ProjectName = "Test",
                ContactEmail = "test@example.com",
                Items = new List<RfqItem>()
            };

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.SubmitRfqAsync(request));
                Assert.That(ex.Message, Does.Contain("Network error"));
            }
        }

        #endregion

        #region Authentication Tests

        [Test]
        public async Task ApiClient_IncludesBearerToken_InRequests()
        {
            // Arrange
            var expectedResponse = @"{""Materials"": [], ""TotalCount"": 0, ""Page"": 1, ""PageSize"": 10}";
            _mockHandler.SetupResponse(HttpStatusCode.OK, expectedResponse);

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act
                await client.GetMaterialsAsync();

                // Assert
                var authHeader = _mockHandler.LastRequest.Headers.Authorization;
                Assert.IsNotNull(authHeader);
                Assert.AreEqual("Bearer", authHeader.Scheme);
                Assert.AreEqual(TestToken, authHeader.Parameter);
            }
        }

        [Test]
        public void ApiClient_NullBaseUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApiClient(null, TestToken));
        }

        [Test]
        public void ApiClient_EmptyBaseUrl_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApiClient("", TestToken));
        }

        [Test]
        public void ApiClient_NullAuthToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApiClient(TestBaseUrl, null));
        }

        [Test]
        public void ApiClient_EmptyAuthToken_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ApiClient(TestBaseUrl, ""));
        }

        #endregion

        #region Request Serialization Tests

        [Test]
        public async Task SubmitAuditAsync_SerializesRequestBody_Correctly()
        {
            // Arrange
            var expectedResponse = @"{""AuditId"": ""test"", ""TotalEmbodiedCarbon"": 0, ""Results"": [], ""Recommendations"": [], ""ProcessedAt"": ""2024-01-15T10:30:00Z""}";
            _mockHandler.SetupResponse(HttpStatusCode.OK, expectedResponse);

            var request = new AuditRequest
            {
                ProjectName = "Test Project",
                ProjectId = "proj-001",
                Materials = new List<MaterialUsage>(),
                AuditDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                RevitVersion = "2024"
            };

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act
                await client.SubmitAuditAsync(request);

                // Assert
                var requestContent = await _mockHandler.LastRequest.Content.ReadAsStringAsync();
                Assert.That(requestContent, Does.Contain("Test Project"));
                Assert.That(requestContent, Does.Contain("proj-001"));
                Assert.That(requestContent, Does.Contain("2024"));
            }
        }

        [Test]
        public async Task GetMaterialsAsync_SetsCorrectHeaders()
        {
            // Arrange
            var expectedResponse = @"{""Materials"": [], ""TotalCount"": 0, ""Page"": 1, ""PageSize"": 10}";
            _mockHandler.SetupResponse(HttpStatusCode.OK, expectedResponse);

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act
                await client.GetMaterialsAsync();

                // Assert
                var acceptHeader = _mockHandler.LastRequest.Headers.Accept.FirstOrDefault();
                Assert.IsNotNull(acceptHeader);
                Assert.AreEqual("application/json", acceptHeader.MediaType);
            }
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetMaterialsAsync_RateLimitExceeded_ThrowsApiException()
        {
            // Arrange
            _mockHandler.SetupResponse((HttpStatusCode)429, "Rate limit exceeded");

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.GetMaterialsAsync());
                Assert.AreEqual(429, ex.StatusCode);
                Assert.That(ex.Message, Does.Contain("Rate limit"));
            }
        }

        [Test]
        public void GetMaterialsAsync_ServiceUnavailable_ThrowsApiException()
        {
            // Arrange
            _mockHandler.SetupResponse(HttpStatusCode.ServiceUnavailable, "Service unavailable");

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.GetMaterialsAsync());
                Assert.AreEqual(503, ex.StatusCode);
                Assert.That(ex.Message, Does.Contain("temporarily unavailable"));
            }
        }

        [Test]
        public void GetMaterialsAsync_Forbidden_ThrowsApiException()
        {
            // Arrange
            _mockHandler.SetupResponse(HttpStatusCode.Forbidden, "Forbidden");

            using (var client = new ApiClient(TestBaseUrl, TestToken, _httpClient))
            {
                // Act & Assert
                var ex = Assert.ThrowsAsync<ApiException>(async () => await client.GetMaterialsAsync());
                Assert.AreEqual(403, ex.StatusCode);
                Assert.That(ex.Message, Does.Contain("Access forbidden"));
            }
        }

        #endregion
    }
}
