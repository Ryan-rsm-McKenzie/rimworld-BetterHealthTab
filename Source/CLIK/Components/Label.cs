#nullable enable

using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class Label : UIComponent
	{
		private readonly CSS.TextStyle _textStyle = new();

		public CSS.Mouseover Mouseover { get; set; } = new();

		public string Text { get; set; } = string.Empty;

		public CSS.TextStyle TextStyle {
			get => this._textStyle;
			set => this._textStyle.Copy(value);
		}

		public override double HeightFor(double width)
		{
			var font = this.TextStyle.Font;
			return font.HasValue ?
				this.Text.DisplayHeight(width, font.Value) :
				this.Text.DisplayHeight(width);
		}

		public override double WidthFor(double height)
		{
			var font = this.TextStyle.Font;
			return font.HasValue ?
				this.Text.DisplayWidth(font.Value) :
				this.Text.DisplayWidth();
		}

		protected override void RepaintNow(Painter painter)
		{
			var rect = this.Rect;
			if (this.Focused) {
				this.Mouseover.Repaint(painter, rect);
			}

			using var _ = new Context.Palette(painter, new() { TextStyle = this.TextStyle });
			painter.Label(rect, this.Text);
		}
	}
}
