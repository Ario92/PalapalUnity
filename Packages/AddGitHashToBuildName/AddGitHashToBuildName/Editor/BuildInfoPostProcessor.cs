#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEditor.Build.Profile;

public class BuildInfoPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        string originalPath = report.summary.outputPath;

        string baseName;
        if (report.summary.platform == BuildTarget.Android)
            baseName = Path.GetFileNameWithoutExtension(originalPath);
        else if (report.summary.platform == BuildTarget.StandaloneWindows64)
            baseName = Path.GetDirectoryName(originalPath);
        else
            return;

        StringBuilder newNameBuilder = GenerateNewName(report, baseName);

        if (report.summary.platform == BuildTarget.Android)
            AndroidCopyBuildWithNewName(report, newNameBuilder);
        else if (report.summary.platform == BuildTarget.StandaloneWindows64)
            WindowsCopyBuildWithNewName(report, newNameBuilder);

    }
    private void WindowsCopyBuildWithNewName(BuildReport report, StringBuilder newNameBuilder)
    {
        string originalPath = report.summary.outputPath;
        string sourceDir = Path.GetDirectoryName(originalPath);
        string destinationDir = newNameBuilder.ToString();
        try
        {
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
            UnityEngine.Debug.Log($"Build renamed to: {destinationDir}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("❌ Failed to copy build with Git hash: " + e.Message);
        }
    }
    private void AndroidCopyBuildWithNewName(BuildReport report, StringBuilder newNameBuilder)
    {
        string originalPath = report.summary.outputPath;

        string extension = Path.GetExtension(originalPath);
        newNameBuilder.Append(extension);

        string newFilename = newNameBuilder.ToString();

        string directory = Path.GetDirectoryName(originalPath);
        string newPath = Path.Combine(directory, newFilename);

        try
        {
            File.Copy(originalPath, newPath, true);
            UnityEngine.Debug.Log($"Build renamed to: {newFilename}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("❌ Failed to copy build with Git hash: " + e.Message);
        }
    }
    private StringBuilder GenerateNewName(BuildReport report, string baseFilename)
    {
        StringBuilder newNameBuilder = new StringBuilder(baseFilename);
#if UNITY_6000_1_OR_NEWER
        BuildProfile buildProfile = BuildProfile.GetActiveBuildProfile();
        if (buildProfile)
            newNameBuilder.Append($"_{buildProfile.name}");
#endif
        string appVersion = PlayerSettings.bundleVersion;
        if (!string.IsNullOrEmpty(appVersion))
        {
            newNameBuilder.Append($"_v{appVersion}");
        }

        string gitHash = GetGitHash();
        if (!string.IsNullOrEmpty(gitHash))
        {
            newNameBuilder.Append($"_{gitHash}");
        }

        return newNameBuilder;
    }
    private string GetGitHash()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Application.dataPath
                }
            };

            process.Start();
            string hash = process.StandardOutput.ReadLine()?.Trim();
            process.WaitForExit();
            return hash;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error getting Git hash: " + e.Message);
            return null;
        }
    }
}
#endif