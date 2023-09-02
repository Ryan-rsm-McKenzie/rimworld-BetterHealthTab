#nullable enable

using CLIK.Components;
using CLIK.Extensions;
using HotSwap;
using UnityEngine;

namespace CLIK
{
	internal interface IButton : IUIComponent
	{
		public abstract void OnButtonDown();
	}

	[HotSwappable]
	internal sealed class ButtonGroup
	{
		private readonly WeakReference<IButton> _which = new();

		public void HandleSelection(IButton button, bool focused)
		{
			Utils.Assert(!Event.current.IsRepaint(),
				"The Button Group needs to run during the input event loop inside InputNow!");

			var current = Event.current;
			if (current.IsMouseUp(0)) {
				this._which.Target = null;
				if (focused) {
					Event.current.Use();
				}
			} else if (focused && (current.IsMouseDown(0) || current.IsMouseDragging(0))) {
				Event.current.Use();
				if (this._which.Target != button) {
					this._which.Target = button;
					button.OnButtonDown();
				}
			}
		}
	}
}
