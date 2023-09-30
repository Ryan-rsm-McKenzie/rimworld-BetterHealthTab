#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CLIK.Components;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using UnityEngine;
using Verse;

namespace BetterHealthTab.HealthTab.Operations
{
	[HotSwappable]
	internal sealed class Docker : UIComponent, IWindow
	{
		public const double ExtraWidth = 400;

		private readonly ButtonTexture _close;

		private readonly ScrollingList _list;

		private readonly List<Operation> _operations = new();

		private readonly SearchBar _search;

		private readonly Stopwatch _watch = new();

		private Pawn? _pawn = null;

		private Thing? _thing = null;

		public Docker()
		{
			this.InvalidateCache();

			this._watch.Start();
			this._close = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
				},
				OnPress = () => Tab.Instance!.ShowOperations(false),
				Texture = TexButton.CloseXSmall,
			};
			this._list = new() {
				Parent = this,
				Spacing = new() {
					Color = Color.clear,
					Size = 1,
				},
				Stripes = Color.white,
				TextStyle = new() {
					Anchor = TextAnchor.MiddleLeft,
					Font = GameFont.Tiny,
				},
			};
			this._search = new() {
				Parent = this,
				Icons = new() {
					Clear = null,
				},
				OnChanged = text => {
					this.QueueAction(() => this._search!.Icons.Clear = text.IsEmpty() ? null : Widgets.CheckboxOffTex);
					this.ApplySearchFilter();
				},
			};
		}

		public (Thing? Thing, Pawn? Pawn) SelectedThings {
			set {
				bool dirty = false;

				if (this._thing != value.Thing) {
					this._thing = value.Thing;
					dirty = true;
				}

				if (this._pawn != value.Pawn) {
					this._pawn = value.Pawn;
					dirty = true;
				}

				if (dirty) {
					this.InvalidateCache();
					this._list.ScrollTo(0);
				}
			}
		}

		public override double HeightFor(double width) => throw new NotImplementedException();

		public void OnOpen() => this.InvalidateCache();

		public override double WidthFor(double height) => ExtraWidth;

		protected override void RecacheNow()
		{
			this._watch.Restart();
			this._operations.Clear();

			if (this._pawn is not null && this._thing is not null) {
				var range = this._thing
					.def
					.AllRecipes
					.Filter(x => x.AvailableNow)
					.Map(x => (Recipe: x, Report: x.Worker.AvailableReport(this._pawn)))
					.Filter(x => x.Report.Accepted || !(x.Report.Reason?.IsEmpty() ?? true))
					.Map(x => (x.Recipe, x.Report, Missing: x.Recipe.PotentiallyMissingIngredients(null, this._thing.MapHeld).ToList()))
					.Filter(x => x.Missing.IsEmpty() || !x.Recipe.dontShowIfAnyIngredientMissing)
					.Filter(x => x.Missing.All(y => !y.isTechHediff && !y.IsDrug))
					.Map(x => {
						if (x.Recipe.targetsBodyPart) {
							return x.Recipe
								.Worker
								.GetPartsToApplyOn(this._pawn, x.Recipe)
								.Filter(y => x.Recipe.AvailableOnNow(this._pawn, y))
								.Map(y => new Operation(this._pawn, this._thing, x.Recipe, x.Report, x.Missing, y));
						} else {
							return Iter.Once(new Operation(this._pawn, this._thing, x.Recipe, x.Report, x.Missing, null));
						}
					})
					.Flatten()
					.OrderBy(x => x.Label);
				this._operations.AddRange(range);
			}

			this.ApplySearchFilter();
		}

		protected override void RepaintNow(Painter painter)
		{
			if (this._watch.Elapsed.TotalSeconds >= 5) {
				this.InvalidateCache();
			}
		}

		protected override void ResizeNow()
		{
			var rect = this.Rect.Contract(GenUI.GapTiny);

			var header = rect.CutTop(Text.LineHeight);
			var close = header.CutRight(this._close.WidthFor(header.Height * 0.80));
			this._close.Geometry = close with {
				Height = close.Height * 0.80,
				Center = close.Center,
			};
			header.CutRight(GenUI.GapTiny);
			this._search.Geometry = header;

			rect.CutTop(GenUI.GapTiny);
			this._list.Geometry = rect;
		}

		private void ApplySearchFilter()
		{
			var range = this._operations
				.Filter(this._search, x => x.Item.Matches(x.Search));
			this._list.Fill(range);
		}
	}
}
