#nullable enable

using System.Collections.Generic;
using CLIK.Extensions;
using Extensions;
using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterHealthTab.HealthTab.Hediffs.Bars
{
	[HotSwappable]
	internal sealed class PartDamage : BasicBar
	{
		private readonly double _progress;

		private PartDamage(Color foreground, string tooltip, double progress)
		{
			this.Primary = foreground;
			this.Tooltip = tooltip;
			this._progress = progress;
		}

		protected override double Fill => this._progress;

		public static (PartDamage? Bar, Color Color) Create(Pawn pawn, BodyPartRecord? record, IEnumerable<Hediff> hediffs)
		{
			if (record is null) {
				return (null, ColorLibrary.Grey);
			}

			double damage = hediffs.GetPartDamage();
			double min = record.def.destroyableByDamage || damage.IsInfinity() ? 0 : 1;
			double max = record.GetMaxHealth(pawn);
			double current = CLIK.Math.Clamp(max - damage, min, max);

			if (current == max) {
				return (null, HealthUtility.GoodConditionColor);
			}

			string tooltip = HealthCardUtility.GetTooltip(pawn, record);
			if (current == 0) {
				var color = Color.Lerp(Color.black, ColorLibrary.Red, 0.15f);
				return (new PartDamage(color, tooltip, 1.00), ColorLibrary.Grey);
			} else {
				double progress = CLIK.Math.Clamp(System.Math.Round(current / max, 2), 0, 1);
				var foreground = Color.Lerp(HealthUtility.RedColor, HealthUtility.SlightlyImpairedColor, (float)progress);
				return (new PartDamage(foreground, tooltip, progress), foreground);
			}
		}
	}
}
