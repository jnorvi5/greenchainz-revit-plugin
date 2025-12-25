using System;
using Autodesk.Revit.UI;
using GreenChainz.Revit.Models;
using GreenChainz.Revit.UI;

namespace GreenChainz.Revit.Commands
{
    public class AuditCompletedHandler : IExternalEventHandler
    {
        private AuditResult _result;
        private AuditProgressWindow _progressWindow;
        private string _errorMessage;

        public void SetData(AuditResult result, AuditProgressWindow progressWindow)
        {
            _result = result;
            _progressWindow = progressWindow;
            _errorMessage = null;
        }

        public void SetError(string errorMessage, AuditProgressWindow progressWindow)
        {
            _result = null;
            _errorMessage = errorMessage;
            _progressWindow = progressWindow;
        }

        public void Execute(UIApplication app)
        {
            // Close progress window if it's open
            if (_progressWindow != null)
            {
                _progressWindow.Close();
                _progressWindow = null;
            }

            if (_errorMessage != null)
            {
                TaskDialog.Show("Error", $"Audit failed: {_errorMessage}");
            }
            else if (_result != null)
            {
                // Show results
                AuditResultsWindow window = new AuditResultsWindow(_result);
                window.ShowDialog();
            }
            else
            {
                TaskDialog.Show("Error", "Audit failed or returned no result.");
            }
        }

        public string GetName()
        {
            return "Audit Completed Handler";
        }
    }
}
