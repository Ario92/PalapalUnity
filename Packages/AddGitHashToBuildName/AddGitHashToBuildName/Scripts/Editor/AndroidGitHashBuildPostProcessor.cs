#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class AndroidGitHashBuildPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        string gitHash = GitBashHelper.GetGitHash();
        if (string.IsNullOrEmpty(gitHash))
        {
            Debug.LogWarning("Git hash not found. Skipping rename.");
            return;
        }

        string originalPath = report.summary.outputPath;
        string directory = Path.GetDirectoryName(originalPath);
        string filename = Path.GetFileNameWithoutExtension(originalPath);
        string extension = Path.GetExtension(originalPath); // .apk or .aab

        string newFilename = $"{filename}_{gitHash}{extension}";
        string newPath = Path.Combine(directory, newFilename);

        try
        {
            File.Copy(originalPath, newPath,true);
            Debug.Log($"Build renamed to: {newFilename}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Failed to copy build with Git hash: " + e.Message);
        }
    }
}
#endif