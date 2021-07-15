/*
The MIT License (MIT)

Copyright (c) 2021 Play-Em

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
	// Editor window for scaling positions in animation clips
	public class ScalePositionsInClip : EditorWindow {
		private bool changeAllPos = false;
		private static int columnWidth = 300;
		private List<AnimationClip> animationClips;

		private Vector2 scrollPos = Vector2.zero;

		// Animation Bindings
		private EditorCurveBinding xBinding;
		private EditorCurveBinding yBinding;
		private EditorCurveBinding zBinding;
		private EditorCurveBinding originalXBinding;
		private EditorCurveBinding originalYBinding;
		private EditorCurveBinding originalZBinding;

		// Animation Curves
		private AnimationCurve curve;
		private AnimationCurve xCurve;
		private AnimationCurve yCurve;
		private AnimationCurve zCurve;

		private float baseScaleSize = 1f;
		private float changedScaleSize = 1f;

		public ScalePositionsInClip() {
			animationClips = new List<AnimationClip>();
		}

		void OnSelectionChange() {
			if (Selection.objects.Length > 1 ) {
				Debug.Log ("Length? " + Selection.objects.Length);
				animationClips.Clear();
				foreach ( Object o in Selection.objects ) {
					if ( o is AnimationClip ) animationClips.Add((AnimationClip)o);
				}
			}
			else if (Selection.activeObject is AnimationClip) {
				animationClips.Clear();
				animationClips.Add((AnimationClip)Selection.activeObject);
			} 
			else {
				animationClips.Clear();
			}
			
			this.Repaint();
		}

		[MenuItem ("Window/Boner2D/Scale Positions in Clips")]
		static void Init () {
			GetWindow (typeof (ScalePositionsInClip));
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
				
				EditorGUILayout.LabelField ("Positions:");

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.Separator();

				changedScaleSize = EditorGUILayout.Slider("Scale Position", baseScaleSize, 0.001f, 10f);

				if (baseScaleSize != changedScaleSize) {
					baseScaleSize = changedScaleSize;
				}

				EditorGUILayout.Separator();

				if (GUILayout.Button("Scale Positions")) {
					changeAllPos = true;
				}

				EditorGUILayout.EndHorizontal();

				for (int i = 0; i < animationClips.Count; i++) {

					// Iterate through all the bindings in the animation clip
					var bindings = AnimationUtility.GetCurveBindings (animationClips[i]);
					for (int n = 0; n < bindings.Length; n++) {

						// If the property is a rotation then edit the new rotation
						if (bindings[n].propertyName.Contains("m_LocalPosition")) {

							// Get the animation curve
							AnimationCurve curve = AnimationUtility.GetEditorCurve (animationClips[i], bindings[n]);

							// Get the curve's keyframes
							Keyframe[] keyframes = curve.keys;

							// Show the binding and the keys length it has in the editor window
							EditorGUILayout.LabelField (bindings[n].path + "/" + bindings[n].propertyName + ", Keys: " + keyframes.Length);
							// Debug.Log(binding.type);

							if (changeAllPos) {
								// Loop through the keyframes and change the value to the new Y adjustment position
								for (int j = 0; j < keyframes.Length; j++) {
									float timeForKey = keyframes[j].time;

									float originalValue = keyframes[j].value;

									keyframes[j] = new Keyframe();

									// set the time
									keyframes[j].time = timeForKey;

									// set the position to original value
									keyframes[j].value = originalValue * baseScaleSize;

									// Create a new curve from these keyframes
									curve = new AnimationCurve(keyframes);

									Debug.Log("Position " + bindings[n].propertyName + ": " + keyframes[j].value);
								}

								string propertyName = "m_LocalPosition";

								if (bindings[n].propertyName.EndsWith("x")) {
									xBinding = EditorCurveBinding.FloatCurve(bindings[n].path, typeof(Transform), propertyName + ".x");
									xCurve = curve;
									originalXBinding = bindings[n];
								}
								else if (bindings[n].propertyName.EndsWith("y")) {
									yBinding = EditorCurveBinding.FloatCurve(bindings[n].path, typeof(Transform), propertyName + ".y");
									yCurve = curve;
									originalYBinding = bindings[n];
								}
								else if (bindings[n].propertyName.EndsWith("z")) {
									zBinding = EditorCurveBinding.FloatCurve(bindings[n].path, typeof(Transform), propertyName + ".z");
									zCurve = curve;
									originalZBinding = bindings[n];
								}

								if (xBinding.propertyName != null && yBinding.propertyName != null && zBinding.propertyName != null 
								&& xCurve != null && yCurve != null && zCurve != null ) {

									// Remove the old bindings
									AnimationUtility.SetEditorCurve(animationClips[i], originalXBinding, null);
									AnimationUtility.SetEditorCurve(animationClips[i], originalYBinding, null);
									AnimationUtility.SetEditorCurve(animationClips[i], originalZBinding, null);

									// Set the new curve to the animation clip
									AnimationUtility.SetEditorCurve(animationClips[i], xBinding, xCurve);
									AnimationUtility.SetEditorCurve(animationClips[i], yBinding, yCurve);
									AnimationUtility.SetEditorCurve(animationClips[i], zBinding, zCurve);

									xBinding = new EditorCurveBinding();
									// Debug.Log(xBinding.propertyName);
									yBinding = new EditorCurveBinding();
									zBinding = new EditorCurveBinding();

									xCurve = null;
									yCurve = null;
									zCurve = null;
								}

								// Track progress
								float fChunk = 1f / animationClips.Count;
								float fProgress = (i * fChunk) + fChunk * ((float) n / (float) bindings.Length);
								
								EditorUtility.DisplayProgressBar(
									"Replacing Positions with new Scaled Positions", 
									"How far along the animation editing has progressed.",
									fProgress);
							}

							// Loop through the keyframes and change the curve to the new control points curve
							for (int k = 0; k < keyframes.Length; k++) {
								// Show the new curve in the editor window
								EditorGUILayout.CurveField (curve, GUILayout.Width(columnWidth * 0.75f));
							}
						}
					}
				}

				EditorUtility.ClearProgressBar();

				// Reset the button
				changeAllPos = false;

				GUILayout.Space(40);

				GUILayout.EndScrollView();
			} 
			else {
				GUILayout.Label("Please select an Animation Clip");
			}
		}

		void OnInspectorUpdate() {
			this.Repaint();
		}
	}
}