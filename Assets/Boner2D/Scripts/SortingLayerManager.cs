/*
 * Copyright (c) 2017 - 2018 Play-Em
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 *    1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 *
 *    2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 *
 *    3. This notice may not be removed or altered from any source
 *    distribution.
 */
 
// For use with modified SortingLayerExposed by Nick Gravelyn //

using UnityEngine;

namespace Boner2D {

	[ExecuteInEditMode]
	public class SortingLayerManager : MonoBehaviour {
		private SortingLayerExposed[] sortingOrders;

		public void UpdateSortingOrders() {
			sortingOrders = GetComponentsInChildren<SortingLayerExposed>(true);

			InitializeSortingOrders();
		}

		public void InitializeSortingOrders() {
			if (sortingOrders != null && sortingOrders.Length > 0) {
				for (int i = 0; i < sortingOrders.Length; i++) {
					if (sortingOrders[i] != null) {
						sortingOrders[i].InitializeSortingOrder();
					}
				}
			}
		}

		public void SetSortingOrders() {
			if (sortingOrders != null && sortingOrders.Length > 0) {
				for (int i = 0; i < sortingOrders.Length; i++) {
					sortingOrders[i].SetSortingOrder();
				}
			}
		}

		void Start() {
			UpdateSortingOrders();
		}

		void LateUpdate() {
			SetSortingOrders();
		}
	}
}
