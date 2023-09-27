#nullable enable

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
using Math = System.Math;
using Rect = CLIK.Painting.Rect;

namespace BetterHealthTab.HealthTab.Hediffs
{
	[HotSwappable]
	internal sealed class Group : UIComponent
	{
		private const double HediffsSplit = 5.0 / 8.0;

		private const double Padding = 2;

		private readonly Background? _background = null;

		private readonly Bars.PartDamage? _damage;

		private readonly Div _div;

		private readonly List<Row> _hediffs;

		private readonly Stack _icons;

		private readonly ButtonTexture? _info = null;

		private readonly Label _label;

		private readonly Hediff? _merged = null;

		private readonly Stack _stack;

		private bool _showDeleteIcon = ShouldShowDeleteIcon();

		public Group(Pawn pawn, BodyPartRecord? record, IEnumerable<Hediff> hediffs)
		{
			(this._damage, var color) = Bars.PartDamage.Create(pawn, record, hediffs);
			(string label, this._merged) = FoldAddedParts(color, record, ref hediffs);

			this._hediffs = hediffs
				.GroupBy(x => x.LabelBase)
				.Map(x => new Row(x))
				.ToList();
			this._stack = new() {
				Parent = this,
				TextStyle = new() {
					Anchor = TextAnchor.MiddleLeft,
				},
			};
			this._stack.Fill(this._hediffs);

			this._div = new() {
				Parent = this,
				Color = Color.black,
				Extent = Window.MarginSize,
			};
			this._label = new() {
				Parent = this,
				Text = label,
			};
			this._icons = new() {
				Parent = this,
				Flow = Stack.Flows.RightToLeft,
				Visible = false,
			};

			if (this._merged is not null) {
				this._background = new(this._merged) { Parent = this };
				this._info = new() {
					Mouseover = new() {
						Sounds = true,
						Tooltip = new("DefInfoTip".Translate()),
					},
					OnPress = () => Find.WindowStack.Add(new Dialog_InfoCard(this._merged)),
					Texture = TexButton.Info,
				};
			}

			if (this._damage is not null) {
				this._damage.Parent = this;
			}

			this.RecacheIcons();
		}

		private bool ShowDeleteIcon {
			get => this._showDeleteIcon;
			set {
				if (this._showDeleteIcon != value) {
					this._showDeleteIcon = value;
					this.InvalidateCache();
					this._hediffs.ForEach(x => x.InvalidateCache());
				}
			}
		}

		public static bool ShouldShowDeleteIcon() =>
			DebugSettings.ShowDevGizmos && Current.ProgramState == ProgramState.Playing;

		public override double HeightFor(double width)
		{
			var rect = new Rect(0, 0, width, 1000);
			var size = this.ResizeNowImpl(rect, true);
			double myHeight = rect.Height - size.Height;
			return Math.Max(myHeight, this._stack.HeightFor(width * HediffsSplit));
		}

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RecacheNow()
		{
			this.RecacheIcons();
			this.InvalidateSize();
			this.Parent?.InvalidateSize();
		}

		protected override void RepaintNow(Painter painter) => this.ShowDeleteIcon = ShouldShowDeleteIcon();

		protected override void ResizeNow() => this.ResizeNowImpl(this.Rect, false);

