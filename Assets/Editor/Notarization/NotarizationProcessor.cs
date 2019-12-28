using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;

namespace Notarization
{

    public class NotarizationProcessor
    {

        static string scriptBase = Directory.GetCurrentDirectory() + "/Assets/Editor/Notarization/script/";
        static string stapleScriptPath = "\"" + scriptBase + "staple-unity.sh" + "\"";
        static string notarizationScriptPath = "\"" + scriptBase + "notarization-unity.sh" + "\"";

        public static string lastBuildFile;

        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.StandaloneOSX)
            {
                lastBuildFile = pathToBuiltProject;
                Settings settings = Storage.LoadOrCreateSettings();
                if (settings.autoNotarizeOnOSXBuild)
                {
                    Notarize(pathToBuiltProject);
                }
            }
        }

        public static void Staple(string path)
        {

            UnityEngine.Debug.Log("Stapling");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string basePath = Path.GetDirectoryName(path);
            string arguments = " \"" + string.Join("\" \"", basePath, fileNameWithoutExtension) + "\"";

            string output = Process(stapleScriptPath + arguments);

            if (output.Contains("The staple and validate action failed"))
            {
                throw new InvalidDataException("Stapling failed, no ticket found.");
            }

            UnityEngine.Debug.Log("Stapling finished");
        }

        public static void Notarize(string path)
        {

            Settings settings = Storage.LoadOrCreateSettings();

            UnityEngine.Debug.Log("Notarization started");
            UnityEngine.Debug.Log("File: " + path);
            UnityEngine.Debug.Log("Certificate id: " + settings.certId);
            UnityEngine.Debug.Log("Bundle id: " + settings.bundleId);
            UnityEngine.Debug.Log("User: " + settings.user);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string basePath = Path.GetDirectoryName(path);
            string arguments = " \"" + string.Join("\" \"",settings.user, SettingsWindow.password, basePath, fileNameWithoutExtension, settings.certId, settings.bundleId) + "\"";

            Process(notarizationScriptPath + arguments);
        }

        private static string Process(string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "/bin/sh";
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                // Synchronously read the standard output of the spawned process.
                StreamReader reader = process.StandardOutput;
                StreamReader errorReader = process.StandardError;
                string output = reader.ReadToEnd();
                string outputError = errorReader.ReadToEnd();

                // Write the redirected output to this application's window.
                UnityEngine.Debug.Log(output);
                UnityEngine.Debug.Log(outputError);

                process.WaitForExit();
                return output;
            }
        }
    }
}