#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CLIK;
using CLIK.Components;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Context = CLIK.Context;

namespace BetterHealthTab.HealthTab.Hediffs.Bars
{
	[HotSwappable]
	internal abstract class Bar : UIComponent
	{
		public Color Primary { get; set; } = Color.white;

		protected TipSignal? Tooltip { private get; set; } = null;

		public static Bar? Create(IEnumerable<Hediff> hediffs)
		{
			var comps = hediffs
				.FilterMap(x => (x as HediffWithComps)?.comps)
				.Flatten()
				.ToList();

			var immunizable = comps
				.FilterMap(x => x as HediffComp_Immunizable)
				.Nth(0);
			if (immunizable is not null) {
				return new Immunizable(immunizable);
			}

			var disappears = comps
				.FilterMap(x => x as HediffComp_Disappears)
				.Nth(0);
			if (disappears is not null) {
				return new Disappears(disappears);
			}

			var chargeable = comps
					.FilterMap(x => x as HediffComp_Chargeable)
					.Nth(0);
			if (chargeable is not null) {
				return new Chargeable(chargeable);
			}

			var modifiers = comps
				.FilterMap(x => x as HediffComp_SeverityModifierBase)
				.Filter(x => x is not HediffComp_TendDuration)
				.Filter(x => !x.parent.IsPermanent())
				.Map(x => x.parent)
				.Chain(hediffs.Filter(x =>
					x.GetType() == typeof(Hediff) &&
					x.def.initialSeverity > 0 &&
					(x.def.maxSeverity < double.MaxValue || x.def.lethalSeverity > 0)))
				.Nth(0);
			if (modifiers is not null) {
				double max =
					modifiers.def.maxSeverity < float.MaxValue ? modifiers.def.maxSeverity :
					modifiers.def.lethalSeverity > 0 ? modifiers.def.lethalSeverity :
					1.00;
				return new Severity(modifiers, max, false);
			}

			var high = hediffs
				.FilterMap(x => x as Hediff_High)
				.Nth(0);
			if (high is not null) {
				return new Severity(high, 1.00, false);
			}

			return null;
		}

		public sealed override double HeightFor(double width) => 4;

		public sealed override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RepaintNow(Painter painter)
		{
			var rect = this.Rect;
			using var _ = new Context.Palette(
				painter,
				new() {
					Color = Color.black with { a = 0.30f },
				});

			painter.FillRect(rect);
			if (this.Focused && this.Tooltip.HasValue) {
				Utils.TooltipRegion(rect, this.Tooltip.Value);
			}
		}
	}
}
