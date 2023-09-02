#nullable enable

using CLIK.Components;
using CLIK.Painting;
using HotSwap;
using UnityEngine;
using Verse;

namespace CLIK
{
	[HotSwappable]
	internal static class App
	{
		public static void Drive(UIComponent root)
		{
			var painter = new Painter();
			using (var _ = new Context.Palette(
				painter,
				new() {
					Anchor = TextAnchor.UpperLeft,
					Color = Color.white,
					Font = GameFont.Small,
				})) {

				switch (Event.current.type) {
					case EventType.Repaint:
						root.HandleRepaint(painter);
						break;
					case EventType.MouseDown:
					case EventType.MouseUp:
					case EventType.MouseDrag:
					case EventType.ContextClick:
					case EventType.ScrollWheel:
					case EventType.KeyDown:
					case EventType.KeyUp:
					case EventType.ValidateCommand:
					case EventType.ExecuteCommand:
						root.HandleInput(painter);
						break;
				}
			}

			Utils.Assert(painter.Count == 0,
				"Not all palettes were popped off of the painter!");
		}
	}
}
