/*
The MIT License (MIT)

Copyright (c) 2014 - 2018 Banbury & Play-Em

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

namespace Boner2D {
	static public class Utils {
		static public float ClampAngle(float x) {
			return x - 360 * Mathf.Floor(x / 360);
		}

		static public float GetWeight(this BoneWeight bw, int index) {
			if (bw.boneIndex0 == index && bw.weight0 > 0) {
				return bw.weight0;
			}

			if (bw.boneIndex1 == index && bw.weight1 > 0) {
				return bw.weight1;
			}

			if (bw.boneIndex2 == index && bw.weight2 > 0) {
				return bw.weight2;
			}

			if (bw.boneIndex3 == index && bw.weight3 > 0) {
				return bw.weight3;
			}

			return 0;
		}

		static public BoneWeight SetWeight(this BoneWeight bw, int index, float value) {
			if (bw.boneIndex0 == index || bw.weight0 == 0) {
				bw.boneIndex0 = index;
				bw.weight0 = value;
			}
			else if (bw.boneIndex1 == index || bw.weight1 == 0) {
				bw.boneIndex1 = index;
				bw.weight1 = value;
			}
			else if (bw.boneIndex2 == index || bw.weight2 == 0) {
				bw.boneIndex2 = index;
				bw.weight2 = value;
			}
			else if (bw.boneIndex3 == index || bw.weight3 == 0) {
				bw.boneIndex3 = index;
				bw.weight3 = value;
			}
			else {
				bw.boneIndex0 = index;
				bw.weight0 = value;
			}

			float max = bw.weight0 + bw.weight1 + bw.weight2 + bw.weight3;

			if (max > 1) {
				bw.weight0 /= max;
				bw.weight1 /= max;
				bw.weight2 /= max;
				bw.weight3 /= max;
			}

			return bw;
		}

		static public void Log(this BoneWeight bw) {
			Debug.Log(
				"Index0: " + bw.boneIndex0 + " Weight0: " + bw.weight0 + "\n" +
				"Index1: " + bw.boneIndex1 + " Weight1: " + bw.weight1 + "\n" +
				"Index2: " + bw.boneIndex2 + " Weight2: " + bw.weight2 + "\n" +
				"Index3: " + bw.boneIndex3 + " Weight3: " + bw.weight3
				);
		}

		static public Mesh Clone(this Mesh m) {
			Mesh copy = new Mesh();

			copy.vertices = m.vertices;
			copy.triangles = m.triangles;
			copy.normals = m.normals;
			copy.bindposes = m.bindposes;
			copy.bounds = m.bounds;
			copy.uv = m.uv;
			copy.uv2 = m.uv2;
			copy.boneWeights = m.boneWeights;
			copy.tangents = m.tangents;

			return copy;
		}

		static public BoneWeight Clone(this BoneWeight bw) {
			BoneWeight ret = new BoneWeight();
			ret.boneIndex0 = bw.boneIndex0;
			ret.boneIndex1 = bw.boneIndex1;
			ret.boneIndex2 = bw.boneIndex2;
			ret.boneIndex3 = bw.boneIndex3;
			ret.weight0 = bw.weight0;
			ret.weight1 = bw.weight1;
			ret.weight2 = bw.weight2;
			ret.weight3 = bw.weight3;

			return ret;
		}
	}
}
