#nullable enable

using System;
using CLIK.Components;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BetterHealthTab.HealthTab.Operations
{
	[HotSwappable]
	internal sealed class Window : UIComponent, IWindow, INavTarget
	{
		private readonly ScrollingList _bills;

		private readonly ButtonText _button;

		private IBillGiver? _billGiver = null;

		private string _label = string.Empty;

		private Pawn? _pawn = null;

		private Thing? _thing = null;

		public Window()
		{
			this.InvalidateCache();
			this.ListensForVisibility = true;

			this._button = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
				},
				Text = "AddBill".Translate(),
				OnPress = () => {
					var tab = Tab.Instance!;
					if (tab.HasDockedWindow()) {
						tab.DockWindow(false);
						SoundDefOf.TabClose.PlayOneShotOnCamera();
					} else {
						tab.DockWindow(true);
						SoundDefOf.TabOpen.PlayOneShotOnCamera();
					}
				},
				TextStyle = new() {
					Anchor = TextAnchor.MiddleCenter,
				},
			};
			this._bills = new() {
				Parent = this,
				Spacing = new() {
					Color = Color.clear,
					Size = GenUI.GapTiny,
				},
				Stripes = Color.white,
				TextStyle = new() {
					Anchor = TextAnchor.MiddleCenter,
				},
			};
		}

		public int Index { get; set; } = -1;

		public string Label {
			get => this._label;
			set {
				if (this._label != value) {
					this._label = value;
					this.Parent?.InvalidateSize();
				}
			}
		}

		public (Thing? Thing, Pawn? Pawn) SelectedThings {
			set {
				bool dirty = false;

				if (this._pawn != value.Pawn) {
					this._pawn = value.Pawn;
					dirty = true;
				}

				if (this._thing != value.Thing) {
					this._thing = value.Thing;
					this._billGiver = value.Thing as IBillGiver;
					dirty = true;
				}

				if (dirty) {
					this.InvalidateCache();
					this._bills.ScrollTo(0);
				}
			}
		}

		public override double HeightFor(double width) => throw new NotImplementedException();

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RecacheNow()
		{
			string label = this._pawn!.RaceProps.IsMechanoid ?
				"MedicalOperationsMechanoidsShort".Translate() :
				"MedicalOperationsShort".Translate();
			if (this._pawn is not null && this._billGiver is not null) {
				var range = this._billGiver
					.BillStack
					.Bills
					.Map(x => new Bill(x) as IListItem);
				this._bills.Fill(range);
				int count = this._bills.Count;
				if (count > 0) {
					label += $" ({count})";
				}
			} else {
				this._bills.Clear();
			}

			this.Label = label;
		}

		protected override void RepaintNow(Painter painter) { }

		protected override void ResizeNow()
		{
			var rect = this.Rect.Contract(GenUI.GapTiny);

			double width = rect.Width / 2;
			var header = rect.CutTop(this._button.HeightFor(width));
			this._button.Geometry = header.CutLeft(width);

			rect.CutTop(GenUI.GapTiny);
			this._bills.Geometry = rect;
		}

		protected override void VisibilityNow()
		{
			var tab = Tab.Instance!;
			if (!this.Visible && tab.HasDockedWindow()) {
				tab.DockWindow(false);
			}
		}
	}
}
