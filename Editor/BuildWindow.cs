using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildTemplates
{
    public class BuildWindow : EditorWindow
    {
        private BuildTarget _buildTarget;
        private BuildTemplate _buildTemplate;

        [MenuItem("Build/Open Build Window")]
        private static void Open()
        {
            var window = GetWindow<BuildWindow>();
            window.titleContent = new GUIContent("Build Window");
            window.Show();
        }

        private void OnEnable()
        {
            _buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var templateGuids = AssetDatabase.FindAssets("t:BuildTemplate");
            if (templateGuids.Length > 0)
            {
                var templatePath = AssetDatabase.GUIDToAssetPath(templateGuids[0]);
                _buildTemplate = AssetDatabase.LoadAssetAtPath<BuildTemplate>(templatePath);
            }
        }

        private void OnGUI()
        {
            _buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", _buildTarget);

            _buildTemplate = (BuildTemplate)EditorGUILayout.ObjectField("Build Template", _buildTemplate, typeof(BuildTemplate), false);
            if (_buildTemplate == null)
            {
                return;
            }

            var buildPath = EditorUserBuildSettings.GetBuildLocation(_buildTarget);
            if (string.IsNullOrEmpty(buildPath))
            {
                buildPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
            }
            buildPath = ReplaceFileName(buildPath, Application.productName + GetExtension(_buildTarget));
            EditorGUILayout.LabelField($"Build path: {GenerateBuildPath(buildPath, _buildTemplate.name)}");
            if (GUILayout.Button("Change build directory"))
            {
                buildPath = SelectSaveFolder(buildPath);
                EditorUserBuildSettings.SetBuildLocation(_buildTarget, buildPath);
            }

            var requireBuild = false;
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build"))
                {
                    requireBuild = true;
                }
                if (GUILayout.Button("Open"))
                {
                    Process.Start(Path.GetDirectoryName(GenerateBuildPath(buildPath, _buildTemplate.name)));
                }
            }
            // To avoid errors, execute the build outside of BeginLayout()/EndLayout().
            if (requireBuild)
            {
                requireBuild = false;
                EditorUserBuildSettings.SetBuildLocation(_buildTarget, buildPath);
                var buildPlayerOptions = new BuildPlayerOptions();
                buildPlayerOptions.scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                buildPlayerOptions.locationPathName = GenerateBuildPath(buildPath, _buildTemplate.name);
                buildPlayerOptions.target = _buildTarget;
                var options = BuildOptions.None;
                options |= _buildTemplate.Development ? BuildOptions.Development : BuildOptions.None;
                options |= _buildTemplate.AutoconnectProfiler ? BuildOptions.AutoRunPlayer : BuildOptions.None;
                options |= _buildTemplate.AllowDebugging ? BuildOptions.AllowDebugging : BuildOptions.None;
                buildPlayerOptions.options = options;
                buildPlayerOptions.extraScriptingDefines = _buildTemplate.ExtraScriptingDefines;
                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                UnityEngine.Debug.Log($"Build Report: {report.summary.result} {report.summary.totalTime}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_buildTemplate.name);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                var editor = Editor.CreateEditor(_buildTemplate);
                editor?.OnInspectorGUI();
            }
        }

        private static string SelectSaveFolder(string defaultPath)
        {
            var fileName = Path.GetFileName(defaultPath);
            var folderName = Path.GetFileName(Path.GetDirectoryName(defaultPath));
            var parent = Directory.GetParent(Path.GetDirectoryName(defaultPath)).FullName;
            var directory = EditorUtility.SaveFolderPanel("Choose Folder", parent, folderName);
            return Path.Combine(directory, fileName);
        }

        private static string GenerateBuildPath(string buildLocation, string templateName)
        {
            var fileName = Path.GetFileName(buildLocation);
            var directory = Path.GetDirectoryName(buildLocation);
            return Path.Combine(directory, templateName, fileName);
        }

        private static string ReplaceFileName(string path, string fileName)
        {
            var directory = Path.GetDirectoryName(path);
            return Path.Combine(directory, fileName);
        }

        private static string GetExtension(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.StandaloneLinux64:
                    return "";
                case BuildTarget.WebGL:
                    return "";
                case BuildTarget.Android:
                    return ".apk";
                case BuildTarget.iOS:
                    return "";
                default:
                    return ".exe";
            }
        }
    }
}
