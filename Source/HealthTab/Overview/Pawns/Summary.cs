#nullable enable

using CLIK.Components;
using HotSwap;
using UnityEngine;
using Verse;

namespace BetterHealthTab.HealthTab.Overview.Pawns
{
	[HotSwappable]
	internal static class Summary
	{
		public static UIComponent? Create(Pawn pawn)
		{
			string key = pawn.gender != Gender.None ?
				"PawnSummaryWithGender" :
				"PawnSummary";
			string summary = key
				.Translate(pawn.Named("PAWN"))
				.CapitalizeFirst();
			return new Label() {
				Mouseover = new() {
					Highlight = Color.white,
					Sounds = true,
					Tooltip = new(() => pawn.ageTracker.AgeTooltipString, Tab.StableTooltipID),
				},
				Text = summary,
				TextStyle = new() {
					Font = GameFont.Tiny,
				},
			};
		}
	}
}
