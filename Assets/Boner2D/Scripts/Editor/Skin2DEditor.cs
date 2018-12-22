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
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif
using System.Collections;
using System.Collections.Generic;

namespace Boner2D {
	[CustomEditor(typeof(Skin2D))]
	public class Skin2DEditor : Editor {
		private Skin2D skin;
		private SkinnedMeshRenderer skinnedMeshRenderer;
		private Mesh skinnedMesh;

		private float baseSelectDistance = 0.1f;
		private float changedBaseSelectDistance = 0.1f;
		private int selectedIndex = -1;
		private Color handleColor = Color.green;

		// Used for drawing control points
		private Mesh bakeMesh;
		private List<Vector3> vertices = new List<Vector3>();
		private List<Vector3> newVertices = new List<Vector3>();

		private Matrix4x4[] boneMatrices;
		private BoneWeight[] weights;
		private Matrix4x4[] bindposes;
		private Vector3[] meshVerts;
		private Vector3[] meshNormals;
		private Transform[] bones;
		private Matrix4x4 vertexMatrix;
		private BoneWeight weight;
		private Vector3 newVert;
		private Vector3 offset;

		private Event e;

		void OnEnable() {
			skin = (Skin2D)target;
			skinnedMeshRenderer = skin.GetComponent<SkinnedMeshRenderer>();
			skinnedMesh = skinnedMeshRenderer.sharedMesh;
		}

		public override void OnInspectorGUI() {
			DrawDefaultInspector();

			EditorGUILayout.Separator();

			if (GUILayout.Button("Toggle Mesh Outline")) {
				Skin2D.showMeshOutline = !Skin2D.showMeshOutline;
			}

			EditorGUILayout.Separator();

			if (skin.skinnedMeshRenderer.sharedMesh != null && GUILayout.Button("Save as Prefab")) {
				skin.SaveAsPrefab();
			}

			EditorGUILayout.Separator();

			if (skin.skinnedMeshRenderer && GUILayout.Button("Recalculate Bone Weights")) {
				skin.RecalculateBoneWeights();
			}

			EditorGUILayout.Separator();

			GUILayout.Label("Control Points", EditorStyles.boldLabel);

			EditorGUILayout.Separator();

			handleColor = EditorGUILayout.ColorField("Handle Color", handleColor);

			changedBaseSelectDistance = EditorGUILayout.Slider("Handle Size", baseSelectDistance, 0, 1);

			EditorGUILayout.Separator();

			if(baseSelectDistance != changedBaseSelectDistance) {
				baseSelectDistance = changedBaseSelectDistance;
				EditorUtility.SetDirty(this);
				SceneView.RepaintAll();
			}

			if (skin.skinnedMeshRenderer.sharedMesh != null && GUILayout.Button("Create Control Points")) {
				skin.CreateControlPoints(skin.skinnedMeshRenderer);
			}

			EditorGUILayout.Separator();

			if (skin.skinnedMeshRenderer.sharedMesh != null && GUILayout.Button("Reset Control Points")) {
				skin.ResetControlPointPositions();
			}

			EditorGUILayout.Separator();

			if (skin.points != null && skin.controlPoints != null && skin.controlPoints.Length > 0 
			&& selectedIndex != -1 && GUILayout.Button("Reset Selected Control Point")) {
				skin.controlPoints[selectedIndex].ResetPosition();
				skin.points.SetPoint(skin.controlPoints[selectedIndex]);
			}

			EditorGUILayout.Separator();

			if (GUILayout.Button("Remove Control Points")) {
				skin.RemoveControlPoints();
			}

			EditorGUILayout.Separator();

			GUILayout.Label("Generate Assets", EditorStyles.boldLabel);

			EditorGUILayout.Separator();

			if (skin.skinnedMeshRenderer.sharedMesh != null && GUILayout.Button("Generate Mesh Asset")) {
				#if UNITY_EDITOR
				// Check if the Meshes directory exists, if not, create it.
				if(!Directory.Exists("Assets/Meshes")) {
					AssetDatabase.CreateFolder("Assets", "Meshes");
					AssetDatabase.Refresh();
				}
				Mesh mesh = skin.skinnedMeshRenderer.sharedMesh.Clone();
				mesh.name = skin.skinnedMeshRenderer.sharedMesh.name.Replace(".SkinnedMesh", ".Mesh");
				ScriptableObjectUtility.CreateAsset(mesh, "Meshes/" + skin.gameObject.name + ".Mesh");
				#endif
			}

			EditorGUILayout.Separator();

			if (skin.skinnedMeshRenderer.sharedMaterial != null && GUILayout.Button("Generate Material Asset")) {
				#if UNITY_EDITOR
				Material material = new Material(skin.skinnedMeshRenderer.sharedMaterial);
				material.CopyPropertiesFromMaterial(skin.skinnedMeshRenderer.sharedMaterial);
				skin.skinnedMeshRenderer.sharedMaterial = material;
				if(!Directory.Exists("Assets/Materials")) {
					AssetDatabase.CreateFolder("Assets", "Materials");
					AssetDatabase.Refresh();
				}
				AssetDatabase.CreateAsset(material, "Assets/Materials/" + material.mainTexture.name + ".mat");
				Debug.Log("Created material " + material.mainTexture.name + " for " + skin.gameObject.name);
				#endif
			}
		}

