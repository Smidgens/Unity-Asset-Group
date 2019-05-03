#if UNITY_EDITOR
namespace SmartAssets.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Callback = System.Action;
	using TextCallback = System.Action<string>;
	using ObjectCallback = System.Action<UnityEngine.Object>;
	using CallbackFunction = UnityEditor.EditorApplication.CallbackFunction;
	using UObject = UnityEngine.Object;

	internal class ObjectSelectDialog : EditorWindow
	{
		private ObjectCallback _onConfirm = null;
		private Callback _onGUI = null;
		private UObject _startValue = null;
		private UObject _value = null;

		public static void Open(string title, UObject value, ObjectCallback onOK)
		{
			var size = new Vector2(200f, 50f);
			ObjectSelectDialog window = CreateInstance<ObjectSelectDialog>();
			window._value = value;
			window._startValue = value;
			window.titleContent = new GUIContent(title);
			window._onConfirm = onOK;
			window.minSize = size;
			window.maxSize = size;
			window.CenterOnMainWin();
			window.ShowUtility();
		}

		private void OnGUI()
		{
			if(_onGUI != null) {  _onGUI(); }
			GUILayout.FlexibleSpace();
			_value = EditorGUILayout.ObjectField(_value, typeof(UObject), false);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			bool temp = GUI.enabled;
			GUI.enabled = _startValue != _value;
			if (GUILayout.Button("Confirm"))
			{
				if(_onConfirm != null) { _onConfirm(_value); }
				Close();
			}
			GUI.enabled = temp;
			if (GUILayout.Button("Cancel")) { Close(); }
			GUILayout.EndHorizontal();
		}
	}

	internal class ConfirmDialog : EditorWindow
	{
		public static void Open(string title, string msg, CallbackFunction onOK)
		{
			ConfirmDialog window = CreateInstance<ConfirmDialog>();
			Vector2 size = new Vector2(200f, 50f);
			window.titleContent = new GUIContent(title);
			window._message = msg;
			window._onOK = onOK;
			window.minSize = size;
			window.maxSize = size;
			window.CenterOnMainWin();
			window.ShowUtility();
		}
		private void OnGUI()
		{
			GUILayout.Label(_message);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("OK", GUILayout.Width(Screen.width * 0.47f))) { _onOK(); Close(); }
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Cancel", GUILayout.Width(Screen.width * 0.47f))) { Close(); }
			GUILayout.EndHorizontal();
		}
		private string _message = "";
		private CallbackFunction _onOK = null;
	}
	
	internal class TextInputDialog : EditorWindow
	{
		public static void Open(string title, string text, TextCallback onOK)
		{
			Vector2 size = new Vector2(200f, 50f);
			TextInputDialog window = CreateInstance<TextInputDialog>();
			window._text = text;
			window._startText = text;
			window.titleContent = new GUIContent(title);
			window._onConfirm = onOK;
			window.minSize = size;
			window.maxSize = size;
			window.CenterOnMainWin();
			window.ShowUtility();
		}

		private TextCallback _onConfirm = null;
		private string _startText = "";
		private string _text = "";

		private void OnGUI()
		{
			GUILayout.FlexibleSpace();
			_text = GUILayout.TextField(_text);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();

			bool temp = GUI.enabled;
			GUI.enabled = _startText != _text;
			if (GUILayout.Button("Confirm"))
			{
				if (_onConfirm != null) { _onConfirm(_text); }
				Close();
			}
			GUI.enabled = temp;
			if (GUILayout.Button("Cancel")) { Close(); }
			GUILayout.EndHorizontal();
		}
	}
}
#endif