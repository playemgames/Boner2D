/*
The MIT License (MIT)

Copyright (c) 2018 Play-Em

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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Boner2D {
	// Editor window for batch copying multiple curves with same data
	public class BatchCopyCurvesEditor : EditorWindow {
		private static int columnWidth = 300;
		private List<AnimationClip> copyFromClips = new List<AnimationClip>();
		private List<AnimationClip> copyToClips = new List<AnimationClip>();

		public struct Curve {
			public AnimationClip clip;
			public EditorCurveBinding binding;
			public AnimationCurve curve;
		}

		private Vector2 scrollPos = Vector2.zero;

		private string filter = "";
		private string pathFilter = "";
		private string propertyName;
		private EditorCurveBinding thisCurve;
		private string pathFilterNotContains = "";
		private string pathFilterToReplace = "";
		private string pathFilterReplace = "";

		public BatchCopyCurvesEditor(){
			copyFromClips = new List<AnimationClip>();
			copyToClips = new List<AnimationClip>();
		}

		private List<AnimationClip> CopyClips(List<AnimationClip> animationClips) {
			if (Selection.objects.Length > 1 ) {
				Debug.Log ("Length? " + Selection.objects.Length);
				animationClips.Clear();

				foreach ( Object o in Selection.objects ) {
					if ( o is AnimationClip ) {
						animationClips.Add((AnimationClip)o);
					}
				}

				animationClips = animationClips.OrderBy(anim => anim.name).ToList();
			}
			else if (Selection.activeObject is AnimationClip) {
				animationClips.Clear();
				animationClips.Add((AnimationClip)Selection.activeObject);
			} 
			else {
				animationClips.Clear();
			}

			this.Repaint();

			return animationClips;
		}

		[MenuItem ("Window/Boner2D/Batch Copy Curves Editor")]
		static void Init () {
			GetWindow (typeof (BatchCopyCurvesEditor));
		}

		public void OnGUI() {
			// Make sure we have more than one clip selected
			if (copyFromClips.Count > 0 ) {
				scrollPos = GUILayout.BeginScrollView(scrollPos, GUIStyle.none);

				EditorGUILayout.BeginHorizontal();

				filter = EditorGUILayout.TextField("Filter by Property Name:", filter, GUILayout.Width(columnWidth));

				EditorGUILayout.EndHorizontal();

				GUILayout.Space(20);

				EditorGUILayout.BeginHorizontal();

				pathFilter = EditorGUILayout.TextField("Path Contains:", pathFilter, GUILayout.Width(columnWidth));

				GUILayout.Space(20);

				pathFilterNotContains = EditorGUILayout.TextField("Path Does Not Contain:", pathFilterNotContains, GUILayout.Width(columnWidth));

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();

				pathFilterToReplace = EditorGUILayout.TextField("Path Filter to Replace:", pathFilterToReplace, GUILayout.Width(columnWidth));

				GUILayout.Space(20);

				pathFilterReplace = EditorGUILayout.TextField("Path Filter Replacement:", pathFilterReplace, GUILayout.Width(columnWidth));

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();

				GUILayout.Label("Copy From Clips:", GUILayout.Width(columnWidth));

				GUILayout.Space(20);

				GUILayout.Label("Copy To Clips:", GUILayout.Width(columnWidth));

				EditorGUILayout.EndHorizontal();

				for ( int i = 0; i < copyFromClips.Count; i++ ) {

					EditorGUILayout.BeginHorizontal();
					copyFromClips[i] = ((AnimationClip)EditorGUILayout.ObjectField(
						copyFromClips[i],
						typeof(AnimationClip),
						true,
						GUILayout.Width(columnWidth))
						);

					GUILayout.Space(20);

					if ( i < copyToClips.Count) {
						copyToClips[i] = ((AnimationClip)EditorGUILayout.ObjectField(
							copyToClips[i],
							typeof(AnimationClip),
							true,
							GUILayout.Width(columnWidth))
							);
					}

					EditorGUILayout.EndHorizontal();
				}

				if (copyToClips.Count < 1) {

					EditorGUILayout.BeginHorizontal();

					GUILayout.Space(20);

					GUILayout.Label("Please select an Animation Clip and Press Copy To Clips Button", GUILayout.Width(columnWidth));

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.BeginHorizontal();

				GUILayout.Space(20);

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button("Copy From Clips", GUILayout.Width(columnWidth))) {
					copyFromClips = CopyClips(copyFromClips);
				}

				GUILayout.Space(20);

				if (GUILayout.Button("Copy To Clips", GUILayout.Width(columnWidth))) {
					copyToClips = CopyClips(copyToClips);
				}

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Separator();

				EditorGUILayout.BeginHorizontal();

				if (copyFromClips.Count > 0 && copyToClips.Count > 0 
				&& copyFromClips.Count == copyToClips.Count) {
					if (GUILayout.Button("Add Curves to Clips", GUILayout.Width(columnWidth * 0.5f))) {
						AddCurves();
						Debug.Log("Added curves to clips");
					}
				}
				else {
					EditorGUILayout.HelpBox("Copy From Clips must be equal to Copy To Clips.", MessageType.Error);
				}

				EditorGUILayout.EndHorizontal();

				EditorUtility.ClearProgressBar();

				GUILayout.Space(40);

				GUILayout.EndScrollView();
			} 
			else {
				GUILayout.Label("Please select an Animation Clip and Press Copy From Clips Button");

				if (GUILayout.Button("Copy From Clips")) {
					copyFromClips = CopyClips(copyFromClips);
				}
			}
		}

		void OnInspectorUpdate() {
			this.Repaint();
		}

		void AddCurves() {
			if (copyToClips.Count > 0 && copyToClips.Count == copyFromClips.Count) {

				float fProgress = 0.0f;

				AssetDatabase.StartAssetEditing();

				for ( int iCurrentClip = 0; iCurrentClip < copyToClips.Count; iCurrentClip++ ) {
					AnimationClip animationClip =  copyToClips[iCurrentClip];

					Undo.RecordObject(animationClip, "Animation Curves Copier Change");

					EditorCurveBinding[] curves = AnimationUtility.GetCurveBindings(copyFromClips[iCurrentClip]);

					for (int i = 0; i < curves.Length; i++) {
						EditorCurveBinding binding = curves[i];

						pathFilter = pathFilter.Replace(", ", ",");
						string[] pathKeywords = pathFilter.Split(',');

						filter = filter.Replace(", ", ",");
						string[] filterKeywords = filter.Split(',');

						pathFilterNotContains = pathFilterNotContains.Replace(", ", ",");
						string[] pathNotKeywords = pathFilterNotContains.Split(',');

						if (pathKeywords.Any(x => binding.path.Contains(x)) 
						&& filterKeywords.Any(x => binding.propertyName.Contains(x)) 
						&& (!string.IsNullOrEmpty(pathFilterNotContains) 
						&& !pathNotKeywords.Any(x => binding.path.Contains(x)) 
						|| string.IsNullOrEmpty(pathFilterNotContains))) {
							AnimationCurve curve = AnimationUtility.GetEditorCurve(copyFromClips[iCurrentClip], binding);

							if (binding.path.Contains(pathFilterToReplace) 
							&& !string.IsNullOrEmpty(pathFilterToReplace) 
							&& !string.IsNullOrEmpty(pathFilterReplace)) {
								binding.path = binding.path.Replace(pathFilterToReplace, pathFilterReplace);
							}

							if ( curve != null ) {
								AnimationUtility.SetEditorCurve(animationClip, binding, null);
								AnimationUtility.SetEditorCurve(animationClip, binding, curve);
							}
							else {
								ObjectReferenceKeyframe[] objectReferenceCurve = AnimationUtility.GetObjectReferenceCurve(copyFromClips[iCurrentClip], binding);
								AnimationUtility.SetObjectReferenceCurve(animationClip, binding, null);
								AnimationUtility.SetObjectReferenceCurve(animationClip, binding, objectReferenceCurve);
							}
						}

						// Update the progress meter
						float fChunk = 1f / copyToClips.Count;
						fProgress = (iCurrentClip * fChunk) + fChunk * ((float) i / (float) curves.Length);

						EditorUtility.DisplayProgressBar(
							"Animation Curves Copier Progress", 
							"Copying curves to animation clips. . .",
							fProgress);
					}

				}

				AssetDatabase.StopAssetEditing();

				EditorUtility.ClearProgressBar();

				this.Repaint();
			}
			else {
				EditorGUILayout.HelpBox("Copy From Clips must be equal to Copy To Clips.", MessageType.Error);
			}
		}
	}
}