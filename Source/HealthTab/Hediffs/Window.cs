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
using RimWorld;
using UnityEngine;
using Verse;
using Rect = CLIK.Painting.Rect;

namespace BetterHealthTab.HealthTab.Hediffs
{
	[HotSwappable]
	internal sealed class Window : UIComponent, IWindow
	{
		public const double MarginSize = 2;

		private static readonly Size s_debugButtonSize = new(115, 25);

		private readonly List<Hediff> _allHediffs = new();

		private readonly List<Hediff> _bleedingHediffs = new();

		private readonly ButtonTexture _close;

		private readonly ScrollingList _list;

		private readonly Label _noHediffs;

		private readonly SearchBar _search;

		private readonly Stopwatch _watch = new();

		private Rect _bleedRate = Rect.Zero;

		private Point _debugButtonAt = Point.Zero;

		private Pawn? _pawn = null;

		private bool _showAllHediffs = HealthCardUtility.showAllHediffs;

		private bool _showDebugButton = ShouldShowDebugButton();

		public Window()
		{
			this.ListensForInput = true;
			this.InvalidateCache();

			this._watch.Start();
			this._close = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
				},
				OnPress = () => Tab.Instance!.Close(),
				Texture = TexButton.CloseXSmall,
			};
			this._list = new() {
				Parent = this,
				Border = new() {
					Color = Color.black,
					Top = MarginSize,
					Bottom = MarginSize,
				},
				Spacing = new() {
					Color = Color.black,
					Size = MarginSize,
				},
				Stripes = Color.white,
				TextStyle = new() {
					Anchor = TextAnchor.MiddleCenter,
				},
				Visible = false,
			};
			this._search = new() {
				Parent = this,
				Icons = new() {
					Clear = null,
				},
				OnChanged = (_) => this.ApplySearchFilter(),
			};
			this._noHediffs = new() {
				Parent = this,
				Text = $"({"NoHealthConditions".Translate()})",
				TextStyle = new() {
					Anchor = TextAnchor.MiddleCenter,
					Color = ColorLibrary.Grey,
					Font = GameFont.Medium,
				},
				Visible = true,
			};
		}

		public (Thing? Thing, Pawn? Pawn) SelectedThings {
			set {
				if (this._pawn != value.Pawn) {
					this._pawn = value.Pawn;
					this.InvalidateCache();
				}
			}
		}

		private bool ShowAllHediffs {
			set {
				if (this._showAllHediffs != value) {
					this._showAllHediffs = value;
					this.InvalidateCache();
				}
			}
		}

		private bool ShowDebugButton {
			get => this._showDebugButton;
			set {
				if (this._showDebugButton != value) {
					this._showDebugButton = value;
					this.InvalidateSize();
				}
			}
		}

		public override double HeightFor(double width) => throw new NotImplementedException();

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void InputNow(Painter painter)
		{
			if (this.ShowDebugButton) {
				var debug = new Rect(this._debugButtonAt, s_debugButtonSize);
				HealthCardUtility.DoDebugOptions(debug.ToUnity(), this._pawn);
			}

			this.ShowDebugButton = ShouldShowDebugButton();
			this.ShowAllHediffs = HealthCardUtility.showAllHediffs;
		}

		protected override void RecacheNow()
		{
			this._watch.Restart();

			var info = this._pawn?.RaceProps.body.BodyInfo();
			var hediffs = this._pawn?.health?.hediffSet.hediffs ?? Iter.Empty<Hediff>();
			var visible =
				this._showAllHediffs ?
				hediffs :
				hediffs
					.Filter(x => x is not Hediff_MissingPart && x.Visible)
					.Chain(hediffs.MissingPartsCommonAncestors());
			var sorted = visible
				.OrderByDescending(x => x.Part?.height ?? (BodyPartHeight.Top + 1)) // top to bottom
				.ThenBy(x => x.Part is not null ? info![x.Part].Depth : -1) // inside to outside
				.ThenBy(x => x.Part?.def.label ?? "") // group left/right parts together
				.ThenBy(x => x.Part?.Label ?? "") // alphabetical
				.ThenByDescending(x => x.TendableNow(true) ? x.TendPriority : -1) // tendable goes to the top
				.ThenByDescending(x => x.TryGetComp<HediffComp_Disappears>()?.ticksToDisappear ?? -1) // temporary after
				.ThenBy(x => x.LabelColor.ToHSV()) // group by color
				.ThenBy(x => x.LabelBase); // alphabetical
			this._allHediffs.Clear();
			this._allHediffs.AddRange(sorted);

			var bleeding = this._allHediffs.Filter(x => x.Bleeding);
			this._bleedingHediffs.Clear();
			this._bleedingHediffs.AddRange(bleeding);

			bool any = !this._allHediffs.IsEmpty();
			this._list.Visible = any;
			this._noHediffs.Visible = !any;

			this.ApplySearchFilter();
			this.InvalidateSize();
		}

		protected override void RepaintNow(Painter painter)
		{
			if (this._watch.Elapsed.TotalSeconds >= 5) {
				this.InvalidateCache();
			}

			if (this.ShowDebugButton) {
				var debug = new Rect(this._debugButtonAt, s_debugButtonSize);
				HealthCardUtility.DoDebugOptions(debug.ToUnity(), this._pawn);
			}

			if (!this._bleedRate.Null) {
				double bleeding = this._bleedingHediffs.BleedRateTotal();
				var pawn = this._pawn!;
				int ticksToDie = this._bleedingHediffs.TicksUntilDeathDueToBloodLoss(bleeding) ?? int.MaxValue;
				string danger =
					ModsConfig.BiotechActive && (pawn.genes?.HasGene(GeneDefOf.Deathless) ?? false) ? "Deathless".Translate() :
					ticksToDie < GenDate.TicksPerDay ?
					"TimeToDeath".Translate(ticksToDie.ToStringTicksToPeriod()) :
					"WontBleedOutSoon".Translate();
				string label = $"{"BleedingRate".Translate()}: {bleeding.ToStringPercent()}/{"LetterDay".Translate()} ({danger})";
				painter.Label(this._bleedRate, label);
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

			if (this.ShowDebugButton) {
				this._debugButtonAt = new(
					header.Right - s_debugButtonSize.Width - 240,
					header.Top + s_debugButtonSize.Height);
				header.CutRight(s_debugButtonSize.Width + GenUI.GapTiny);
			}

			this._search.Geometry = header;

			rect.CutTop(GenUI.GapTiny);
			this._bleedRate = !this._bleedingHediffs.IsEmpty() ?
				rect.CutBottom(Text.LineHeight) :
				Rect.Zero;
			this._list.Geometry = rect;
			this._noHediffs.Geometry = rect;
		}

		private static bool ShouldShowDebugButton() =>
			Prefs.DevMode && Current.ProgramState == ProgramState.Playing;

		private void ApplySearchFilter()
		{
			string wholeBody = "WholeBody".Translate();
			var range = this._allHediffs
				.Filter(this._search, x => x.Search.Matches(x.Item.LabelBase) || x.Search.Matches(x.Item.Part?.Label ?? wholeBody))
				.GroupBy(x => x.Part)
				.Map(x => new Group(this._pawn!, x.Key, x));
			this._list.Fill(range);
		}
	}
}
