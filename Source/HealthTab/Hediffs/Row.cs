#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CLIK.Components;
using CLIK.Extensions;
using CLIK.Painting;
using Extensions;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Rect = CLIK.Painting.Rect;
using Utils = CLIK.Utils;

namespace BetterHealthTab.HealthTab.Hediffs
{
	[HotSwappable]
	internal sealed class Row : UIComponent
	{
		private const double Padding = 2;

		private readonly Bars.Bar? _bar;

		private readonly Background _bg;

		private readonly List<Hediff> _hediffs;

		private readonly Stack _icons;

		private readonly Label _label;

		private int _stage;

		public Row(IEnumerable<Hediff> hediffs)
		{
			this.InvalidateCache();

			this._hediffs = hediffs.ToList();
			Utils.Assert(!this._hediffs.IsEmpty());

			this._stage = this._hediffs[0].CurStageIndex;
			this._icons = new() {
				Parent = this,
				Border = new() {
					Color = Color.clear,
					Left = 1,
				},
				Flow = Stack.Flows.RightToLeft,
				Spacing = new() {
					Color = Color.clear,
					Size = 1,
				},
			};

			var bar = Bars.Bar.Create(this._hediffs);
			if (bar is not null) {
				bar.Parent = this;
				this._bar = bar;
			}

			this._label = new() {
				Parent = this,
				TextStyle = new() {
					Color = bar?.Primary ?? this._hediffs[0].LabelColor,
				},
			};

			this._bg = new Background() { Parent = this };
			this.RecacheNow();
		}

		private int Stage {
			set {
				if (this._stage != value) {
					this._stage = value;
					this.InvalidateCache();
				}
			}
		}

		private string Tooltip {
			get {
				var hediff = this._hediffs[0];
				return hediff.GetTooltip(hediff.pawn, HealthCardUtility.showHediffsDebugInfo);
			}
		}

		public override double HeightFor(double width)
		{
			var rect = new Rect(0, 0, width, 1000);
			var size = this.ResizeNowImpl(rect, true);
			return rect.Height - size.Height;
		}

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RecacheNow()
		{
			this.InvalidateSize();
			this.RecacheIcons();
			this.RecacheLabel();
		}

		protected override void RepaintNow(Painter painter)
		{
			this.Stage = this._hediffs[0].CurStageIndex;

			var region = this._label.Geometry;
			if (Utils.MouseIsOver(region)) {
				Utils.TooltipRegion(region, new(this.Tooltip, Tab.StableTooltipID), 1);
				if (this._hediffs.Count > 1) {
					Utils.TooltipRegion(
						region,
						new(this._hediffs.Count == 2 ? "..." : $"... x{this._hediffs.Count - 1}", Tab.StableTooltipID + 1),
						0);
				}
			}
		}

		protected override void ResizeNow() => this.ResizeNowImpl(this.Rect, false);

		private void RecacheIcons()
		{
			var icons = new List<UIComponent>();
			if (Group.ShouldShowDeleteIcon()) {
				icons.Add(new ButtonTexture() {
					Colors = new() {
						Out = ColorLibrary.Red,
						Over = ColorLibrary.Red * GenUI.MouseoverColor,
					},
					Mouseover = new() {
						Tooltip = new("DEV: Remove hediff"),
					},
					OnPress = () => this._hediffs.ForEach(x => x.pawn.health.RemoveHediff(x)),
					Texture = TexButton.DeleteX,
				});
			}

			icons.Add(new ButtonTexture() {
				Mouseover = new() {
					Sounds = true,
					Tooltip = new("DefInfoTip".Translate()),
				},
				OnPress = () => Find.WindowStack.Add(new Dialog_InfoCard(this._hediffs[0])),
				Texture = TexButton.Info,
			});

			var bleeding = this._hediffs
				.Filter(x => x.Bleeding)
				.ToList();
			if (!bleeding.IsEmpty()) {
				var stack = new IconStack() {
					Flow = IconStack.Flows.RightToLeft,
					Mouseover = new() {
						Tooltip = new($"{bleeding.BleedRateTotal().ToStringPercent()}/{"LetterDay".Translate()}", Tab.StableTooltipID),
					},
					Spacing = GenUI.GapTiny,
				};
				stack.Fill(bleeding
					.Map(x => new Icon() {
						Texture = HealthCardUtility.BleedingIcon,
					})
					.Take(10));
				icons.Add(stack);
			}

			var states = this._hediffs
				.Filter(x => x.StateIcon.HasValue)
				.ToList();
			if (!states.IsEmpty()) {
				var stack = new IconStack() {
					Flow = IconStack.Flows.RightToLeft,
					Spacing = GenUI.GapTiny,
				};
				stack.Fill(states
					.Map(x => new Icon() {
						Color = x.StateIcon.Color,
						Texture = x.StateIcon.Texture,
					})
					.Take(10));
				icons.Add(stack);
			}

			this._icons.Fill(icons);
		}

		private void RecacheLabel()
		{
			var hediff = this._hediffs[0];
			if (hediff is Hediff_MissingPart) {
				this._label.Text = $"<i>{hediff.LabelCap}</i>";
			} else {
				int count = this._hediffs.Count;
				int variations = this._hediffs
					.Map(x => x.Label)
					.Distinct()
					.Count();
				this._label.Text =
					(variations == 1 && hediff.IsPermanent() ? hediff.LabelCap : hediff.LabelBaseCap) +
					(count > 1 ? $" x{count}" : "");
			}
		}

		private Size ResizeNowImpl(Rect rect, bool test)
		{
			if (this._bar is not null) {
				var bar = rect.CutTop(this._bar.HeightFor(rect.Width));
				if (!test) {
					this._bar.Geometry = bar;
				}
			}

			rect = rect.Contract(Padding / 2);
			if (!test) {
				this._bg.Geometry = rect;
			}

			double iconsHeight = Text.LineHeight * 0.90;
			var icons = rect.CutRight(this._icons.WidthFor(iconsHeight));
			if (!test) {
				this._label.Geometry = rect;
				this._icons.Geometry = icons with {
					Height = iconsHeight,
					Center = icons.Center,
				};
			} else {
				rect.CutTop(this._label.HeightFor(rect.Width));
			}

			return rect.Size;
		}

		[HotSwappable]
		private sealed class Background : UIComponent
		{
			public override double HeightFor(double width) => throw new NotImplementedException();

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RepaintNow(Painter painter)
			{
				if (this.Focused) {
					var rect = this.Rect;
					painter.DrawHighlight(rect);
					painter.PlayMouseoverSounds(rect);
				}
			}
		}
	}
}
