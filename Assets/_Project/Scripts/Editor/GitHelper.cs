using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace SimpleQaidah.Editor
{
    public static class GitHelper
    {
        public static string ExecuteGit(string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.Combine(Application.dataPath, "..")
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    return $"git {arguments}\nExit Code: {process.ExitCode}\nOutput:\n{output}\nError:\n{error}";
                }
            }
            catch (System.Exception ex)
            {
                return $"git {arguments}\nException:\n{ex.ToString()}";
            }
        }
    }
}
