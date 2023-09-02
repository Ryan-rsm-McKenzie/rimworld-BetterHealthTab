#nullable enable

using System;
using System.Collections.Generic;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using UnityEngine;
using Rect = CLIK.Painting.Rect;

namespace CLIK.Components
{
	internal interface IUIComponent
	{
		public abstract Rect Geometry { get; set; }

		public abstract UIComponent? Parent { get; set; }

		public abstract Rect Rect { get; }

		public abstract Size Size { get; }

		public abstract bool Visible { get; set; }

		public abstract void HandleRepaint(Painter painter);

		public abstract double HeightFor(double width);

		public abstract void InvalidateCache();

		public abstract void InvalidateSize();

		public abstract double WidthFor(double height);
	}

	[HotSwappable]
	internal abstract class UIComponent
	{
		private readonly List<UIComponent> _children = new();

		private readonly WeakReference<UIComponent> _parent = new();

		private List<Action>? _actionQueue = null;

		private Flags _flags = Flags.Visible;

		private Rect _geometry = Rect.Zero;

		[Flags]
		private enum Flags
		{
			None = 0,

			Visible = 1 << 0,

			HasPalette = 1 << 1,

			ListensForInput = 1 << 2,

			ListensForVisibility = 1 << 3,

			NeedsRecache = 1 << 4,

			NeedsResize = 1 << 5,
		}

		public Rect Geometry {
			get => this._geometry;
			set {
				if (this._geometry.Size != value.Size) {
					this.InvalidateSize();
				}
				this._geometry = value;
			}
		}

		public double Height {
			get => this._geometry.Height;
			set {
				if (this._geometry.Height != value) {
					this._geometry.Height = value;
					this.InvalidateSize();
				}
			}
		}

		public UIComponent? Parent {
			get => this._parent.Target;
			set {
				Utils.Assert(this != value, "Can not assign a component as its own parent!");
				var old = this._parent.Target;
				if (old != value) {
					old?.RemoveChild(this);
					this._parent.Target = value;
					value?.AddChild(this);
				}
			}
		}

		public Rect Rect => new(Point.Zero, this.Size);

		public Size Size {
			get => this._geometry.Size;
			set {
				if (this._geometry.Size != value) {
					this._geometry.Size = value;
					this.InvalidateSize();
				}
			}
		}

		public bool Visible {
			get => this._flags.HasFlag(Flags.Visible);
			set {
				if (this.Visible != value) {
					if (value) {
						this._flags |= Flags.Visible;
					} else {
						this._flags &= ~Flags.Visible;
					}

					if (this.ListensForVisibility) {
						this.VisibilityNow();
					}
				}
			}
		}

		public double Width {
			get => this._geometry.Width;
			set {
				if (this._geometry.Width != value) {
					this._geometry.Width = value;
					this.InvalidateSize();
				}
			}
		}

		protected IReadOnlyCollection<UIComponent> Children => this._children;

		protected bool Focused => Utils.MouseIsOver(this.Rect);

		protected bool HasPalette {
			get => this._flags.HasFlag(Flags.HasPalette);
			set {
				if (value) {
					this._flags |= Flags.HasPalette;
				} else {
					this._flags &= ~Flags.HasPalette;
				}
			}
		}

		protected bool ListensForInput {
			get => this._flags.HasFlag(Flags.ListensForInput);
			set {
				if (value) {
					this._flags |= Flags.ListensForInput;
				} else {
					this._flags &= ~Flags.ListensForInput;
				}
			}
		}

		protected bool ListensForVisibility {
			get => this._flags.HasFlag(Flags.ListensForVisibility);
			set {
				if (value) {
					this._flags |= Flags.ListensForVisibility;
				} else {
					this._flags &= ~Flags.ListensForVisibility;
				}
			}
		}

