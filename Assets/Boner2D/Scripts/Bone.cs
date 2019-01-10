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
#endif
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Boner2D {
	[ExecuteInEditMode]
	public class Bone : MonoBehaviour {
		// Index number of the bone in the Skeleton
		public int index = 0;

		// Length of the bone
		public float length = 1.0f;

		// Snap this bone to the parent bone
		public bool snapToParent = true;

		// This is controlled by the bone's Skeleton
		private bool _editMode = true;
		public bool editMode {
			get {
				if (skeleton != null) {
					return skeleton.editMode;
				}
				else {
					return _editMode;
				}
			}

			set {
				_editMode = value;
			}
		}

		// Visually show the influence this bone has over the skin and other bones
		private bool _showInfluence = true;
		public bool showInfluence {
			get {
				if (skeleton != null) {
					return skeleton.showBoneInfluence;
				}
				else {
					return _showInfluence;
				}
			}

			set {
				_showInfluence = value;
			}
		}

		// The amount of influence this bone has on the tail over the skin and other bones
		public float influenceTail = 0f;

		
		// The amount of influence this bone has on the head over the skin and other bones
		public float influenceHead = 0f;

		// Color of the bone
		public Color color = Color.cyan;

		// Use this to flip the bone on the Y axis
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

		// Use this to flip the bone on the X axis
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

		private Bone _parent = null;
		public Bone parent {
			get {
				if (gameObject.transform.parent != null && _parent == null 
				|| gameObject.transform.parent != null && _parent != gameObject.transform.parent) {
					_parent = gameObject.transform.parent.GetComponent<Bone>();
				}

				return _parent;
			}
		}

		private Skeleton[] skeletons;

		private Skeleton _skeleton = null;
		public Skeleton skeleton {
			get {
				if (_skeleton == null || _skeleton != null && !Application.isPlaying && !transform.IsChildOf(_skeleton.transform)) {
					skeletons = transform.root.gameObject.GetComponentsInChildren<Skeleton>(true);

					foreach (Skeleton s in skeletons) {
						if (transform.IsChildOf(s.transform)) {
							_skeleton = s;
							break;
						}
					}
				}

				return _skeleton;
			}
		}

		private Dictionary<Transform, Vector3> childPositions = new Dictionary<Transform, Vector3>();
		private Dictionary<Transform, Quaternion> childRotations = new Dictionary<Transform, Quaternion>();

		private Dictionary<Transform, float> renderers = new Dictionary<Transform, float>();

		private SkinnedMeshRenderer[] skins;
		private SpriteRenderer[] spriteRenderers;

		private int spriteNormal;
		private MaterialPropertyBlock propertyBlock;
		private Vector3 frontNormal;
		private Vector3 reverseNormal;

		// The head position of the bone
		private Vector3 _head;
		public Vector3 Head {
			get {
				_head = Vector3.up * length;
				_head = transform.TransformPoint(_head);
				return _head;
			}
		}

		// Used for checking bone names for Left and Right colors
		private string boneName;
		private string[] rightSuffixes = new string[] { " R", "_R", ".R", "RIGHT" };
		private bool isRight = false;
		private string[] leftSuffixes = new string[] { " L", "_L", ".L", "LEFT" };
		private bool isLeft = false;

		// For drawing the bone
		private int div;
		private Vector3 HeadProjected;
		private Vector3 v;

		private InverseKinematics _ik;
		public InverseKinematics ik {
			get {
				if (_ik == null ) {
					_ik = GetComponent<InverseKinematics>();
				}

				return _ik;
			}
		}

		#if UNITY_EDITOR
		[MenuItem("Boner2D/Bone")]
		public static Bone Create() {
			GameObject b = new GameObject("Bone");

			Undo.RegisterCreatedObjectUndo(b, "Add child bone");

			Bone bone = b.AddComponent<Bone>();

			if (Selection.activeGameObject != null) {
				GameObject sel = Selection.activeGameObject;
				b.transform.parent = sel.transform;

				if (sel.GetComponent<Bone>() != null) {
					Bone p = sel.GetComponent<Bone>();
					b.transform.position = p.Head;
					b.transform.localRotation = Quaternion.Euler(0, 0, 0);
				}
			}

			Skeleton skel = b.transform.root.gameObject.GetComponentInChildren<Skeleton>();

			if (skel != null) {
				Bone[] bones = skel.gameObject.GetComponentsInChildren<Bone>();

				int index = bones.Max(bn => bn.index) + 1;

				b.GetComponent<Bone>().index = index;

				skel.CalculateWeights();
			}

			bone.name = bone.name + " " + bone.index;

			Selection.activeGameObject = b;

			return bone;
		}

		// Split the bone into 2 bones
		public static void Split() {
			if (Selection.activeGameObject != null) {
				Undo.IncrementCurrentGroup();

				string undo = "Split bone";

				GameObject old = Selection.activeGameObject;

				Undo.RecordObject(old, undo);

				Bone b = old.GetComponent<Bone>();

				GameObject n1 = new GameObject(old.name);

				Undo.RegisterCreatedObjectUndo(n1, undo);

				Bone b1 = n1.AddComponent<Bone>();

				b1.index = b.index;
				b1.transform.parent = b.transform.parent;
				b1.snapToParent = b.snapToParent;
				b1.length = b.length / 2;
				b1.transform.localPosition = b.transform.localPosition;
				b1.transform.localRotation = b.transform.localRotation;

				GameObject n2 = new GameObject("Bone " + b.GetMaxIndex());

				Undo.RegisterCreatedObjectUndo(n2, undo);

				Bone b2 = n2.AddComponent<Bone>();

				b2.index = b.GetMaxIndex();
				b2.length = b.length / 2;

				n2.transform.parent = n1.transform;

				b2.transform.localRotation = Quaternion.Euler(0, 0, 0);

				n2.transform.position = b1.Head;

				var children = (from Transform child in b.transform select child).ToArray<Transform>();

				b.transform.DetachChildren();

				foreach (Transform child in children) {
					Undo.SetTransformParent(child, n2.transform, undo);
					child.parent = n2.transform;
				}

				Undo.DestroyObjectImmediate(old);

				Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

				Selection.activeGameObject = b2.gameObject;
			}
		}

		// Add IK component to this bone
		public void AddIK() {
			Undo.AddComponent<InverseKinematics>(gameObject);
		}

		// Save the bone's children positions and rotations
		public void SaveChildPosRot() {
			if (Application.isEditor && editMode) {
				Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);

				foreach (Transform child in children) {
					if (!child.GetComponent<Bone>()) {
						childPositions[child] = new Vector3(child.position.x, child.position.y, child.position.z);
						childRotations[child] = new Quaternion(child.rotation.x, child.rotation.y, child.rotation.z, child.rotation.w);
					}
				}
			}
			else {
				Debug.Log("Skeleton needs to be in Edit Mode");
			}
		}

		// Load the bone's children saved positions and rotations
		public void LoadChildPosRot() {
			if (Application.isEditor && editMode) {
				Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);

				foreach (Transform child in children) {
					if (childPositions.ContainsKey(child)) {
						child.position = childPositions[child];
						child.rotation = childRotations[child];
					}
				}
			}
			else {
				Debug.Log("Skeleton needs to be in Edit Mode");
			}
		}

		// Does this bone have it's children positions and rotations saved?
		public bool HasChildPositionsSaved(){
			return (childPositions.Count > 0 && childRotations.Count > 0);
		}
		#endif

		// Use this for initialization
		void Start () {
			spriteNormal = Shader.PropertyToID("_Normal");
			propertyBlock = new MaterialPropertyBlock();
			frontNormal = new Vector3(0, 0, -1);
			reverseNormal = new Vector3(0, 0, 1);
		}

		#if UNITY_EDITOR
		// Update is called once per frame
		void Update () {
			if (!Application.isPlaying) {
				if (Application.isEditor && snapToParent && parent != null) {
					gameObject.transform.position = parent.Head;
				}
			}
		}
		#endif

	#if UNITY_EDITOR
		void OnDrawGizmos() {
			// Color the bones
			if (gameObject.Equals(Selection.activeGameObject)) {
				Gizmos.color = Color.yellow;
			}
			else {

				if (editMode) {
					if (skeleton != null) {
						boneName = gameObject.name.ToUpper();
						isRight = rightSuffixes.Any(x => boneName.EndsWith(x));
						isLeft = leftSuffixes.Any(x => boneName.EndsWith(x));

						if (isRight) {
							Gizmos.color = new Color(skeleton.colorRight.r * 0.75f, skeleton.colorRight.g * 0.75f, skeleton.colorRight.b * 0.75f, skeleton.colorRight.a);
						}
						else if (isLeft) {
							Gizmos.color = new Color(skeleton.colorLeft.r * 0.75f, skeleton.colorLeft.g * 0.75f, skeleton.colorLeft.b * 0.75f, skeleton.colorLeft.a);
						}
						else {
							Gizmos.color = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, color.a);
						}
					}
					else {
						Gizmos.color = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, color.a);
					}
				}
				else {
					if (skeleton != null) {
						boneName = gameObject.name.ToUpper();
						isRight = rightSuffixes.Any(x => boneName.EndsWith(x));
						isLeft = leftSuffixes.Any(x => boneName.EndsWith(x));

						if (isRight) {
							Gizmos.color = skeleton.colorRight;
						}
						else if (isLeft) {
								Gizmos.color = skeleton.colorLeft;
						}
						else {
							Gizmos.color = color;
						}
					}
					else {
						Gizmos.color = color;
					}
				}
			}

			// Draw the bone
			div = 5;

			HeadProjected = new Vector3(Head.x, Head.y, gameObject.transform.position.z);
			v = Quaternion.AngleAxis(45, Vector3.forward) * ((Head - gameObject.transform.position) / div);

			Gizmos.DrawLine(gameObject.transform.position, gameObject.transform.position + v);
			Gizmos.DrawLine(gameObject.transform.position + v, HeadProjected);

			v = Quaternion.AngleAxis(-45, Vector3.forward) * ((Head - gameObject.transform.position) / div);

			Gizmos.DrawLine(gameObject.transform.position, gameObject.transform.position + v);
			Gizmos.DrawLine(gameObject.transform.position + v, HeadProjected);

			Gizmos.DrawLine(gameObject.transform.position, HeadProjected);

			Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);

			// Show influence is controlled by the Skeleton of this Bone
			if (editMode && showInfluence) {
				Gizmos.DrawWireSphere(transform.position, influenceTail);
				Gizmos.DrawWireSphere(Head, influenceHead);
			}
		}
	#endif

		// Get the influence from a Vector
		public float GetInfluence(Vector3 p) {
			Vector3 wv = Head - transform.position;

			float dist = 0;

			if (length == 0) {
				dist = (p - transform.position).magnitude;
				if (dist > influenceTail)
					return 0;
				else
					return influenceTail;
			}
			else {
				float t = Vector3.Dot(p - transform.position, wv) / wv.sqrMagnitude;

				if (t < 0) {
					dist = (p - transform.position).magnitude;
					if (dist > influenceTail)
						return 0;
					else
						return (influenceTail - dist) / influenceTail;
				}
				else if (t > 1.0f) {
					dist = (p - Head).magnitude;
					if (dist > influenceHead)
						return 0;
					else
						return (influenceHead - dist) / influenceHead;
				}
				else {
					Vector3 proj = transform.position + (wv * t);
					dist = (proj - p).magnitude;

					float s = (influenceHead - influenceTail);
					float i = influenceTail + s * t;

					if (dist > i)
						return 0;
					else
						return (i - dist) / i;
				}
			}
		}

		// Get the max index for this bone
		internal int GetMaxIndex() {
			Bone[] bones = transform.root.gameObject.GetComponentsInChildren<Bone>(true);

			if (bones == null || bones.Length == 0)
				return 0;

			return bones.Max(b => b.index) + 1;
		}

		// Move the renderer positions to the new z positions
		private void MoveRenderersPositions() {
			foreach (Transform renderer in renderers.Keys){
				#if UNITY_EDITOR
				Undo.RecordObject(renderer, "Move Render Position");
				#endif

				// Move the renderer's transform to the new Z position
				renderer.position = new Vector3(renderer.position.x, renderer.position.y, (float)renderers[renderer]);

				#if UNITY_EDITOR
				EditorUtility.SetDirty (renderer);
				#endif
			}
		}

		// Flip the bone on the Y axis
		public void FlipY() {
			int normal = -1;

			// Rotate the skeleton's local transform
			if (!flipY) {
				renderers = new Dictionary<Transform, float>();

				// Get the new positions for the renderers from the rotation of this transform
				renderers = GetRenderersZ();

				#if UNITY_EDITOR
				Undo.RecordObject(transform, "Flip Y");
				#endif

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

				transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, 180.0f, transform.localEulerAngles.z);

				MoveRenderersPositions();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (transform);
				#endif
			}

			if ((int)transform.localEulerAngles.x == 0 && (int)transform.localEulerAngles.y == 180 
			|| (int)transform.localEulerAngles.x == 180 && (int)transform.localEulerAngles.y == 0) {
				normal = 1;
			}

			if (skeleton != null && skeleton.useShadows) {
				ChangeRendererNormals(normal);
			}
		}

		// Flip the bone on the X axis
		public void FlipX() {
			int normal = -1;

			// Rotate the skeleton's local transform
			if (!flipX) {
				renderers = new Dictionary<Transform, float>();

				// Get the new positions for the renderers from the rotation of this transform
				renderers = GetRenderersZ();

				#if UNITY_EDITOR
				Undo.RecordObject(transform, "Flip X");
				#endif

				// Flip the bone local rotation
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

				// Flip the bone local rotation
				transform.localEulerAngles = new Vector3(180.0f, transform.localEulerAngles.y, transform.localEulerAngles.z);

				MoveRenderersPositions();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (transform);
				#endif
			}

			if ((int)transform.localEulerAngles.x == 0 && (int)transform.localEulerAngles.y == 180 
			|| (int)transform.localEulerAngles.x == 180 && (int)transform.localEulerAngles.y == 0) {
				normal = 1;
			}

			if (skeleton != null) {
				ChangeRendererNormals(normal);
			}
		}

		// Get all the renderers z positions for each transform
		public Dictionary<Transform, float> GetRenderersZ() {
			renderers = new Dictionary<Transform, float>();

			Bone[] childBones = gameObject.GetComponentsInChildren<Bone>(true);

			for( int b = 0; b < childBones.Length; b++) {
				renderers[childBones[b].transform] = childBones[b].transform.localPosition.z;
			}

			if (skeleton != null) {
				//find all SkinnedMeshRenderer elements
				skins = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

				for( int i = 0; i < skins.Length; i++) {
					if (skeleton.spriteShadowsShader != null && skins[i].sharedMaterial.shader == skeleton.spriteShadowsShader) {
						renderers[skins[i].transform] = skins[i].transform.localPosition.z;
					}
				}

				//find all SpriteRenderer elements
				spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);

				for( int j = 0; j < spriteRenderers.Length; j++) {
					if (skeleton.spriteShadowsShader != null && spriteRenderers[j].sharedMaterial.shader == skeleton.spriteShadowsShader) {
						renderers[spriteRenderers[j].transform] = spriteRenderers[j].transform.localPosition.z;
					}
				}
			}

			return renderers;
		}

		// Change the renderer normals of the sprites
		public void ChangeRendererNormals(int normal) {
			if (skeleton != null) {
				//find all SkinnedMeshRenderer elements
				skins = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

				if ( propertyBlock == null ) {
					propertyBlock = new MaterialPropertyBlock();
				}

				for( int i = 0; i < skins.Length; i++) {
					if (skeleton.spriteShadowsShader != null && skins[i].sharedMaterial.shader == skeleton.spriteShadowsShader) {
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

				//find all SpriteRenderer elements
				spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);

				for( int j = 0; j < spriteRenderers.Length; j++) {
					if (skeleton.spriteShadowsShader != null && spriteRenderers[j].sharedMaterial.shader == skeleton.spriteShadowsShader) {
						spriteRenderers[j].GetPropertyBlock(propertyBlock);

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
}
