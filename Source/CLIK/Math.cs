#nullable enable

using System;
using System.Linq;
using CLIK.Extensions;

namespace CLIK
{
	internal static class Math
	{
		public static T Clamp<T>(T v, T lo, T hi)
			where T : IComparable<T>
		{
			return v.CompareTo(lo) < 0 ? lo : v.CompareTo(hi) > 0 ? hi : v;
		}

		public static double InverseLerp(double a, double b, double v)
		{
			return (v - a) / (b - a);
		}

		// https://github.com/microsoft/STL/blob/48eedd369d6c88130fff4d15afb88f016fa6e648/stl/inc/cmath#L1388
		public static double Lerp(double a, double b, double t)
		{
			bool tIsFinite = t.IsFinite();
			if (tIsFinite && a.IsFinite() && b.IsFinite()) {
				if ((a <= 0 && b >= 0) || (a >= 0 && b <= 0)) {
					return t * b + (1 - t) * a;
				}

				if (t == 1) {
					return b;
				}

				double candidate = (t * (b - a)) + a;
				if ((t > 1) == (b > a)) {
					if (b > candidate) {
						return b;
					}
				} else {
					if (candidate > b) {
						return b;
					}
				}

				return candidate;
			}

			if (a.IsNaN() || b.IsNaN()) {
				return (a + b) + t;
			}

			if (t.IsNaN()) {
				return t + t;
			}

			if (tIsFinite) {
				if (t < 0) {
					return a - b;
				} else if (t <= 1) {
					return t * b + (1 - t) * a;
				} else {
					return b - a;
				}
			} else {
				return t * (b - a);
			}
		}

		public static T Max<T>(params T[] values) => values.Max();

		public static T Min<T>(params T[] values) => values.Min();

		public static double Remap(double inMin, double inMax, double outMin, double outMax, double v)
		{
			return Lerp(outMin, outMax, InverseLerp(inMin, inMax, v));
		}
	}
}
