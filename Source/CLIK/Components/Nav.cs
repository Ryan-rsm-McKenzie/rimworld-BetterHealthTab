#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Rect = CLIK.Painting.Rect;

namespace CLIK.Components
{
	internal interface INavTarget : IUIComponent
	{
		public abstract int Index { get; set; }

		public abstract string Label { get; }
	}

	[HotSwappable]
	internal sealed class Nav : UIComponent
	{
		private readonly List<Tab> _tabs = new();

		private readonly CSS.TextStyle _textStyle;

		private readonly UVMap _uv = new();

		private Rect _body = Rect.Zero;

		private Rect _header = Rect.Zero;

		private int _index = -1;

		private int? _indexToSet = null;

		public Nav()
		{
			this.ListensForInput = true;
			this._textStyle = new(this.InvalidateSize);
		}

		public int Count => this._tabs.Count;

		public int CurrentIndex {
			get => this._index;
			set {
				if (this._index != value) {
					this._indexToSet = value;
					if (Event.current.IsRepaint()) {
						this.SetIndexNow();
					} else {
						this.InvalidateCache();
					}
				}
			}
		}

		public Color Highlight { get; set; } = ColorLibrary.Yellow;

		public IEnumerable<INavTarget> Tabs => this._tabs.Map(x => x.Target);

		public CSS.TextStyle TextStyle {
			get => this._textStyle;
			set => this._textStyle.Copy(value);
		}

		public void Clear()
		{
			this._tabs.Clear();
			this._index = -1;
			this.InvalidateSize();
		}

		public void Fill(IEnumerable<INavTarget> data)
		{
			var range = data
				.Enumerate()
				.Map(x => {
					x.Item.Index = x.Enumerator;
					return new Tab(x.Item);
				});
			this.Clear();
			this._tabs.AddRange(range);
		}

		public override double HeightFor(double width)
		{
			if (this.Count == 0) {
				return 0;
			} else {
				var header = this.ComputeHeaderSize(width);
				double body = this._tabs.Map(x => x.Target.HeightFor(width)).Max();
				return header.Height + body;
			}
		}

		public override double WidthFor(double height)
		{
			if (this.Count == 0) {
				return 0;
			} else {
				var header = this.ComputeHeaderSize(double.PositiveInfinity);
				height -= header.Height;
				double body = this._tabs.Map(x => x.Target.WidthFor(height)).Max();
				return System.Math.Max(header.Width, body);
			}
		}

		protected override void InputNow(Painter painter)
		{
			if (Event.current.IsMouseDown(0)) {
				int idx = this.GetFocusedTab();
				if (idx >= 0 && idx != this._index) {
					Event.current.Use();
					SoundDefOf.RowTabSelect.PlayOneShotOnCamera();
					this.CurrentIndex = idx;
				}
			}
		}

		protected override void RecacheNow()
		{
			if (this._indexToSet != null) {
				this.SetIndexNow();
			}
		}

		protected override void RepaintNow(Painter painter)
		{
			int focused = this.GetFocusedTab();
			Action<Tab, int> draw = (tab, index) => {
				var rect = tab.Geometry;

				var left = rect.CutLeft(TabRecord.TabEndWidth);
				left.Right += 1;
				painter.DrawAtlas(left, this._uv.Atlas.Texture, this._uv.Left);

				var right = rect.CutRight(TabRecord.TabEndWidth);
				right.Left -= 1;
				painter.DrawAtlas(right, this._uv.Atlas.Texture, this._uv.Right);

				painter.DrawAtlas(rect, this._uv.Atlas.Texture, this._uv.Middle);

				Color? color;
				if (index == focused) {
					color = this.Highlight;
					painter.PlayMouseoverSounds(tab.Geometry, SoundDefOf.Mouseover_Tab);
				} else {
					color = this.TextStyle.Color;
				}

				using var _ = new Context.Palette(
					painter,
					new() {
						Anchor = this.TextStyle.Anchor,
						Color = color,
						Font = this.TextStyle.Font,
					});
				painter.Label(rect, tab.Target.Label);
			};

			this._tabs
				.Enumerate()
				.Filter(x => x.Enumerator != this._index)
				.ForEach(x => draw(x.Item, x.Enumerator));

			var body = this._body;
			var top = this._header.GetBottom(1);
			using (var _ = new Context.Palette(painter, new() { Color = Widgets.MenuSectionBGFillColor }))
				painter.FillRect(body);
			using (var _ = new Context.Palette(painter, new() { Color = Widgets.MenuSectionBGBorderColor })) {
				painter.FillRect(top);
				painter.FillRect(body.GetLeft(1));
				painter.FillRect(body.GetRight(1));
				painter.FillRect(body.GetBottom(1));
			}

			if (this._index >= 0) {
				draw(this._tabs[this._index], this._index);
			}
		}

