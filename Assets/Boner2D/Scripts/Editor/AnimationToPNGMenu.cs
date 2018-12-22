using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Boner2D {
	public class AnimationToPNGMenu  {
		[MenuItem("Boner2D/Animation To PNG")]
		public static void Create() {
			GameObject o = new GameObject("Animation To PNG");
			Undo.RegisterCreatedObjectUndo(o, "Create skeleton");
			o.AddComponent<AnimationToPNG>();
		}
	}
}
