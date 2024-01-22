using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
namespace Emptybraces.Editor
{
	class ContextMenu_ExecuteProcessSettingsProvider : SettingsProvider
	{
		SerializedObject _settings;
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
		}

		public override void OnGUI(string searchContext)
		{
			// Use IMGUI to display UI:
			EditorGUILayout.PropertyField(_settings.FindProperty("process1"));
			EditorGUILayout.PropertyField(_settings.FindProperty("process2"));
			EditorGUILayout.PropertyField(_settings.FindProperty("process3"));
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
	}

	class ContextMenu_ExecuteProcessSettings : ScriptableObject
	{
		public const string k_Path = "Assets/ContextMenu_ExecuteProcessSettings.asset";
		[System.Serializable]
		class Data
		{
			public string Filename;
			public string Args;
		}
		[SerializeField] Data process1;
		[SerializeField] Data process2;
		[SerializeField] Data process3;
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

		[MenuItem("Assets/ExecuteProcess/Process_1")]
		static void _Process_1()
		{
			var settings = AssetDatabase.LoadAssetAtPath<ContextMenu_ExecuteProcessSettings>(k_Path);
			if (settings != null)
				_Process(settings.process1.Filename, settings.process1.Args);
		}
		[MenuItem("Assets/ExecuteProcess/Process_2")]
		static void _Process_2()
		{
			var settings = AssetDatabase.LoadAssetAtPath<ContextMenu_ExecuteProcessSettings>(k_Path);
			if (settings != null)
				_Process(settings.process2.Filename, settings.process2.Args);
		}
		[MenuItem("Assets/ExecuteProcess/Process_3")]
		static void _Process_3()
		{
			var settings = AssetDatabase.LoadAssetAtPath<ContextMenu_ExecuteProcessSettings>(k_Path);
			if (settings != null)
				_Process(settings.process3.Filename, settings.process3.Args);
		}

		static void _Process(string filename, string args)
		{
			if (string.IsNullOrEmpty(filename))
				return;
			args = args.Replace("ABS_PATH", $"{Directory.GetParent(Application.dataPath)}/{AssetDatabase.GetAssetPath(Selection.activeObject)}");
			args = args.Replace("FILENAME_EXT", Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)));
			args = args.Replace("FILENAME", Selection.activeObject.name);

			var info = new ProcessStartInfo();
			info.WindowStyle = ProcessWindowStyle.Normal;
			info.FileName = filename;
			info.UseShellExecute = true;
			info.Verb = "RunAs";
			info.CreateNoWindow = false;
			info.RedirectStandardOutput = false;
			info.Arguments = args;
			Process.Start(info);
		}
	}
}