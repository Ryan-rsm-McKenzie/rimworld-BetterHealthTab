#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using CLIK.Extensions;
using CLIK.Painting;
using CLIK.Windows;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Rect = CLIK.Painting.Rect;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class ScrollingList : CoreList
	{
		private Context.ScrollView? _context = null;

		private DraggableProxy? _dragging = null;

		private Flows _flow;

		private Point _scrolling = Point.Zero;

		private bool _visibleRangeDirty = true;

		public ScrollingList()
			: base((Side)Flows.TopToBottom)
		{
			this.HasPalette = true;
			this.ListensForInput = true;

			this._flow = (Flows)this.Side;
		}

		public enum Flows : byte
		{
			TopToBottom = Side.Top,

			LeftToRight = Side.Left,
		}

		public Flows Flow {
			get => this._flow;
			set {
				if (this._flow != value) {
					this._flow = value;
					this.Side = (Side)value;
					this.InvalidateSize();
				}
			}
		}

		public void DragItem(int index, Point position, Action onPositionChanged)
		{
			var item = this.Data.Find(x => x.Index == index);
			Utils.Assert(item is not null,
				"Asked list to drag an item, but the item could not be found!");
			this._dragging = new DraggableProxy(item!, onPositionChanged);
			this.QueueAction(() => this._dragging.Div.Parent = this);
			DragAndDrop.Start(this._dragging, position);
		}

		public override Palette Palette()
		{
			Utils.Assert(this._context is not null,
				"Expected to propagate a scrollview context to our children, but there was none!");
			var context = this._context;
			this._context = null;
			return base.Palette() with { Context = context };
		}

		public void ScrollTo(int index)
		{
			index = Math.Clamp(index, this.Count > 0 ? 0 : -1, this.Count - 1);
			if (index == -1) {
				this._scrolling = Point.Zero;
			} else {
				var geometry = this.Data[index].Geometry;
				var viewport = this.GetViewport();
				if (!viewport.Contains(geometry)) {
					var (min, max) = this.GetScrollableRange();
					if (this.Horizontal()) {
						this._scrolling.X = geometry.Center.X > viewport.Center.X ?
							this._scrolling.X = Math.Clamp(geometry.Right, min, max) :
							this._scrolling.X = geometry.Left;
					} else {
						this._scrolling.Y = geometry.Center.Y > viewport.Center.Y ?
							this._scrolling.Y = Math.Clamp(geometry.Bottom, min, max) :
							this._scrolling.Y = geometry.Top;
					}
				}
			}
		}

		protected override void ComputeSize(Size bounds, out Size computed, out Size actual)
		{
			bool horizontal = this.Horizontal();

			if (horizontal) {
				if (bounds.Width.IsInfinity()) {
					bounds.Height -= GenUI.ScrollBarWidth;
				}
			} else {
				if (bounds.Height.IsInfinity()) {
					bounds.Width -= GenUI.ScrollBarWidth;
				}
			}

			base.ComputeSize(bounds, out computed, out actual);

			if (horizontal) {
				if (!bounds.Width.IsInfinity() && computed.Width > this.Width) {
					bounds.Height -= GenUI.ScrollBarWidth;
					base.ComputeSize(bounds, out computed, out actual);
				}
			} else {
				if (!bounds.Height.IsInfinity() && computed.Height > this.Height) {
					bounds.Width -= GenUI.ScrollBarWidth;
					base.ComputeSize(bounds, out computed, out actual);
				}
			}
		}

		protected override void InputNow(Painter painter) => this._context = this.MakeScrollView();

		protected override void RepaintNow(Painter painter)
		{
			Utils.Assert(this._context is null, "Scrollview context was not cleared from last repaint!");

			if (this._visibleRangeDirty) {
				this._visibleRangeDirty = false;
				var viewport = this.GetViewport();
				foreach (var child in this.Children) {
					child.Visible = viewport.Intersects(child.Geometry);
				}
			}

			this._context = this.MakeScrollView();
			base.RepaintNow(painter);

			if (this._dragging is not null) {
				this.DraggableMainLoop();
			}
		}

		protected override void ResizeNow()
		{
			base.ResizeNow();
			this.InvalidateVisibleRange();
		}

		private void DraggableDrawInList(bool focused, IListItem? before)
		{
			var dragging = this._dragging!;
			dragging.Div.Visible = focused;
			if (!focused) {
				return;
			}

			bool horizontal = this.Horizontal();
			Rect where;
			Side side;
			if (before is not null) {
				side = horizontal ? Side.Left : Side.Top;
				where = before.Geometry;
			} else {
				side = horizontal ? Side.Right : Side.Bottom;
				where = this.Data.Last().Geometry;
			}

			where = where.Get(side, 0).Add(side, this.Spacing.Size);
			var size = horizontal ?
				new Size(GenUI.ScrollBarWidth, where.Height) :
				new Size(where.Width, GenUI.ScrollBarWidth);
			var div = new Rect(Point.Zero, size) { Center = where.Center };
			dragging.Div.Geometry = div;

			if (dragging.LastDivAt != div.TopLeft) {
				dragging.LastDivAt = div.TopLeft;
				SoundDefOf.Mouseover_Standard.PlayOneShotOnCamera();
			}

			this.DraggableScrolling();
		}

		private void DraggableFinishedCleanup(bool focused, IListItem? before)
		{
			var dragging = this._dragging;
			this._dragging = null;
			if (!focused) {
				return;
			}

			int src = dragging!.Item.Index;
			int dst = before?.Index ?? this.Count;
			if (dst != src && dst != src + 1) {
				this.QueueAction(() => {
					var data = this.Data;
					if (dst < src) {
						data[src].Index = dst;
						for (int i = dst; i < src; ++i) {
							data[i].Index = i + 1;
						}
					} else {
						data[src].Index = dst - 1;
						for (int i = src + 1; i < dst; ++i) {
							data[i].Index = i - 1;
						}
					}
					this.InvalidateSize();
					dragging.Callback.Invoke();
				});
			}
		}

		private void DraggableMainLoop()
		{
			var mouse = (Point)Event.current.mousePosition;
			bool focused = this.ViewRect.Contains(mouse);
			var data = this.Data;

			Func<IListItem, bool> finder = this.Horizontal() ?
				x => mouse.X < x.Geometry.Center.X :
				x => mouse.Y < x.Geometry.Center.Y;
			var before = data.Find(finder);

			if (this._dragging!.IsDragging) {
				this.DraggableDrawInList(focused, before);
			} else {
				this.DraggableFinishedCleanup(focused, before);
			}
		}

		private void DraggableScrolling()
		{
			var (min, max) = this.GetScrollableRange();
			double pos = this.Horizontal() ? this._scrolling.X : this._scrolling.Y;
			double old = pos;
			double lineHeight = Text.LineHeightOf(GameFont.Small);

			if (!Mathf.Approximately(Input.mouseScrollDelta.y, 0)) {
				double sign = Input.mouseScrollDelta.y >= 0 ? -1 : +1;
				double delta = sign * lineHeight * 3;
				pos = Math.Clamp(pos + delta, min, max);
			} else if (this._dragging!.Timer.ElapsedMilliseconds >= 50) {
				this._dragging.Timer.Restart();
				var mouse = (Point)Event.current.mousePosition;
				var viewport = new Rect(this._scrolling, this.Size);
				var from = this.Side;
				var to = this.Side.Opposite();
				double magnitude =
					viewport.Get(from, lineHeight).Contains(mouse) ? -1 :
					viewport.Get(to, lineHeight).Contains(mouse) ? +1 : 0;
				pos = Math.Clamp(magnitude * lineHeight + pos, min, max);
			}

			if (pos != old) {
				this.InvalidateVisibleRange();
				if (this.Horizontal()) {
					this._scrolling.X = pos;
				} else {
					this._scrolling.Y = pos;
				}
			}
		}

		private (double Min, double Max) GetScrollableRange()
		{
			return this.Horizontal() ?
				(0, this.ViewRect.Right - this.Size.Width) :
				(0, this.ViewRect.Bottom - this.Size.Height);
		}

		private Rect GetViewport()
		{
			return this.Horizontal() ?
				new Rect(this._scrolling.X, 0, this._scrolling.X + this.Width, this.Height) :
				new Rect(0, this._scrolling.Y, this.Width, this._scrolling.Y + this.Height);
		}

		private void InvalidateVisibleRange() => this._visibleRangeDirty = true;

		private Context.ScrollView MakeScrollView()
		{
			var old = this._scrolling;
			var result = new Context.ScrollView(this.Rect, ref this._scrolling, this.ViewRect, true);
			if (this._scrolling != old) {
				this.InvalidateVisibleRange();
			}

			return result;
		}

		[HotSwappable]
		private sealed class DraggableDiv : UIComponent
		{
			public override double HeightFor(double width) => throw new NotImplementedException();

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RepaintNow(Painter painter)
			{
				var style = GUI.skin.horizontalScrollbarThumb;
				style.Draw(
					position: this.Rect.ToUnity(),
					content: GUIContent.none,
					controlID: 0,
					on: false,
					hover: false);
			}
		}

		[HotSwappable]
		private sealed class DraggableProxy : IDraggableTarget
		{
			public readonly Action Callback;

			public readonly DraggableDiv Div = new();

			public readonly IListItem Item;

			public readonly Stopwatch Timer = new();

			public DraggableProxy(IListItem item, Action callback)
			{
				this.Timer.Start();
				this.Item = item;
				this.Callback = callback;
			}

			public bool IsDragging { get; private set; } = true;

			public Point LastDivAt { get; set; } = Point.Zero;

			public void OnDrag(Point where)
			{
				var painter = new Painter(dragging: true);
				painter.Push(
					new() {
						Color = Color.white with { a = 0.50f },
					});

				using var _2 = new Context.Group(new(where, this.Item.Size));
				painter.DrawHighlight(this.Item.Rect);

				var saved = this.Item.Geometry;
				this.Item.Geometry = this.Item.Rect;
				this.Item.HandleRepaint(painter);
				this.Item.Geometry = saved;
			}

			public void OnDragBegin() { }

			public void OnDragEnd()
			{
				this.IsDragging = false;
				this.Div.Parent = null;
				SoundDefOf.Tick_Low.PlayOneShotOnCamera();
			}
		}
	}
}
