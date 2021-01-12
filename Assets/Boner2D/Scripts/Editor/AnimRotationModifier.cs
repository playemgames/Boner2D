/*
The MIT License (MIT)

Copyright (c) 2018 Play-Em

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
using System.Collections;
using System.Collections.Generic;

namespace Boner2D {

	[InitializeOnLoad]
	public class AnimRotationModifier {
		static AnimRotationModifier () {
			AnimationUtility.onCurveWasModified += OnCurveWasModified;
		}

		static void OnCurveWasModified (AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType modified) {
			AnimationUtility.onCurveWasModified -= OnCurveWasModified;

			bool fixRot = (Event.current == null || (Event.current != null && Event.current.type != EventType.ExecuteCommand));

			if (fixRot 
			&& modified == AnimationUtility.CurveModifiedType.CurveModified 
			&& binding.type == typeof(Transform) 
			&& binding.propertyName.Contains("localEulerAnglesRaw")
			&& AnimWindowModified.rootGameObject != null) {
				Transform transform = AnimWindowModified.rootGameObject.transform.Find(binding.path);
				Vector3 eulerAngles = LocalEulerAngleHint.GetLocalEulerAngles(transform);

				int frame = AnimWindowModified.frame;

				AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

				for (int i = 0; i < curve.length; i++) {
					Keyframe keyframe = curve[i];

					int currentFrame = (int)AnimWindowModified.TimeToFrame(keyframe.time);

					if (frame == currentFrame) {
						if (binding.propertyName.Contains(".x")) {
							if (keyframe.value != eulerAngles.x) {
								keyframe.value = eulerAngles.x;

								//Debug.Log(binding.propertyName + "  " + keyframe.value + ": " + eulerAngles.x.ToString());
							}
						}
						else if (binding.propertyName.Contains(".y")) {
							if (keyframe.value != eulerAngles.y) {
								keyframe.value = eulerAngles.y;

								//Debug.Log(binding.propertyName + "  " + keyframe.value + ": " + eulerAngles.y.ToString());
							}
						}
						else if (binding.propertyName.Contains(".z")) {
							if (keyframe.value != eulerAngles.z) {
								keyframe.value = eulerAngles.z;

								//Debug.Log(binding.propertyName + "  " + keyframe.value + ": " + eulerAngles.z.ToString());
							}
						}

						curve.MoveKey(i, keyframe);

						AnimWindowModified.UpdateTangentsFromModeSurrounding(curve, i);

						break;
					}
				}

				AnimationUtility.SetEditorCurve(clip, binding, curve);
			}

			AnimationUtility.onCurveWasModified += OnCurveWasModified;
		}
	}
}
