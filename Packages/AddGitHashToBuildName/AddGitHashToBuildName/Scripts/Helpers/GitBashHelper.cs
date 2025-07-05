using UnityEngine;
using System.Diagnostics;
public class GitBashHelper
{
     public static string GetGitHash()
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
