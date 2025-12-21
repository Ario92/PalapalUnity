using UnityEngine;

namespace Palapal.IssueConsole.Runtime
{
    public class ExampleUsage : MonoBehaviour
    {
        [ContextMenu("Report Example Issue")]
        public void ReportExample()
        {
            IssueLogger.ReportIssue("Example: Found a PascalCase problem in SomeAsset", LogType.Warning, source: "ExampleUsage");
        }

        [ContextMenu("Log Issue via Unity Log")]
        public void LogIssueViaUnityLog()
        {
            Debug.Log("[Issue] Example: You can also send issues by prefixing Unity logs with [Issue]");
        }
    }
}
