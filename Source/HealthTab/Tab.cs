﻿#nullable enable

using System;
using System.Collections.Generic;
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
using Context = CLIK.Context;

namespace BetterHealthTab.HealthTab
{
	[HotSwappable]
	internal sealed class Tab : ITab
	{
		public const int StableTooltipID = 7857;

		public static Tab? Instance = null;

		private static readonly Size s_initialSize = new(630 + 100, 430);

		private Component? _impl = null;

		private bool _open = false;

		public Tab()
		{
			this.size = (Vector2)s_initialSize;
			this.labelKey = "TabHealth";
			this.tutorTag = "Health";

			Instance = this;
		}

		private Thing? SelectedThing => (Current.rootInt?.uiRoot as UIRoot_Play)?.mapUI!.selector!.SingleSelectedThing;

		public void Close() => this.CloseTab();

		public void InvalidateBills(Thing? other)
		{
			if (this._open && this.SelectedThing == other) {
				this._impl!.InvalidateBills();
			}
		}

		public void InvalidateHediffs(Pawn? other)
		{
			if (this._open && this.SelectedThing?.PawnForHealth() == other) {
				this._impl!.InvalidateHediffs();
			}
		}

		public bool IsOperationsVisible()
		{
			Utils.Assert(this._open);
			return this._impl!.IsOperationsVisible();
		}

		public override void OnOpen()
		{
			this._open = true;
			this._impl ??= new();
			this._impl.OnOpen();
		}

		public void ShowOperations(bool visible)
		{
			Utils.Assert(this._open);
			var size = s_initialSize;
			size.Width += visible ? Operations.Docker.ExtraWidth : 0;
			this.size = (Vector2)size;
			this._impl!.ShowOperations(visible);
		}

		protected override void CloseTab()
		{
			this._open = false;
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
				this._impl!.ThingForMedBills = this.SelectedThing;
				this._impl.Size = (Size)this.size;
				App.Drive(this._impl);
			}
		}

		[HotSwappable]
		public sealed class Component : UIComponent
		{
			private readonly Operations.Window _bills;

			private readonly Hediffs.Window _hediffs;

			private readonly Nav _nav;

			private readonly Operations.Docker _operations;

			private readonly Overview.Window _overview;

			private Thing? _thingForMedBills = null;

			public Component()
			{
				this.InvalidateCache();

				this._overview = new();
				this._bills = new();

				this._nav = new Nav() {
					Parent = this,
					TextStyle = new() {
						Anchor = TextAnchor.MiddleCenter,
					},
				};
				this._hediffs = new Hediffs.Window() { Parent = this };
				this._operations = new Operations.Docker() {
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

			public override double HeightFor(double width) => throw new NotImplementedException();

			public void InvalidateBills() => this._bills.InvalidateBills();

			public void InvalidateHediffs() => this._hediffs.InvalidateCache();

			public bool IsOperationsVisible() => this._operations.Visible;

			public void OnOpen()
			{
				this._overview.OnOpen();
				this._bills.OnOpen();
				this._hediffs.OnOpen();
				this._operations.OnOpen();
			}

			public void ShowOperations(bool visible)
			{
				this.InvalidateSize();
				this.QueueAction(() => this._operations.Visible = visible);
			}

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RecacheNow()
			{
				int wanted = this.ShouldAllowOperations() ? 2 : 1;
				if (wanted != this._nav.Count) {
					var tabs = new List<INavTarget>() { this._overview };
					if (wanted == 2) {
						tabs.Add(this._bills);
					}

					this._nav.Fill(tabs);
				}

				var thing = this._thingForMedBills;
				var selected = (thing, thing?.PawnForHealth());
				this._hediffs.SelectedThings = selected;
				this._operations.SelectedThings = selected;
				this._nav.Tabs
					.Cast<IWindow>()
					.ForEach(x => x.SelectedThings = selected);

				this.InvalidateSize();
			}

			protected override void RepaintNow(Painter painter)
			{
				if (this._operations.Visible) {
					var border = this._hediffs.Geometry.GetRight(1);
					using var _ = new Context.Palette(painter, new() { Color = Widgets.WindowBGBorderColor });
					painter.FillRect(border);
				}
			}

			protected override void ResizeNow()
			{
				var rect = this.Rect;
				if (this._operations.Visible) {
					this._operations.Geometry = rect.CutRight(Operations.Docker.ExtraWidth);
				}

				this._hediffs.Geometry = rect.CutRight(rect.Width * 5.0 / 8.0);
				this._nav.Geometry = rect;
			}

			private bool ShouldAllowOperations()
			{
				var thing = this.ThingForMedBills;
				var pawn = thing?.PawnForHealth();
				if (thing is not null && pawn is not null) {
					bool ok =
						!pawn.Dead &&
						thing.def.AllRecipes.Any(x => x.AvailableNow && x.AvailableOnNow(pawn)) &&
						pawn.OperableOn();
					ok = ok || Compatibility.UltratechAlteredCarbon.ShouldAllowOperations(pawn);
					return ok;
				} else {
					return false;
				}
			}
		}
	}
}