		protected bool NeedsRecache {
			get => this._flags.HasFlag(Flags.NeedsRecache);
			private set {
				if (value) {
					this._flags |= Flags.NeedsRecache;
				} else {
					this._flags &= ~Flags.NeedsRecache;
				}
			}
		}

		protected bool NeedsResize {
			get => this._flags.HasFlag(Flags.NeedsResize);
			private set {
				if (value) {
					this._flags |= Flags.NeedsResize;
				} else {
					this._flags &= ~Flags.NeedsResize;
				}
			}
		}

		public T? GetAncestorByType<T>()
			where T : UIComponent
		{
			return this.WalkAncestors()
				.FilterMap(x => x as T)
				.Nth(0);
		}

		public T? GetDescendantByType<T>()
			where T : UIComponent
		{
			return this.WalkDescendants()
				.FilterMap(x => x as T)
				.Nth(0);
		}

		public void HandleInput(Painter painter)
		{
			using var _1 = new Context.Group(this.Geometry);
			if (this.ListensForInput) {
				this.InputNow(painter);
			}

			using var _2 = this.HasPalette ? new Context.Palette(painter, this.Palette()) : null;
			foreach (var child in this._children) {
				if (child.Visible) {
					child.HandleInput(painter);
				}
			}
		}

		public void HandleRepaint(Painter painter)
		{
			Utils.Assert(Event.current.IsRepaint(), "Repaint is only valid to call during a repaint event!");
			Utils.Assert(this.Geometry.Valid, "Attempting to paint on an invalid rect!");
			using var _1 = new Context.Group(this.Geometry);

			if (this._actionQueue is not null) {
				foreach (var action in this._actionQueue) {
					action.Invoke();
				}
				this._actionQueue.Clear();
			}

			if (this.NeedsRecache) {
				//Utils.DebugMessage($"Recaching {this.GetType().Name}...");
				this.RecacheNow();
				this.NeedsRecache = false;
			}

			if (this.NeedsResize) {
				//Utils.DebugMessage($"Resizing {this.GetType().Name}...");
				this.ResizeNow();
				this.NeedsResize = false;
			}

			this.RepaintNow(painter);
			using var _2 = this.HasPalette ? new Context.Palette(painter, this.Palette()) : null;
			foreach (var child in this._children) {
				if (child.Visible) {
					child.HandleRepaint(painter);
				}
			}
		}

		public abstract double HeightFor(double width);

		public void InvalidateCache() => this.NeedsRecache = true;

		public void InvalidateSize() => this.NeedsResize = true;

		public virtual Palette Palette() => new();

		public abstract double WidthFor(double height);

		protected virtual void InputNow(Painter painter) { }

		protected void QueueAction(Action action)
		{
			this._actionQueue ??= new();
			this._actionQueue.Add(action);
		}

		protected virtual void RecacheNow() { }

		protected abstract void RepaintNow(Painter painter);

		protected virtual void ResizeNow() { }

		protected virtual void VisibilityNow() { }

		private void AddChild(UIComponent child)
		{
			Utils.Assert(this._children.IndexOf(child) == -1,
				"Asked self to add a child component, but component is already a child of self!");
			this._children.Add(child);
		}

		private void RemoveChild(UIComponent child)
		{
			bool removed = this._children.Remove(child);
			Utils.Assert(removed,
				"Asked self to remove a child component, but the component was not a child of self!");
		}

		private IEnumerable<UIComponent> WalkAncestors()
		{
			for (var iter = this.Parent; iter is not null; iter = iter.Parent) {
				yield return iter;
			}
		}

		private IEnumerable<UIComponent> WalkDescendants()
		{
			var c = new Queue<UIComponent>();
			Action<UIComponent> enqueue = component => {
				foreach (var child in component._children) {
					c.Enqueue(child);
				}
			};

			enqueue(this);
			while (!c.IsEmptyRO()) {
				var it = c.Dequeue();
				yield return it;
				enqueue(it);
			}
		}
	}
}
