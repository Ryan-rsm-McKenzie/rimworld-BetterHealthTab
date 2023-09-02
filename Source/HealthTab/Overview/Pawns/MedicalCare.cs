#nullable enable

using System;
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

namespace BetterHealthTab.HealthTab.Overview.Pawns
{
	[HotSwappable]
	internal sealed class MedicalCare : UIComponent
	{
		private readonly ButtonText _defaults;

		private readonly ButtonGroup _group = new();

		private readonly Stack _stack;

		public MedicalCare(Pawn pawn)
		{
			this._defaults = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
				},
				OnPress = () => Find.WindowStack.Add(new Dialog_MedicalDefaults()),
				Text = "MedGroupDefaults".Translate(),
				TextStyle = new() {
					Anchor = TextAnchor.MiddleCenter,
				},
			};

			this._stack = new() {
				Parent = this,
				Flow = Stack.Flows.LeftToRight,
				Spacing = new() {
					Color = Color.clear,
					Size = 1,
				},
			};
			this._stack.Fill(
				Enum.GetValues(typeof(MedicalCareCategory))
					.Cast<MedicalCareCategory>()
					.Map(x => new Category(pawn, x, this._group)));
		}

		public static UIComponent? Create(Pawn pawn)
		{
			return pawn.RaceProps.IsFlesh &&
				(pawn.Faction == Faction.OfPlayer ||
					pawn.HostFaction == Faction.OfPlayer ||
					(pawn.NonHumanlikeOrWildMan() && pawn.InBed() && pawn.CurrentBed().Faction == Faction.OfPlayer)) &&
				pawn.playerSettings is not null &&
				!pawn.Dead &&
				Current.ProgramState == ProgramState.Playing ?
				new MedicalCare(pawn) : null;
		}

		public override double HeightFor(double width) => Text.LineHeight;

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RepaintNow(Painter painter) { }

		protected override void ResizeNow()
		{
			var rect = this.Rect;
			double defaults = this._defaults.WidthFor(rect.Height);
			this._defaults.Geometry = rect.CutRight(defaults);
			this._stack.Geometry = rect;
		}

		[HotSwappable]
		private sealed class Category : UIComponent
		{
			private readonly ButtonTexture _button;

			private readonly Highlight _highlight;

			public Category(Pawn pawn, MedicalCareCategory category, ButtonGroup group)
			{
				this._button = new() {
					Parent = this,
					ButtonGroup = group,
					Colors = new() {
						All = Color.white,
					},
					Mouseover = new() {
						Sounds = true,
						Tooltip = new(category.GetLabel().CapitalizeFirst()),
					},
					OnPress = () => pawn.playerSettings.medCare = category,
					Sound = SoundDefOf.Tick_High,
					Texture = MedicalCareUtility.careTextures[(int)category],
				};
				this._highlight = new(pawn, category) {
					Parent = this,
				};
			}

			public override double HeightFor(double width) => this._button.HeightFor(width);

			public override double WidthFor(double height) => this._button.WidthFor(height);

			protected override void RepaintNow(Painter painter)
			{
				if (this.Focused) {
					painter.DrawHighlight(this.Rect);
				}
			}

			protected override void ResizeNow()
			{
				this._button.Geometry = this.Rect;
				this._highlight.Geometry = this.Rect;
			}
		}

		[HotSwappable]
		private sealed class Highlight : UIComponent
		{
			private readonly MedicalCareCategory _category;

			private readonly Pawn _pawn;

			public Highlight(Pawn pawn, MedicalCareCategory category)
			{
				this._pawn = pawn;
				this._category = category;
			}

			public override double HeightFor(double width) => throw new NotImplementedException();

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RepaintNow(Painter painter)
			{
				if (this._pawn.playerSettings.medCare == this._category) {
					using var _ = new Context.Palette(painter, new() { Color = Color.white });
					painter.OutlineRect(this.Rect, 1);
				}
			}
		}
	}
}
