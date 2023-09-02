#nullable enable

using System;
using CLIK.Painting;
using HotSwap;
using UnityEngine;
using Verse;
using Rect = CLIK.Painting.Rect;

namespace CLIK.CSS
{
	internal sealed class Border
	{
		private readonly Action? _onChanged = null;

		private double _bottom = 0;

		private double _left = 0;

		private double _right = 0;

		private double _top = 0;

		public Border(Action onChanged) => this._onChanged = onChanged;

		public Border() { }

		public double Bottom {
			get => this._bottom;
			set {
				value = System.Math.Abs(value);
				if (this._bottom != value) {
					this._bottom = value;
					this._onChanged?.Invoke();
				}
			}
		}

		public Color Color { get; set; } = Color.white;

		public double Left {
			get => this._left;
			set {
				value = System.Math.Abs(value);
				if (this._left != value) {
					this._left = value;
					this._onChanged?.Invoke();
				}
			}
		}

		public Margins Margins => new(this.Left, this.Top, this.Right, this.Bottom);

		public double Right {
			get => this._right;
			set {
				value = System.Math.Abs(value);
				if (this._right != value) {
					this._right = value;
					this._onChanged?.Invoke();
				}
			}
		}

		public double Size {
			set {
				this.Top = value;
				this.Bottom = value;
				this.Left = value;
				this.Right = value;
			}
		}

		public double Top {
			get => this._top;
			set {
				value = System.Math.Abs(value);
				if (this._top != value) {
					this._top = value;
					this._onChanged?.Invoke();
				}
			}
		}

		public void Copy(Border other)
		{
			this.Color = other.Color;
			this.Top = other.Top;
			this.Bottom = other.Bottom;
			this.Left = other.Left;
			this.Right = other.Right;
		}
	}

	internal sealed class Mouseover
	{
		public Color? Highlight { get; set; } = null;

		public bool Sounds { get; set; } = false;

		public TipSignal? Tooltip { get; set; } = null;

		public void Repaint(Painter painter, Rect rect)
		{
			if (this.Highlight.HasValue) {
				using var _ = new Context.Palette(painter, new() { Color = this.Highlight.Value });
				painter.DrawHighlight(rect);
			}

			if (this.Sounds) {
				painter.PlayMouseoverSounds(rect);
			}

			if (this.Tooltip.HasValue) {
				Utils.TooltipRegion(rect, this.Tooltip.Value);
			}
		}
	}

	internal sealed class Spacing
	{
		private readonly Action? _onChanged = null;

		private double _size = 0;

		public Spacing(Action onChanged) => this._onChanged = onChanged;

		public Spacing() { }

		public Color Color { get; set; } = Color.white;

		public double Size {
			get => this._size;
			set {
				value = System.Math.Abs(value);
				if (this._size != value) {
					this._size = value;
					this._onChanged?.Invoke();
				}
			}
		}

		public void Copy(Spacing other)
		{
			this.Color = other.Color;
			this.Size = other.Size;
		}
	}

	[HotSwappable]
	internal sealed class TextStyle
	{
		private readonly Action? _onChanged = null;

		private TextAnchor? _anchor = null;

		private GameFont? _font = null;

		public TextStyle(Action onChanged) => this._onChanged = onChanged;

		public TextStyle() { }

		public TextAnchor? Anchor {
			get => this._anchor;
			set {
				if (this._anchor != value) {
					this._anchor = value;
					this._onChanged?.Invoke();
				}
			}
		}

		public Color? Color { get; set; } = UnityEngine.Color.white;

		public GameFont? Font {
			get => this._font;
			set {
				if (this._font != value) {
					this._font = value;
					this._onChanged?.Invoke();
				}
			}
		}

		public void Copy(TextStyle other)
		{
			this.Color = other.Color;
			this.Font = other.Font;
			this.Anchor = other.Anchor;
		}
	}
}
