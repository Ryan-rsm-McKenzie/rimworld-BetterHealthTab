#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using UnityEngine;
using Verse;
using Rect = CLIK.Painting.Rect;

namespace CLIK.Components
{
	internal interface IListItem : IUIComponent
	{
		public abstract int Index { get; set; }
	}

	[HotSwappable]
	internal abstract class CoreList : UIComponent
	{
		private readonly CSS.Border _border;

		private readonly List<IListItem> _data = new();

		private readonly CSS.Spacing _spacing;

		private readonly CSS.TextStyle _textStyle;

		private Size _actualSize = Size.Zero;

		private Rect _viewRect = Rect.Zero;

		public CoreList(Side side)
		{
			this.HasPalette = true;

			this.Side = side;
			this._border = new(this.InvalidateSize);
			this._spacing = new(this.InvalidateSize);
			this._textStyle = new(this.InvalidateSize);
		}

		public CSS.Border Border {
			get => this._border;
			set => this._border.Copy(value);
		}

		public Color Color { get; set; } = Color.white;

		public int Count => this._data.Count;

		public IReadOnlyList<IListItem> Data => this._data;

		public CSS.Spacing Spacing {
			get => this._spacing;
			set => this._spacing.Copy(value);
		}

		public Color? Stripes { get; set; } = null;

		public CSS.TextStyle TextStyle {
			get => this._textStyle;
			set => this._textStyle.Copy(value);
		}

		protected Side Side { get; set; }

		protected Rect ViewRect => this._viewRect;

		public void Clear()
		{
			this._data.Clear();
			this.InvalidateSize();
		}

		public void Fill(IEnumerable<IListItem> data)
		{
			this.Clear();
			var range = data
				.Enumerate()
				.Map(x => {
					x.Item.Index = x.Enumerator;
					return x.Item;
				});
			this._data.AddRange(range);
		}

		public void Fill(IEnumerable<UIComponent> data) => this.Fill(data.Map(x => x.WrapForList()));

		public sealed override double HeightFor(double width)
		{
			this.ComputeSize(new(width, double.PositiveInfinity), out var computed, out _);
			return computed.Height;
		}

		public override Palette Palette()
		{
			return new() {
				Color = this.TextStyle.Color * this.Color,
				Anchor = this.TextStyle.Anchor,
				Font = this.TextStyle.Font,
			};
		}

		public sealed override double WidthFor(double height)
		{
			this.ComputeSize(new(double.PositiveInfinity, height), out var computed, out _);
			return computed.Width;
		}

		protected virtual void ComputeSize(Size bounds, out Size computed, out Size actual)
		{
			Utils.Assert(!bounds.Width.IsInfinity() || !bounds.Height.IsInfinity(),
				"Can not compute size when both parameters are left unbounded!");
			using var _ = new Context.GUIStyle(this.TextStyle);
			bool horizontal = bounds.Width.IsInfinity() || bounds.Height.IsInfinity() ?
				bounds.Width.IsInfinity() :
				this.Horizontal();

			if (horizontal) {
				double height = bounds.Height - this.Border.Top - this.Border.Bottom;
				double sum = this._data
					.Map(x => x.WidthFor(height))
					.Sum();
				sum += this.Count > 0 ? (this.Count - 1) * this.Spacing.Size : 0;
				sum += sum > 0 ? this.Border.Left + this.Border.Right : 0;
				actual = new(sum, bounds.Height);
				computed = new(
					bounds.Width.IsInfinity() ? sum : System.Math.Max(bounds.Width, sum),
					bounds.Height);
			} else {
				double width = bounds.Width - this.Border.Left - this.Border.Right;
				double sum = this._data
					.Map(x => x.HeightFor(width))
					.Sum();
				sum += this.Count > 0 ? (this.Count - 1) * this.Spacing.Size : 0;
				sum += sum > 0 ? this.Border.Top + this.Border.Bottom : 0;
				actual = new(bounds.Width, sum);
				computed = new(
					bounds.Width,
					bounds.Height.IsInfinity() ? sum : System.Math.Max(bounds.Height, sum));
			}
		}

