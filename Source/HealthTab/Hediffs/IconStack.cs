#nullable enable

using System.Collections.Generic;
using System.Linq;
using CLIK.Components;
using CLIK.Painting;
using Iterator;
using Verse;
using CSS = CLIK.CSS;

namespace BetterHealthTab.HealthTab.Hediffs
{
	internal sealed class IconStack : UIComponent
	{
		private readonly List<UIComponent> _data = new();

		private Flows _flows = Flows.LeftToRight;

		private double _spacing = GenUI.GapTiny;

		public enum Flows
		{
			LeftToRight = Side.Left,

			RightToLeft = Side.Right,
		}

		public Flows Flow {
			get => this._flows;
			set {
				if (this._flows != value) {
					this._flows = value;
					this.InvalidateSize();
				}
			}
		}

		public CSS.Mouseover Mouseover { get; set; } = new();

		public double Spacing {
			get => this._spacing;
			set {
				if (this._spacing != value) {
					this._spacing = value;
					this.InvalidateSize();
				}
			}
		}

		public void Clear()
		{
			this._data.Clear();
			this.InvalidateSize();
		}

		public void Fill(IEnumerable<UIComponent> data)
		{
			this.Clear();
			this._data.AddRange(data);
		}

		public override double HeightFor(double width)
		{
			return this._data
				.Map(x => x.HeightFor(width))
				.Max();
		}

		public override double WidthFor(double height)
		{
			double where = 0;
			return this._data
				.Map(x => {
					double max = x.WidthFor(height) + where;
					where += this.Spacing;
					return max;
				})
				.Max();
		}

		protected override void RepaintNow(Painter painter)
		{
			if (this.Focused) {
				this.Mouseover.Repaint(painter, this.Rect);
			}
		}

		protected override void ResizeNow()
		{
			this.Children.ForEach(x => x.Parent = null);

			var side = (Side)this.Flow;
			var rect = this.Rect;
			foreach (var elem in this._data) {
				elem.Parent = this;
				elem.Geometry = rect.Get(side, elem.WidthFor(rect.Height));
				rect.CutRight(this.Spacing);
			}
		}
	}
}
