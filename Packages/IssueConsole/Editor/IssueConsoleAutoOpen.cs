#if UNITY_EDITOR
using UnityEditor;
using Palapal.IssueConsole.Runtime;

namespace Palapal.IssueConsole.Editor
{
    [InitializeOnLoad]
    internal static class IssueConsoleAutoOpen
{
    static IssueConsoleAutoOpen()
    {
        IssueConsoleAPI.OnEntryReported += OnEntryReported;
    }

    private static void OnEntryReported(IssueConsoleEntry entry)
    {
        // If no instances of the Issue Console window are open, open one when a new entry is reported
        if (!EditorWindow.HasOpenInstances<IssueConsoleWindow>())
        {
            IssueConsoleWindow.Open();
        }
    }
    }
}
#endif
