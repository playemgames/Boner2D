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
using System;
using System.Collections;
using System.Reflection;

namespace Boner2D {
	[InitializeOnLoad]
	public class AnimWindowModified {
		static protected Type animationWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimationWindow");
		static protected Type animationWindowStateType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.AnimationWindowState");
		static protected Type animationKeyTimeType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.AnimationKeyTime");
		static protected Type animEditorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimEditor");
		static protected Type curveUtilityType = typeof(EditorWindow).Assembly.GetType("UnityEditor.CurveUtility");

		static public FieldInfo animEditorFieldInfo = null;
		static public FieldInfo stateFieldInfo = null;

		static public PropertyInfo currentFramePropertyInfo = null;
		static public PropertyInfo activeRootGameObjectPropertyInfo = null;

		static public MethodInfo frameToTimeMethodInfo = null;
		static public MethodInfo timeToFrameMethodInfo = null;
		static public MethodInfo timeMethodInfo = null;
		static public MethodInfo updateTangentsFromModeSurroundingMethodInfo = null;

		static public void Initialize() {
			animEditorFieldInfo = animationWindowType.GetField( "m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic );

			stateFieldInfo = animEditorType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);

			currentFramePropertyInfo = animationWindowStateType.GetProperty("currentFrame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			activeRootGameObjectPropertyInfo = animationWindowStateType.GetProperty("activeRootGameObject", BindingFlags.Instance | BindingFlags.Public);

			frameToTimeMethodInfo = animationWindowStateType.GetMethod("FrameToTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			timeToFrameMethodInfo = animationWindowStateType.GetMethod("TimeToFrame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			timeMethodInfo = animationKeyTimeType.GetMethod("Time", BindingFlags.Public | BindingFlags.Static);

			updateTangentsFromModeSurroundingMethodInfo = curveUtilityType.GetMethod("UpdateTangentsFromModeSurrounding", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		}

		static public ScriptableObject animEditor {
			get {
				if (AnimWindowModified.animationWindow != null && animEditorFieldInfo != null) {
					return (ScriptableObject)animEditorFieldInfo.GetValue( AnimWindowModified.animationWindow );
				}
				return null;
			}
		}

		static public object state {
			get {
				if (animEditor && stateFieldInfo != null) {
					return stateFieldInfo.GetValue(animEditor);
				}
				return null;
			}
			
		}

		static public int frame {
			get {
				if (state != null && currentFramePropertyInfo != null) {
					return (int)currentFramePropertyInfo.GetValue(state, null);
				}

				return 0;
			}

			set {
				if (state != null && currentFramePropertyInfo != null) {
					currentFramePropertyInfo.SetValue(state, value, null);
				}
			}
		}

		static EditorWindow _animationWindow = null;
		static public EditorWindow animationWindow {
			get	{
				if ( _animationWindow == null ) {
					_animationWindow = FindWindowOpen( animationWindowType );
				}
				return _animationWindow;
			}
		}

		static EditorWindow FindWindowOpen(Type winType) {
			UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll( winType );

			foreach ( UnityEngine.Object o in objects ) {
				if ( o.GetType() == winType ) {
					return (EditorWindow)o;
				}
			}

			return null;
		}

		static AnimWindowModified() {
			Initialize();
		}

		static public GameObject rootGameObject {
			get {
				if (state != null && activeRootGameObjectPropertyInfo != null) {
					return (GameObject)activeRootGameObjectPropertyInfo.GetValue(state, null);
				}

				return null;
			}
		}

		static public float FrameToTime(int frame) {
			if (state != null && frameToTimeMethodInfo != null) {
				object[] parameters = { (float)frame };
				return (float)frameToTimeMethodInfo.Invoke(state, parameters);
			}
			return 0f;
		}

		static public float TimeToFrame(float time) {
			if (state != null && timeToFrameMethodInfo != null) {
				object[] parameters = { (float)time };
				return (float)timeToFrameMethodInfo.Invoke(state, parameters);
			}
			return 0f;
		}

		static public object AnimationKeyTime(float time, float frameRate) {
			if (timeMethodInfo != null) {
				object[] parameters = { time, frameRate };
				return timeMethodInfo.Invoke(null, parameters);
			}

			return null;
		}

		static public void UpdateTangentsFromModeSurrounding(AnimationCurve curve, int index) {
			if (updateTangentsFromModeSurroundingMethodInfo != null) {
				object[] parameters = { curve, index };
				updateTangentsFromModeSurroundingMethodInfo.Invoke(null, parameters );
			}
		}
	}
}
