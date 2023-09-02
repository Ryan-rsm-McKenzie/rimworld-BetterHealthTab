#nullable enable

using System;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class ButtonTexture : UIComponent, IButton
	{
		public ButtonTexture() => this.ListensForInput = true;

		public ButtonGroup? ButtonGroup { get; set; } = null;

		public CColors Colors { get; set; } = new();

		public CSS.Mouseover Mouseover { get; set; } = new();

		public Action? OnPress { get; set; } = null;

		public SoundDef? Sound { get; set; } = null;

		public Texture2D Texture { get; set; } = BaseContent.BadTex;

		public override double HeightFor(double width) =>
			this.Texture.ScaleToFit(width, double.PositiveInfinity).Height;

		public void OnButtonDown()
		{
			this.Sound?.PlayOneShotOnCamera();
			this.OnPress?.Invoke();
		}

		public override double WidthFor(double height) =>
			this.Texture.ScaleToFit(double.PositiveInfinity, height).Width;

		protected override void InputNow(Painter painter)
		{
			bool focused = this.Focused;
			this.ButtonGroup?.HandleSelection(this, focused);
			if (focused && Event.current.IsMouseDown(0)) {
				Event.current.Use();
				this.OnButtonDown();
			}
		}

		protected override void RepaintNow(Painter painter)
		{
			bool focused = this.Focused;
			var rect = this.Rect;
			if (focused) {
				this.Mouseover.Repaint(painter, rect);
			}

			var color = focused ? this.Colors.Over : this.Colors.Out;
			using var _ = new Context.Palette(painter, new() { Color = color });
			painter.DrawTexture(rect, this.Texture);
		}

		[HotSwappable]
		public sealed class CColors
		{
			public Color Out = Color.white;

			public Color Over = GenUI.MouseoverColor;

			public Color All {
				set {
					this.Out = value;
					this.Over = value;
				}
			}
		}
	}
}
