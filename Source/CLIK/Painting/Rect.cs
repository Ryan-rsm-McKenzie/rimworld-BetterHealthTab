#nullable enable

using System;
using CLIK.Extensions;
using HotSwap;

namespace CLIK.Painting
{
	[HotSwappable]
	internal record struct Rect
	{
		public static readonly Rect Zero = new(0, 0, 0, 0);

		private double _height = 0;

		private double _width = 0;

		private double _x = 0;

		private double _y = 0;

		public Rect() { }

		public Rect(double x, double y, double width, double height)
		{
			this._x = x;
			this._y = y;
			this._width = width;
			this._height = height;
		}

		public Rect(Point topLeft, Point bottomRight)
			: this(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y)
		{ }

		public Rect(Point topLeft, Size size)
			: this(topLeft.X, topLeft.Y, size.Width, size.Height)
		{ }

		public Rect(Rect other)
			: this(other.Left, other.Top, other.Width, other.Height)
		{ }

		public Rect(UnityEngine.Rect other)
			: this(other.x, other.y, other.width, other.height)
		{ }

		public double Bottom {
			readonly get => this._y + this._height;
			set => this._height = value - this._y;
		}

		public Point BottomLeft {
			readonly get => new(this.Left, this.Bottom);
			set => (this.Left, this.Bottom) = value;
		}

		public Point BottomRight {
			readonly get => new(this.Right, this.Bottom);
			set => (this.Right, this.Bottom) = value;
		}

		public Point Center {
			readonly get => new(this._x + this._width / 2, this._y + this._height / 2);
			set {
				this._x = value.X - this._width / 2;
				this._y = value.Y - this._height / 2;
			}
		}

		public readonly bool Empty => !this.Valid;

		public double Height {
			readonly get => this._height;
			set => this._height = value;
		}

		public double Left {
			readonly get => this._x;
			set {
				this._width -= value - this._x;
				this._x = value;
			}
		}

		public readonly Rect Normalized {
			get {
				var (left, right) = this.Width >= 0 ?
					(this.Left, this.Right) :
					(this.Right, this.Left);
				var (top, bottom) = this.Height >= 0 ?
					(this.Top, this.Bottom) :
					(this.Bottom, this.Top);
				return new(new Point(left, top), new Point(right, bottom));
			}
		}

		public readonly bool Null => this.Width == 0 && this.Height == 0;

		public double Right {
			readonly get => this._x + this._width;
			set => this._width = value - this._x;
		}

		public Size Size {
			readonly get => new(this._width, this._height);
			set => (this._width, this._height) = value;
		}

		public double Top {
			readonly get => this._y;
			set {
				this._height -= value - this._y;
				this._y = value;
			}
		}

		public Point TopLeft {
			readonly get => new(this.Left, this.Top);
			set => (this.Left, this.Top) = value;
		}

		public Point TopRight {
			readonly get => new(this.Right, this.Top);
			set => (this.Right, this.Top) = value;
		}

		public readonly bool Valid => this.Width > 0 && this.Height > 0;

		public double Width {
			readonly get => this._width;
			set => this._width = value;
		}

		public static explicit operator Rect(UnityEngine.Rect self) => new(self);

		public static Rect operator -(Rect left, Margins right) => left.Contract(right);

		public static Rect operator +(Rect left, Margins right) => left.Expand(right);

		public static explicit operator UnityEngine.Rect(Rect self) => new((float)self.Left, (float)self.Top, (float)self.Width, (float)self.Height);

		public readonly Rect Add(Side side, double value)
		{
			return side switch {
				Side.Top => this.AddTop(value),
				Side.Bottom => this.AddBottom(value),
				Side.Left => this.AddLeft(value),
				Side.Right => this.AddRight(value),
				_ => throw new NotImplementedException(),
			};
		}

		public readonly Rect AddBottom(double value) =>
			new(this.Left, this.Top, this.Width, this.Height + value);

		public readonly Rect AddLeft(double value) =>
			new(this.Left - value, this.Top, this.Width + value, this.Height);

		public readonly Rect AddRight(double value) =>
			new(this.Left, this.Top, this.Width + value, this.Height);

		public readonly Rect AddTop(double value) =>
			new(this.Left, this.Top - value, this.Width, this.Height + value);

		public readonly Rect Ceiling() => new(this.Left.Ceiling(), this.Top.Ceiling(), this.Width.Ceiling(), this.Height.Ceiling());

		public readonly bool Contains(Point other)
		{
			Utils.Assert(this.Valid, "Can not computer geometric intersections on an invalid Rect!");
			return
				other.X >= this.Left &&
				other.X <= this.Right &&
				other.Y >= this.Top &&
				other.Y <= this.Bottom;
		}

