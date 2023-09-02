#nullable enable

using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using UnityEngine;
using Verse;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class Icon : UIComponent
	{
		public Color Color { get; set; } = Color.white;

		public CSS.Mouseover Mouseover { get; set; } = new();

		public Texture2D Texture { get; set; } = BaseContent.BadTex;

		public override double HeightFor(double width) =>
			this.Texture.ScaleToFit(width, double.PositiveInfinity).Height;

		public override double WidthFor(double height) =>
			this.Texture.ScaleToFit(double.PositiveInfinity, height).Width;

		protected override void RepaintNow(Painter painter)
		{
			var rect = this.Rect;
			bool focused = this.Focused;
			if (focused) {
				this.Mouseover.Repaint(painter, rect);
			}

			using var _ = new Context.Palette(painter, new() { Color = this.Color });
			painter.DrawTexture(rect, this.Texture);
		}
	}
}
