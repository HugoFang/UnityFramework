using UnityEngine;
using UnityEditor;

using System;

namespace Framework
{
	namespace Serialization
	{
		public abstract class SerializedObjectEditorGUI<T> : ScriptableObject, ICustomEditorInspector, IComparable where T : class
		{
			#region Private Data
			private bool _dirty;
			private SerializedObjectEditor<T> _editor;
			private T _editableObject;

			[SerializeField]
			private string _undoObjectSerialized = null;
			#endregion

			#region Public Interfacce
			public SerializedObjectEditorGUI()
			{
				Undo.undoRedoPerformed += UndoRedoCallback;
			}

			~SerializedObjectEditorGUI()
			{
				Undo.undoRedoPerformed -= UndoRedoCallback;
			}

			public void Init(SerializedObjectEditor<T> editor, T obj)
			{
				_editor = editor;
				SetEditableObject(obj);
			}

			public SerializedObjectEditor<T> GetEditor()
			{
				return _editor;
			}

			public void SetEditableObject(T obj)
			{
				if (obj == null)
					throw new Exception();

				_editableObject = obj;
				OnSetObject();
			}

			public T GetEditableObject()
			{
				if(_editableObject == null)
					throw new Exception();

				return _editableObject;
			}

			public bool IsDirty()
			{
				return _dirty;
			}

			public void MarkAsDirty(bool dirty)
			{
				_dirty = dirty;

				if (_dirty)
					EditorUtility.SetDirty(this);
			}

			public bool IsValid()
			{
				return _editableObject != null && _editor != null;
			}

			public void CacheUndoStatePreChanges()
			{
				_undoObjectSerialized = Serializer.ToString(_editableObject);
			}

			public void SaveUndoStatePostChanges()
			{
				_undoObjectSerialized = Serializer.ToString(_editableObject);
			}

			public void RenderProperties()
			{
				if (_editableObject == null)
					throw new Exception();

				//If store an undo command on a temp string representing event, then on undo performed callback recreate event from string.
				string undoObjectSerialized = Serializer.ToString(_editableObject);
				
				if (RenderObjectProperties(GUIContent.none))
				{
					_undoObjectSerialized = undoObjectSerialized;
					Undo.RegisterCompleteObjectUndo(this, GetEditableObject().GetType().Name + " changed");
					SaveUndoStatePostChanges();

					GetEditor().SetNeedsRepaint();
					MarkAsDirty(true);
				}
			}
			#endregion

			#region Virtual Interface
			public abstract Rect GetBounds();

			public abstract Vector2 GetPosition();

			public abstract void SetPosition(Vector2 position);

			protected abstract void OnSetObject();
			#endregion

			#region ICustomEditable
			public virtual bool RenderObjectProperties(GUIContent label)
			{
				bool dataChanged = false;
				_editableObject = SerializationEditorGUILayout.ObjectField(_editableObject, label, ref dataChanged);
				return dataChanged;
			}
			#endregion

			#region IComparable
			public virtual int CompareTo(object obj)
			{
				SerializedObjectEditorGUI<T> editorGUI = obj as SerializedObjectEditorGUI<T>;

				if (editorGUI == null)
					return 1;

				if (editorGUI == this)
					return 0;

				return this.GetHashCode().CompareTo(editorGUI.GetHashCode());
			}
			#endregion
			
			#region Private Functions
			private void UndoRedoCallback()
			{
				if (this != null)
				{
					if (!string.IsNullOrEmpty(_undoObjectSerialized) && _editableObject != null)
					{
						string undoObjectSerialized = Serializer.ToString(_editableObject);

						if (_undoObjectSerialized != undoObjectSerialized)
						{
							SetEditableObject((T)Serializer.FromString(_editableObject.GetType(), _undoObjectSerialized));
							GetEditor().SetNeedsRepaint();
							MarkAsDirty(true);
						}
					}
				}
			}
			#endregion
		}
	}
}
