/*
The MIT License (MIT)

Copyright (c) 2014 - 2018 Banbury & Play-Em

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

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
using System.Collections;

namespace Boner2D {
	[CustomEditor(typeof(Bone))]
	public class BoneEditor : Editor {
		private Bone bone;

		private float length;

		void OnEnable() {
			bone = (Bone)target;
		}

		public override void OnInspectorGUI() {
			DrawDefaultInspector();

			EditorGUILayout.Separator();

			if (bone.editMode) {
				EditorGUILayout.HelpBox("Edit Mode enabled in Skeleton.", MessageType.Info);
			}

			EditorGUILayout.Separator();

			if (GUILayout.Button("FlipY") && !bone.editMode) {
				bone.flipY = !bone.flipY;
			}
			else if (bone.editMode) {
				EditorGUILayout.HelpBox("Need to uncheck Edit in Skeleton.", MessageType.Error);
			}

			EditorGUILayout.Separator();

			if (GUILayout.Button("FlipX") && !bone.editMode) {
				bone.flipX = !bone.flipX;
			}
			else if (bone.editMode){
				EditorGUILayout.HelpBox("Need to uncheck Edit in Skeleton.", MessageType.Error);
			}

			EditorGUILayout.Separator();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Add child")) {
				Bone.Create();
			}

			if (GUILayout.Button("Split")) {
				Bone.Split();
			}

			if (GUILayout.Button("Add IK")) {
				bone.AddIK();
			}

			GUILayout.EndHorizontal();
		}

		void OnSceneGUI() {
			Handles.color = Color.green;

			if (bone.editMode) {
				Event current = Event.current;

				if (bone.enabled && !current.control) {
					EditorGUI.BeginChangeCheck();

					Vector3 v = Handles.FreeMoveHandle(bone.Head, Quaternion.identity, 0.1f, Vector3.zero, Handles.RectangleHandleCap);

					Undo.RecordObject(bone.transform, "Change bone transform");
					Undo.RecordObject(bone, "Change bone");

					bone.length = Vector2.Distance(v, bone.transform.position);
					bone.transform.up = (v - bone.transform.position).normalized;

					if (EditorGUI.EndChangeCheck()) {
						EditorUtility.SetDirty(bone);
					}
				}

				int controlID = GUIUtility.GetControlID(FocusType.Passive);

				if (current.control) {
					switch (current.GetTypeForControl(controlID)) {
						case EventType.MouseDown:
							current.Use();
							break;
						case EventType.MouseUp:
							Undo.FlushUndoRecordObjects();
							Bone b = Bone.Create();
							Selection.activeGameObject = b.gameObject;

							Vector3 p = HandleUtility.GUIPointToWorldRay(current.mousePosition).origin;
							p = new Vector3(p.x, p.y);
							b.length = Vector3.Distance(p, bone.Head);
							b.transform.up = p - (Vector3)bone.Head;

							Event.current.Use();
							break;
						case EventType.Layout:
							HandleUtility.AddDefaultControl(controlID);
							break;
					}
				}

				if (Event.current.control && Event.current.type == EventType.MouseDown) {
				}
			}
			else {
				if (bone.parent != null 
				&& (bone.ik == null 
				|| bone.ik != null && !bone.ik.enabled 
				|| bone.ik != null && bone.ik.influence == 0) 
				&& bone.snapToParent) {

					length = Vector3.Distance(bone.parent.transform.position, bone.transform.position);

					if (Mathf.Abs(bone.parent.length - length) > 0.0001) {
						bone.transform.parent = null;
						bone.parent.transform.parent.up = (bone.transform.position - bone.parent.transform.position).normalized;
						bone.parent.length = length;
						bone.transform.parent = bone.parent.transform;
					}
				}
			}
		}
	}
}
