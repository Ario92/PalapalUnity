#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class WindowsGitHashBuildPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows64)
            return;

        string gitHash = GitBashHelper.GetGitHash();
        if (string.IsNullOrEmpty(gitHash))
        {
            UnityEngine.Debug.LogWarning("Git hash not found. Skipping rename.");
            return;
        }
        try
        {
            string originalPath = report.summary.outputPath;

            string sourceDir = Path.GetDirectoryName(originalPath);
            string destinationDir = sourceDir + "_" + gitHash;
            Directory.CreateDirectory(destinationDir);


            var allDirectories = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);
            foreach (string dir in allDirectories)
            {
                string dirToCreate = dir.Replace(sourceDir, destinationDir);
                Directory.CreateDirectory(dirToCreate);

            }

            var allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            foreach (string file in allFiles)
            {
                string fileToCopy = file.Replace(sourceDir, destinationDir);
                File.Copy(file, fileToCopy, true);

            }
            Debug.Log($"Build renamed to: {destinationDir}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Failed to copy build with Git hash: " + e.Message);
        }
    }
}
#endif