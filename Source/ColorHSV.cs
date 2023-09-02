#nullable enable

using System;
using UnityEngine;

namespace BetterHealthTab
{
	internal struct ColorHSV : IComparable<ColorHSV>
	{
		public float H;

		public float S;

		public float V;

		public ColorHSV(float h = 0, float s = 0, float v = 0)
		{
			this.H = h;
			this.S = s;
			this.V = v;
		}

		public static bool operator !=(ColorHSV left, ColorHSV right)
		{
			return left.CompareTo(right) != 0;
		}

		public static bool operator <(ColorHSV left, ColorHSV right)
		{
			return left.CompareTo(right) < 0;
		}

		public static bool operator <=(ColorHSV left, ColorHSV right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator ==(ColorHSV left, ColorHSV right)
		{
			return left.CompareTo(right) == 0;
		}

		public static bool operator >(ColorHSV left, ColorHSV right)
		{
			return left.CompareTo(right) > 0;
		}

		public static bool operator >=(ColorHSV left, ColorHSV right)
		{
			return left.CompareTo(right) >= 0;
		}

		public readonly int CompareTo(ColorHSV other)
		{
			return
				this.H != other.H ? this.H < other.H ? -1 : 1 :
				this.S != other.S ? this.S < other.S ? -1 : 1 :
				this.V != other.V ? this.V < other.V ? -1 : 1 :
				0;
		}

		public override readonly bool Equals(object obj)
		{
			if (obj is ColorHSV other) {
				return this == other;
			} else {
				return false;
			}
		}

		public override readonly int GetHashCode()
		{
			return (this.H, this.S, this.V).GetHashCode();
		}
	}

	internal static class ColorHSVExt
	{
		public static ColorHSV ToHSV(this Color self)
		{
			var result = new ColorHSV();
			Color.RGBToHSV(self, out result.H, out result.S, out result.V);
			return result;
		}
	}
}
