#nullable enable

using CLIK.Components;
using CLIK.Painting;
using HotSwap;
using Iterator;
using UnityEngine;
using Verse;

namespace BetterHealthTab.HealthTab.Overview
{
	[HotSwappable]
	internal sealed class Window : UIComponent, IWindow, INavTarget
	{
		public const double EntryGap = 1;

		private readonly ScrollingList _entries;

		private Pawn? _pawn = null;

		public Window()
		{
			this.InvalidateCache();
			this.InvalidateSize();

			this._entries = new() {
				Parent = this,
				Spacing = new() {
					Color = Color.clear,
					Size = EntryGap,
				},
				TextStyle = new() {
					Anchor = TextAnchor.MiddleLeft,
					Color = new(0.90f, 0.90f, 0.90f),
					Font = GameFont.Small,
				},
			};
		}

		public int Index { get; set; } = -1;

		public string Label { get; } = "HealthOverview".Translate();

		public (Thing? Thing, Pawn? Pawn) SelectedThings {
			set {
				if (this._pawn != value.Pawn) {
					this._pawn = value.Pawn;
					this.InvalidateCache();
				}
			}
		}

		public override double HeightFor(double width) => this._entries.HeightFor(width);

		public override double WidthFor(double height) => this._entries.WidthFor(height);

		protected override void RecacheNow()
		{
			var pawn = this._pawn;
			var range = pawn is not null ?
				new UIComponent?[] {
					Pawns.Summary.Create(pawn),
					Pawns.FoodRestrictions.Create(pawn),
					Pawns.Tending.Create(pawn),
					Pawns.MedicalCare.Create(pawn),
					Pawns.Pain.Create(pawn),
					Pawns.Capacities.Create(pawn),
					Compatibility.AutoExtractGenes.Create(pawn),
				} : Iter.Empty<UIComponent?>();
			this._entries.Fill(range.FilterMap(x => x));
		}

		protected override void RepaintNow(Painter painter) { }

		protected override void ResizeNow()
		{
			this._entries.Geometry = this.Rect.Contract(GenUI.GapTiny);
		}
	}
}
