/*
The MIT License (MIT)

Copyright (c) 2014 - 2018 Play-Em

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
	public class SortLayerMaterialEditor : EditorWindow {
		private GameObject o;
		private GameObject copyFrom;
		private GameObject copyTo;
		private GameObject copyPosFrom;
		private GameObject copyPosTo;

		private Renderer thisRenderer;
		private int addToLayer = 0;

		private bool includeChildrenForSorting = false;
		private bool includeChildrenForMaterials = false;

		[MenuItem("Boner2D/Sorting Layers And Materials")]
		protected static void ShowSortLayerMaterialEditor() {
			var wnd = GetWindow<SortLayerMaterialEditor>();
			wnd.titleContent.text = "Sort Layers and Create Materials";
			wnd.Show();
		}

		static void CreateMaterial (GameObject go) {
			// Create a simple material asset
			Renderer ren = go.GetComponent<Renderer>();

			if (ren != null) {
				Material material = new Material(ren.sharedMaterial);

				material.CopyPropertiesFromMaterial(ren.sharedMaterial);

				ren.sharedMaterial = material;

				MaterialPropertyBlock block = new MaterialPropertyBlock();

				ren.GetPropertyBlock(block);

				#if UNITY_EDITOR
				if (!Directory.Exists("Assets/Materials")) {
					AssetDatabase.CreateFolder("Assets", "Materials");
					AssetDatabase.Refresh();
				}

				string textureName = null;

				if (block.GetTexture(0).name != null) {
					textureName = block.GetTexture(0).name;
				} 
				else {
					textureName = material.mainTexture.name;
				}

				AssetDatabase.CreateAsset(material, "Assets/Materials/" + textureName + ".mat");

				Debug.Log("Created material " + textureName + " for " + go.name);
				#endif
			}
		}

		static void ResetButton () {
			#if UNITY_EDITOR
			if (Selection.activeGameObject != null) {
				GameObject o = Selection.activeGameObject;

				Renderer ren = o.GetComponent<Renderer>();
				if (ren != null) {
					ren.sortingLayerName = "Default";
					ren.sortingOrder = 0;
				}

				Transform[] children = o.GetComponentsInChildren<Transform>(true);

				foreach (Transform child in children) {
					Renderer childRenderer = child.gameObject.GetComponent<Renderer>();
					if (childRenderer != null) {
						childRenderer.sortingLayerName = "Default";
						childRenderer.sortingOrder = 0;
					}
				}
			}
			#endif
		}

		public void OnGUI() {
			GUILayout.Label("GameObject", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();

			if (Selection.activeGameObject != null) {
				o = Selection.activeGameObject;
			}

			EditorGUILayout.ObjectField(o, typeof(GameObject), true);
			EditorGUI.EndChangeCheck();

			if (o != null) {
				GUILayout.Label("Reset Sorting Layers ", EditorStyles.boldLabel);

				EditorGUILayout.Separator();

				if (GUILayout.Button("Reset Sorting Layers")) {
					#if UNITY_EDITOR
					ResetButton();
					#endif
				}

				EditorGUILayout.Separator();

				GUILayout.Label("Add to Sorting Layers ", EditorStyles.boldLabel);

				EditorGUILayout.Separator();

				addToLayer = EditorGUILayout.IntField("Add:", addToLayer);

				includeChildrenForSorting = EditorGUILayout.Toggle("Include Children", includeChildrenForSorting);

				EditorGUILayout.Separator();

				if (GUILayout.Button("Add to Sorting Layers")) {
					#if UNITY_EDITOR
					if (Selection.activeGameObject != null) {
						o = Selection.activeGameObject;

						thisRenderer = o.GetComponent<Renderer>();

						if (thisRenderer != null) {
							thisRenderer.sortingOrder = thisRenderer.sortingOrder + addToLayer;
						}

						if (includeChildrenForSorting) {
							Transform[] children = o.GetComponentsInChildren<Transform>(true);

							foreach (Transform child in children) {
								Renderer childRenderer = child.gameObject.GetComponent<Renderer>();

								if (childRenderer != null) {
									childRenderer.sortingOrder = childRenderer.sortingOrder + addToLayer;
								}
							}
						}
					}
					#endif
				}

				EditorGUILayout.Separator();

				GUILayout.Label("Create Materials from Renderer", EditorStyles.boldLabel);

				EditorGUILayout.Separator();

				includeChildrenForMaterials = EditorGUILayout.Toggle("Include Children", includeChildrenForMaterials);

				EditorGUILayout.Separator();

				if (GUILayout.Button("Create Material")) {
					#if UNITY_EDITOR
					if (Selection.activeGameObject != null) {
						o = Selection.activeGameObject;

						if (o.GetComponent<Renderer>()) {
							CreateMaterial(o);
						}

						if (includeChildrenForMaterials) {
							Transform[] children = o.GetComponentsInChildren<Transform>(true);

							foreach (Transform child in children) {
								if (child.gameObject.GetComponent<Renderer>() != null) {
									CreateMaterial(child.gameObject);
								}
							}
						}
					}
					#endif
				}

				EditorGUILayout.Separator();

				GUILayout.Label("Copy Sorting Layers Between Objects", EditorStyles.boldLabel);

				EditorGUILayout.Separator();

				EditorGUI.BeginChangeCheck();

				copyFrom = (GameObject)EditorGUILayout.ObjectField("Copy From: ", copyFrom, typeof(GameObject), true);

				EditorGUILayout.Separator();

				copyTo = (GameObject)EditorGUILayout.ObjectField("Copy To: ", copyTo, typeof(GameObject), true);

				EditorGUILayout.Separator();

				EditorGUI.EndChangeCheck();

				if (GUILayout.Button("Copy Sorting Layers")) {
					#if UNITY_EDITOR
					if (copyFrom != null && copyTo != null) {
						Transform[] copyFromChildren = copyFrom.GetComponentsInChildren<Transform>(true);

						Transform[] copyToChildren = copyTo.GetComponentsInChildren<Transform>(true);

						foreach (Transform copyFromChild in copyFromChildren) {
							Renderer copyFromChildRenderer = copyFromChild.gameObject.GetComponent<Renderer>();

							if (copyFromChildRenderer != null) {
								foreach (Transform copyToChild in copyToChildren) {
									Renderer copyToChildRenderer = copyToChild.gameObject.GetComponent<Renderer>();

									if (copyToChildRenderer != null && copyToChild.name == copyFromChild.name) {
										copyToChildRenderer.sortingOrder = copyFromChildRenderer.sortingOrder;
									}
								}
							}
						}
					}
					else {
						EditorGUILayout.HelpBox("No Gameobjects selected.", MessageType.Error);
					}
					#endif
				}

				EditorGUILayout.Separator();

				GUILayout.Label("Copy Positions and Rotations Between Objects", EditorStyles.boldLabel);

				EditorGUILayout.Separator();

				EditorGUI.BeginChangeCheck();

				copyPosFrom = (GameObject)EditorGUILayout.ObjectField("Copy From: ", copyPosFrom, typeof(GameObject), true);

				EditorGUILayout.Separator();

				copyPosTo = (GameObject)EditorGUILayout.ObjectField("Copy To: ", copyPosTo, typeof(GameObject), true);

				EditorGUI.EndChangeCheck();

				EditorGUILayout.Separator();

				if (GUILayout.Button("Copy Positions and Rotations")) {
					#if UNITY_EDITOR
					if (copyPosFrom != null && copyPosTo != null) {
						Transform[] copyFromChildren = copyPosFrom.GetComponentsInChildren<Transform>(true);

						Transform[] copyToChildren = copyPosTo.GetComponentsInChildren<Transform>(true);

						foreach (Transform copyFromChild in copyFromChildren) {
							foreach (Transform copyToChild in copyToChildren) {
								if (copyToChild.name == copyFromChild.name) {
									copyToChild.localPosition = new Vector3(copyFromChild.localPosition.x, copyFromChild.localPosition.y, copyFromChild.localPosition.z);

									copyToChild.localRotation = new Quaternion(copyFromChild.localRotation.x, copyFromChild.localRotation.y, copyFromChild.localRotation.z, copyFromChild.localRotation.w);
								}
							}
						}
					}
					else {
						EditorGUILayout.HelpBox("No Gameobjects selected.", MessageType.Error);
					}
					#endif
				}
			}
		}
	}
}
