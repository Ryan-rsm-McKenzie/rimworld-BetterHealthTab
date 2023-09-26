#nullable enable

using System;
using System.Linq;
using CLIK;
using CLIK.Components;
using CLIK.Painting;
using Extensions;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterHealthTab.HealthTab.Overview.Pawns
{
	[HotSwappable]
	internal static class Capacities
	{
		public static UIComponent? Create(Pawn pawn)
		{
			if (pawn.Dead) {
				return null;
			} else {
				var stack = new Stack() {
					Spacing = new() {
						Color = Color.clear,
						Size = Window.EntryGap,
					},
					TextStyle = new() {
						Anchor = TextAnchor.MiddleLeft,
					},
				};

				Func<PawnCapacityDef, bool> filter =
					pawn.def.race.Humanlike ? x => x.showOnHumanlikes :
					pawn.def.race.Animal ? x => x.showOnAnimals :
					pawn.def.race.IsMechanoid ? x => x.showOnMechanoids :
					_ => false;
				stack.Fill(
					DefDatabase<PawnCapacityDef>
						.AllDefs
						.Filter(x => filter(x) && x.PawnCanEverDo(pawn))
						.OrderBy(x => x.listOrder)
						.Map(x => new Capacity(pawn, x)));

				return stack;
			}
		}

		[HotSwappable]
		private sealed class Capacity : UIComponent
		{
			private readonly Label _left;

			private readonly Label _right;

			private readonly TipSignal _tooltip;

			public Capacity(Pawn pawn, PawnCapacityDef capacity)
			{
				this._tooltip = new(HealthCardUtility.GetPawnCapacityTip(pawn, capacity));

				var (value, color) = HealthCardUtility.GetEfficiencyLabel(pawn, capacity);
				this._left = new() {
					Parent = this,
					Text = capacity.GetLabelFor(pawn).CapitalizeFirst(),
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
}
