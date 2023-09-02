#nullable enable

using System;
using CLIK;
using CLIK.Components;
using CLIK.Painting;
using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterHealthTab.HealthTab.Overview.Pawns
{
	[HotSwappable]
	internal sealed class Pain : UIComponent
	{
		private readonly Label _left;

		private readonly Label _right;

		private readonly TipSignal _tooltip;

		public Pain(Pawn pawn)
		{
			double pain = pawn.health.hediffSet.PainTotal;
			string value;
			Color color;
			if (Mathf.Approximately((float)pain, 0)) {
				value = "NoPain".Translate();
				color = HealthUtility.GoodConditionColor;
			} else if (pain < 0.15) {
				value = "LittlePain".Translate();
				color = ColorLibrary.Grey;
			} else if (pain < 0.40) {
				value = "MediumPain".Translate();
				color = HealthCardUtility.MediumPainColor;
			} else if (pain < 0.80) {
				value = "SeverePain".Translate();
				color = HealthCardUtility.SeverePainColor;
			} else {
				value = "ExtremePain".Translate();
				color = HealthUtility.RedColor;
			}

			this._tooltip = new(HealthCardUtility.GetPainTip(pawn));
			this._left = new() {
				Parent = this,
				Text = "PainLevel".Translate(),
			};
			this._right = new() {
				Parent = this,
				Text = value,
				TextStyle = new() {
					Anchor = TextAnchor.MiddleRight,
					Color = color,
				},
			};
		}

		public static UIComponent? Create(Pawn pawn)
		{
			return pawn.def.race.IsFlesh ? new Pain(pawn) : null;
		}

		public override double HeightFor(double width) => Text.LineHeight;

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RepaintNow(Painter painter)
		{
			if (this.Focused) {
				var rect = this.Rect;
				painter.DrawHighlight(rect);
				painter.PlayMouseoverSounds(rect);
				Utils.TooltipRegion(rect, this._tooltip);
			}
		}

		protected override void ResizeNow()
		{
			var rect = this.Rect;
			this._left.Geometry = rect;
			this._right.Geometry = rect;
		}
	}
}