		private static (string Label, Hediff? Merged) FoldAddedParts(Color color, BodyPartRecord? record, ref IEnumerable<Hediff> hediffs)
		{
			(string Label, Hediff? Merged) result;
			string colorHex = color.ToHexString();
			result.Label = $"<color={colorHex}>{record?.LabelCap ?? "WholeBody".Translate()}</color>";
			result.Merged = null;

			if (LanguageDatabase.activeLanguage == LanguageDatabase.defaultLanguage) {
				var added = hediffs.FilterMap(x => x as Hediff_AddedPart).Nth(0);
				var addedParts = added?.LabelBaseCap.Split<char>().ToList() ?? new();
				var labelParts = record?.LabelCap.Split<char>().ToList() ?? new();
				if (!addedParts.IsEmpty() && !labelParts.IsEmpty()) {
					string last = addedParts.Last().ToLower();
					int index = labelParts.Position(x => x.ToLower() == last);
					if (index != -1) {
						result.Label = string.Concat(
							Iter.Empty<string>()
								.Chain(Iter.Once($"<color={colorHex}>"))
								.Chain(labelParts.GetRange(0, index).Intersperse(" "))
								.Chain(Iter.Once("</color> "))
								.Chain(Iter.Once($"<color={added!.LabelColor.ToHexString()}>"))
								.Chain(addedParts.Take(addedParts.Count - 1).Intersperse(" "))
								.Chain(Iter.Once("</color> "))
								.Chain(Iter.Once($"<color={colorHex}>"))
								.Chain(labelParts.GetRange(index, labelParts.Count - index).Intersperse(" "))
								.Chain(Iter.Once("</color>")));
					} else {
						result.Label += $"<color={colorHex}>,</color> <color={added!.LabelColor.ToHexString()}>{added.LabelBaseCap}</color>";
					}

					result.Merged = added;
					hediffs = hediffs.Except(added);
				}
			}

			return result;
		}

		private void RecacheIcons()
		{
			var icons = new List<UIComponent>();

			if (this.ShowDeleteIcon && this._merged is not null) {
				icons.Add(new ButtonTexture() {
					Colors = new() {
						Out = ColorLibrary.Red,
						Over = ColorLibrary.Red * GenUI.MouseoverColor,
					},
					Mouseover = new() {
						Tooltip = new("DEV: Remove hediff"),
					},
					OnPress = () => {
						this._merged.pawn.health.RemoveHediff(this._merged);
						this.Parent?.InvalidateCache();
					},
					Texture = TexButton.DeleteX,
				});
			}

			if (this._info is not null) {
				icons.Add(this._info);
			}

			this._icons.Fill(icons);
			this._icons.Visible = !icons.IsEmpty();
		}

		private Size ResizeNowImpl(Rect rect, bool test)
		{
			Action<UIComponent, Rect> apply;
			if (test) {
				apply = (_, _) => { return; };
			} else {
				apply = (l, r) => l.Geometry = r;
			}

			var stack = rect.CutRight(rect.Width * HediffsSplit);
			apply(this._stack, stack);

			var div = rect.CutRight(this._div.WidthFor(Window.MarginSize));
			apply(this._div, div);

			if (this._damage is not null) {
				double damage = this._damage.HeightFor(rect.Width);
				apply(this._damage, rect.CutTop(damage));
			}

			rect = rect.Contract(Padding / 2);
			if (!test && this._background is not null) {
				this._background.Geometry = rect;
			}

			if (this._icons.Visible) {
				double height = Text.LineHeight * 0.90;
				double width = this._icons.WidthFor(height);
				var info = rect.CutRight(width);
				info = info with {
					Height = height,
					Center = info.Center,
				};
				apply(this._icons, info);
			}

			if (test) {
				double label = this._label.HeightFor(rect.Width);
				rect.CutTop(label);
			} else {
				this._label.Geometry = rect;
			}

			return rect.Size;
		}

		[HotSwappable]
		private sealed class Background : UIComponent
		{
			private readonly Hediff _hediff;

			public Background(Hediff hediff) => this._hediff = hediff;

			public override double HeightFor(double width) => throw new NotImplementedException();

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RepaintNow(Painter painter)
			{
				if (this.Focused) {
					var rect = this.Rect;
					painter.DrawHighlight(rect);
					painter.PlayMouseoverSounds(rect);

					var pawn = this._hediff.pawn;
					string tooltip = this._hediff.GetTooltip(pawn, HealthCardUtility.showHediffsDebugInfo);
					Utils.TooltipRegion(rect, new(tooltip, Tab.StableTooltipID));
				}
			}
		}
	}
}
