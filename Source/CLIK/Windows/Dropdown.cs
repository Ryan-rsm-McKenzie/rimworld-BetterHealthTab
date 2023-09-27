#nullable enable

using System.Collections.Generic;
using System.Linq;
using CLIK.Components;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Rect = CLIK.Painting.Rect;

namespace CLIK.Windows
{
	[HotSwappable]
	internal sealed class Dropdown : Window
	{
		private readonly Stack _items;

		private readonly double _width;

		public Dropdown(IEnumerable<UIComponent> items)
		{
			const GameFont Font = GameFont.Small;
			const double Margin = 1;

			this.closeOnClickedOutside = true;
			this.doWindowBackground = false;
			this.drawShadow = false;
			this.layer = WindowLayer.Super;
			this.soundAppear = null;
			this.soundClose = SoundDefOf.Click;

			var color = Color.Lerp(FloatMenuOption.ColorBGActive, Color.white, 0.20f);
			this._items = new() {
				Border = new() {
					Color = color,
					Size = Margin,
				},
				Spacing = new() {
					Color = color,
					Size = Margin,
				},
				TextStyle = new() {
					Anchor = TextAnchor.MiddleLeft,
					Color = FloatMenuOption.ColorTextActive,
					Font = Font,
				},
			};

			var l = items
				.Map(x => new Item(x))
				.ToList();
			double height = Text.LineHeightOf(Font) + Item.Margins.Top + Item.Margins.Bottom;
			double width = l
				.Map(x => x.WidthFor(height) + 2 * Margin)
				.Max();
			this._width = Math.Clamp(width, FloatMenu.MinimumColumnWidth, FloatMenuOption.MaxWidth);
			this._items.Fill(l.Cast<IListItem>());
		}

		protected override float Margin => 0;

		public static new bool IsOpen() => Find.WindowStack.IsOpen<Dropdown>();

		public static void Start(IEnumerable<UIComponent> items) => Find.WindowStack.Add(new Dropdown(items));

		public static void Stop() => Find.WindowStack.RemoveWindowsOfType<Dropdown>();

		public override void DoWindowContents(UnityEngine.Rect inRect)
		{
			bool focused = Mouse.IsOver(inRect);
			double alpha = 1.00;
			if (!focused) {
				double distance = GenUI.DistFromRect(inRect.ExpandedBy(FloatMenu.FadeStartMouseDist), Event.current.mousePosition);
				if (distance > FloatMenu.FinishDistFromStartDist) {
					this.soundClose = SoundDefOf.FloatMenu_Cancel;
					this.Close(true);
				}
				alpha = Math.Remap(0, FloatMenu.FinishDistFromStartDist, 1.00, 0.00, distance);
			}

			this._items.Color = Color.white with { a = (float)alpha };
			this._items.Geometry = (Rect)inRect;
			App.Drive(this._items);
		}

		public override void PostOpen()
		{
			SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
			SoundDefOf.DialogBoxAppear.PlayOneShotOnCamera();
		}

		public override void PreOpen()
		{
			base.PreOpen();
			Find.WindowStack.RemoveWindowsOfType<FloatMenu>();
			this.soundClose = SoundDefOf.Click;
		}

		protected override void SetInitialSizeAndPosition()
		{
			var size = new Vector2((float)this._width, (float)this._items.HeightFor(this._width));

			var position = UI.MousePositionOnUIInverted + FloatMenu.InitialPositionShift;
			position.x = System.Math.Min(position.x, UI.screenWidth - size.x);
			position.y = System.Math.Min(position.y, UI.screenHeight - size.y);

			this.windowRect = new UnityEngine.Rect(position, size);
		}

		[HotSwappable]
		private sealed class Item : UIComponent, IListItem
		{
			public static readonly Margins Margins = new() {
				Top = FloatMenuOption.TinyVerticalMargin,
				Bottom = FloatMenuOption.TinyVerticalMargin,
				Left = FloatMenuOption.TinyHorizontalMargin,
				Right = FloatMenuOption.TinyHorizontalMargin,
			};

			private readonly UIComponent _component;

			private bool _mouseover = false;

			public Item(UIComponent component)
			{
				component.Parent = this;
				this._component = component;
			}

			public int Index { get; set; } = -1;

			private bool Mouseover {
				set {
					if (this._mouseover != value) {
						this._mouseover = value;
						this.InvalidateSize();
					}
				}
			}

			public override double HeightFor(double width)
			{
				width -= Margins.Left + Margins.Right + FloatMenuOption.MouseOverLabelShift;
				return this._component.HeightFor(width) + Margins.Top + Margins.Bottom;
			}

			public override double WidthFor(double height)
			{
				height -= Margins.Top + Margins.Bottom;
				return this._component.WidthFor(height) + Margins.Left + Margins.Right + FloatMenuOption.MouseOverLabelShift;
			}

			protected override void RepaintNow(Painter painter)
			{
				var rect = this.Rect;
				bool focused = this.Focused;

				this.Mouseover = focused;
				if (focused) {
					painter.PlayMouseoverSounds(rect);
				}

				var bg = focused ? FloatMenuOption.ColorBGActiveMouseover : FloatMenuOption.ColorBGActive;
				using var _ = new Context.Palette(painter, new() { Color = bg });
				painter.FillRect(rect);
			}

			protected override void ResizeNow()
			{
				var geometry = this.Rect.Contract(Margins);
				if (this.Focused) {
					geometry.Translate(+FloatMenuOption.MouseOverLabelShift, 0);
				}

				this._component.Geometry = geometry;
			}
		}
	}
}
