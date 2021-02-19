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
using System.Linq;

namespace Boner2D {
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(SkinnedMeshRenderer))]
	[ExecuteInEditMode]
	public class Skin2D : MonoBehaviour {

		// The sprite reference for the Skin2D
		public Sprite sprite;

		#if UNITY_EDITOR
		// The bone weights assigned to the Skin2D
		[HideInInspector]
		public Bone2DWeights boneWeights;
		#endif

		// Material for generating mesh lines for debugging
		private Material lineMaterial;

		private Material LineMaterial {
			get {
				if (lineMaterial == null) {
					lineMaterial = new Material(Shader.Find("Lines/Colored Blended"));
					lineMaterial.hideFlags = HideFlags.HideAndDontSave;
					lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
				}
				return lineMaterial;
			}
		}


		// The MeshFilter of the Skin2D
		private MeshFilter _meshFilter;
		public MeshFilter meshFilter {
			get {
				if (_meshFilter == null) {
					_meshFilter = GetComponent<MeshFilter>();
				}
				return _meshFilter;
			}
		}

		// SkinnedMeshRenderer of the Skin2D
		private SkinnedMeshRenderer _skinnedMeshRenderer;
		public SkinnedMeshRenderer skinnedMeshRenderer {
			get {
				if (_skinnedMeshRenderer == null) {
					_skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
				}
				return _skinnedMeshRenderer;
			}
		}

		// Last selected GameObject for calculating vertex colors
		private GameObject lastSelected = null;

		// Prevent the bone weights from being recalculated
		public bool lockBoneWeights = false;

		// The referenced material for this skin
		public Material referenceMaterial;

		// The Point array from the ControlPoints component of this Skin2D
		public ControlPoints.Point[] controlPoints;

		// The actual ControlPoints component referenced for the control points
		public ControlPoints points;

		// Show the Skin2D mesh outline or not
		static public bool showMeshOutline = false;

		// Vertices of the Skin2D
		private List<Vector3> vertices = new List<Vector3>();

		// Reference to the original mesh for the skinned mesh renderer
		public Mesh referenceMesh;

		// Bool to check if this Skin2D is being edited
		private bool _editingPoints = false;
		public bool editingPoints {
			get {
				return _editingPoints;
			}

			set {
				_editingPoints = value;
			}
		}

		private int c;
		private int count;
		private bool updateControlPoints;

		private Skeleton[] skeletons;

		// Get the Skeleton of this Skin2D
		private Skeleton _skeleton = null;
		private Skeleton skeleton {
			get {
				skeletons = transform.root.gameObject.GetComponentsInChildren<Skeleton>(true);

				foreach (Skeleton s in skeletons) {
					if (transform.IsChildOf(s.transform)) {
						_skeleton = s;
						break;
					}
				}

				return _skeleton;
			}
		}

		#if UNITY_EDITOR
		[MenuItem("Boner2D/Skin 2D")]
		public static void Create () {
			// If we have a GameObject selected then use this to create the Skin2D
			if (Selection.activeGameObject != null) {
				GameObject o = Selection.activeGameObject;
				SkinnedMeshRenderer skin = o.GetComponent<SkinnedMeshRenderer>();
				SpriteRenderer spriteRenderer = o.GetComponent<SpriteRenderer>();

				// If we do not have a Skin2D already on this object and a SpriteRenderer then create it
				if (skin == null && spriteRenderer != null) {
					Sprite thisSprite = spriteRenderer.sprite;
					SpriteMesh spriteMesh = new SpriteMesh();
					spriteMesh.spriteRenderer = spriteRenderer;
					spriteMesh.CreateSpriteMesh();
					Texture2D spriteTexture = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(spriteRenderer.sprite, false);

					// Copy the sprite material
					Material spriteMaterial = new Material(spriteRenderer.sharedMaterial);
					spriteMaterial.CopyPropertiesFromMaterial(spriteRenderer.sharedMaterial);
					spriteMaterial.mainTexture = spriteTexture;

					// Copy the sorting order and sorting layers of the Sprite
					string sortLayerName = spriteRenderer.sortingLayerName;
					int sortOrder = spriteRenderer.sortingOrder;

					// Kill the SpriteRenderer as we do not need it any more.
					DestroyImmediate(spriteRenderer);

					// Add the Skin2D to this object and assign the sprite
					Skin2D skin2D = o.AddComponent<Skin2D>();
					skin2D.sprite = thisSprite;
					skin = o.GetComponent<SkinnedMeshRenderer>();
					MeshFilter filter = o.GetComponent<MeshFilter>();
					skin.material = spriteMaterial;

					// Save out the material from the sprite so we have a default material
					if(!Directory.Exists("Assets/Materials")) {
						AssetDatabase.CreateFolder("Assets", "Materials");
						AssetDatabase.Refresh();
					}
					AssetDatabase.CreateAsset(spriteMaterial, "Assets/Materials/" + spriteMaterial.mainTexture.name + ".mat");
					Debug.Log("Created material " + spriteMaterial.mainTexture.name + " for " + skin.gameObject.name);
					skin2D.referenceMaterial = spriteMaterial;
					skin.sortingLayerName = sortLayerName;
					skin.sortingOrder = sortOrder;

					// Create the mesh from the selection
					filter.mesh = (Mesh)Selection.activeObject;
					if (filter.sharedMesh != null && skin.sharedMesh == null) {
						skin.sharedMesh = filter.sharedMesh;
						skin2D.referenceMesh = skin.sharedMesh;
					}
					// Recalculate the bone weights for the new mesh
					skin2D.RecalculateBoneWeights();
				}
				else {
					// If there is a Skin2D or no SpriteRenderer then create a new GameObject with a Skin2D component
					o = new GameObject ("Skin2D");
					Undo.RegisterCreatedObjectUndo (o, "Create Skin2D");
					o.AddComponent<Skin2D> ();
				}
			}
			else {
				// If there is no selected GameObject then create a new GameObject with a Skin2D component
				GameObject o = new GameObject ("Skin2D");
				Undo.RegisterCreatedObjectUndo (o, "Create Skin2D");
				o.AddComponent<Skin2D> ();
			}
		}

		// Assign a reference mesh to the Skin2D to use as a fallback for bringing the Skin2D to a default state
		public void AssignReferenceMesh() {
			// If there is not a reference mesh then create one
			if (referenceMesh == null) {
				if (_skinnedMeshRenderer.sharedMesh == null 
				|| _skinnedMeshRenderer.sharedMesh != null && _skinnedMeshRenderer.sharedMesh.name.Contains("(Clone)")) {
					if (meshFilter == null) {
						Debug.Log("Need a meshFilter for " + name);
						return;
					}

					// Use the Skin2D's MeshFilter as a reference point for finding the reference mesh
					string meshName = _meshFilter.sharedMesh.name.Replace(".Mesh", ".SkinnedMesh");
					Debug.Log(meshName);

					// Get the skeleton for this Skin2D
					if (skeleton == null) {
						Debug.LogError("No Skeleton for this Skin2D: " + name);
						return;
					}

					// Load the reference mesh from the default SkinnedMeshes folder
					referenceMesh = AssetDatabase.LoadAssetAtPath ("Assets/Meshes/SkinnedMeshes/" + skeleton.gameObject.name + "/" + meshName + ".asset", typeof(Mesh)) as Mesh;

					// Assign the reference mesh if the SkinnedMeshRenderer's sharedMesh is null
					if (_skinnedMeshRenderer.sharedMesh == null) {
						_skinnedMeshRenderer.sharedMesh = referenceMesh;
						Debug.Log(meshName + " assigned to skin.");
					}
				} 
				else {
					// Since the SkinnedMeshRenderer's sharedMesh is not null and it is not an instance 
					// assign the reference mesh from the sharedMesh
					referenceMesh = _skinnedMeshRenderer.sharedMesh;
				}
			}
		}

		// Assign a reference material to the Skin2D to use as a fallback for bringing the Skin2D to a default state
		public void AssignReferenceMaterial() {
			// If there is not a reference material then create one
			if (referenceMaterial == null) {
				if (_skinnedMeshRenderer.sharedMaterial != null) {
					// If the sharedMaterial of the SkinnedMeshRenderer is an instance
					// assign the reference material to this sharedMaterial's asset location
					if (_skinnedMeshRenderer.sharedMaterial.name.Contains(" (Instance)")) {
						string materialName = _skinnedMeshRenderer.sharedMaterial.name.Replace(" (Instance)", "");
						Material material = AssetDatabase.LoadAssetAtPath("Assets/Materials/" + materialName + ".mat", typeof(Material)) as Material;
						referenceMaterial = material;
					} 
					else {
						referenceMaterial = _skinnedMeshRenderer.sharedMaterial;
					}
				}
			}
			else {
				// If there is a reference material and the current material is an instance
				// assign the original material from the asset location
				if (referenceMaterial.name.Contains(" (Instance)")) {
					string materialName = referenceMaterial.name.Replace(" (Instance)", "");
					Material material = AssetDatabase.LoadAssetAtPath("Assets/Materials/" + materialName + ".mat", typeof(Material)) as Material;
					referenceMaterial = material;
				}

				// if there is no sharedMaterial on the SkinnedMeshRenderer then assign the reference material
				if (_skinnedMeshRenderer.sharedMaterial == null) {
					_skinnedMeshRenderer.sharedMaterial = referenceMaterial;
				}
			}
		}
		#endif

	#if UNITY_EDITOR
		void OnDrawGizmos() {
			// Show the mesh outline
			if (Application.isEditor && meshFilter.sharedMesh != null && showMeshOutline) {
				CalculateVertexColors();
				GL.wireframe = true;
				LineMaterial.SetPass(0);
				Graphics.DrawMeshNow(meshFilter.sharedMesh, transform.position, transform.rotation);
				GL.wireframe = false;
			}

		}

		// Create or replace the mesh asset
		void CreateOrReplaceAsset (Mesh mesh, string path) {
			Mesh meshAsset = AssetDatabase.LoadAssetAtPath (path, typeof(Mesh)) as Mesh;
			if (meshAsset == null) {
				meshAsset = new Mesh ();
				// Hack to display mesh once saved
				CombineInstance[] combine = new CombineInstance[1];
				combine[0].mesh = mesh;
				combine[0].transform = Matrix4x4.identity;
				meshAsset.CombineMeshes(combine);

				EditorUtility.CopySerialized (mesh, meshAsset);
				AssetDatabase.CreateAsset (meshAsset, path);
			} 
			else {
				meshAsset.Clear();
				// Hack to display mesh once saved
				CombineInstance[] combine = new CombineInstance[1];
				combine[0].mesh = mesh;
				combine[0].transform = Matrix4x4.identity;
				meshAsset.CombineMeshes(combine);

				EditorUtility.CopySerialized (mesh, meshAsset);
				AssetDatabase.SaveAssets ();
			}
		}

		// Calculate the bone weights of this Skin2D while not weighting them to only the parent bone
		public void CalculateBoneWeights(Bone[] bones) {
			CalculateBoneWeights(bones, false);
		}

		// Calculate the bone weights of this Skin2D and optionally weight to the parent bone
		public void CalculateBoneWeights(Bone[] bones, bool weightToParent) {
			if (!lockBoneWeights) {
				if (meshFilter.sharedMesh == null) {
					Debug.Log("No Shared Mesh.");
					return;
				}

				// Create a new mesh and copy over the sharedMesh from the MeshFilter
				Mesh mesh = meshFilter.sharedMesh.Clone();
				mesh.name = meshFilter.sharedMesh.name;
				mesh.name = mesh.name.Replace(".Mesh", ".SkinnedMesh");

				if (bones != null && mesh != null) {
					boneWeights = new Bone2DWeights();
					boneWeights.weights = new Bone2DWeight[] { };

					int index = 0;
					foreach (Bone bone in bones) {
						int i = 0;

						// Save a reference to this bone if it is active or not
						bool boneActive = bone.gameObject.activeSelf;

						// Activate the bone so we can weight it properly
						bone.gameObject.SetActive(true);

						foreach (Vector3 v in mesh.vertices) {
							float influence;

							// If we are not weighting to the parent bone and the bone has a parent
							// Get the influence of the bone weight from the vertex and transform positions
							if (!weightToParent || weightToParent && bone.transform != transform.parent) {
								influence = bone.GetInfluence(v + transform.position);
							}
							else {
								// If we are weighting to the parent bone, then make it fully influenced by the bone
								influence = 1.0f;
							}

							boneWeights.SetWeight(i, bone.name, index, influence);

							i++;
						}

						index++;

						// Set the bone back to its initial active state
						bone.gameObject.SetActive(boneActive);
					}

					BoneWeight[] unitweights = boneWeights.GetUnityBoneWeights();
					mesh.boneWeights = unitweights;

					Transform[] bonesArr = bones.Select(b => b.transform).ToArray();
					Matrix4x4[] bindPoses = new Matrix4x4[bonesArr.Length];

					for (int i = 0; i < bonesArr.Length; i++) {
						bindPoses[i] = bonesArr[i].worldToLocalMatrix * transform.localToWorldMatrix;
					}

					mesh.bindposes = bindPoses;

					skinnedMeshRenderer.bones = bonesArr;

					// Get the Skeleton of this Skin2D
					if (skeleton == null) {
						Debug.LogError("No Skeleton for this Skin2D: " + name);
						return;
					}

					// Use the skeleton name to find the SkinnedMesh of this Skin2D
					DirectoryInfo meshSkelDir = new DirectoryInfo("Assets/Meshes/SkinnedMeshes/" + skeleton.gameObject.name);

					// Create the directory if it does not exist
					if (Directory.Exists(meshSkelDir.FullName) == false) {
						Directory.CreateDirectory(meshSkelDir.FullName);
					}

					string path = "Assets/Meshes/SkinnedMeshes/" + skeleton.gameObject.name + "/" + mesh.name + ".asset";

					// Create or replace the mesh asset
					CreateOrReplaceAsset (mesh, path);

					AssetDatabase.Refresh();

					Mesh generatedMesh = AssetDatabase.LoadAssetAtPath (path, typeof(Mesh)) as Mesh;

					// Ensure it has bindPoses and weights
					generatedMesh.boneWeights = unitweights;
					generatedMesh.bindposes = bindPoses;

					// Set the reference and sharedMesh to the generated mesh
					skinnedMeshRenderer.sharedMesh = generatedMesh;
					referenceMesh = generatedMesh;

					EditorUtility.SetDirty(skinnedMeshRenderer.gameObject);
					AssetDatabase.SaveAssets();
				}
				else {
					Debug.Log("No bones or mesh for this Skin2D: " + name);
				}
			}
		}

		// Calculate the debug vertex colors for this Skin2D
		private void CalculateVertexColors() {
			GameObject go = Selection.activeGameObject;

			if (go == lastSelected || meshFilter.sharedMesh == null) {
				return;
			}

			lastSelected = go;

			Mesh m = skinnedMeshRenderer.sharedMesh;

			Color[] colors = new Color[m.vertexCount];

			for (int i = 0; i < colors.Length; i++) {
				colors[i] = Color.black;
			}

			if (go != null) {
				Bone bone = go.GetComponent<Bone>();

				if (bone != null) {
					if (skinnedMeshRenderer.bones.Any(b => b.gameObject.GetInstanceID() == bone.gameObject.GetInstanceID())) {
						for (int i = 0; i < colors.Length; i++) {
							float value = 0;

							BoneWeight bw = m.boneWeights[i];
							if (bw.boneIndex0 == bone.index)
								value = bw.weight0;
							else if (bw.boneIndex1 == bone.index)
								value = bw.weight1;
							else if (bw.boneIndex2 == bone.index)
								value = bw.weight2;
							else if (bw.boneIndex3 == bone.index)
								value = bw.weight3;

							colors[i] = Util.HSBColor.ToColor(new Util.HSBColor(0.7f - value, 1.0f, 0.5f));
						}
					}
				}
			}

			meshFilter.sharedMesh.colors = colors;
		}

		// Save this Skin2D and SkinnedMesh as a prefab
		public void SaveAsPrefab() {

			// Check if the Prefabs directory exists, if not, create it.
			DirectoryInfo prefabDir = new DirectoryInfo("Assets/Prefabs");
			if (Directory.Exists(prefabDir.FullName) == false) {
				Directory.CreateDirectory(prefabDir.FullName);
			}

			// Get the skeleton of this Skin2D
			if (skeleton == null) {
				Debug.LogError("No Skeleton for this Skin2D: " + name);
				return;
			}

			// Use the skeleton name to find the SkinnedMesh of this Skin2D
			DirectoryInfo prefabSkelDir = new DirectoryInfo("Assets/Prefabs/" + skeleton.gameObject.name);

			// Create the directory if it does not exist
			if (Directory.Exists(prefabSkelDir.FullName) == false) {
				Directory.CreateDirectory(prefabSkelDir.FullName);
			}

			string path = "Assets/Prefabs/" + skeleton.gameObject.name + "/" + gameObject.name + ".prefab";

			// Need to create a new Mesh because replacing the prefab will erase the sharedMesh 
			// on the SkinnedMeshRenderer if it is linked to the prefab
			Mesh mesh = meshFilter.sharedMesh.Clone();
			mesh.name = meshFilter.sharedMesh.name.Replace(".Mesh", ".SkinnedMesh");

			// Get the directory for the SkinnedMeshes using the skeleton name
			DirectoryInfo meshSkelDir = new DirectoryInfo("Assets/Meshes/SkinnedMeshes/" + skeleton.gameObject.name);

			// If there is no directory then create one
			if (Directory.Exists(meshSkelDir.FullName) == false) {
				Directory.CreateDirectory(meshSkelDir.FullName);
			}

			string meshPath = "Assets/Meshes/SkinnedMeshes/" + skeleton.gameObject.name + "/" + mesh.name + ".asset";
			Mesh generatedMesh = AssetDatabase.LoadMainAssetAtPath (meshPath) as Mesh;

			if (generatedMesh == null) {
				generatedMesh = new Mesh();
				// Hack to display mesh once saved
				CombineInstance[] combine = new CombineInstance[1];
				combine[0].mesh = mesh;
				combine[0].transform = Matrix4x4.identity;
				generatedMesh.CombineMeshes(combine);

				EditorUtility.CopySerialized(mesh, generatedMesh);
				AssetDatabase.CreateAsset(generatedMesh, meshPath);
				AssetDatabase.Refresh();
			}

			// Ensure there is a reference material for the renderer
			AssignReferenceMaterial();

			// Create a new prefab erasing the old one
			Object obj = PrefabUtility.CreateEmptyPrefab(path);

			// Make sure the skinned mesh renderer and reference mesh are using the stored generated mesh
			skinnedMeshRenderer.sharedMesh = generatedMesh;

			referenceMesh = generatedMesh;

			// Make sure the renderer is using a material
			if (referenceMaterial != null) {
				skinnedMeshRenderer.sharedMaterial = referenceMaterial;
			}

			// Replace the prefab
			PrefabUtility.ReplacePrefab(gameObject, obj, ReplacePrefabOptions.ConnectToPrefab);
			EditorUtility.SetDirty(skinnedMeshRenderer.gameObject);
			AssetDatabase.SaveAssets();
		}

		// Recalculate the bone weights of this Skin2D
		public void RecalculateBoneWeights() {
			if (skeleton != null) {
				skeleton.CalculateWeights(true);
				// Debug.Log("Calculated weights for " + gameObject.name);
			}
			else {
				Debug.Log("No Skeleton for this Skin2D: " + name);
			}
		}

		// Reset the control point positions of this Skin2D
		public void ResetControlPointPositions() {
			if (controlPoints != null && controlPoints.Length > 0) {
				for (int i = 0; i < controlPoints.Length; i++) {
					if (controlPoints[i].originalPosition != meshFilter.sharedMesh.vertices[i]) {
						controlPoints[i].originalPosition = meshFilter.sharedMesh.vertices[i];
					}
					controlPoints[i].ResetPosition();
					points.SetPoint(controlPoints[i]);
				}
			}
			else {
				Debug.Log("No control points for Skin2D: " + name);
			}
		}

		// Create the control points for this Skin2D
		public void CreateControlPoints(SkinnedMeshRenderer skin) {
			if (skin.sharedMesh != null) {
				controlPoints = new ControlPoints.Point[skin.sharedMesh.vertices.Length];

				if (points == null) {
					points = gameObject.GetComponent<ControlPoints>();

					if (points == null) {
						points = gameObject.AddComponent<ControlPoints>();
					}
				}

				for (int i = 0; i < skin.sharedMesh.vertices.Length; i++) {
					Vector3 originalPos = skin.sharedMesh.vertices[i];

					controlPoints[i] = new ControlPoints.Point(originalPos);
					controlPoints[i].index = i;
					points.SetPoint(controlPoints[i]);
				}
			}
			else {
				Debug.LogError("There is no shared mesh for this Skin2D: " + name);
			}
		}

		// Remove the control points for this Skin2D
		public void RemoveControlPoints() {
			controlPoints = null;

			if (points != null) {
				DestroyImmediate(points);
			}

			points = null;
		}
	#endif

		public void UpdateControlPoints() {
			count = controlPoints.Length;

			if (vertices == null || vertices.Count != _skinnedMeshRenderer.sharedMesh.vertexCount) {
				vertices.Clear();
				_skinnedMeshRenderer.sharedMesh.GetVertices(vertices);
			}
			else {
				_skinnedMeshRenderer.sharedMesh.GetVertices(vertices);
			}

			updateControlPoints = false;

			for (c = 0; c < count; c++) {
				if (!updateControlPoints) {
					if (vertices[c] != points.GetPoint(controlPoints[c])) {
						updateControlPoints = true;
					}
				}

				if (updateControlPoints) {
					vertices[c] = points.GetPoint(controlPoints[c]);
				}
			}

			if (updateControlPoints) {
				_skinnedMeshRenderer.sharedMesh.SetVertices(vertices);
			}
		}

		void OnEnable() {
			// Only skin up to 2 bones, more than this messes up the skinning.
			if (skinnedMeshRenderer != null) {
				skinnedMeshRenderer.quality = SkinQuality.Bone2;
			}
		}

		// Use this for initialization
		void Start() {
			if (Application.isPlaying) {
				_editingPoints = false;
			}

			// Only skin up to 2 bones, more than this messes up the skinning.
			if (skinnedMeshRenderer != null) {
				skinnedMeshRenderer.quality = SkinQuality.Bone2;
			}

	#if UNITY_EDITOR
			// Show outlines for the mesh if toggled
			if (!Application.isPlaying && showMeshOutline) {
				CalculateVertexColors();
			}

			// Make sure there is an original mesh to instantiate from
			if (!Application.isPlaying && referenceMesh == null) {
				AssignReferenceMesh();
			}
	#endif

			// Always use a clone of the original mesh when the application is playing
			if (Application.isPlaying) {
				if (skinnedMeshRenderer.sharedMesh != null && referenceMesh == null) {
					referenceMesh = skinnedMeshRenderer.sharedMesh;
				}

				if (referenceMesh != null) {
					Mesh newMesh = (Mesh)Object.Instantiate(referenceMesh);
					skinnedMeshRenderer.sharedMesh = newMesh;
				}
			}
		}

		#if UNITY_EDITOR
		// Update is called once per frame
		void Update () {
			if (!Application.isPlaying) {
				// Ensure there is a reference material for the renderer
				AssignReferenceMaterial();

				// Make sure the renderer is using a material if it is nullified
				if (_skinnedMeshRenderer.sharedMaterial == null) {
					_skinnedMeshRenderer.sharedMaterial = referenceMaterial;
				}

				// Ensure there is a reference material for the renderer
				if (referenceMesh == null) {
					AssignReferenceMesh();
				}

				// Ensure SkinnedMeshRenderer has a sharedMesh if there is a referenceMesh
				if (_skinnedMeshRenderer != null 
				&& _skinnedMeshRenderer.sharedMesh == null 
				&& referenceMesh != null) {
					_skinnedMeshRenderer.sharedMesh = referenceMesh;
				}

				// Use a clone of the mesh when we are animating so we do not alter the original skin
				if (!Application.isPlaying && AnimationMode.InAnimationMode() 
				&& skinnedMeshRenderer.sharedMesh != null 
				&& referenceMesh != null 
				&& skinnedMeshRenderer.sharedMesh == referenceMesh) {
					Mesh newMesh = (Mesh)Object.Instantiate(referenceMesh);
					skinnedMeshRenderer.sharedMesh = newMesh;
				}

				// Revert to the reference mesh when we are finished animating
				else if (!Application.isPlaying && !AnimationMode.InAnimationMode() 
				&& skinnedMeshRenderer.sharedMesh != null 
				&& referenceMesh != null 
				&& skinnedMeshRenderer.sharedMesh != referenceMesh) {
					skinnedMeshRenderer.sharedMesh = referenceMesh;
				}
			}
		}
		#endif

		// Update is called once per frame
		void LateUpdate () {
			if (_editingPoints) {
				return;
			}

			if (Application.isPlaying && !_skinnedMeshRenderer.isVisible) {
				return;
			}

			if (controlPoints == null || points == null) {
				return;
			}

			if (!_editingPoints && controlPoints.Length > 0) {
				UpdateControlPoints();
			}
		}

		void OnDisable() {
			#if UNITY_EDITOR
			AssignReferenceMesh();

			if (!Application.isPlaying && !AnimationMode.InAnimationMode() 
			&& skinnedMeshRenderer.sharedMesh != null 
			&& referenceMesh != null 
			&& skinnedMeshRenderer.sharedMesh != referenceMesh) {
				skinnedMeshRenderer.sharedMesh = referenceMesh;
			}
			#endif

			_editingPoints = false;
		}
	}
}
