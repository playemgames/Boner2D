/*
The MIT License (MIT)

Copyright (c) 2017 - 2018 Play-Em

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
using System.Reflection;

namespace Boner2D {
	public class LocalEulerAngleHint {
	#if UNITY_EDITOR
		static System.Reflection.MethodInfo _getLocalEulerAngles = typeof(Transform).GetMethod("GetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);

		static System.Reflection.MethodInfo _setLocalEulerAngles = typeof(Transform).GetMethod("SetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic);

		static System.Reflection.PropertyInfo _rotationOrder = typeof(Transform).GetProperty("rotationOrder",  BindingFlags.Instance | BindingFlags.NonPublic);

		static public Vector3 GetLocalEulerAngles(Transform t) {
			object[] arg = new object[1];
			arg[0] = _rotationOrder.GetValue(t, null);
			Vector3 val = (Vector3)_getLocalEulerAngles.Invoke(t, arg);
			return val;
		}

		static public void SetLocalEulerAngles(Transform t, Vector3 euler) {
			object[] arg = new object[2];
			arg[0] = _rotationOrder.GetValue(t, null);
			arg[1] = euler;
			_setLocalEulerAngles.Invoke(t, arg);
		}
	#endif
	}
}