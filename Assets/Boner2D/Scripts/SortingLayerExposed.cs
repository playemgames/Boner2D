/*
 * Copyright (c) 2014 Nick Gravelyn
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

// Modified code for Boner2D by Play-Em to animate Renderer sorting orders //

using UnityEngine;

namespace Boner2D {
    // Component does nothing; editor script does all the magic
    [AddComponentMenu("Boner2D/Sorting Layer Exposed")]
	[ExecuteInEditMode]
	// Now it does something
    public class SortingLayerExposed : MonoBehaviour {
		[Header("Use a Sorting Layer Manager to Animate")]
		public int sortingOrder = 0;

		private Renderer ren;

		public void InitializeSortingOrder() {
			if (ren == null) {
				ren = GetComponent<Renderer>();
			}

			if (ren != null) {
				SetSortingOrder(ren.sortingOrder);
			}
		}

		public void SetSortingOrder() {
			SetSortingOrder(this.sortingOrder);
		}

		public void SetSortingOrder(int order) {
			if (sortingOrder != order) {
				sortingOrder = order;
				ren.sortingOrder = sortingOrder;
			}
		}

		void Reset() {
			SortingLayerManager sortingLayerManager = GetComponentInParent<SortingLayerManager>();
			if (sortingLayerManager != null) {
				sortingLayerManager.UpdateSortingOrders();
			}
		}
    }
}