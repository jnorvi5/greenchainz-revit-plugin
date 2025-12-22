using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Interface for connecting to various Autodesk Platform Services tools.
    /// Implement this interface to add support for additional tools like EC3, ACC, BIM360, etc.
    /// </summary>
    public interface IAutodeskToolConnector
    {
        /// <summary>
        /// Gets the display name of the tool.
        /// </summary>
        string ToolName { get; }

        /// <summary>
        /// Gets the unique identifier for this tool connector.
        /// </summary>
        string ToolId { get; }

        /// <summary>
        /// Tests if the tool API is available and credentials are valid.
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Fetches data from the tool based on the provided parameters.
        /// </summary>
        /// <param name="parameters">Action-specific parameters for the request.</param>
        /// <returns>Dictionary containing the response data.</returns>
        Task<Dictionary<string, object>> FetchDataAsync(Dictionary<string, string> parameters);
    }
}
