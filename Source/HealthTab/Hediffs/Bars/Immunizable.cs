#nullable enable

using CLIK.Painting;
using HotSwap;
using UnityEngine;
using Verse;
using Context = CLIK.Context;

namespace BetterHealthTab.HealthTab.Hediffs.Bars
{
	[HotSwappable]
	internal sealed class Immunizable : Bar
	{
		private readonly HediffComp_Immunizable _comp;

		public Immunizable(HediffComp_Immunizable comp)
		{
			this._comp = comp;
			this.Primary = comp.parent.LabelColor;
		}

		protected sealed override void RepaintNow(Painter painter)
		{
			base.RepaintNow(painter);
			double infection = CLIK.Math.Lerp(
					this._comp.parent.def.minSeverity,
					this._comp.parent.def.lethalSeverity,
					this._comp.parent.Severity);
			double immunity = this._comp.Immunity;
			var first = new Item(ColorLibrary.Red, infection);
			var second = new Item(ColorLibrary.Green, immunity);
			if (immunity >= infection) {
				(first, second) = (second, first);
			}

			var rect = this.Rect;
			foreach (var x in new Item[] { first, second }) {
				using var _ = new Context.Palette(painter, new() { Color = x.Color });
				painter.FillRect(rect.GetLeft(rect.Width * x.Fill));
			}
		}

		[HotSwappable]
		private readonly struct Item
		{
			public readonly Color Color;

			public readonly double Fill;

			public Item(Color color, double fill)
			{
				this.Color = color;
				this.Fill = fill;
			}
		}
	}
}
