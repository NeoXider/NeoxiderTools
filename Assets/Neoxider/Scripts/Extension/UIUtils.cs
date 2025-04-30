using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Neo
{
	/// <summary>
	/// Utility methods for Unity UI operations
	/// </summary>
	public static class UIUtils
	{
		/// <summary>
		/// Gets all UI elements hit by the current mouse position
		/// </summary>
		/// <returns>List of raycast results for UI elements under the cursor</returns>
		public static List<RaycastResult> GetUIElementsUnderCursor()
		{
			if (EventSystem.current == null)
				return new List<RaycastResult>();

			PointerEventData pointer = new PointerEventData(EventSystem.current);
			pointer.position = Input.mousePosition;
			List<RaycastResult> results = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointer, results);
			return results;
		}
	}
}