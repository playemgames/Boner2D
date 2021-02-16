/*
The MIT License (MIT)

Copyright (c) 2014 - 2019 Banbury & Play-Em

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
using System.Reflection;
using System.Linq;


namespace Boner2D {

	[ExecuteInEditMode]
	[SelectionBase]
	public class Skeleton : MonoBehaviour {
		public bool editMode = true;
		public bool showBoneInfluence = true;
		public bool IK_Enabled = true;

		#if UNITY_EDITOR
		public Pose basePose;

		private Pose tempPose;
		#endif

		[SerializeField] 
		[HideInInspector]
		private bool _flipY = false;

		public bool flipY {
			get { return _flipY; }
			set {
				_flipY = value;
				FlipY();
			}
		}

		[SerializeField] 
		[HideInInspector]
		private bool _flipX = false;

		public bool flipX {
			get { return _flipX; }
			set {
				_flipX = value;
				FlipX();
			}
		}

		[SerializeField] 
		[HideInInspector]
		private bool _useShadows = false;

		public bool useShadows {
			get { return _useShadows; }
			set {
				_useShadows = value;
				UseShadows();
			}
		}

		[SerializeField] 
		[HideInInspector]
		private bool _useZSorting = false;

		public bool useZSorting {
			get { return _useZSorting; }
			set {
				_useZSorting = value;
				UseZSorting();
			}
		}

		[Header("Skeleton Shaders")]
		// Shaders used for sprites in the skeletons, spriteShadowsShader is for rendering lit sprites
		public Shader spriteShader;
		public Shader spriteShadowsShader;

		[Header("Bone Colors")]
		// Colors for Right and Left bone colors
		public Color colorRight = new Color(255.0f/255.0f, 128.0f/255.0f, 0f, 255.0f/255.0f);
		public Color colorLeft = Color.magenta;

		[HideInInspector]
		public bool hasChildPositionsSaved = false;

		private InverseKinematics[] iks;

		private Bone[] _bones = null;
		public Bone[] bones {
			get {
				if (!Application.isPlaying) { 
					if (_bones == null || _bones != null && _bones.Length <= 0 || Application.isEditor){
						_bones = gameObject.GetComponentsInChildren<Bone>(true);
						iks = gameObject.GetComponentsInChildren<InverseKinematics>(true);
					}
				}

				return _bones;
			}
		}

		private Dictionary<Transform, float> renderers = new Dictionary<Transform, float>();

		private SkinnedMeshRenderer[] skins;
		private Skin2D[] skin2Ds;
		private SpriteRenderer[] spriteRenderers;
		private Renderer[] sortingLayers;

		private int spriteColor;
		private int spriteNormal;
		private MaterialPropertyBlock propertyBlock;
		private Vector3 frontNormal;
		private Vector3 reverseNormal;

		// Are we in record mode in the animation editor?
		private bool recordMode = false;

		[Header("Debug")]
		// Enable if you want verbose Debug Logs
		public bool verboseLogs = false;


	/// METHODS ///

		private void IKUpdate() {
			if (IK_Enabled) {
				if (bones != null && iks != null) {
					for (int i = 0; i < iks.Length; i++) {
						if (!editMode && iks[i] != null 
						&& iks[i].enabled && iks[i].influence > 0 
						&& iks[i].gameObject.activeInHierarchy) {
							iks[i].ResolveSK2D();
						}
					}
				}
			}
		}

	#if UNITY_EDITOR
		[MenuItem("Boner2D/Skeleton")]
		public static void Create() {
			Undo.IncrementCurrentGroup ();

			GameObject o = new GameObject ("Skeleton");
			Undo.RegisterCreatedObjectUndo (o, "Create skeleton");
			o.AddComponent<Skeleton> ();

			GameObject b = new GameObject ("Bone");
			Undo.RegisterCreatedObjectUndo (b, "Create Skeleton");
			b.AddComponent<Bone> ();

			b.transform.parent = o.transform;

			Undo.CollapseUndoOperations (Undo.GetCurrentGroup ());
		}

		// Create a pose, include disabled objects or not
		public Pose CreatePose(bool includeDisabled) {
			Pose pose = ScriptableObject.CreateInstance<Pose>();

			var poseBones = gameObject.GetComponentsInChildren<Bone>(includeDisabled);

			/// WILL BE DEPRECIATED IN FUTURE RELEASE AS CONTROL POINT IS NO LONGER SUPPORTED ///
			var cps = gameObject.GetComponentsInChildren<ControlPoint>(includeDisabled);

			var poseSkin2Ds = gameObject.GetComponentsInChildren<Skin2D>(includeDisabled);

			List<RotationValue> rotations = new List<RotationValue>();
			List<PositionValue> positions = new List<PositionValue>();
			List<PositionValue> targets = new List<PositionValue>();
			List<PositionValue> controlPoints = new List<PositionValue>();
			List<SortOrderValue> sortOrders = new List<SortOrderValue>();

			foreach (Bone b in poseBones) {
				rotations.Add(new RotationValue(b.name, b.transform.localRotation));
				positions.Add(new PositionValue(b.name, b.transform.localPosition));

				if (b.GetComponent<InverseKinematics>() != null) {
					targets.Add(new PositionValue(b.name, b.GetComponent<InverseKinematics>().target.localPosition));
				}

				var rens = b.gameObject.GetComponentsInChildren<Renderer>(includeDisabled);

				foreach (Renderer ren in rens) {
					if (ren.GetComponentInParent<Bone>() == b) {
						string pathToRenderer = ren.name;
						Transform nextParent = ren.transform.parent;
						while (nextParent != b.transform) {
							pathToRenderer = nextParent.name + "/" + pathToRenderer;
							nextParent = nextParent.parent;
						}
						sortOrders.Add(new SortOrderValue(b.name + "/" + pathToRenderer, ren.sortingOrder));
					}
				}

			}

			/// WILL BE DEPRECIATED IN FUTURE RELEASE AS CONTROL POINT IS NO LONGER SUPPORTED ///
			// Use bone parent name + control point name for the search
			foreach (ControlPoint cp in cps) {
				controlPoints.Add(new PositionValue(cp.name, cp.transform.localPosition));
			}

			// Use bone parent name + control point name for the search
			foreach (Skin2D skin in poseSkin2Ds) {
				if (skin.controlPoints != null && skin.controlPoints.Length > 0) {
					for (int c = 0; c < skin.controlPoints.Length; c++) {
						string index = "";

						if (c > 0) {
							index = c.ToString();
						}

						controlPoints.Add(new PositionValue(skin.name + " Control Point" + index, skin.points.GetPoint(skin.controlPoints[c])));
					}
				}
			}

			pose.rotations = rotations.ToArray();
			pose.positions = positions.ToArray();
			pose.targets = targets.ToArray();
			pose.controlPoints = controlPoints.ToArray();
			pose.sortOrders = sortOrders.ToArray();

			return pose;
		}

		public Pose CreatePose() {
			return CreatePose(true);
		}

		// Save a pose to a file
		public void SavePose(string poseFileName) {
			if (!Directory.Exists("Assets/Poses")) {
				AssetDatabase.CreateFolder("Assets", "Poses");
				AssetDatabase.Refresh();
			}

			if (poseFileName != null && poseFileName.Trim() != "") {
				ScriptableObjectUtility.CreateAsset(CreatePose(), "Poses/" + poseFileName);
			}
			else {
				ScriptableObjectUtility.CreateAsset(CreatePose());
			}
		}
	#endif

		// Restore a saved pose for the Skeleton
		public void RestorePose(Pose pose) {
			var bones = gameObject.GetComponentsInChildren<Bone>();

			/// WILL BE DEPRECIATED IN FUTURE RELEASE AS ControlPoint IS NO LONGER SUPPORTED ///
			var cps = gameObject.GetComponentsInChildren<ControlPoint>();

			var poseSkin2Ds = gameObject.GetComponentsInChildren<Skin2D>();

			#if UNITY_EDITOR
			Undo.RegisterCompleteObjectUndo(bones, "Assign Pose");
			Undo.RegisterCompleteObjectUndo(cps, "Assign Pose");
			Undo.RegisterCompleteObjectUndo(poseSkin2Ds, "Assign Pose");
			#endif

			if (bones.Length > 0) {
				for ( int i = 0; i < pose.rotations.Length; i++) {
					bool hasRot = false;
					for ( int b = 0; b < bones.Length; b++) {
						if (bones[b].name == pose.rotations[i].name) {
							#if UNITY_EDITOR
							Undo.RecordObject(bones[b].transform, "Assign Pose");
							#endif

							// Set the bone rotation to the pose rotation
							bones[b].transform.localRotation = pose.rotations[i].rotation;

							#if UNITY_EDITOR
							EditorUtility.SetDirty (bones[b].transform);
							#endif
							hasRot = true;
						}
					}

					if (!hasRot) {
						if (verboseLogs) {
							Debug.Log("This skeleton has no bone '" + pose.rotations[i].name + "' enabled");
						}
					}
				}

				for ( int j = 0; j < pose.positions.Length; j++) {
					bool hasPos = false;
					for ( int o = 0; o < bones.Length; o++) {
						if (bones[o].name == pose.positions[j].name) {
							#if UNITY_EDITOR
							Undo.RecordObject(bones[o].transform, "Assign Pose");
							#endif

							// Set the bone position to the pose position
							bones[o].transform.localPosition = pose.positions[j].position;

							#if UNITY_EDITOR
							EditorUtility.SetDirty (bones[o].transform);
							#endif
							hasPos = true;
						}
					}

					if (!hasPos) {
						if (verboseLogs) {
							Debug.Log("This skeleton has no bone '" + pose.positions[j].name + "' enabled");
						}
					}
				}

				for ( int k = 0; k < pose.targets.Length; k++) {
					bool hasTarget = false;
					for ( int n = 0; n < bones.Length; n++) {
						if (bones[n].name == pose.targets[k].name) {
							InverseKinematics ik = bones[n].GetComponent<InverseKinematics>();

							if (ik != null) {
								#if UNITY_EDITOR
								Undo.RecordObject(ik.target, "Assign Pose");
								#endif

								// Set IK position to the pose IK target position
								ik.target.transform.localPosition = pose.targets[k].position;

								#if UNITY_EDITOR
								EditorUtility.SetDirty (ik.target.transform);
								#endif
							}
							else {
								if (verboseLogs) {
									Debug.Log("This skeleton has no ik for bone '" + bones[n].name + "' enabled");
								}
							}
							hasTarget = true;
						}
					}

					if (!hasTarget) {
						if (verboseLogs) {
							Debug.Log("This skeleton has no bone '" + pose.targets[k].name + "' enabled");
						}
					}
				}

				if (pose.sortOrders.Length > 0) {
					for ( int so = 0; so < pose.sortOrders.Length; so++) {
						bool hasSortingOrder = false;

						for ( int p = 0; p < bones.Length; p++) {
							List<Renderer> rens = new List<Renderer>();

							for (int c = 0; c < bones[p].transform.childCount; c++) {
								Renderer ren = bones[p].transform.GetChild(c).GetComponent<Renderer>();

								if (ren != null) {
									rens.Add(ren);
								}
							}

							if (rens.Count > 0) {
								for ( int r = 0; r < rens.Count; r++) {
									string pathToRenderer = rens[r].name;
									Transform nextParent = rens[r].transform.parent;

									while (nextParent != bones[p].transform) {
										pathToRenderer = nextParent.name + "/" + name;
										nextParent = nextParent.parent;
									}

									if (bones[p].name + "/" + pathToRenderer == pose.sortOrders[so].name) {
										#if UNITY_EDITOR
										Undo.RecordObject(rens[r], "Assign Pose");
										#endif

										rens[r].sortingOrder = pose.sortOrders[so].sortingOrder;

										#if UNITY_EDITOR
										EditorUtility.SetDirty (rens[r]);
										#endif

										hasSortingOrder = true;
									}
								}
							}
							else {
								if (verboseLogs) {
									Debug.Log("There are no Renderers in this Bone: " + name + "/" + bones[p].name + " enabled");
								}
							}
						}

						if (!hasSortingOrder) {
							if (verboseLogs) {
								Debug.Log("There is no sorting order '" + pose.sortOrders[so].name + "' enabled");
							}
						}
					}
				}
				else {
					Debug.Log("There are no sorting orders for this Pose.");
				}
			}

			if (pose.controlPoints.Length > 0) {
				for ( int l = 0; l < pose.controlPoints.Length; l++) {
					bool hasControlPoint = false;

					/// WILL BE DEPRECIATED IN FUTURE RELEASE AS CONTROL POINT IS NO LONGER SUPPORTED ///
					if (cps.Length > 0) {
						for ( int c = 0; c < cps.Length; c++) {
							if (cps[c].name == pose.controlPoints[l].name) {
								#if UNITY_EDITOR
								Undo.RecordObject(cps[c].transform, "Assign Pose");
								#endif

								// Set the control point transform position to the control point position
								cps[c].transform.localPosition = pose.controlPoints[l].position;

								#if UNITY_EDITOR
								EditorUtility.SetDirty (cps[c].transform);
								#endif
								hasControlPoint = true;
							}
						}
					}

					// Move control points in Skin2D component
					if (poseSkin2Ds.Length > 0) {
						for ( int s = 0; s < poseSkin2Ds.Length; s++) {
							if (poseSkin2Ds[s].points != null && poseSkin2Ds[s].controlPoints != null 
							&& poseSkin2Ds[s].controlPoints.Length > 0 
							&& pose.controlPoints[l].name.StartsWith(poseSkin2Ds[s].name + " Control Point")){
								#if UNITY_EDITOR
								Undo.RecordObject(poseSkin2Ds[s], "Assign Pose");
								Undo.RecordObject(poseSkin2Ds[s].points, "Assign Pose");
								#endif

								// Get control point index by name in pose
								int index = GetControlPointIndex(pose.controlPoints[l].name);
								poseSkin2Ds[s].controlPoints[index].position = pose.controlPoints[l].position;
								poseSkin2Ds[s].points.SetPoint(poseSkin2Ds[s].controlPoints[index]);

								#if UNITY_EDITOR
								EditorUtility.SetDirty (poseSkin2Ds[s]);
								EditorUtility.SetDirty (poseSkin2Ds[s].points);
								#endif

								hasControlPoint = true;

								// Debug.Log("Found " + pose.controlPoints[l].name + " set to " + index + poseSkin2Ds[s].points.GetPoint(poseSkin2Ds[s].controlPoints[index]));
							}
						}
					}

					if (!hasControlPoint) {
						if (verboseLogs) {
							Debug.Log("There is no control point '" + pose.controlPoints[l].name + "'" + " enabled." );
						}
					}
				}
			}
		}

		// Get the index from the control point name
		int GetControlPointIndex(string controlPointName) {
			int index = controlPointName.LastIndexOf(" ");
			string cpName = controlPointName.Substring(index + 1);
			cpName = cpName.Replace("Point", "");
			int cpIndex = 0;

			if (cpName != "") {
				cpIndex = int.Parse(cpName);
			}

			return cpIndex;
		}

		#if UNITY_EDITOR
		// Set the base pose for the Skeleton
		public void SetBasePose(Pose pose) {
			basePose = pose;
		}
		#endif

		// Set the Edit mode to manipulate the bones and IK
		public void SetEditMode(bool edit) {
	#if UNITY_EDITOR
			if (!editMode && edit) {
				AnimationMode.StopAnimationMode();

				tempPose = CreatePose();
				tempPose.hideFlags = HideFlags.HideAndDontSave;

				if (basePose != null) {
					RestorePose(basePose);
				}
			}
			else if (editMode && !edit) {
				if (tempPose != null) {
					RestorePose(tempPose);
					Object.DestroyImmediate(tempPose);
				}
			}
	#endif

			editMode = edit;
		}

	#if UNITY_EDITOR
		// Calculate Bone Weights for skeletons for all bones
		public void CalculateWeights() {
			CalculateWeights(false);
		}

		// Calulate the bone weights to the parent only?
		public void CalculateWeights(bool weightToParent) {
			if (bones == null || bones.Length == 0) {
				Debug.Log("No bones in skeleton");
				return;
			}

			// Find all Skin2D elements
			skin2Ds = gameObject.GetComponentsInChildren<Skin2D>(true);

			for ( int i = 0; i < skin2Ds.Length; i++) {
				bool skinActive = skin2Ds[i].gameObject.activeSelf;
				skin2Ds[i].gameObject.SetActive(true);
				skin2Ds[i].CalculateBoneWeights(bones, weightToParent);
				skin2Ds[i].gameObject.SetActive(skinActive);
			}
		}
	#endif

		// Move the Renderer transform positions
		private void MoveRenderersPositions() {
			foreach (Transform renderer in renderers.Keys){
				#if UNITY_EDITOR
				Undo.RecordObject(renderer, "Move Render Position");
				#endif

				// Move the renderer's transform position to the new position
				renderer.position = new Vector3(renderer.position.x, renderer.position.y, (float)renderers[renderer]);

				#if UNITY_EDITOR
				EditorUtility.SetDirty (renderer);
				#endif
			}
		}

		// Flip this Skeleton on the Y axis
		public void FlipY() {
			// Default the normal value
			int normal = -1;

			// Rotate the skeleton's local transform
			if (!flipY) {
				renderers = new Dictionary<Transform, float>();

				// Get the new positions for the renderers from the rotation of this transform
				renderers = GetRenderersZ();

				#if UNITY_EDITOR
				Undo.RecordObject(transform, "Flip Y");
				#endif

				// Flip the rotation of the transform
				transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, 0.0f, transform.localEulerAngles.z);

				MoveRenderersPositions();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (transform);
				#endif
			}
			else {
				renderers = new Dictionary<Transform, float>();

				// Get the new positions for the renderers from the rotation of this transform
				renderers = GetRenderersZ();

				// Get the new positions for the renderers from the rotation of this transform
				#if UNITY_EDITOR
				Undo.RecordObject(transform, "Flip Y");
				#endif

				// Flip the rotation of the transform
				transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, 180.0f, transform.localEulerAngles.z);

				MoveRenderersPositions();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (transform);
				#endif
			}

			// Set the normal
			if ((int)transform.localEulerAngles.x == 0 && (int)transform.localEulerAngles.y == 180 
			|| (int)(int)transform.localEulerAngles.x == 180 && (int)transform.localEulerAngles.y == 0) {
				normal = 1;
			}

			ChangeRendererNormals(normal);
		}

		// Flip the Skeleton on the X axis
		public void FlipX () {
			// Default normal value
			int normal = -1;

			// Rotate the skeleton's local transform
			if (!flipX) {
				renderers = new Dictionary<Transform, float>();

				// Get the new positions for the renderers from the rotation of this transform
				renderers = GetRenderersZ();

				#if UNITY_EDITOR
				Undo.RecordObject(transform, "Flip X");
				#endif

				// Flip the rotation of the transform
				transform.localEulerAngles = new Vector3(0.0f, transform.localEulerAngles.y, transform.localEulerAngles.z);

				MoveRenderersPositions();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (transform);
				#endif
			}
			else {
				renderers = new Dictionary<Transform, float>();

				// Get the new positions for the renderers from the rotation of this transform
				renderers = GetRenderersZ();

				#if UNITY_EDITOR
				Undo.RecordObject(transform, "Flip X");
				#endif

				// Flip the rotation of the transform
				transform.localEulerAngles = new Vector3(180.0f, transform.localEulerAngles.y, transform.localEulerAngles.z);

				MoveRenderersPositions();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (transform);
				#endif

			}

			// Set the normal
			if ((int)transform.localEulerAngles.x == 0 && (int)transform.localEulerAngles.y == 180 
			|| (int)transform.localEulerAngles.x == 180 && (int)transform.localEulerAngles.y == 0) {
				normal = 1;
			}

			ChangeRendererNormals(normal);
		}

		// Get the renderers for Z sorting
		public Dictionary<Transform, float> GetRenderersZ() {
			renderers = new Dictionary<Transform, float>();

			if (bones != null) {
				for ( int i = 0; i < _bones.Length; i++) {
					renderers[_bones[i].transform] = _bones[i].transform.localPosition.z;
				}
			}

			if (useZSorting) {
				//find all SkinnedMeshRenderer elements
				skins = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

				for ( int i = 0; i < skins.Length; i++) {
					renderers[skins[i].transform] = skins[i].transform.localPosition.z;
				}

				//find all SpriteRenderer elements
				SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);

				for ( int j = 0; j < spriteRenderers.Length; j++) {
					renderers[spriteRenderers[j].transform] = spriteRenderers[j].transform.localPosition.z;
				}
			}

			return renderers;
		}

		// Change the color of the renderers
		public void ChangeColors(Color color) {
			// Find all SkinnedMeshRenderer elements
			if ( skins == null ) {
				skins = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			}

			if ( propertyBlock == null ) {
				propertyBlock = new MaterialPropertyBlock();
				spriteColor = Shader.PropertyToID("_Color");
			}

			for ( int i = 0; i < skins.Length; i++) {
				if (skins[i] != null 
				&& skins[i].sharedMaterial != null) {
					skins[i].GetPropertyBlock(propertyBlock);
					propertyBlock.SetColor(spriteColor, color);
					skins[i].SetPropertyBlock(propertyBlock);
				}
			}

			// Find all SpriteRenderer elements
			if (spriteRenderers == null) {
				spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
			}

			for ( int j = 0; j < spriteRenderers.Length; j++) {
				if (spriteRenderers[j] != null 
				&& spriteRenderers[j].sharedMaterial != null) {
					spriteRenderers[j].GetPropertyBlock(propertyBlock);
					propertyBlock.SetColor(spriteColor, color);
					spriteRenderers[j].SetPropertyBlock(propertyBlock);
				}
			}
		}

		// Change the normals for the renderer
		public void ChangeRendererNormals(int normal) {
			// Only do this if we are using shadows
			if (useShadows) {
				if (spriteShadowsShader != null) { 
					// Find all SkinnedMeshRenderer elements
					skins = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

					if ( propertyBlock == null ) {
						propertyBlock = new MaterialPropertyBlock();
					}

					for ( int i = 0; i < skins.Length; i++) {
						if (skins[i] != null 
						&& skins[i].sharedMaterial.shader == spriteShadowsShader) {
							skins[i].GetPropertyBlock(propertyBlock);

							if (normal == -1) {
								propertyBlock.SetVector(spriteNormal, frontNormal);
								skins[i].SetPropertyBlock(propertyBlock);
							}
							else {
								propertyBlock.SetVector(spriteNormal, reverseNormal);
								skins[i].SetPropertyBlock(propertyBlock);
							}
						}
					}

					// Find all SpriteRenderer elements
					spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);

					for ( int j = 0; j < spriteRenderers.Length; j++) {
						spriteRenderers[j].GetPropertyBlock(propertyBlock);

						if (spriteRenderers[j] != null 
						&& spriteRenderers[j].sharedMaterial.shader == spriteShadowsShader) {
							if (normal == -1) {
								propertyBlock.SetVector(spriteNormal, frontNormal);
								spriteRenderers[j].SetPropertyBlock(propertyBlock);
							}
							else {
								propertyBlock.SetVector(spriteNormal, reverseNormal);
								spriteRenderers[j].SetPropertyBlock(propertyBlock);
							}
						}
					}
				}
			}
		}

		// Use shadows for renderers
		public void UseShadows() {
			// Find all SkinnedMeshRenderer elements
			skins = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

			for ( int i = 0; i < skins.Length; i++) {
				if (useShadows && spriteShadowsShader != null) {
					#if UNITY_EDITOR
					Undo.RecordObject(skins[i].sharedMaterial.shader, "Use Shadows");
					#endif

					skins[i].sharedMaterial.shader = spriteShadowsShader;

					#if UNITY_EDITOR
					EditorUtility.SetDirty (skins[i].sharedMaterial.shader);
					#endif
				}
				else {
					#if UNITY_EDITOR
					Undo.RecordObject(skins[i].sharedMaterial.shader, "Use Shadows");
					#endif

					skins[i].sharedMaterial.shader = spriteShader;

					#if UNITY_EDITOR
					EditorUtility.SetDirty (skins[i].sharedMaterial.shader);
					#endif
				}

				// Set the shadow casting mode for using shadows or not
				if (useShadows){
					skins[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
				}
				else {
					skins[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				}

				skins[i].receiveShadows = useShadows;
			}

			// Find all SpriteRenderer elements
			spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);

			for ( int j = 0; j < spriteRenderers.Length; j++) {
				if (useShadows && spriteShadowsShader != null) {
					#if UNITY_EDITOR
					Undo.RecordObject(spriteRenderers[j].sharedMaterial.shader, "Use Shadows");
					#endif

					spriteRenderers[j].sharedMaterial.shader = spriteShadowsShader;

					#if UNITY_EDITOR
					EditorUtility.SetDirty (spriteRenderers[j].sharedMaterial.shader);
					#endif
				}
				else {
					#if UNITY_EDITOR
					Undo.RecordObject(spriteRenderers[j].sharedMaterial.shader, "Use Shadows");
					#endif

					spriteRenderers[j].sharedMaterial.shader = spriteShader;

					#if UNITY_EDITOR
					EditorUtility.SetDirty (spriteRenderers[j].sharedMaterial.shader);
					#endif
				}

				// Set the shadow casting mode for using shadows or not
				if (useShadows){
					spriteRenderers[j].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
				}
				else {
					spriteRenderers[j].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				}

				spriteRenderers[j].receiveShadows = useShadows;
			}
		}

		// Use Z sorting for renderers
		public void UseZSorting() {
			// Get all the Renderer components for sorting layers
			sortingLayers = gameObject.GetComponentsInChildren<Renderer>(true);

			for ( int i = 0; i < sortingLayers.Length; i++) {
				if (useZSorting) {
					#if UNITY_EDITOR
					Undo.RecordObject(sortingLayers[i].transform, "Use Z Sorting");
					#endif

					float z = (float)sortingLayers[i].sortingOrder / -10000f;

					sortingLayers[i].transform.localPosition = new Vector3(
						sortingLayers[i].transform.localPosition.x, 
						sortingLayers[i].transform.localPosition.y, 
						z);

					sortingLayers[i].sortingLayerName = "Default";

					sortingLayers[i].sortingOrder = 0;

					#if UNITY_EDITOR
					EditorUtility.SetDirty (sortingLayers[i].transform);
					#endif
				}
				else {
					#if UNITY_EDITOR
					Undo.RecordObject(sortingLayers[i].transform, "Use Z Sorting");
					#endif

					float sortLayer = Mathf.Round(sortingLayers[i].transform.localPosition.z * -10000f);
					sortingLayers[i].transform.localPosition = new Vector3(
						sortingLayers[i].transform.localPosition.x, 
						sortingLayers[i].transform.localPosition.y, 
						0);

					sortingLayers[i].sortingLayerName = "Default";

					sortingLayers[i].sortingOrder = (int)sortLayer;

					Debug.Log(sortingLayers[i].name + " " + sortingLayers[i].sortingOrder);

					#if UNITY_EDITOR
					EditorUtility.SetDirty (sortingLayers[i].transform);
					#endif
				}
			}
		}


	/// UNITY METHODS ///

		// Use this for initialization
		void Start () {
			// Set Default Shaders
			spriteShader = Shader.Find("Sprites/Default");
			spriteShadowsShader = Shader.Find("Sprites/Skeleton-CutOut");

			// Set MaterialPropertyBlock
			spriteColor = Shader.PropertyToID("_Color");
			spriteNormal = Shader.PropertyToID("_Normal");
			propertyBlock = new MaterialPropertyBlock();

			// Normals for Sprites
			frontNormal = new Vector3(0, 0, -1);
			reverseNormal = new Vector3(0, 0, 1);

			// Initialize Z-Sorting and Shadows
			skins = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

			for ( int i = 0; i < skins.Length; i++) {
				if (!Mathf.Approximately(skins[i].transform.localPosition.z, 0) && skins[i].sortingOrder == 0) {
					_useZSorting = true;
					// Debug.Log(name + " " + skins[i].name);
				}
				else if (skins[i].sortingOrder != 0) {
					_useZSorting = false;
					// Debug.Log(name + " " + skins[i].name);
				}

				if (skins[i].receiveShadows) {
					_useShadows = true;
				}
			}

			spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);

			for ( int n = 0; n < spriteRenderers.Length; n++){
				if (!Mathf.Approximately(spriteRenderers[n].transform.localPosition.z, 0) && spriteRenderers[n].sortingOrder == 0) {
					_useZSorting = true;
					// Debug.Log(name + " " + spriteRenderers[n].name);
				}
				else if (spriteRenderers[n].sortingOrder != 0) {
					_useZSorting = false;
					// Debug.Log(name + " " + spriteRenderers[n].name);
				}

				if (spriteRenderers[n].receiveShadows) {
					_useShadows = true;
				}
			}

			skin2Ds = gameObject.GetComponentsInChildren<Skin2D>(true);

			// Turn Edit mode off when playing
			if (Application.isPlaying) {
				SetEditMode(false);
			}

			// Default normal value
			int normal = -1;

			if ((int)transform.localEulerAngles.x == 0 && (int)transform.localEulerAngles.y == 180 
			|| (int)transform.localEulerAngles.x == 180 && (int)transform.localEulerAngles.y == 0) {
				normal = 1;

				#if UNITY_EDITOR
				Debug.Log("Changing normals for " + name);
				#endif
			}

			ChangeRendererNormals(normal);

		}


		void OnEnable() {

			if (!Application.isPlaying) { 
				// Default normal value
				int normal = -1;

				if ((int)transform.localEulerAngles.x == 0 && (int)transform.localEulerAngles.y == 180 
				|| (int)transform.localEulerAngles.x == 180 && (int)transform.localEulerAngles.y == 0) {
					normal = 1;

					Debug.Log("Changing normals for " + name);
				}

				ChangeRendererNormals(normal);
			}
			else {
				basePose = null;

				if (IK_Enabled) {
					if (_bones == null){
						_bones = gameObject.GetComponentsInChildren<Bone>(true);
						iks = gameObject.GetComponentsInChildren<InverseKinematics>(true);
					}
				}
			}
		}


	#if UNITY_EDITOR
		void OnDisable() {
			// Sets the skins to use reference mesh on disable
			if (!Application.isPlaying) {
				skin2Ds = gameObject.GetComponentsInChildren<Skin2D>(true);

				if (skin2Ds != null && recordMode) {
					for (int i = 0; i < skin2Ds.Length; i++) {
						skin2Ds[i].AssignReferenceMesh();

						if (skin2Ds[i].skinnedMeshRenderer.sharedMesh != null 
						&& skin2Ds[i].referenceMesh != null 
						&& skin2Ds[i].skinnedMeshRenderer.sharedMesh != skin2Ds[i].referenceMesh) {
							skin2Ds[i].skinnedMeshRenderer.sharedMesh = skin2Ds[i].referenceMesh;
						}
					}

					Debug.Log("Reassigned Meshes for skins for " + name);
				}
			}
		}

		// Update is called once per frame
		void Update () {
			if (!Application.isPlaying) {
				// Get Shaders if they are null
				if (spriteShader == null) {
					spriteShader = Shader.Find("Sprites/Default");
				}

				if (spriteShadowsShader == null) {
					spriteShadowsShader = Shader.Find("Sprites/Skeleton-CutOut");
				}

				// Check to see if the Animation Mode is in Record
				if (AnimationMode.InAnimationMode() && !recordMode) {
					recordMode = true;
				}

				// If the Animation Window was recording, set skins back to use reference mesh
				if (!AnimationMode.InAnimationMode() && recordMode) {
					skin2Ds = gameObject.GetComponentsInChildren<Skin2D>(true);

					if (skin2Ds != null) {
						for (int i = 0; i < skin2Ds.Length; i++) {
							skin2Ds[i].AssignReferenceMesh();

							if (skin2Ds[i].skinnedMeshRenderer.sharedMesh != null 
							&& skin2Ds[i].referenceMesh != null 
							&& skin2Ds[i].skinnedMeshRenderer.sharedMesh != skin2Ds[i].referenceMesh) {
								skin2Ds[i].skinnedMeshRenderer.sharedMesh = skin2Ds[i].referenceMesh;
							}
						}
					}

					recordMode = false;
				}
			}
		}
	#endif

		void LateUpdate() {
			IKUpdate();
		}
	}
}
