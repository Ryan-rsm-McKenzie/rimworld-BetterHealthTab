#nullable enable

using System;
using CLIK.Painting;

namespace CLIK.Context
{
	internal sealed class Group : IDisposable
	{
		public Group(Rect rect)
		{
			Verse.Widgets.BeginGroup((UnityEngine.Rect)rect);
		}

		public void Dispose()
		{
			Verse.Widgets.EndGroup();
			GC.SuppressFinalize(this);
		}
	}

	internal sealed class GUIStyle : IDisposable
	{
		private readonly UnityEngine.TextAnchor _anchor;

		private readonly UnityEngine.Color _color;

		private readonly Verse.GameFont _font;

		public GUIStyle(CSS.TextStyle text)
			: this(text.Font, text.Anchor, text.Color)
		{ }

		public GUIStyle(
			Verse.GameFont? font = null,
			UnityEngine.TextAnchor? anchor = null,
			UnityEngine.Color? color = null)
		{
			this._font = Verse.Text.Font;
			if (font.HasValue) {
				Verse.Text.Font = font.Value;
			}

			this._anchor = Verse.Text.Anchor;
			if (anchor.HasValue) {
				Verse.Text.Anchor = anchor.Value;
			}

			this._color = UnityEngine.GUI.color;
			if (color.HasValue) {
				UnityEngine.GUI.color = color.Value;
			}
		}

		public void Dispose()
		{
			Verse.Text.Font = this._font;
			Verse.Text.Anchor = this._anchor;
			UnityEngine.GUI.color = this._color;
			GC.SuppressFinalize(this);
		}
	}

	internal sealed class Palette : IDisposable
	{
		private readonly Painter _painter;

		public Palette(Painter painter, Painting.Palette palette)
		{
			painter.Push(palette);
			this._painter = painter;
		}

		public void Dispose()
		{
			this._painter.Pop();
			GC.SuppressFinalize(this);
		}
	}

	internal sealed class ScrollView : IDisposable
	{
		public ScrollView(
			Rect position,
			ref Point scrollPosition,
			Rect viewRect,
			bool showScrollbars = true)
		{
			var scroll = (UnityEngine.Vector2)scrollPosition;
			Verse.Widgets.BeginScrollView(
				(UnityEngine.Rect)position,
				ref scroll,
				(UnityEngine.Rect)viewRect,
				showScrollbars);
			scrollPosition = (Point)scroll;
		}

		public void Dispose()
		{
			Verse.Widgets.EndScrollView();
			GC.SuppressFinalize(this);
		}
	}
}
