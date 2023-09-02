#nullable enable

using System;
using CLIK.Extensions;

namespace CLIK.Painting
{
	internal record struct Size
	{
		public static readonly Size Zero = new(0, 0);

		public Size() { }

		public Size(double width, double height)
		{
			this.Width = width;
			this.Height = height;
		}

		public Size(Size other)
			: this(other.Width, other.Height)
		{ }

		public Size(UnityEngine.Vector2 other)
			: this(other.x, other.y)
		{ }

		public readonly bool Empty => this.Width <= 0 || this.Height <= 0;

		public double Height { get; set; } = 0;

		public readonly bool Null => this.Width == 0 && this.Height == 0;

		public readonly bool Valid => this.Width >= 0 && this.Height >= 0;

		public double Width { get; set; } = 0;

		public static explicit operator Size(UnityEngine.Vector2 self) => new(self);

		public static Size operator -(Size left, Size right) => new(left.Width - right.Width, left.Height - right.Height);

		public static Size operator *(double left, Size right) => right * left;

		public static Size operator *(Size left, double right) => new(left.Width * right, left.Height * right);

		public static Size operator /(Size left, double right) => new(left.Width / right, left.Height / right);

		public static Size operator +(Size left, Size right) => new(left.Width + right.Width, left.Height + right.Height);

		public static explicit operator UnityEngine.Vector2(Size self) => new((float)self.Width, (float)self.Height);

		public readonly Size Ceiling() => new(this.Width.Ceiling(), this.Height.Ceiling());

		public readonly void Deconstruct(out double width, out double height)
		{
			width = this.Width;
			height = this.Height;
		}

		public readonly Size ExpandedTo(Size other) =>
			new(System.Math.Max(this.Width, other.Width), System.Math.Max(this.Height, other.Height));

		public readonly Size Floor() => new(this.Width.Floor(), this.Height.Floor());

		public readonly override string? ToString() => $"(W:{this.Width},H:{this.Height})";
	}
}
