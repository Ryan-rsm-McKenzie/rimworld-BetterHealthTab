#nullable enable

using System;
using System.Collections.Generic;
using CLIK.Components;
using CLIK.Painting;
using HotSwap;
using Iterator;
using UnityEngine;
using Verse;

namespace CLIK.Extensions
{
	internal static class CharExt
	{
		public static bool IsWhiteSpace(this char self) => char.IsWhiteSpace(self);
	}

	internal static class DoubleExt
	{
		public static double Ceiling(this double self) => System.Math.Ceiling(self);

		public static double CeilingToNearestPixel(this double self) =>
			(self * GUIUtility.pixelsPerPoint + 0.48).Ceiling() / GUIUtility.pixelsPerPoint;

		public static double Floor(this double self) => System.Math.Floor(self);

		public static double FloorToNearestPixel(this double self) =>
			(self * GUIUtility.pixelsPerPoint + 0.48).Floor() / GUIUtility.pixelsPerPoint;

		// https://github.com/dotnet/runtime/blob/867f5d483e340af033f2ba83d29b21bab0e3bcb3/src/libraries/System.Private.CoreLib/src/System/Double.cs#L155
		public static unsafe bool IsFinite(this double self)
		{
			long bits = BitConverter.DoubleToInt64Bits(self);
			return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
		}

		public static bool IsInfinity(this double self) => double.IsInfinity(self);

		public static bool IsNaN(this double self) => double.IsNaN(self);

		public static bool IsNegativeInfinity(this double self) => double.IsNegativeInfinity(self);

		public static bool IsPositiveInfinity(this double self) => double.IsPositiveInfinity(self);
	}

	internal static class EventExt
	{
		public static bool IsKeyDown(this Event self) => self.type == EventType.KeyDown;

		public static bool IsKeyDown(this Event self, KeyCode key) => self.IsKeyDown() && self.keyCode == key;

		public static bool IsLayout(this Event self) => self.type == EventType.Layout;

		public static bool IsMouseDown(this Event self) => self.type == EventType.MouseDown;

		public static bool IsMouseDown(this Event self, int button) => self.IsMouseDown() && self.button == button;

		public static bool IsMouseDragging(this Event self) => self.type == EventType.MouseDrag;

		public static bool IsMouseDragging(this Event self, int button) => self.IsMouseDragging() && self.button == button;

		public static bool IsMouseUp(this Event self) => self.type == EventType.MouseUp;

		public static bool IsMouseUp(this Event self, int button) => self.IsMouseUp() && self.button == button;

		public static bool IsRepaint(this Event self) => self.type == EventType.Repaint;
	}

	internal static class IEnumerableExt
	{
		public static IEnumerable<T> Filter<T>(this IEnumerable<T> self, SearchBar search, Func<(T Item, SearchBar Search), bool> predicate)
		{
			search.NoMatches = true;
			return self.Filter(value => {
				if (predicate((value, search))) {
					search.NoMatches = false;
					return true;
				} else {
					return false;
				}
			});
		}
	}

	internal static class IntExt
	{
		public static bool IsEven(this int self) => (self & 0b1) == 0b0;

		public static bool IsOdd(this int self) => (self & 0b1) == 0b1;
	}

	internal static class SideExt
	{
		public static bool Horizontal(this Side self) => ((byte)self & 0b1) == 0b1;

		public static Side Opposite(this Side self) => (Side)((byte)self ^ 0b10);

		public static bool Vertical(this Side self) => ((byte)self & 0b1) == 0b0;
	}

	[HotSwappable]
	internal static class StringExt
	{
		public static double DisplayHeight(this string self, double width)
		{
			var style = Text.CurFontStyle;
			int lines = 0;
			float contentWidth = (float)width.FloorToNearestPixel();

			self = self.StripTags();
			while (!self.IsEmpty()) {
				lines += 1;

				int characters = Math.Clamp(style.GetNumCharactersThatFitWithinWidth(self, contentWidth), 0, self.Length);
				if (characters == 0) {
					break;
				}

				self = self.Substring(characters);
			}

			return lines > 0 ?
				lines * Text.LineHeight + (lines - 1) * Text.SpaceBetweenLines :
				0;
		}

		public static double DisplayWidth(this string self)
		{
			var content = GUIContent.Temp(self);
			var style = Text.CurFontStyle;
			return ((double)style.CalcSize(content).x).CeilingToNearestPixel();
		}
	}

	internal static class TextureExt
	{
		public static Size ScaleToFit(this Texture self, Size dimensions)
		{
			var size = self.SizeOf();
			double scale = System.Math.Min(dimensions.Width / size.Width, dimensions.Height / size.Height);
			return size * scale;
		}

		public static Size ScaleToFit(this Texture self, double width, double height) => self.ScaleToFit(new(width, height));

		public static Size SizeOf(this Texture self) => new(self.width, self.height);
	}

	internal static class UIComponentExt
	{
		public static IListItem WrapForList(this UIComponent self) => new ListItemRendererWrapper(self);
	}

	internal static class WindowStackExt
	{
		public static void RemoveWindowsOfType<T>(this WindowStack self)
		{
			self.RemoveWindowsOfType(typeof(T));
		}
	}
}
