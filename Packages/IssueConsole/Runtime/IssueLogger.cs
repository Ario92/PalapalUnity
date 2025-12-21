using System.Collections.Generic;
using UnityEngine;

namespace Palapal.IssueConsole.Runtime
{
    // Lightweight helper to report issues from code without depending on internal editor types.
    public static class IssueLogger
    {
        // Convenience: report a message as an Issue Console entry
        public static void ReportIssue(string message, LogType type = LogType.Warning, string source = null, Dictionary<string, string> metadata = null)
        {
            IssueConsoleAPI.Report(message, null, type, source, metadata);
        }
    }
}