		public readonly bool Contains(Rect other)
		{
			Utils.Assert(this.Valid && other.Valid, "Can not computer geometric intersections on invalid Rect(s)!");
			return
				other.Left >= this.Left &&
				other.Right <= this.Right &&
				other.Top >= this.Top &&
				other.Bottom <= this.Bottom;
		}

		public readonly Rect Contract(Margins margins)
		{
			var tl = this.TopLeft + new Point(margins.Left, margins.Top);
			var br = this.BottomRight - new Point(margins.Right, margins.Bottom);
			return new(tl, br);
		}

		public readonly Rect Contract(double value)
		{
			value = Math.Min(value, this.Width / 2, this.Height / 2);
			return new(
				this.Left + value,
				this.Top + value,
				this.Width - 2 * value,
				this.Height - 2 * value);
		}

		public Rect Cut(Side side, double value)
		{
			return side switch {
				Side.Top => this.CutTop(value),
				Side.Bottom => this.CutBottom(value),
				Side.Left => this.CutLeft(value),
				Side.Right => this.CutRight(value),
				_ => throw new NotImplementedException(),
			};
		}

		public Rect CutBottom(double value)
		{
			value = System.Math.Min(value, this.Height);
			this.Bottom -= value;
			return new(this.Left, this.Bottom, this.Width, value);
		}

		public Rect CutLeft(double value)
		{
			value = System.Math.Min(value, this.Width);
			this.Left += value;
			return new(this.Left - value, this.Top, value, this.Height);
		}

		public Rect CutRight(double value)
		{
			value = System.Math.Min(value, this.Width);
			this.Right -= value;
			return new(this.Right, this.Top, value, this.Height);
		}

		public Rect CutTop(double value)
		{
			value = System.Math.Min(value, this.Height);
			this.Top += value;
			return new(this.Left, this.Top - value, this.Width, value);
		}

		public readonly Rect Expand(Margins margins)
		{
			var tl = this.TopLeft - new Point(margins.Left, margins.Top);
			var br = this.BottomRight + new Point(margins.Right, margins.Bottom);
			return new(tl, br);
		}

		public readonly Rect Expand(double value)
		{
			return new(
				this.Left - value,
				this.Top - value,
				this.Width + 2 * value,
				this.Height + 2 * value);
		}

		public readonly Rect Floor() => new(this.Left.Floor(), this.Top.Floor(), this.Width.Floor(), this.Height.Floor());

		public readonly Rect FromScreenSpace() => new(this.TopLeft.FromScreenSpace(), this.Size);

		public readonly Rect Get(Side side, double value)
		{
			return side switch {
				Side.Top => this.GetTop(value),
				Side.Bottom => this.GetBottom(value),
				Side.Left => this.GetLeft(value),
				Side.Right => this.GetRight(value),
				_ => throw new NotImplementedException(),
			};
		}

		public readonly Rect GetBottom(double value)
		{
			value = System.Math.Min(value, this.Height);
			return new(this.Left, this.Bottom - value, this.Width, value);
		}

		public readonly Rect GetLeft(double value)
		{
			value = System.Math.Min(value, this.Width);
			return new(this.Left, this.Top, value, this.Height);
		}

		public readonly Rect GetRight(double value)
		{
			value = System.Math.Min(value, this.Width);
			return new(this.Right - value, this.Top, value, this.Height);
		}

		public readonly Rect GetTop(double value)
		{
			value = System.Math.Min(value, this.Height);
			return new(this.Left, this.Top, this.Width, value);
		}

		public readonly bool Intersects(Rect other)
		{
			Utils.Assert(this.Valid && other.Valid, "Can not computer geometric intersections on invalid Rect(s)!");
			return
				this.Left <= other.Right &&
				other.Left <= this.Right &&
				this.Top <= other.Bottom &&
				other.Top <= this.Bottom;
		}

		public readonly Rect ToScreenSpace() => new(this.TopLeft.ToScreenSpace(), this.Size);

		public readonly override string? ToString() => $"(X:{this._x},Y:{this._y},W:{this._width},H:{this._height})";

		public readonly UnityEngine.Rect ToUnity() => (UnityEngine.Rect)this;

		public void Translate(double dx, double dy)
		{
			this._x += dx;
			this._y += dy;
		}

		public void Translate(Point delta) => this.Translate(delta.X, delta.Y);

		public readonly Rect Translated(double dx, double dy) =>
			new(this.Left + dx, this.Top + dy, this.Width, this.Height);

		public readonly Rect Translated(Point delta) => new(this.TopLeft + delta, this.Size);
	}
}
