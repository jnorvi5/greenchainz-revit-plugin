using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenChainz.Revit.Services
{
    /// <summary>
    /// Registry for managing multiple Autodesk tool connectors.
    /// Use this to register and access different tool integrations.
    /// </summary>
    public class ToolConnectorRegistry
    {
        private readonly Dictionary<string, IAutodeskToolConnector> _connectors;

        public ToolConnectorRegistry()
        {
            _connectors = new Dictionary<string, IAutodeskToolConnector>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a tool connector with the registry.
        /// </summary>
        public void Register(IAutodeskToolConnector connector)
        {
            if (connector == null)
                throw new ArgumentNullException(nameof(connector));

            _connectors[connector.ToolId] = connector;
        }

        /// <summary>
        /// Gets a connector by its tool ID.
        /// </summary>
        public IAutodeskToolConnector GetConnector(string toolId)
        {
            if (_connectors.TryGetValue(toolId, out var connector))
                return connector;

            return null;
        }

        /// <summary>
        /// Gets a strongly-typed connector by its tool ID.
        /// </summary>
        public T GetConnector<T>(string toolId) where T : class, IAutodeskToolConnector
        {
            return GetConnector(toolId) as T;
        }

        /// <summary>
        /// Gets all registered connectors.
        /// </summary>
        public IEnumerable<IAutodeskToolConnector> GetAllConnectors()
        {
            return _connectors.Values;
        }

        /// <summary>
        /// Gets all available (connected and authenticated) connectors.
        /// </summary>
        public async Task<IEnumerable<IAutodeskToolConnector>> GetAvailableConnectorsAsync()
        {
            var availableConnectors = new List<IAutodeskToolConnector>();

            foreach (var connector in _connectors.Values)
            {
                try
                {
                    if (await connector.IsAvailableAsync())
                    {
                        availableConnectors.Add(connector);
                    }
                }
                catch
                {
                    // Connector not available, skip it
                }
            }

            return availableConnectors;
        }

        /// <summary>
        /// Checks if a specific tool is registered.
        /// </summary>
        public bool IsRegistered(string toolId)
        {
            return _connectors.ContainsKey(toolId);
        }

        /// <summary>
        /// Gets the count of registered connectors.
        /// </summary>
        public int Count => _connectors.Count;
    }
}
