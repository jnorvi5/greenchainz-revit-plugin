using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JavaScriptSerializer _serializer;
        private const string BaseUrl = "http://localhost:5000"; // Assuming local backend or placeholder

        public ApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _serializer = new JavaScriptSerializer();
        }

        public async Task<AuditResult> SubmitAuditAsync(AuditRequest request)
        {
            try
            {
                string json = _serializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("/api/audit/extract-materials", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return _serializer.Deserialize<AuditResult>(responseString);
                }
                else
                {
                    // Handle error, maybe return a default result with error info or throw
                    return new AuditResult
                    {
                        CarbonScore = -1,
                        Rating = "Error",
                        Recommendations = new System.Collections.Generic.List<string> { "API Call Failed: " + response.ReasonPhrase }
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuditResult
                {
                    CarbonScore = -1,
                    Rating = "Error",
                    Recommendations = new System.Collections.Generic.List<string> { "Exception: " + ex.Message }
                };
            }
        }

        // Synchronous wrapper if needed for Revit command context which might not support async/await fully in older versions
        // But usually we can run async.
        public AuditResult SubmitAudit(AuditRequest request)
        {
            return Task.Run(() => SubmitAuditAsync(request)).Result;
        }
    }
}
