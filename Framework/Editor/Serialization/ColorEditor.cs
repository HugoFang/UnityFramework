using UnityEngine;
using UnityEditor;

namespace Framework
{
	namespace Serialization
	{
		[SerializedObjectEditor(typeof(Color), "PropertyField")]
		public static class ColorEditor
		{
			#region SerializedObjectEditor
			public static object PropertyField(object obj, GUIContent label, ref bool dataChanged, GUIStyle style, params GUILayoutOption[] options)
			{
				EditorGUI.BeginChangeCheck();
				obj = EditorGUILayout.ColorField(label, (Color)obj);
				if (EditorGUI.EndChangeCheck())
					dataChanged = true;

				return obj;
			}
			#endregion
		}
	}
}