/*
The MIT License (MIT)

Copyright (c) 2014 - 2017 Banbury & Play-Em & SirKurt

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
using System;
using System.Collections.Generic;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Boner2D {
	[ExecuteInEditMode]
	[RequireComponent(typeof(Bone))]
	public class InverseKinematics : MonoBehaviour {
		[HideInInspector]
		public float influence = 1.0f;
		public int chainLength = 0;
		public Transform target;

		private int n = 0;

		private Bone parent;

		private int _chainLength {
			get {
				if (chainLength > 0) {
					return chainLength;
				}
				else {
					n = 0;

					parent = bone.parent;

					while (parent != null) {
						n++;
						parent = parent.parent;
					}

					return n + 1;
				}
			}
		}

		public int iterations = 5;

		[Range(0.01f, 1)]
		public float damping = 1;

		public Node[] angleLimits = new Node[0];

		Dictionary<Transform, Node> nodeCache; 
		[System.Serializable]
		public class Node {
			public Transform Transform;
			[Range(0,360)]
			public float from;
			[Range(0,360)]
			public float to;
		}

		private int i;
		private Transform boneTransform;
		private Bone _bone = null;
		public Bone bone {
			get {
				if (_bone == null) {
					_bone = GetComponent<Bone>();
				}

				return _bone;
			}
		}

		private Vector3 rootPos;
		private Vector3 root2tip;
		private Vector3 root2target;
		private Vector3 dir;
		private Vector3 targetPos;

		private float angle;
		private float yAngle;
		private int yModifier;

		private float newAngle;
		private float sign;

		private Quaternion newRotation;
		private Quaternion boneRotation;

		void Start() {
			// Cache optimization
			if (angleLimits.Length > 0 
			&& nodeCache != null 
			&& nodeCache.Count != angleLimits.Length 
			|| angleLimits.Length > 0 
			&& nodeCache == null){
				nodeCache = new Dictionary<Transform, Node>(angleLimits.Length);

				foreach (var node in angleLimits) {
					if (node != null 
					&& node.Transform != null 
					&& nodeCache != null 
					&& !nodeCache.ContainsKey(node.Transform)) {
						nodeCache.Add(node.Transform, node);
					}
				}
			}
		}

		#if UNITY_EDITOR
		void Update() {
			if (!Application.isPlaying) {
				if (chainLength < 0) {
					chainLength = 0;
				}

				Start();
			}
		}
		#endif

		/**
		 * Code ported from the Gamemaker tool SK2D by Drifter
		 * http://gmc.yoyogames.com/index.php?showtopic=462301
		 * Angle Limit code adapted from Veli-Pekka Kokkonen's SimpleCCD http://goo.gl/6oSzDx
		 **/
		public void ResolveSK2D() { 
			if (target != null) {
				targetPos = target.transform.position;
			}

			for (int it = 0; it < iterations; it++) {
				i = _chainLength;

				boneTransform = transform;

				while (--i >= 0 && boneTransform != null) {
					rootPos = boneTransform.position;

					// Z position can be different than 0
					root2tip = (bone.Head - rootPos);
					root2target = (((target != null) ? targetPos : bone.Head) - rootPos);

					// Calculate how much we should rotate to get to the target
					angle = SignedAngle(root2tip, root2target, boneTransform);

					// If you want to flip the bone on the y axis invert the angle
					yAngle = Utils.ClampAngle(boneTransform.rotation.eulerAngles.y);

					// If the skeleton is rotated then make sure the angle is modified accordingly
					yModifier = (bone.skeleton != null && bone.skeleton.transform.localRotation.eulerAngles.y == 180.0f 
									&& bone.skeleton.transform.localRotation.eulerAngles.x == 0.0f) ? 1 : -1;

					if (yAngle > 90 && yAngle < 270) {
						angle *= yModifier;
					}

					// "Slows" down the IK solving
					angle *= damping;

					// Wanted angle for rotation
					boneRotation = boneTransform.localRotation;

					angle = -(angle - boneRotation.eulerAngles.z);

					if (nodeCache != null && nodeCache.ContainsKey(boneTransform)) {
						// Clamp angle in local space
						var node = nodeCache[boneTransform];
						angle = ClampAngle(angle, node.from, node.to);
					}

					newRotation = Quaternion.Euler(boneRotation.eulerAngles.x, boneRotation.eulerAngles.y, angle);

					if (!IsNaNRot(newRotation)) {
						boneTransform.localRotation = newRotation;
					}

					boneTransform = boneTransform.parent;
				}
			}
		}

		public float SignedAngle (Vector3 a, Vector3 b, Transform t) {
			newAngle = Vector3.Angle (a, b);

			// Use skeleton as root, change dir if the rotation is flipped
			dir = (bone.skeleton != null && bone.skeleton.transform.localRotation.eulerAngles.y == 180.0f && bone.skeleton.transform.localRotation.eulerAngles.x == 0.0f) ? Vector3.forward : Vector3.back;

			sign = Mathf.Sign (Vector3.Dot (dir, Vector3.Cross (a, b)));

			newAngle = newAngle * sign;

			// Flip sign if character is turned around
			newAngle *= Mathf.Sign(t.root.localScale.x);

			return newAngle;
		}

		float ClampAngle(float thisAngle, float from, float to) {
			thisAngle = Mathf.Abs((angle % 360) + 360) % 360;

			//Check limits
			if (from > to && (thisAngle > from || thisAngle < to)) {
				return thisAngle;
			}
			else if (to > from && (thisAngle < to && thisAngle > from)) {
				return thisAngle;
			}

			//Return nearest limit if not in bounds
			return (Mathf.Abs(thisAngle - from) < Mathf.Abs(thisAngle - to) 
				&& Mathf.Abs(thisAngle - from) < Mathf.Abs((thisAngle + 360) - to)) 
				|| (Mathf.Abs(thisAngle - from - 360) < Mathf.Abs(thisAngle - to) 
				&& Mathf.Abs(thisAngle - from - 360) < Mathf.Abs((thisAngle + 360) - to)) ? from : to;
		}

		private bool IsNaNRot(Quaternion q) {
			return (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w));
		}
	}
}
