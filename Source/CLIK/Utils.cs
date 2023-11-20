#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;
using Rect = CLIK.Painting.Rect;

namespace CLIK
{
	[HotSwappable]
	internal static class Utils
	{
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Assert(
			bool condition,
			string reason = "",
			[CallerMemberName] string caller = "",
			[CallerFilePath] string file = "",
			[CallerLineNumber] int line = 0)
		{
			if (!condition) {
				if (!reason.IsEmpty()) {
					reason = $"{reason}, ";
				}
				Log.Error($"Assertion failed: {reason}{file}:{caller}({line})");
			}
		}

		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void DebugMessage(string message)
		{
			Messages.Message(message, MessageTypeDefOf.NeutralEvent, false);
		}

		public static bool MouseIsOver(Rect region)
		{
			return region.Contains((Point)Event.current.mousePosition) && !Mouse.IsInputBlockedNow;
		}

		public static void TooltipRegion(Rect region, TipSignal signal, int priority)
		{
			Assert(Event.current.IsRepaint(),
				"Tooltips are only valid during repaint events!");
			Assert(MouseIsOver(region),
				"Tooltips are only valid if the mouse is over the given region!");

			if (Event.current.IsRepaint() && !SteamDeck.KeyboardShowing) {
				if (DebugViewSettings.drawTooltipEdges) {
					using var _ = new Context.GUIStyle(color: Color.white);
					Widgets.DrawBox(region.ToUnity(), 1);
				}

				if (!TooltipHandler.activeTips.TryGetValue(signal.uniqueId, out var tip)) {
					tip = new(signal) {
						firstTriggerTime = Time.realtimeSinceStartup,
					};
					TooltipHandler.activeTips.Add(signal.uniqueId, tip);
				}

				Assert(signal.textGetter is null || signal.text.IsEmpty(),
					"Only one text method can be assigned at a time!");
				tip.lastTriggerFrame = TooltipHandler.frame;
				tip.signal.text = signal.text;
				tip.signal.textGetter = signal.textGetter;
				tip.signal.priority = (TooltipPriority)priority;
			}
		}

		public static void TooltipRegion(Rect region, TipSignal signal) => TooltipRegion(region, signal, (int)signal.priority);
	}
}
