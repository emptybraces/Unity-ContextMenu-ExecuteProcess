using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
namespace Emptybraces.Editor
{
	class ContextMenu_ExecuteProcessSettingsProvider : SettingsProvider
	{
		SerializedObject _settings;
		SerializedProperty _processes;
		SerializedProperty _outputPath;
		public ContextMenu_ExecuteProcessSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
			: base(path, scope) { }

		// public static bool IsSettingsAvailable()
		// {
		// 	return File.Exists(ContextMenu_ExecuteProcess_Settings.k_Path);
		// }

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			// This function is called when the user clicks on the MyCustom element in the Settings window.
			_settings = ContextMenu_ExecuteProcessSettings.GetSerializedSettings();
			_processes = _settings.FindProperty(nameof(ContextMenu_ExecuteProcessSettings.Processes));
			_outputPath = _settings.FindProperty(nameof(ContextMenu_ExecuteProcessSettings.OutputPath));
		}

		public override void OnGUI(string searchContext)
		{
			// Use IMGUI to display UI:
			EditorGUILayout.PropertyField(_processes);
			EditorGUILayout.PropertyField(_outputPath);
			GUI.enabled = 0 < _processes.arraySize;
			if (GUILayout.Button("Output File"))
				_OutputFile();
			GUI.enabled = true;
			EditorGUILayout.Space(20);
			EditorGUILayout.LabelField("Selected Asset Path:");
			EditorGUILayout.TextField("Abosolute Path", "ABS_PATH");
			EditorGUILayout.TextField("File name", "FILENAME");
			EditorGUILayout.TextField("File name with extension", "FILENAME_EXT");
			if (_settings.ApplyModifiedPropertiesWithoutUndo())
			{
				EditorUtility.SetDirty(_settings.targetObject);
			}
		}

		// Register the SettingsProvider
		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			var provider = new ContextMenu_ExecuteProcessSettingsProvider("ContextMenu_ExecuteProcess", SettingsScope.User);
			// Automatically extract all keywords from the Styles.
			// provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
			return provider;
		}

		void _OutputFile()
		{
			string contents = @"
using UnityEditor;
namespace Emptybraces.Editor
{
	public class ContextMenu_ExecuteProcessMenu
	{
		// INSERT_HERE
	}
}
";
			var settings = AssetDatabase.LoadAssetAtPath<ContextMenu_ExecuteProcessSettings>(ContextMenu_ExecuteProcessSettings.k_Path);
			var method_contents = new StringBuilder();
			foreach (var data in settings.Processes)
			{
				if (data.MenuName == "")
					continue;
				method_contents.AppendLine($"[MenuItem(\"Assets/ExecuteProcess/{data.MenuName.Replace(" ", "_")}\")]");
				method_contents.AppendLine($"		static void {data.MenuName.Replace(" ", "_")}()");
				method_contents.AppendLine("		{");
				method_contents.AppendLine($"			var settings = AssetDatabase.LoadAssetAtPath<ContextMenu_ExecuteProcessSettings>(ContextMenu_ExecuteProcessSettings.k_Path);");
				method_contents.AppendLine($"			if (settings != null)");
				method_contents.AppendLine($"				ContextMenu_ExecuteProcessSettings.Process(\"{data.ProcessPath.Replace("\\", "\\\\")}\", \"{data.Args.Replace("\\", "\\\\")}\");");
				method_contents.AppendLine("		}");
			}

			var path = Path.Combine(Application.dataPath, settings.OutputPath);
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			File.WriteAllText(path + "/ContextMenu_ExecuteProcessMenu.cs", contents.Replace("// INSERT_HERE", method_contents.ToString()));
			AssetDatabase.Refresh();
		}
	}
	class ContextMenu_ExecuteProcessSettings : ScriptableObject
	{
		public const string k_Path = "Assets/ContextMenu_ExecuteProcessSettings.asset";
		[System.Serializable]
		public class Data
		{
			public string MenuName;
			public string ProcessPath;
			public string Args;
		}
		public Data[] Processes;
		public string OutputPath = "Editor/";
		internal static ContextMenu_ExecuteProcessSettings GetOrCreateSettings()
		{
			var settings = AssetDatabase.LoadAssetAtPath<ContextMenu_ExecuteProcessSettings>(k_Path);
			if (settings == null)
			{
				settings = ScriptableObject.CreateInstance<ContextMenu_ExecuteProcessSettings>();
				AssetDatabase.CreateAsset(settings, k_Path);
				AssetDatabase.SaveAssets();
			}
			return settings;
		}

		internal static SerializedObject GetSerializedSettings()
		{
			return new SerializedObject(GetOrCreateSettings());
		}

		public static void Process(string filename, string args)
		{
			if (string.IsNullOrEmpty(filename))
				return;
			args = args.Replace("ABS_PATH", $"{Directory.GetParent(Application.dataPath)}/{AssetDatabase.GetAssetPath(Selection.activeObject)}");
			args = args.Replace("FILENAME_EXT", Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)));
			args = args.Replace("FILENAME", Selection.activeObject.name);

			var info = new ProcessStartInfo();
			info.WindowStyle = ProcessWindowStyle.Normal;
			info.WorkingDirectory = Directory.GetParent(filename).FullName;
			info.FileName = filename;
			info.UseShellExecute = true;
			info.Verb = "RunAs";
			info.CreateNoWindow = false;
			info.RedirectStandardOutput = false;
			info.Arguments = args;
			System.Diagnostics.Process.Start(info);
		}
	}
}