#nullable enable

namespace CLIK.Painting
{
	internal record struct Margins
	{
		public static readonly Margins Zero = new(0, 0, 0, 0);

		public Margins() { }

		public Margins(double left, double top, double right, double bottom)
		{
			this.Left = left;
			this.Top = top;
			this.Right = right;
			this.Bottom = bottom;
		}

		public Margins(Margins other)
			: this(other.Left, other.Top, other.Right, other.Bottom)
		{ }

		public Margins(UnityEngine.RectOffset other)
			: this(other.left, other.top, other.right, other.bottom)
		{ }

		public double Bottom { get; set; } = 0;

		public double Left { get; set; } = 0;

		public readonly bool Null =>
			this.Left == 0 &&
			this.Top == 0 &&
			this.Right == 0 &&
			this.Bottom == 0;

		public double Right { get; set; } = 0;

		public double Top { get; set; } = 0;

		public static explicit operator Margins(UnityEngine.RectOffset self) => new(self);

		public readonly void Deconstruct(out double left, out double top, out double right, out double bottom)
		{
			left = this.Left;
			top = this.Top;
			right = this.Right;
			bottom = this.Bottom;
		}
	}
}
