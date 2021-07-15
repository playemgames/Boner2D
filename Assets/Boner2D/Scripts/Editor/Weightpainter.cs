/*
The MIT License (MIT)

Copyright (c) 2013 - 2021 Banbury & Play-Em

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
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Boner2D {
	public enum PaintingMode {
		Add, Subtract
	}

	[InitializeOnLoad]
	public class Weightpainter : EditorWindow {
		public SkinnedMeshRenderer skin;
		static public bool isPainting = false;
		private bool isDrawing = false;
		private bool wasDrawing = false;
		private bool recalculateMesh = false;
		private float brushSize = 0.5f;
		private float weight = 1.0f;
		private float colorTransparency = 1.0f;
		private PaintingMode mode = PaintingMode.Add;
		private int boneIndex = 0;
		private string[] boneNames;

		private List<Vector3> vertices = new List<Vector3>();
		private Mesh mesh;
		private Bone[] bones;

		private Vector3 mpos;
		private Vector3 v;
		private float w;
		private float d;

		private BoneWeight bw;
		private float vw;

		private Color[] colors;
		private float value;

		private Material mat;

		[MenuItem("Boner2D/Weight painting")]
		protected static void ShowWeightpainterWindow() {
			var wnd = GetWindow<Weightpainter>();
			wnd.titleContent.text = "Weight painting";

			if (Selection.activeGameObject != null) {
				SkinnedMeshRenderer skin = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();

				if (skin != null) {
					wnd.skin = skin;
				}
			}

			SceneView.duringSceneGui += wnd.OnSceneGUI;
			wnd.Show();
		}

		public void OnDestroy() {
			isPainting = false;
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		public void OnGUI() {
			EditorGUI.BeginChangeCheck();

			skin = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skin", skin, typeof(SkinnedMeshRenderer), true);

			if (EditorGUI.EndChangeCheck() || skin != null && bones == null) {
				if (skin != null) {
					bones = new Bone[skin.bones.Length];

					for (int i = 0; i < skin.bones.Length; i++) {
						bones[i] = skin.bones[i].GetComponent<Bone>();
					}
				}
				else {
					if (bones != null) {
						bones = null;
					}
				}
			}

			if (skin != null && skin.bones.Length > 0) {
				GUI.color = (isPainting) ? Color.green : Color.white;

				if (GUILayout.Button("Paint")) {
					isPainting = !isPainting;

					if (isPainting) {
						Selection.objects = new GameObject[] { skin.gameObject };
					}
					else {
						// Reset the mesh
						mesh = null;

						// Save the asset when finished painting
						if (wasDrawing && !isPainting) {
							EditorUtility.SetDirty(skin.gameObject);

							if (PrefabUtility.GetPrefabType(skin.gameObject) != PrefabType.None) {
								AssetDatabase.SaveAssets();
							}

							wasDrawing = false;
							recalculateMesh = true;
						}
					}

					if (SceneView.currentDrawingSceneView != null) {
						SceneView.currentDrawingSceneView.Repaint();
					}
				}

				GUI.color = Color.white;

				brushSize = EditorGUILayout.FloatField("Brush size", brushSize * 2) / 2;

				weight = Mathf.Clamp(EditorGUILayout.FloatField("Weight", weight), 0, 1);

				mode = (PaintingMode)EditorGUILayout.EnumPopup("Mode", mode);

				if (bones != null) {
					boneNames = bones.Select(b => b.gameObject.name).ToArray();

					boneIndex = EditorGUILayout.Popup("Bone", boneIndex, boneNames);
				}

				colorTransparency = Mathf.Clamp(EditorGUILayout.FloatField("Color Transparency", colorTransparency), 0, 1);

			} 
			else {
				EditorGUILayout.HelpBox("SkinnedMeshRenderer not assigned to any bones, Recalculate Bone Weights.", MessageType.Error);

				if (SceneView.currentDrawingSceneView != null) {
					SceneView.currentDrawingSceneView.Repaint();
				}
			}
		}

		public void OnSceneGUI(SceneView sceneView) {
			if (skin != null && isPainting) {
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

				if (mesh == null || recalculateMesh) {
					mesh = new Mesh();
					skin.BakeMesh(mesh);

					mesh.normals = skin.sharedMesh.normals;
					mesh.bindposes = skin.sharedMesh.bindposes;
					mesh.bounds = skin.sharedMesh.bounds;
					mesh.uv = skin.sharedMesh.uv;
					mesh.uv2 = skin.sharedMesh.uv2;
					mesh.boneWeights = skin.sharedMesh.boneWeights;
					mesh.tangents = skin.sharedMesh.tangents;

					mesh.GetVertices(vertices);

					for (int i = 0; i < mesh.vertexCount; i++) {
						vertices[i] = new Vector3(
							vertices[i].x * skin.transform.lossyScale.x, 
							vertices[i].y * skin.transform.lossyScale.y, 
							vertices[i].z * skin.transform.lossyScale.z);
					}

					mesh.SetVertices(vertices);

					recalculateMesh = false;
				}
				else {
					mesh.boneWeights = skin.sharedMesh.boneWeights;
				}

				CalculateVertexColors(skin.bones, bones[boneIndex]);

				List<BoneWeight> weights = mesh.boneWeights.ToList();

				Event current = Event.current;

				if (mat == null) {
					mat = new Material(Shader.Find("Lines/Colored Blended"));
				}

				mat.SetPass(0);

				Graphics.DrawMeshNow(mesh, skin.transform.localToWorldMatrix);

				for (int b = 0; b < bones.Length; b++) {
					if (bones[b] == bones[boneIndex]) {
						Handles.color = Color.yellow;
					}
					else {
						Handles.color = Color.gray;
					}

					Handles.DrawLine(bones[b].transform.position, bones[b].Head);
				}

				Handles.color = Color.red;

				mpos = HandleUtility.GUIPointToWorldRay(current.mousePosition).origin;

				mpos = new Vector3(mpos.x, mpos.y);

				Handles.DrawWireDisc(mpos, Vector3.forward, brushSize);

				if (isPainting) {
					if (current.type == EventType.ScrollWheel && current.modifiers == EventModifiers.Control) {
						brushSize = Mathf.Clamp(brushSize + (float)System.Math.Round(current.delta.y / 30, 2), 0, float.MaxValue);

						Repaint();

						current.Use();
					} 
					else if (current.type == EventType.MouseDown && current.button == 0) {
						isDrawing = true;
						wasDrawing = false;
					} 
					else if (current.type == EventType.MouseUp && current.button == 0) {
						if (isDrawing) {
							wasDrawing = true;
							recalculateMesh = true;
						}

						isDrawing = false;
					} 
					else if (current.type == EventType.MouseDrag && isDrawing && current.button == 0) {
						w = weight * ((mode == PaintingMode.Subtract) ? -1 : 1);

						for (int i = 0; i < mesh.vertices.Length; i++) {
							v = mesh.vertices[i];

							d = (v - skin.gameObject.transform.InverseTransformPoint(mpos)).magnitude;

							if (d <= brushSize) {
								bw = weights[i];
								vw = bw.GetWeight(boneIndex);
								vw = Mathf.Clamp(vw + (1 - d / brushSize) * w, 0, 1);
								bw = bw.SetWeight(boneIndex, vw);
								weights[i] = bw.Clone();
							}
						}

						skin.sharedMesh.boneWeights = weights.ToArray();

						#if UNITY_2019_1_OR_NEWER
						// Force vertices to only have 2 bone influences and use new BoneWeight1 since SkinQuality is ignored in builds
						skin.sharedMesh.ConvertToBoneWeight1();
						#endif
					}
				}

				sceneView.Repaint();
			}
		}

		private void CalculateVertexColors(Transform[] bones, Bone bone) { 
			if (mesh == null) { 
				return;
			}

			colors = new Color[mesh.vertexCount];

			for (int i = 0; i < colors.Length; i++) {
				colors[i] = Color.black;
			}

			if (bones.Any(b => b.gameObject.GetInstanceID() == bone.gameObject.GetInstanceID())) { 

				#if UNITY_2019_1_OR_NEWER
				// Get all the bone weights, in vertex index order
				var boneWeights = mesh.GetAllBoneWeights();

				// Keep track of where we are in the array of BoneWeights, as we iterate over the vertices
				var boneWeightIndex = 0;

				var bonesPerVertex = mesh.GetBonesPerVertex();
				#endif

				for (int i = 0; i < colors.Length; i++) { 
					value = 0;

					#if UNITY_2019_1_OR_NEWER
					var numberOfBonesForThisVertex = bonesPerVertex[i];

					// For each vertex, iterate over its BoneWeights
					for (var v = 0; v < numberOfBonesForThisVertex; v++) { 
						if (boneWeights[boneWeightIndex].boneIndex == boneIndex) {
							value = boneWeights[boneWeightIndex].weight;
						}

						boneWeightIndex++;
					}
					#else
					if (mesh.boneWeights[i].boneIndex0 == boneIndex) {
						value = mesh.boneWeights[i].weight0;
					}
					else if (mesh.boneWeights[i].boneIndex1 == boneIndex) {
						value = mesh.boneWeights[i].weight1;
					}
					else if (mesh.boneWeights[i].boneIndex2 == boneIndex) {
						value = mesh.boneWeights[i].weight2;
					}
					else if (mesh.boneWeights[i].boneIndex3 == boneIndex) {
						value = mesh.boneWeights[i].weight3;
					}
					#endif

					Util.HSBColor hsbColor = new Util.HSBColor(0.7f - value, 1.0f, 0.5f);
					hsbColor.a = colorTransparency;
					colors[i] = Util.HSBColor.ToColor(hsbColor);
				}
			}

			mesh.colors = colors;
		}
	}
}
