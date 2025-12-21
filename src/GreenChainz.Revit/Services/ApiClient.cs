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
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://api.greenchainz.com"; // Placeholder URL

        public async Task<string> SubmitRFQ(RFQRequest request)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Assuming POST /api/rfqs
                HttpResponseMessage response = await _httpClient.PostAsync($"{BaseUrl}/api/rfqs", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Assuming response contains an ID or success message.
                    // Let's parse it or just return it.
                    // If response is like { "id": "RFQ-123" }
                    return responseBody;
                }
                else
                {
                    throw new Exception($"Error submitting RFQ: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                // Log exception if logging exists
                throw new Exception($"Failed to submit RFQ: {ex.Message}", ex);
            }
        }
    }
}
