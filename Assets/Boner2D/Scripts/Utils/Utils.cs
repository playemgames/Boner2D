/*
The MIT License (MIT)

Copyright (c) 2014 - 2021 Banbury & Play-Em

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
#if UNITY_2019_1_OR_NEWER
using Unity.Collections;
#endif

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
			#if UNITY_2019_1_OR_NEWER
			copy.ConvertToBoneWeight1();
			#endif
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

		#if UNITY_2019_1_OR_NEWER
		// Convert the mesh's bone weights to BoneWeight1 structs since it is broken in builds since SkinQuality is ignored

		static public void ConvertToBoneWeight1(this Mesh mesh) { 
			if (mesh == null) {
				return;
			}

			// Get the number of bone weights per vertex
			var bonesPerVertex = mesh.GetBonesPerVertex();

			if (bonesPerVertex.Length == 0) { 
				return;
			}

			// Get all the bone weights, in vertex index order
			var boneWeights = mesh.GetAllBoneWeights();

			// Keep track of where we are in the array of BoneWeights, as we iterate over the vertices
			var boneWeightIndex = 0;

			//In order to define for each bones how many vertices are afected, in this case we only set 1 to all because the mesh only have 4 vertices
			NativeArray<byte> bonesPerVert = new NativeArray<byte>(bonesPerVertex.Length, Allocator.Persistent);
			//The bone weights for each vertex must be sorted with the most significant weights first.  Zero weights will be ignored.
			NativeArray<BoneWeight1> weightsB1 = new NativeArray<BoneWeight1>(boneWeights.Length, Allocator.Persistent);

			// Iterate over the vertices
			for (var vertIndex = 0; vertIndex < mesh.vertexCount; vertIndex++) { 
				var numberOfBonesForThisVertex = bonesPerVertex[vertIndex];

				// Debug.Log("Vertex " + vertIndex + " has " + numberOfBonesForThisVertex + " bone influences");

				// For each vertex, iterate over its BoneWeights
				for (var i = 0; i < numberOfBonesForThisVertex; i++) {
					var currentBoneWeight = boneWeights[boneWeightIndex];

					if (i > 0) { 
						// Reorder the weights if the influence is less than the current weight
						if (boneWeights[boneWeightIndex - 1].weight < currentBoneWeight.weight) {
							BoneWeight1 boneWeight1 = boneWeights[boneWeightIndex - 1];

							boneWeight1.boneIndex = currentBoneWeight.boneIndex;
							boneWeight1.weight = currentBoneWeight.weight;

							BoneWeight1 boneWeight2 = boneWeights[boneWeightIndex];

							boneWeight2.boneIndex = boneWeights[boneWeightIndex - 1].boneIndex;
							boneWeight2.weight = boneWeights[boneWeightIndex - 1].weight;

							weightsB1[boneWeightIndex - 1] = boneWeight1;
							weightsB1[boneWeightIndex] = boneWeight2;

							currentBoneWeight = weightsB1[boneWeightIndex];
						}
					}

					// Cull any influences above 2
					if (i > 1) {
						currentBoneWeight.weight = 0f;
					}

					weightsB1[boneWeightIndex] = currentBoneWeight;

					boneWeightIndex++;
				}

				bonesPerVert[vertIndex] = numberOfBonesForThisVertex;
			}

			mesh.SetBoneWeights(bonesPerVert, weightsB1);

			bonesPerVert.Dispose();
			weightsB1.Dispose();
		}
		#endif
	}
}
