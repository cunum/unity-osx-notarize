using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Diagnostics;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;
#if UNITY_2018_1_OR_NEWER
	using UnityEditor.Build.Reporting;
#else
	using UnityEditor.Callbacks;
#endif

namespace Notarization
{

	#if UNITY_2018_1_OR_NEWER
    	public class NotarizationProcessor: IPostprocessBuildWithReport
	#else
    	public class NotarizationProcessor
	#endif
    {

        static string scriptBase = Directory.GetCurrentDirectory() + "/Assets/Editor/Notarization/script/";
        static string stapleScriptPath = string.Concat("\"", scriptBase,  "staple-unity.sh", "\"");
        static string notarizationScriptPath = string.Concat("\"", scriptBase, "notarization-unity.sh", "\"");
        static string notarizationStatusScriptPath = string.Concat("\"", scriptBase, "notarization-status-unity.sh", "\"");
        static string notarizationErrorsScriptPath = string.Concat("\"", scriptBase, "notarization-errors-unity.sh", "\"");
        static string notarizedFileValidationScriptPath = string.Concat("\"", scriptBase, "notarized-file-validation-unity.sh", "\"");

        public static string lastBuildFile;

		#if UNITY_2018_1_OR_NEWER

        public int callbackOrder => Int32.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (isOSXBuild(report) && !isDevelopmentBuild(report))
            {
                lastBuildFile = report.summary.outputPath;
                Settings settings = Storage.LoadOrCreateSettings();
                if (settings.autoNotarizeOnOSXBuild)
                {
                    Notarize(lastBuildFile);
                }
            }
        }

		#else

		[PostProcessBuild(Int32.MaxValue)]
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

		#endif

        private bool isOSXBuild(BuildReport report)
        {
            return report.summary.platform == BuildTarget.StandaloneOSX;
        }

        private bool isDevelopmentBuild(BuildReport report)
        {
            return (report.summary.options & BuildOptions.Development) != 0;
        }

        public static void Staple(string path)
        {

            Debug.Log("Stapling");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string basePath = Path.GetDirectoryName(path);
            string arguments = GetArgumentsAsString(new[] { basePath, fileNameWithoutExtension });

            Debug.Log(stapleScriptPath + arguments);
            string output = Process(stapleScriptPath + arguments);

            if (output.Contains("The staple and validate action failed"))
            {
                throw new InvalidDataException("Stapling failed, no ticket found.");
            }

            if (!(output.Contains("valid on disk") && output.Contains("satisfies its Designated Requirement") && output.Contains("explicit requirement satisfied")))
            {
                throw new InvalidDataException("File validation failed.");
            }

            Debug.Log("Stapling finished");
        }

        private static string GetEntitlementsFilePath(Settings settings)
        {
            string entitlementsFile = "entitlements";

            if (settings.mono)
            {
                entitlementsFile += "-mono";
            }

            if (settings.steamOverlay)
            {
                entitlementsFile += "-steam";
            }

            return entitlementsFile + ".xml";
        }

        public static void ValidateFile(string path)
        {

            Debug.Log("Stapling");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string basePath = Path.GetDirectoryName(path);
            string arguments = GetArgumentsAsString(new[] { basePath, fileNameWithoutExtension });

            Debug.Log(notarizedFileValidationScriptPath + arguments);
            string output = Process(notarizedFileValidationScriptPath + arguments);

            if (!(output.Contains("valid on disk") && output.Contains("satisfies its Designated Requirement") && output.Contains("explicit requirement satisfied")))
            {
                throw new InvalidDataException("File validation failed.");
            }

            Debug.Log("Validation finished");
        }

        public static void GetLastNotarizationStatus()
        {
            Settings settings = Storage.LoadOrCreateSettings();

            string arguments = GetArgumentsAsString(new[] { settings.user, SettingsWindow.password });
            string argumentsToLog = GetArgumentsAsString(new[] { settings.user, "***" });

            Debug.Log(notarizationStatusScriptPath + argumentsToLog);
            string output = Process(notarizationStatusScriptPath + arguments);

            string[] lines = output.Split('\n');

            List<NotarizationStatus> statusList = new List<NotarizationStatus>();
            foreach (string line in lines)
            {
                string[] data = line.Split(' ');
                if (data.Length > 4 && (data[4].Equals("success") || data[4].Equals("invalid")))
                {
                    statusList.Add(new NotarizationStatus
                    {
                        success = data[4].Equals("success"),
                        id = data[3],
                        dateTime = data[0] + data[1]
                    });
                }
            }

            if (statusList.Count > 0)
            {
                NotarizationStatus status = statusList[0];
                status.url = GetNotarizationErrorUrl(status.id);
                SettingsWindow.status = statusList[0];
            }
        }

        public static string GetNotarizationErrorUrl(string id)
        {
            Settings settings = Storage.LoadOrCreateSettings();

            string arguments = GetArgumentsAsString(new[] { settings.user, SettingsWindow.password, id });
            string argumentsToLog = GetArgumentsAsString(new[] { settings.user, "***", id });

            Debug.Log(notarizationErrorsScriptPath + argumentsToLog);
            string url = Process(notarizationErrorsScriptPath + arguments);

            foreach (string line in url.Split('\n'))
            {
                if (line.Trim().StartsWith("LogFileURL"))
                {
                    return line.Replace("LogFileURL:", "");
                }
            }
            return null;
        }

        public static void Notarize(string path)
        {

            Settings settings = Storage.LoadOrCreateSettings();

            Debug.Log("Notarization started");
            Debug.Log("File: " + path);
            Debug.Log("Certificate id: " + settings.certId);
            Debug.Log("Bundle id: " + settings.bundleId);
            Debug.Log("User: " + settings.user);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string basePath = Path.GetDirectoryName(path);
            string entitlementsFile = GetEntitlementsFilePath(settings);
            string arguments = GetArgumentsAsString(new[] { settings.user, SettingsWindow.password, basePath, fileNameWithoutExtension, settings.certId, settings.bundleId, entitlementsFile });
            string argumentsToLog = GetArgumentsAsString(new[] { settings.user, "***", basePath, fileNameWithoutExtension, settings.certId, settings.bundleId, entitlementsFile });

            Debug.Log(notarizationScriptPath + argumentsToLog);
            Process(notarizationScriptPath + arguments, settings.blockUntilFinished);
        }

        private static string GetArgumentsAsString(string[] args)
        {
            return string.Concat(" \"", string.Join("\" \"", args), "\"");
        }

        private static string Process(string arguments, bool blockUntilFinished = true)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "/bin/sh";
                process.StartInfo.Arguments = arguments;

                if (blockUntilFinished)
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();

                    // Synchronously read the standard output of the spawned process.
                    StreamReader reader = process.StandardOutput;
                    StreamReader errorReader = process.StandardError;
                    string output = reader.ReadToEnd();
                    string outputError = errorReader.ReadToEnd();

                    // Write the redirected output to this application's window.
                    Debug.Log(output);
                    Debug.Log(outputError);

                    process.WaitForExit();
                    return output + " " + outputError;
                }
                else
                {
                    process.Start();
                    return null;
                }
            }
        }
    }

    public class NotarizationStatus
    {
        public bool success;
        public string id;
        public string dateTime;
        public string url;
    }
}
