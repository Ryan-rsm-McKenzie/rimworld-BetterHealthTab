#nullable enable

using CLIK.Painting;
using HotSwap;
using Context = CLIK.Context;

namespace BetterHealthTab.HealthTab.Hediffs.Bars
{
	[HotSwappable]
	internal abstract class BasicBar : Bar
	{
		protected abstract double Fill { get; }

		protected sealed override void RepaintNow(Painter painter)
		{
			base.RepaintNow(painter);
			using var _ = new Context.Palette(
				painter,
				new() {
					Color = this.Primary with { a = 0.60f },
				});

			var rect = this.Rect.GetLeft(this.Width * this.Fill);
			painter.FillRect(rect);
		}
	}
}