		protected bool Horizontal() => this.Side.Horizontal();

		protected override void RepaintNow(Painter painter)
		{
			if (!this.Children.IsEmptyRO()) {
				var color = this.Color * this.Border.Color;
				var rect = new Rect(this._viewRect.TopLeft, this._actualSize);
				using (var _ = new Context.Palette(painter, new() { Color = color })) {
					if (this.Border.Top > 0)
						painter.FillRect(rect.CutTop(this.Border.Top));
					if (this.Border.Bottom > 0)
						painter.FillRect(rect.CutBottom(this.Border.Bottom));
					if (this.Border.Left > 0)
						painter.FillRect(rect.CutLeft(this.Border.Left));
					if (this.Border.Right > 0)
						painter.FillRect(rect.CutRight(this.Border.Right));
				}

				if (this.Stripes is not null) {
					using var _ = new Context.Palette(painter, new() { Color = this.Stripes });
					this._data
						.Filter(x => x.Visible && x.Index.IsEven())
						.ForEach(x => painter.DrawHighlight(x.Geometry));
				}
			}
		}

		protected override void ResizeNow()
		{
			using var _ = new Context.GUIStyle(this.TextStyle);
			this.Children
				.ToList()
				.ForEach(x => x.Parent = null);
			this._data.SortBy(x => x.Index);
			this._data
				.Enumerate()
				.ForEach(x => x.Item.Index = x.Enumerator);
			this._viewRect = Rect.Zero;

			this.ComputeSize(this.Size, out var computed, out this._actualSize);
			this._viewRect.Size = computed;

			Func<IListItem, double> sizer = this.Horizontal() ?
				x => x.WidthFor(computed.Height) :
				x => x.HeightFor(computed.Width);
			var iter = this._data.Map(x => new Item(x, sizer(x)));
			if (this.Spacing.Size > 0) {
				iter = iter
					.IntersperseWith(
					this.Count,
					() => {
						var div = new Div() {
							Color = this.Spacing.Color,
							Extent = this.Spacing.Size,
						};
						return new Item(div.WrapForList(), div.Extent);
					});
			}

			var rect = this._viewRect - this.Border.Margins;
			iter
				.ForEach(x => {
					x.Parent = this;
					x.Geometry = rect.Cut(this.Side, x.Extent);
					x.Visible = true;
				});
		}

		[HotSwappable]
		private readonly struct Item
		{
			public readonly double Extent;

			public readonly IListItem ListItem;

			public Item(IListItem listItem, double extent)
			{
				this.ListItem = listItem;
				this.Extent = extent;
			}

			public Rect Geometry {
				get => this.ListItem.Geometry;
				set => this.ListItem.Geometry = value;
			}

			public UIComponent? Parent {
				get => this.ListItem.Parent;
				set => this.ListItem.Parent = value;
			}

			public bool Visible {
				get => this.ListItem.Visible;
				set => this.ListItem.Visible = value;
			}
		}
	}

	[HotSwappable]
	internal sealed class ListItemRendererWrapper : IListItem
	{
		private readonly UIComponent _component;

		public ListItemRendererWrapper(UIComponent component) => this._component = component;

		public Rect Geometry {
			get => this._component.Geometry;
			set => this._component.Geometry = value;
		}

		public int Index { get; set; } = -1;

		public UIComponent? Parent {
			get => this._component.Parent;
			set => this._component.Parent = value;
		}

		public Rect Rect => this._component.Rect;

		public Size Size => this._component.Size;

		public bool Visible {
			get => this._component.Visible;
			set => this._component.Visible = value;
		}

		public void HandleRepaint(Painter painter) => this._component.HandleRepaint(painter);

		public double HeightFor(double width) => this._component.HeightFor(width);

		public void InvalidateCache() => this._component.InvalidateCache();

		public void InvalidateSize() => this._component.InvalidateSize();

		public double WidthFor(double height) => this._component.WidthFor(height);
	}
}