		protected override void ResizeNow()
		{
			using var _ = new Context.GUIStyle(this.TextStyle);
			this.Children
				.ToList()
				.ForEach(x => x.Parent = null);
			this._tabs.SortBy(x => x.Target.Index);

			this.CurrentIndex = Math.Clamp(this._index, this.Count == 0 ? -1 : 0, this.Count - 1);
			this._body = this.Rect;

			var header = this.ComputeHeaderSize(this.Width);
			this._header = this._body.CutTop(header.Height);
			var rect = this._header;

			this._tabs
				.Enumerate()
				.ForEach(x => {
					var (enumerator, item) = x;
					var target = item.Target;

					target.Index = enumerator;
					target.Parent = this;
					target.Visible = this._index == enumerator;
					target.Geometry = this._body;

					double width = System.Math.Min(target.Label.DisplayWidth(), TabDrawer.MaxTabWidth) + 2 * TabRecord.TabEndWidth;
					item.Geometry = rect.CutLeft(width);

					rect.Left -= TabDrawer.TabHoriztonalOverlap;
				});
		}

		private Size ComputeHeaderSize(double width)
		{
			if (this.Count == 0) {
				return Size.Zero;
			}

			using var _ = new Context.GUIStyle(this._textStyle);

			double ends = 2 * this.Count * TabRecord.TabEndWidth;
			double overlap = (this.Count - 1) * TabDrawer.TabHoriztonalOverlap;
			width = width - ends + overlap;

			var labels = this
				._tabs
				.Map(x => x.Target.Label)
				.ToList();
			var widths = labels
				.Map(x => {
					double size = Math.Min(x.DisplayWidth(), TabDrawer.MaxTabWidth, width);
					width -= size;
					return size;
				})
				.ToList();
			var heights = labels
				.Zip(widths)
				.Map(x => x.First.DisplayHeight(x.Second));

			return new(
				widths.Sum() + ends - overlap,
				System.Math.Max(heights.Max(), 1.5 * Text.LineHeight));
		}

		private int GetFocusedTab()
		{
			if (this.Focused) {
				int? idx = this._tabs
					.Enumerate()
					.Reverse()
					.Filter(x => Utils.MouseIsOver(x.Item.Geometry))
					.Map(x => {
						return x.Enumerator != this._index && Utils.MouseIsOver(this._tabs[this._index].Geometry) ?
							this._index :
							x.Enumerator;
					})
					.Nth(0);
				if (idx.HasValue) {
					return idx.Value;
				}
			}

			return -1;
		}

		private void SetIndexNow()
		{
			int old = this._index;
			this._index = this._indexToSet!.Value;
			this._indexToSet = null;

			if (0 <= old && old < this._tabs.Count)
				this._tabs[old].Target.Visible = false;
			if (0 <= this._index && this._index < this._tabs.Count)
				this._tabs[this._index].Target.Visible = true;
		}

		[HotSwappable]
		private readonly struct Atlas
		{
			public readonly Size Size;

			public readonly Texture2D Texture;

			public Atlas(Texture2D texture)
			{
				this.Texture = texture;
				this.Size = texture.SizeOf();
			}
		}

		[HotSwappable]
		private readonly struct UVMap
		{
			public readonly Atlas Atlas;

			public readonly Rect Left;

			public readonly Rect Middle;

			public readonly Rect Right;

			public UVMap()
			{
				this.Atlas = new(TabRecord.TabAtlas);
				var rect = new Rect(
					0,
					0,
					TabRecord.TabMiddleGraphicWidth + 2 * TabRecord.TabEndWidth,
					TabDrawer.TabHeight);
				this.Left = Scale(rect.CutLeft(TabRecord.TabEndWidth), this.Atlas.Size);
				this.Middle = Scale(rect.CutLeft(TabRecord.TabMiddleGraphicWidth), this.Atlas.Size);
				this.Right = Scale(rect.CutLeft(TabRecord.TabEndWidth), this.Atlas.Size);
			}

			private static Rect Scale(Rect rect, Size size) =>
				new(rect.Left / size.Width, rect.Top / size.Height, rect.Width / size.Width, rect.Height / size.Height);
		}

		[HotSwappable]
		private sealed class Tab
		{
			public readonly INavTarget Target;

			public Rect Geometry = Rect.Zero;

			public Tab(INavTarget target) => this.Target = target;
		}
	}
}
