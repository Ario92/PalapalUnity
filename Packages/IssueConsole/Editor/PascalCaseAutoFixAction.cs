#if UNITY_EDITOR
using Palapal.IssueConsole.Runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


namespace Palapal.IssueConsole.Editor
{
    [InitializeOnLoad]
    public class PascalCaseAutoFixAction : IIssueAction
{
    public string Name => "Auto-Fix PascalCase";

    static PascalCaseAutoFixAction()
    {
        // Register ourselves
        var a = new PascalCaseAutoFixAction();
        IssueConsoleAPI.RegisterAction(a);
    }

    public bool CanHandle(IEnumerable<IssueConsoleEntry> entries)
    {
        return entries != null && entries.Any(e => e.message != null && e.message.Contains("PascalCase"));
    }

    public void Execute(IEnumerable<IssueConsoleEntry> entries)
    {
        var targets = entries.Where(e => e.message != null && e.message.Contains("PascalCase")).ToList();
        if (targets.Count == 0) return;

        var resolved = new List<IssueConsoleEntry>();

        foreach (var e in targets)
        {
            string assetPath = null;
            if (e.metadata != null && e.metadata.ContainsKey("assetPath"))
                assetPath = e.metadata["assetPath"];

            if (string.IsNullOrEmpty(assetPath))
            {
                // try to parse asset path from message or stackTrace
                assetPath = ParseFirstAssetPath(e.message) ?? ParseFirstAssetPath(e.stackTrace);
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"PascalCaseAutoFix: Could not find asset path for issue '{e.message}'");
                continue;
            }

            if (!File.Exists(assetPath) && !AssetDatabase.IsValidFolder(assetPath))
            {
                Debug.LogWarning($"PascalCaseAutoFix: Asset not found at '{assetPath}'");
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            string newName = ToPascalCase(fileName);
            if (string.IsNullOrEmpty(newName) || newName == fileName)
            {
                // Nothing to do
                resolved.Add(e);
                continue;
            }

            string err = AssetDatabase.RenameAsset(assetPath, newName);
            if (string.IsNullOrEmpty(err))
            {
                resolved.Add(e);
                Debug.Log($"PascalCaseAutoFix: Renamed '{assetPath}' -> '{newName}'");
            }
            else
            {
                Debug.LogWarning($"PascalCaseAutoFix: Failed to rename '{assetPath}': {err}");
            }
        }

        if (resolved.Count > 0)
        {
            IssueConsoleAPI.Resolve(resolved);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Auto-Fix PascalCase: Marked {resolved.Count} issue(s) resolved and applied renames.");
        }
    }

    private string ParseFirstAssetPath(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var m = Regex.Match(text, @"(Assets/[A-Za-z0-9_\-./]+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        // split on non-alphanumeric chars and underscores
        var parts = Regex.Split(name, "[^A-Za-z0-9]+").Where(p => !string.IsNullOrEmpty(p)).ToArray();
        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 0) continue;
            parts[i] = char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1) : "");
        }
        return string.Join("", parts);
    }
    }
}
#endif
