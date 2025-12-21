#if UNITY_EDITOR
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Palapal.IssueConsole.Runtime;

public class PascalCaseAutoFixTests
{
    private const string TestFolder = "Assets/TempIssueConsoleTests";

    [SetUp]
    public void SetUp()
    {
        if (!AssetDatabase.IsValidFolder(TestFolder))
            AssetDatabase.CreateFolder("Assets", "TempIssueConsoleTests");
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(TestFolder);
        AssetDatabase.Refresh();
    }

    [Test]
    public void PascalCaseAutoFix_RenamesAssetAndResolvesIssue()
    {
        // Create a test asset with non-pascal name
        var so = ScriptableObject.CreateInstance<ScriptableObject>();
        string path = TestFolder + "/my_test_asset.asset";
        AssetDatabase.CreateAsset(so, path);
        AssetDatabase.SaveAssets();

        // Report issue with metadata pointing to the asset
        var meta = new Dictionary<string, string> { { "assetPath", path } };
        IssueConsoleAPI.Report("PascalCase violation", null, UnityEngine.LogType.Warning, "unit-test", meta);

        // Find the registered PascalCaseAutoFixAction
        var action = (Palapal.IssueConsole.Runtime.IIssueAction)null;
        foreach (var a in IssueConsoleAPI.RegisteredActions)
        {
            if (a.Name.Contains("PascalCase")) { action = a; break; }
        }
        Assert.IsNotNull(action, "PascalCase action should be registered");

        // Grab the entries (wait briefly for OnEntryReported to be invoked)
        var entryList = new List<IssueConsoleEntry>();
        IssueConsoleAPI.OnEntryReported += (e) => entryList.Add(e);
        // Manually trigger report again to ensure we have entry
        IssueConsoleAPI.Report("PascalCase violation", null, UnityEngine.LogType.Warning, "unit-test", meta);

        Assert.IsTrue(entryList.Count > 0, "Entry should be reported");
        var entry = entryList[entryList.Count - 1];

        // Execute action
        action.Execute(new[] { entry });

        // Validate asset renamed
        var newPath = AssetDatabase.GetAssetPath(so);
        Assert.IsFalse(newPath.EndsWith("my_test_asset.asset"), "Asset should be renamed to PascalCase");

        // Clean up
        AssetDatabase.DeleteAsset(newPath);
    }
}
#endif
