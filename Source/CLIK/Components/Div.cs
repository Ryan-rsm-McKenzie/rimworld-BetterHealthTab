#nullable enable

using CLIK.Painting;
using HotSwap;
using UnityEngine;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class Div : UIComponent
	{
		public Color Color { get; set; } = Color.white;

		public double Extent { get; set; } = 0;

		public override double HeightFor(double width) => this.Extent;

		public override double WidthFor(double height) => this.Extent;

		protected override void RepaintNow(Painter painter)
		{
			using var _ = new Context.Palette(painter, new() { Color = this.Color });
			painter.FillRect(this.Rect);
		}
	}
}
