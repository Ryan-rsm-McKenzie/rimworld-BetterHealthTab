#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CLIK;
using CLIK.Components;
using CLIK.Painting;
using HotSwap;
using Iterator;
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
			var (comps, other) = hediffs.Partition(x => x is HediffWithComps);
			var bar = comps
				.Cast<HediffWithComps>()
				.Map(x => x.comps)
				.Flatten()
				.FilterMap(x => {
					Bar? result = x switch {
						HediffComp_Chargeable chargeable => new Chargeable(chargeable),
						HediffComp_Disappears disappears => new Disappears(disappears),
						HediffComp_Immunizable immunizable => new Immunizable(immunizable),
						HediffComp_Pollution pollution => Severity.Create(pollution),
						HediffComp_RandomizeSeverityPhases phases => Severity.Create(phases),
						HediffComp_SelfHeal heal => Severity.Create(heal),
						HediffComp_SeverityFromGas gas => Severity.Create(gas),
						HediffComp_SeverityFromHemogen hemogen => Severity.Create(hemogen),
						HediffComp_SeverityModifierBase modifier => Severity.Create(modifier),
						_ => null,
					};
					return result;
				})
				.Nth(0);
			bar ??= other
				.FilterMap(x => Severity.CreateSpecial(x) ?? Compatibility.UltratechAlteredCarbon.Create(x))
				.Nth(0);
			return bar;
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
