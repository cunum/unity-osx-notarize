using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Notarization {

    public class SettingsWindow : EditorWindow {

        public static string password;
        public static NotarizationStatus status;

        private string requestUUID = "";

        Settings _settings;
        Settings Settings {
            get {
                if (_settings == null) {
                    _settings = Storage.LoadOrCreateSettings();
                }
                return _settings;
            }
        }
        SerializedObject _serializedSettings;
        SerializedObject SerializedSettings {
            get {
                if (_serializedSettings == null) {
                    _serializedSettings = new SerializedObject(Settings);
                }
                return _serializedSettings;
            }
        }

        GUIStyle _actionButtonStyle;
        GUIStyle ActionButtonStyle {
            get {
                if (_actionButtonStyle == null) {
                    _actionButtonStyle = new GUIStyle(GUI.skin.button);
                    _actionButtonStyle.fontStyle = FontStyle.Bold;
                    _actionButtonStyle.normal.textColor = Color.white;
                }
                return _actionButtonStyle;
            }
        }
        GUIStyle _labelMarginStyle;
        GUIStyle LabelMarginStyle {
            get {
                if (_labelMarginStyle == null) {
                    _labelMarginStyle = new GUIStyle();
                    _labelMarginStyle.margin.left = GUI.skin.label.margin.left;
                }
                return _labelMarginStyle;
            }
        }

        [MenuItem ("Tools/Notarization...")]
        public static void ShowWindow () {
            EditorWindow.GetWindow(typeof(SettingsWindow), false, "Notarization");
        }

        void GuiLine(int i_height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        void OnGUI () {

            SerializedSettings.Update();

            GUILayout.Label ("OSX Build Notarization Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("user"));
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Password");
            string pass = EditorGUILayout.PasswordField(password);
            if (!string.IsNullOrEmpty(pass))
            {
                password = pass;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("certId"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("bundleId"));

            EditorGUILayout.Space();

            EditorGUILayout.Space();

            GUILayout.Label ("Entitlement options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("mono"));
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("steamOverlay"));

            EditorGUILayout.Space();

            GuiLine();

            GUILayout.Label ("Auto notarize options", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("autoNotarizeOnOSXBuild"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("blockUntilFinished"));

            EditorGUILayout.Space();

            GuiLine();

            GUILayout.Label("Manual notarization", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(GUI.skin.button.CalcHeight(new GUIContent("..."), 30)));
            EditorGUILayout.BeginVertical(LabelMarginStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.PropertyField(SerializedSettings.FindProperty("file"));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                var prop = SerializedSettings.FindProperty("file");
                string directory = prop.stringValue;
                if (string.IsNullOrEmpty(prop.stringValue) && !string.IsNullOrEmpty(NotarizationProcessor.lastBuildFile))
                {
                    directory = NotarizationProcessor.lastBuildFile;
                }
                var fld = EditorUtility.OpenFilePanel("Pick file to notorize and press button", directory, "app");
                if (!string.IsNullOrEmpty(fld))
                {
                    prop.stringValue = fld;
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0,0.6f,0,1);
            if (GUILayout.Button("Notarize file", ActionButtonStyle, GUILayout.MinHeight(30))) {
                Notarize();
            }

            EditorGUILayout.Space();

            GUILayout.Label("After successful notarization (email)", EditorStyles.boldLabel);

            if (GUILayout.Button("Staple ticket to file", ActionButtonStyle, GUILayout.MinHeight(30)))
            {
                Staple();
            }

            EditorGUILayout.Space();

            GUILayout.Label("Notarization status", EditorStyles.boldLabel);

            if (GUILayout.Button("Get last status", ActionButtonStyle, GUILayout.MinHeight(30)))
            {
                NotarizationStatus();
            }

            if (status != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Last Request: " + status.id, EditorStyles.label);
                GUILayout.Label("Date: " + status.dateTime, EditorStyles.label);
                GUILayout.Label("Status: " + (status.success ? "Successful" : "Failed"), EditorStyles.label);
                GUILayout.TextArea(status.url, EditorStyles.label);
            }

            EditorGUILayout.Space();

            GUILayout.Label("Notarized file valid", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate notarized file", ActionButtonStyle, GUILayout.MinHeight(30)))
            {
                ValidateFile();
            }

            // GUIContent label = GUI.Label(yourLabelRect, status.url, EditorStyles.linkLabel);
            // Rect yourLabelRect = GUILayoutUtility.GetRect(status.url, EditorStyles.linkLabel);
            // if (Event.current.type == EventType.MouseUp && yourLabelRect.Contains(Event.current.mousePosition))
            //     Application.OpenURL(status.url);

            // This applies any changes to the underlying asset and marks dirty if needed
            // this is what ensures the asset gets saved
            SerializedSettings.ApplyModifiedProperties();
        }

        void Staple()
        {
            try
            {
                var prop = SerializedSettings.FindProperty("file");
                NotarizationProcessor.Staple(prop.stringValue);
                EditorUtility.DisplayDialog("Stapling successful", "Notarization process complete, you can now distribute the app", "Close");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Stapling failed", e.Message, "Close");
            }
        }

        void ValidateFile()
        {
            try
            {
                var prop = SerializedSettings.FindProperty("file");
                NotarizationProcessor.ValidateFile(prop.stringValue);
                EditorUtility.DisplayDialog("Validation successful", "File successfully notarized", "Close");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Validation failed", e.Message, "Close");
            }
        }

        void Notarize()
        {

            try
            {
                var prop = SerializedSettings.FindProperty("file");
                NotarizationProcessor.Notarize(prop.stringValue);
            } catch (Exception e) {
                EditorUtility.DisplayDialog("Notarize error", e.Message, "Close");
            }
        }

        void NotarizationStatus()
        {

            try
            {
                NotarizationProcessor.GetLastNotarizationStatus();
            } catch (Exception e) {
                EditorUtility.DisplayDialog("Fetch error", e.Message, "Close");
            }
        }
    }
}
