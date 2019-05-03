#if UNITY_EDITOR
namespace SmartAssets.Editor
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using ReorderableList = UnityEditorInternal.ReorderableList;
	using Object = UnityEngine.Object;
	using CallbackFunction = UnityEditor.EditorApplication.CallbackFunction;

	[CustomEditor(typeof(AssetGroup))]
	internal class Editor_AssetGroup : Editor
	{
		public override void OnInspectorGUI()
		{
			if(_simpleBox == null)
			{
				_simpleBox = new GUIStyle(EditorStyles.helpBox);
				_simpleBox.normal.background = null;
				_simpleBox.active.background = null;
				_simpleBox.padding.left += 4;
			}

			if (_labelStyle == null)
			{
				_labelStyle = new GUIStyle(EditorStyles.label);
				_labelStyle.richText = true;
				_labelStyle.alignment = TextAnchor.MiddleLeft;
			}

			_displayList.index = Mathf.Clamp(_displayList.index, 0, _displayList.count - 1);
			// asset list
			_displayList.DoLayoutList();
			// drag/drop asset area
			OnDragDropGUI();

			EditorGUILayout.Space();
			_displayList.index = Mathf.Clamp(_displayList.index, 0, _assetList.Count - 1);
			if(_displayList.index >= 0 && _displayList.index < _assetList.Count)
			{
				OnObjectInspector(_assetList[_displayList.index]);
			}
		}

		protected override bool ShouldHideOpenButton() { return true; }

		private ReorderableList _displayList = null;
		private List<Object> _assetList = null;
		private Editor _editor = null;
		private GenericMenu _addMenu = null;
		private GUIStyle _labelStyle = null;
		private GUIStyle _simpleBox = null;
		private int _lastDeleted = -1;

		private void OnEnable()
		{
			if(_assetList == null) { _assetList = target.GetSubAssets(); }
			_displayList = new ReorderableList(_assetList, typeof(Object), false, false, false, false);
		
			_displayList.footerHeight = 0f;
			_displayList.drawHeaderCallback = DrawListHeader;
			_displayList.onRemoveCallback += l => RemoveAsset(_displayList.index);
			_displayList.elementHeight = EditorGUIUtility.singleLineHeight * 1.5f;
			_displayList.drawElementCallback = DrawListItem;


			if(_addMenu == null)
			{
				_addMenu = new GenericMenu();
				var createInfos = AssetGroupOption.FindMethods().OrderBy(m => m.menuName).ToArray();
				for (int i = 0; i < createInfos.Length;i++)
				{
					var info = createInfos[i];
					_addMenu.AddItem(new GUIContent(info.menuName), false, () => AddAsset(info.Instantiate));
				}
				Undo.undoRedoPerformed += OnUndo;
			}
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndo;
		}

		private void OnUndo()
		{
			var subassets = target.GetSubAssets();
			if(subassets.Count != _assetList.Count)
			{
				_assetList = subassets;
				target.Import();
				OnDisable();
				OnEnable();

				if(_lastDeleted >= 0 && _lastDeleted < subassets.Count) { _displayList.index = _lastDeleted; } 
				
			}
			if(_editor && _editor.target)
			{
				_editor.serializedObject.Update(); _editor.Repaint();
			}
		}

		private void DrawListHeader(Rect r)
		{
			Rect[] rects = r.SliceHorizontalMixed(1f, 70f, 70f);

			if(GUI.Button(rects[1], "Browse...", EditorStyles.miniButton))
			{
				OpenBrowser();
			}
			if(GUI.Button(rects[2], "Add New ▼", EditorStyles.miniButtonRight))
			{
				_addMenu.ShowAsContext();
			}
		}

		private void DrawListItem(Rect r, int i, bool a, bool f)
		{
			if (i >= _assetList.Count || !_assetList[i]) { return; }
			Rect rec = r.SetHeight(EditorGUIUtility.singleLineHeight);
			Rect[] recs = rec.SliceHorizontalMixed(20f, 0.6f, 0.4f);
			EditorGUI.LabelField(recs[0], new GUIContent(AssetPreview.GetMiniThumbnail(_assetList[i])));
			EditorGUI.LabelField(recs[1], _assetList[i].name, _labelStyle);
			EditorGUI.LabelField(recs[2], _assetList[i].GetType().Name, _labelStyle);
			if(f && RightClicked(r))
			{
				GetOptionsMenu(_assetList[i], () => RemoveAsset(i)).ShowAsContext();
				Event.current.Use();
			}
		}

		private void OnDragDropGUI()
		{
			var area = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f));
			var e = Event.current;
			
			if(e.type == EventType.MouseDrag)
			{
				if(area.Contains(e.mousePosition))
				{
					EditorGUI.DrawRect(area.Resize(5f, 5f), Color.blue);
				}
			}
			
			DragDropArea.DoGUI(area, "Drop", (Object o) => {
				
				AddAsset(() => o.Duplicate(), false);
			}, () =>
			{
				Repaint();
			});
		}

		private void OpenBrowser()
		{
			ObjectSelectDialog.Open("Select", null, o =>
			{
				AddAsset(() => o.Duplicate(), false);
			});
		}

		private void OnObjectInspector(Object o)
		{
			if (!o) { return; }
			if (_editor && _editor.target != o) { DestroyImmediate(_editor); _editor = null; }
			if (!_editor) { _editor = CreateEditor(o); }
			if (!_editor) { return; }
			_editor.DrawHeader();

			EditorGUILayout.BeginVertical(_simpleBox);
			_editor.OnInspectorGUI();
			if(GUI.changed)
			{
				_editor.serializedObject.ApplyModifiedProperties();
				_editor.serializedObject.Update();
			}
			EditorGUILayout.EndVertical();
		}		

		private void AddAsset(Func<Object> fn, bool isNew = true)
		{
			var ass = target.AddAsset(fn, isNew);
			if (!ass) { return; }
			_assetList.Add(ass);
			_assetList.SortByTypeFirst();
			var index = _assetList.IndexOf(ass);
			_displayList.index = index;
		}

		private void RemoveAsset(int index)
		{
			if (index < 0) { return; }
			var o = _assetList[index];
			_assetList.RemoveAt(index);
			_displayList.list = _assetList;
			Undo.DestroyObjectImmediate(o);
			target.Import();
			_lastDeleted = index;
		}

		private void RenameAsset(Object o, string name)
		{
			o.Rename(name);
			_assetList.SortByTypeFirst();
		}

		private GenericMenu GetOptionsMenu(Object o, CallbackFunction onRemove)
		{
			GenericMenu settingsMenu = new GenericMenu();
			settingsMenu.AddItem(new GUIContent("Rename"), false, () => TextInputDialog.Open("Rename Asset", o.name, t => RenameAsset(o, t)));
			settingsMenu.AddSeparator("");
			settingsMenu.AddItem(new GUIContent("Duplicate"), false, () => AddAsset(() => o.Duplicate(), false));
			settingsMenu.AddSeparator("");
			settingsMenu.AddItem(new GUIContent("Delete"), false, () => ConfirmDialog.Open("Delete", "Delete Asset?", onRemove));
			return settingsMenu;
		}
		
		private static bool RightClicked(Rect r)
		{
			if(Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				return r.Contains(Event.current.mousePosition);
			}
			return false;
		}
	}
}
#endif