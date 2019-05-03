#if UNITY_EDITOR
namespace SmartAssets.Editor
{
	using System;
	using Object = UnityEngine.Object;
	using UnityEngine;
	using UnityEditor;
	using System.Linq;
	using System.Collections.Generic;
	using AssetDatabase = UnityEditor.AssetDatabase;
	using Undo = UnityEditor.Undo;
	using ObjectList = System.Collections.Generic.List<UnityEngine.Object>;

	internal static class Extensions
	{
		public static ObjectList GetSubAssets(this Object target)
		{
			var l = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(target)).Where(a => a != target)
			.OrderBy(a => a.GetType().Name).ThenBy(a => a.name);
			return l.ToList();
		}

		public static void SortByTypeFirst(this List<Object> l)
		{
			l.Sort((x,y) => x.GetType() == y.GetType() ? 
			string.Compare(x.name, y.name) : 
			string.Compare(x.GetType().Name, y.GetType().Name));
		}

		public static void SortByNameFirst(this List<Object> l)
		{
			l.Sort((x,y) => x.name == y.name ? 
			string.Compare(x.GetType().Name, y.GetType().Name) : 
			string.Compare(x.name, y.name));
		}

		public static Object Duplicate(this Object o)
		{
			return o ? Object.Instantiate(o) : default(Object);
		}

		public static T AddAsset<T>(this Object parent, System.Func<T> createCB, bool isNew = true) where T : Object
		{
			T ob = createCB();
			if(ob)
			{
				if (isNew) { ob.name = string.Format("New {0}", ob.GetType().Name); }
				AssetDatabase.AddObjectToAsset(ob, parent);
				parent.hideFlags = UnityEngine.HideFlags.None;
				parent.Import();
			}
			return ob;
		}
		public static void Rename(this Object o, string n)
		{
			if (n == o.name) { return; }
			Undo.RecordObject(o, "Changed asset name");
			o.name = n;
			o.Import();
		}

		public static void Import(this Object o)
		{
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(o));
		}

		public static Rect Resize(this Rect r, float xOffset, float yOffset)
		{
			Rect rr = new Rect(r.x, r.y, r.width + xOffset, r.height + yOffset);
			rr.center = r.center;
			return rr;
		}

		public static Rect SetHeight(this Rect r, float height)
		{
			Vector2 center = r.center;
			r.height = height;
			r.center = center;
			return r;
		}

		public static Rect[] SliceHorizontalMixed(this Rect r, params float[] widths)
		{
			return r.SliceHorizontalMixedPadded(0f, widths);
		}

		public static Rect[] SliceHorizontalMixedPadded(this Rect r, float padding, params float[] widths)
		{
			float absoluteWidth = padding * (widths.Length - 1);
			for(int i = 0; i < widths.Length; i++)
			{
				if(widths[i] <= 1f){ continue; }
				absoluteWidth += widths[i];
			}
			float remainder = r.width - absoluteWidth;
			if(remainder < 0) { remainder = 0f; }

			Rect[] rects = new Rect[widths.Length];
			float offset = 0f;
			for(int i = 0; i < widths.Length; i++)
			{
				rects[i] = new Rect(r.x + offset, r.y, widths[i] <= 1f ? widths[i] * remainder : widths[i], r.height);
				offset += rects[i].width + padding;
			}
			return rects;
		}
	}

	internal static class DragDropArea
	{
		public static void DoGUI<T>(Rect area, string label, System.Action<T> onDrop, System.Action onMouseUp) where T : Object
		{
			Event currentEvent = Event.current;
			GUI.Box(area, GUIContent.none, EditorStyles.helpBox);
			if (!area.Contains(currentEvent.mousePosition))
			{
				GUI.Box(area, new GUIContent("Drop assets here."), EditorStyles.centeredGreyMiniLabel);
				return;
			}
			if(DragAndDrop.objectReferences.Length == 0)  { return; }
			
			Color tc = GUI.backgroundColor;
			GUI.Box(area, GUIContent.none, (GUIStyle)"Icon.LockedBG");
			// GUI.Box(area, GUIContent.none, (GUIStyle)"TL SelectionButton PreDropGlow");
			GUI.backgroundColor = tc;

			if (onMouseUp != null && currentEvent.type == EventType.MouseUp) { onMouseUp(); }
			if (onDrop != null)
			{
				if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					if (currentEvent.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();
						foreach (var item in DragAndDrop.objectReferences)
						{
							onDrop(item as T);
						}
					}
					Event.current.Use();
				}
			}
		}
	}

	internal static class Helpers
	{
		/*
		Window utils derived from here: https://answers.unity.com/questions/960413/editor-window-how-to-center-a-window.html
		Author: https://answers.unity.com/users/6612/bunny83.html
		 */
		public static Rect GetMainWindowRect()
		{
			var containerWinType = GetDerivedTypes<ScriptableObject>(t => t.Name == "ContainerWindow").FirstOrDefault();
			if (containerWinType == null)
			{
				throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
			}
			var showModeField = containerWinType.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var positionProperty = containerWinType.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (showModeField == null || positionProperty == null)
			{
				throw new MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
			}
			var windows = Resources.FindObjectsOfTypeAll(containerWinType);
			foreach (var win in windows)
			{
				if (((int) showModeField.GetValue(win)) == 4) // main window
				{
					return (Rect) positionProperty.GetValue(win, null);
				}
			}
			throw new NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
		}
		public static void CenterOnMainWin(this UnityEditor.EditorWindow aWin)
		{
			var main = GetMainWindowRect();
			var pos = aWin.position;
			pos.x = main.x + ((main.width - pos.width) * 0.5f);
			pos.y = main.y + ((main.height - pos.height) * 0.5f);
			aWin.position = pos;
		}

		private static Type[] GetDerivedTypes<T>(this AppDomain ad)
		{
			var result = new List<Type>();
			var t = typeof(T);
			var assemblies = ad.GetAssemblies();
			for(int i = 0; i < assemblies.Length; i++)
			{
				var types = assemblies[i].GetTypes();
				for (int j = 0; j < types.Length; j++)
				{
					if (types[j].IsSubclassOf(t)) { result.Add(types[j]); }
				}
			}
			return result.ToArray();
		}
		private static IEnumerable<Type> GetDerivedTypes<T>(Func<Type, bool> p)
		{
			return AppDomain.CurrentDomain.GetDerivedTypes<T>().Where(p);
		}
	}
}
#endif