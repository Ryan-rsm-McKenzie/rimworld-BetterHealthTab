#nullable enable

using CLIK.Extensions;

namespace CLIK.Painting
{
	internal record struct Point
	{
		public static readonly Point Zero = new(0, 0);

		public Point() { }

		public Point(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}

		public Point(Point other)
			: this(other.X, other.Y)
		{ }

		public Point(UnityEngine.Vector2 other)
			: this(other.x, other.y)
		{ }

		public readonly bool Null => this.X == 0 && this.Y == 0;

		public double X { get; set; } = 0;

		public double Y { get; set; } = 0;

		public static explicit operator Point(UnityEngine.Vector2 self) => new(self);

		public static Point operator -(Point left, Point right) => new(left.X - right.X, left.Y - right.Y);

		public static Point operator *(Point left, double right) => new(left.X * right, left.Y * right);

		public static Point operator *(double left, Point right) => right * left;

		public static Point operator /(Point left, double right) => new(left.X / right, left.Y / right);

		public static Point operator +(Point left, Point right) => new(left.X + right.X, left.Y + right.Y);

		public static explicit operator UnityEngine.Vector2(Point self) => new((float)self.X, (float)self.Y);

		public readonly Point Ceiling() => new(this.X.Ceiling(), this.Y.Ceiling());

		public readonly void Deconstruct(out double x, out double y)
		{
			x = this.X;
			y = this.Y;
		}

		public readonly Point Floor() => new(this.X.Floor(), this.Y.Floor());

		public readonly Point FromScreenSpace() =>
			(Point)UnityEngine.GUIUtility.ScreenToGUIPoint((UnityEngine.Vector2)this);

		public readonly Point ToScreenSpace() =>
			(Point)UnityEngine.GUIUtility.GUIToScreenPoint((UnityEngine.Vector2)this);

		public readonly override string? ToString() => $"(X:{this.X},Y:{this.Y})";

		public readonly UnityEngine.Vector2 ToUnity() => (UnityEngine.Vector2)this;
	}
}
