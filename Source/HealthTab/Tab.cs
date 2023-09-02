#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CLIK;
using CLIK.Components;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Context = CLIK.Context;

namespace BetterHealthTab.HealthTab
{
	[HotSwappable]
	internal sealed class Tab : ITab
	{
		public const int StableTooltipID = 7857;

		public static Tab? Instance = null;

		private static Size s_initialSize = new(630 + 100, 430);

		private Component? _impl = null;

		private bool _open = false;

		public Tab()
		{
			this.size = (Vector2)s_initialSize;
			this.labelKey = "TabHealth";
			this.tutorTag = "Health";

			Instance = this;
		}

		public void Close() => this.CloseTab();

		public void DockWindow(bool visible)
		{
			Utils.Assert(this._open);
			var size = s_initialSize;
			size.Width += visible ? Operations.Docker.ExtraWidth : 0;
			this.size = (Vector2)size;
			this._impl!.DockWindow(visible);
		}

		public bool HasDockedWindow()
		{
			Utils.Assert(this._open);
			return this._impl!.HasDockedWindow();
		}

		public void InvalidateHediffs(Pawn pawn)
		{
			if (this._open && this.SelThing?.PawnForHealth() == pawn) {
				this._impl!.InvalidateHediffs();
			}
		}

		public override void OnOpen()
		{
			this.size = (Vector2)s_initialSize;
			this._impl = new();
			this._open = true;
		}

		protected override void CloseTab()
		{
			this._open = false;
			this._impl = null;
			base.CloseTab();
		}

		protected override void ExtraOnGUI()
		{
			Find.WindowStack.ImmediateWindow(
				235086,
				this.TabRect,
				WindowLayer.GameUI,
				() => {
					if (this.StillValid && this.IsVisible) {
						try {
							this.FillTab();
						} catch (Exception e) {
							string what = $"Exception filling tab {this.GetType()}: {e}";
#if DEBUG
							Log.Error(what);
#else
							Log.ErrorOnce(what, 49827);
#endif
						}
					}
				});
		}

		protected override void FillTab()
		{
			if (this._open) {
				this._impl!.ThingForMedBills = this.SelThing;
				this._impl!.Size = (Size)this.size;
				App.Drive(this._impl);
			}
		}

		[HotSwappable]
		public sealed class Component : UIComponent
		{
			private static int s_savedIndex = 0;

			private readonly IWindow _docked;

			private readonly Nav _nav;

			private readonly IWindow _right;

			private Thing? _thingForMedBills = null;

			public Component()
			{
				this.InvalidateCache();

				this._nav = new Nav() {
					Parent = this,
					OnCurrentChanged = (int index) => s_savedIndex = index,
					TextStyle = new() {
						Anchor = TextAnchor.MiddleCenter,
					},
				};
				this._right = new Hediffs.Window() { Parent = this };
				this._docked = new Operations.Docker() {
					Parent = this,
					Visible = false,
				};
			}

			public Thing? ThingForMedBills {
				get => this._thingForMedBills;
				set {
					if (this._thingForMedBills != value) {
						this._thingForMedBills = value;
						this.InvalidateCache();
					}
				}
			}

			public void DockWindow(bool visible)
			{
				this.InvalidateSize();
				this.QueueAction(() => this._docked.Visible = visible);
			}

			public bool HasDockedWindow() => this._docked.Visible;

			public override double HeightFor(double width) => throw new NotImplementedException();

			public void InvalidateHediffs() => this._right.InvalidateCache();

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RecacheNow()
			{
				int wanted = this.ShouldAllowOperations() ? 2 : 1;
				if (this._nav.Count != wanted) {
					IEnumerable<INavTarget> targets = Iter.Once(new Overview.Window());
					if (wanted == 2) {
						var pawn = this.ThingForMedBills!.PawnForHealth();
						targets = targets.Chain(
							Iter.Once(new Operations.Window() {
								Label = pawn!.RaceProps.IsMechanoid ?
									"MedicalOperationsMechanoidsShort".Translate() :
									"MedicalOperationsShort".Translate(),
							}));
					}

					this._nav.Fill(targets);
				}

				s_savedIndex = CLIK.Math.Clamp(s_savedIndex, 0, this._nav.Count - 1);
				this._nav.CurrentIndex = s_savedIndex;

				var thing = this._thingForMedBills;
				var selected = (thing, thing?.PawnForHealth());
				this._right.SelectedThings = selected;
				this._docked.SelectedThings = selected;
				this._nav.Tabs
					.Cast<IWindow>()
					.ForEach(x => x.SelectedThings = selected);
			}

			protected override void RepaintNow(Painter painter)
			{
				if (this._docked.Visible) {
					var border = this._right.Geometry.GetRight(1);
					using var _ = new Context.Palette(painter, new() { Color = Widgets.WindowBGBorderColor });
					painter.FillRect(border);
				}
			}

			protected override void ResizeNow()
			{
				var rect = this.Rect;
				if (this._docked.Visible) {
					this._docked.Geometry = rect.CutRight(Operations.Docker.ExtraWidth);
				}

				this._right.Geometry = rect.CutRight(rect.Width * 5.0 / 8.0);
				this._nav.Geometry = rect;
			}

			private bool ShouldAllowOperations()
			{
				var thing = this.ThingForMedBills;
				var pawn = thing?.PawnForHealth();
				return
					thing is not null &&
					pawn is not null &&
					!pawn.Dead &&
					thing.def.AllRecipes.Any(x => x.AvailableNow && x.AvailableOnNow(pawn)) &&
					pawn.OperableOn();
			}
		}
	}
}