		// This is for editing control points
		void OnSceneGUI() {
			if (Weightpainter.isPainting) {
				if (skin != null) {
					skin.editingPoints = false;
				}
				return;
			}

			// If we have a skin and control points then update them
			if (skin != null && skinnedMeshRenderer != null && skinnedMesh != null 
			&& skin.controlPoints != null && skin.controlPoints.Length > 0 && skin.points != null) {
				e = Event.current;

				EditorGUI.BeginChangeCheck();

				Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);
				Vector2 mousePos = r.origin;

				float selectDistance = HandleUtility.GetHandleSize(mousePos) * baseSelectDistance;
				if (e.type == EventType.MouseDrag && e.button == 0 && e.isMouse) {
					skin.editingPoints = true;
				}
				else if (e.type == EventType.MouseUp || vertices.Count != skinnedMeshRenderer.sharedMesh.vertexCount) {
					skin.editingPoints = false;
				}

				#region Draw vertex handles
				Handles.color = handleColor;

				// Create the vertices here for the handle points
				if (!skin.editingPoints) {
					bakeMesh = new Mesh();
					skinnedMeshRenderer.BakeMesh(bakeMesh);

					// Always clear vertices before getting new ones
					vertices.Clear();
					bakeMesh.GetVertices(vertices);

					skinnedMesh = skinnedMeshRenderer.sharedMesh;

					boneMatrices = new Matrix4x4[skinnedMeshRenderer.bones.Length];
					weights = skinnedMesh.boneWeights;
					bindposes = skinnedMesh.bindposes;
					bones = skinnedMeshRenderer.bones;

					// First apply the scale, then transform it to World Space
					for (int i = 0; i < bakeMesh.vertexCount; i++) {
						vertices[i] = Vector3.Scale(bakeMesh.vertices[i], skinnedMeshRenderer.transform.lossyScale);
						vertices[i] = skinnedMeshRenderer.transform.TransformPoint(vertices[i]);
					}

					// Always clear vertices before getting new ones
					newVertices.Clear();
					skinnedMeshRenderer.sharedMesh.GetVertices(newVertices);
				}

				for (int i = 0; i < skin.controlPoints.Length; i++) {
					if (Handles.Button(vertices[i], Quaternion.identity, selectDistance, selectDistance, Handles.CircleHandleCap)) {
						selectedIndex = i;
					}

					if (selectedIndex == i) {
						EditorGUI.BeginChangeCheck();

						// If we are editing points then the position handle drives the vertex
						if (skin.editingPoints) {
							vertices[i] = Handles.DoPositionHandle(vertices[i], Quaternion.identity);

							// Need to create matrices based on the skin's bones
							for (int b = 0; b < boneMatrices.Length; b++) {
								if (bones[b] != null) {
									boneMatrices[b] = bones[b].localToWorldMatrix * bindposes[b];
								}
							}

							weight = weights[i];

							vertexMatrix = new Matrix4x4();

							// Since we are only using 2 bones for the Skin2D, only use the first 2 bones and weights
							for (int n = 0; n < 16; n++) {
								vertexMatrix[n] =
									boneMatrices[weight.boneIndex0][n] * weight.weight0 +
									boneMatrices[weight.boneIndex1][n] * weight.weight1;
							}

							// DEBUG HERE TO CHECK FOR DISCREPANCIES //
							/*newVert = vertexMatrix.MultiplyPoint(skinnedMesh.vertices[i]);

							Debug.Log("New Vertex: " + newVert.x + ", " + newVert.y + ", " + newVert.z);
							Debug.Log("Original Vertex: " + vertices[i].x + ", " + vertices[i].y + ", " + vertices[i].z);

							Handles.DotHandleCap(
								0,
								newVert,
								Quaternion.identity,
								selectDistance,
								EventType.Repaint
								);*/

							// Invert the matrix to get the local space position of the vertex
							newVert = vertexMatrix.inverse.MultiplyPoint(vertices[i]);

							skin.controlPoints[i].position = newVert;

							if (EditorGUI.EndChangeCheck()) {
								skin.points.SetPoint(skin.controlPoints[i]);
								newVertices[i] = skin.controlPoints[i].position;

								Undo.RecordObject(skin, "Changed Control Point");
								Undo.RecordObject(skin.points, "Changed Control Point");

								EditorUtility.SetDirty(this);
							}
						}
						else {
							// If we are not editing points then just use the world space offset for the position handle
							offset = vertices[i] - skin.transform.TransformPoint(skin.points.GetPoint(skin.controlPoints[i]));

							vertices[i] = Handles.DoPositionHandle(skin.transform.TransformPoint(skin.points.GetPoint(skin.controlPoints[i])) + offset, Quaternion.identity);
						}
					}
				}

				if (skin.editingPoints) {
					skinnedMeshRenderer.sharedMesh.SetVertices(newVertices);
				}
				#endregion
			}

			skin.editingPoints = false;
		}
	}
}
