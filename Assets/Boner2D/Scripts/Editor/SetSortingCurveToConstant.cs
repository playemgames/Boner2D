/*
The MIT License (MIT)

Copyright (c) 2017 - 2018 Play-Em

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

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Boner2D {
	// Editor window for setting sortingOrder curves to constant
	public class SetSortingCurveToConstant : EditorWindow {
		private static int columnWidth = 300;
		private List<AnimationClip> animationClips = new List<AnimationClip>();

		public struct Curve {
			public AnimationClip clip;
			public EditorCurveBinding binding;
			public AnimationCurve curve;
		}

		private List<Curve> curves = new List<Curve>();

		private Vector2 scrollPos = Vector2.zero;

		public SetSortingCurveToConstant(){
			animationClips = new List<AnimationClip>();
		}

		void OnSelectionChange() {
			if (Selection.objects.Length > 1 ) {
				Debug.Log ("Length? " + Selection.objects.Length);
				animationClips.Clear();

				foreach ( Object o in Selection.objects ) {
					if ( o is AnimationClip ) {
						animationClips.Add((AnimationClip)o);
					}
				}
			}
			else if (Selection.activeObject is AnimationClip) {
				animationClips.Clear();
				animationClips.Add((AnimationClip)Selection.activeObject);
			} 
			else {
				animationClips.Clear();
			}

			curves.Clear();
			if (animationClips.Count > 0) {
				CheckForCurves();
			}

			this.Repaint();
		}

		[MenuItem ("Window/Boner2D/Set Sorting Curves to Constant")]
		static void Init () {
			GetWindow (typeof (SetSortingCurveToConstant));
		}

		public void OnGUI() {
			// Make sure we have more than one clip selected
			if (animationClips.Count > 0 ) {
				scrollPos = GUILayout.BeginScrollView(scrollPos, GUIStyle.none);

				EditorGUILayout.BeginHorizontal();

				GUILayout.Label("Animation Clip:", GUILayout.Width(columnWidth));

				if ( animationClips.Count == 1 ) {
					animationClips[0] = ((AnimationClip)EditorGUILayout.ObjectField(
						animationClips[0],
						typeof(AnimationClip),
						true,
						GUILayout.Width(columnWidth))
						);
				} 
				else {
					GUILayout.Label("Multiple Anim Clips: " + animationClips.Count, GUILayout.Width(columnWidth));
				}

				EditorGUILayout.EndHorizontal();

				GUILayout.Space(20);

				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button("Set Curves To Constant")) {
					ChangeToConstant();
				}

				EditorGUILayout.EndHorizontal();

				GUILayout.Space(20);

				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.LabelField ("Sorting Curves:");

				EditorGUILayout.EndHorizontal();

				if (curves.Count > 0) {
					for (int i = 0; i < curves.Count; i++) {
						// Show the binding and the keys length it has in the editor window
						EditorGUILayout.LabelField (curves[i].binding.path + "/" + curves[i].binding.propertyName + ", Keys: " + curves[i].curve.length);
						// Debug.Log(animTime);

						// Show the new curve in the editor window
						EditorGUILayout.CurveField (curves[i].curve, GUILayout.Width(columnWidth * 0.75f));
					}
				}

				EditorUtility.ClearProgressBar();

				GUILayout.Space(40);

				GUILayout.EndScrollView();
			} 
			else {
				GUILayout.Label("Please select an Animation Clip");
			}
		}

		void CheckForCurves() {
			for (int i = 0; i < animationClips.Count; i++) {
				// Iterate through all the bindings in the animation clip
				var bindings = AnimationUtility.GetCurveBindings (animationClips[i]);

				for (int n = 0; n < bindings.Length; n++) {

					// If the property is the x,y,z position of the control point then edit the new position
					if (bindings[n].propertyName == "sortingOrder") {

						// Get the animation curve
						AnimationCurve curve = AnimationUtility.GetEditorCurve (animationClips[i], bindings[n]);

						// Get the curve's keyframes
						Keyframe[] keyframes = curve.keys;

						// Add to curve list
						Curve newCurve = new Curve();
						newCurve.clip = animationClips[i];
						newCurve.binding = bindings[n];
						newCurve.curve = curve;

						curves.Add(newCurve);
					}

					// Track progress
					float fChunk = 1f / animationClips.Count;
					float fProgress = (i * fChunk) + fChunk * ((float) n / (float) bindings.Length);

					EditorUtility.DisplayProgressBar(
						"Checking for new curves from animation clips", 
						"How far along the checking has progressed.",
						fProgress);
				}
			}
		}

		void ChangeToConstant() {
			if (curves.Count > 0) {
				for (int i = 0; i < curves.Count; i++) {
					// First you need to create a Editor Curve Binding
					EditorCurveBinding curveBinding = new EditorCurveBinding();

					// I want to change the SortingLayerExposed of the sortingOrder, so I put the typeof(SortingLayerExposed) as the binding type.
					curveBinding.type = typeof(SortingLayerExposed);

					// Regular path to the control point gameobject will be changed to the parent
					curveBinding.path = curves[i].binding.path;

					// This is the property name to change to the matching control point
					curveBinding.propertyName = "sortingOrder";

					Keyframe[] keyframes = curves[i].curve.keys;

					// Create a new curve from these keyframes
					AnimationCurve curve = new AnimationCurve(keyframes);

					// Loop through the keyframes and set the TangentMode to Constant
					for (int k = 0; k < keyframes.Length; k++) {

						// They will always be constants
						AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Constant); 

						AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Constant); 
					}

					// Set the new curve to the animation clip
					AnimationUtility.SetEditorCurve(curves[i].clip, curveBinding, curve);

					// Track progress
					float fChunk = 1f / animationClips.Count;
					float fProgress = (i * fChunk) + fChunk * ((float) i / (float) curves.Count);

					EditorUtility.DisplayProgressBar(
						"Setting Curves to Constant", 
						"How far along the animation editing has progressed.",
						fProgress);
				}
			}

			curves.Clear();
		}

		void OnInspectorUpdate() {
			this.Repaint();
		}
	}
}