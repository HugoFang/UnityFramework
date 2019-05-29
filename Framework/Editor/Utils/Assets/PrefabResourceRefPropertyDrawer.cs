using UnityEngine;
using UnityEditor;

namespace Framework
{
	namespace Utils
	{
		[CustomPropertyDrawer(typeof(PrefabResourceRef))]
		public class PrefabResourceRefPropertyDrawer : PropertyDrawer
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				SerializedProperty filePath = property.FindPropertyRelative("_filePath");
				SerializedProperty fileGUID = property.FindPropertyRelative("_fileGUID");
				
				GameObject prefabAsset = null;

				if (!string.IsNullOrEmpty(fileGUID.stringValue))
				{
					string filepath = AssetDatabase.GUIDToAssetPath(fileGUID.stringValue);
					prefabAsset = AssetDatabase.LoadAssetAtPath(filepath, typeof(GameObject)) as GameObject;
					if (prefabAsset != null)
						filePath.stringValue = filepath;
				}

				if (prefabAsset == null && !string.IsNullOrEmpty(filePath.stringValue))
				{
					prefabAsset = AssetDatabase.LoadAssetAtPath(filePath.stringValue, typeof(GameObject)) as GameObject;
					if (prefabAsset != null)
						fileGUID.stringValue = AssetDatabase.AssetPathToGUID(filePath.stringValue);
				}

				EditorGUI.BeginChangeCheck();

				Rect objectFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
				prefabAsset = EditorGUI.ObjectField(objectFieldRect, label, prefabAsset, typeof(GameObject), false) as GameObject;

				if (EditorGUI.EndChangeCheck())
				{
					if (prefabAsset != null)
					{
						filePath.stringValue = AssetDatabase.GetAssetPath(prefabAsset);
						fileGUID.stringValue = AssetDatabase.AssetPathToGUID(filePath.stringValue);
					}
					else
					{
						filePath.stringValue = null;
						fileGUID.stringValue = null;
					}
				}
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return EditorGUIUtility.singleLineHeight;
			}
		}
	}
}