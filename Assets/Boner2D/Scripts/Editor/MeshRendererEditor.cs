/*
The MIT License (MIT)

Copyright (c) 2017 -2018 Play-Em

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions)

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using UnityEngine; 
using UnityEditor;

namespace Boner2D {
	// Edit sorting order of Mesh Renderers in Animation Window
	[CustomEditor(typeof(MeshRenderer)), CanEditMultipleObjects] 
	public class MeshRendererEditor : Editor { 
		private int[] sortingLayerIds = null; 
		private GUIContent[] layerIDContents = null; 

		private void OnEnable() { 
			string[] sortingLayerNames = MeshRendererEditor.GetSortingLayerNames(); 

			this.layerIDContents = new GUIContent[sortingLayerNames.Length]; 

			for (int i = 0; i < sortingLayerNames.Length; ++i) {
				this.layerIDContents[i] = new GUIContent(sortingLayerNames[i]);
			}

			this.sortingLayerIds = MeshRendererEditor.GetSortingLayerUniqueIDs(); 
		} 

		public override void OnInspectorGUI() { 
			this.DrawDefaultInspector();

			SerializedProperty propSortingLayerID = this.serializedObject.FindProperty("m_SortingLayerID");

			SerializedProperty propSortingOrder = this.serializedObject.FindProperty("m_SortingOrder");

			EditorGUILayout.IntPopup(propSortingLayerID, this.layerIDContents, sortingLayerIds);

			EditorGUILayout.PropertyField(propSortingOrder);

			this.serializedObject.ApplyModifiedProperties(); 
		} 

		private static string[] GetSortingLayerNames() { 
			System.Type internalEditorUtilityType = typeof(UnityEditorInternal.InternalEditorUtility);

			System.Reflection.PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

			return sortingLayersProperty.GetValue(null, null) as string[]; 
		} 

		private static int[] GetSortingLayerUniqueIDs() { 
			System.Type internalEditorUtilityType = typeof(UnityEditorInternal.InternalEditorUtility); 

			System.Reflection.PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

			return sortingLayerUniqueIDsProperty.GetValue(null, null) as int[]; 
		} 
	}
}