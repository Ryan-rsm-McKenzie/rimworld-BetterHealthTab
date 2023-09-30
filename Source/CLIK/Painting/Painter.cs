#nullable enable

using System;
using System.Collections.Generic;
using CLIK.Extensions;
using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CLIK.Painting
{
	[HotSwappable]
	internal sealed class Painter
	{
		private readonly bool _dragging;

		private readonly Stack<Frame> _frames = new();

		public Painter(bool dragging = false) => this._dragging = dragging;

		public int Count => this._frames.Count;

		public void DrawAtlas(Rect rect, Texture2D atlas, Rect uvmap)
		{
			var uv = uvmap with {
				Top = 1 - uvmap.Top - uvmap.Height,
				Height = uvmap.Height,
			};
			GUI.DrawTextureWithTexCoords(
				position: rect.ToUnity(),
				image: atlas,
				texCoords: uv.ToUnity(),
				true);
		}

		public void DrawHighlight(Rect rect) => this.DrawTexture(rect, TexUI.HighlightTex);

		public void DrawTexture(Rect rect, Texture2D texture)
		{
			GUI.DrawTexture(
				position: rect.ToUnity(),
				image: texture,
				scaleMode: ScaleMode.StretchToFill,
				alphaBlend: true,
				imageAspect: 0,
				color: GUI.color,
				borderWidths: Vector4.zero,
				borderRadiuses: Vector4.zero);
		}

		public void FillRect(Rect rect) =>
			this.DrawTexture(rect, BaseContent.WhiteTex);

		public void Label(Rect rect, string text)
		{
			var content = new GUIContent(text);
			var style = Text.CurFontStyle;
			style.Draw(rect.ToUnity(), content, -1, false, false);
		}

		public void OutlineRect(Rect rect, double size)
		{
			this.FillRect(rect.GetTop(size));
			this.FillRect(rect.GetBottom(size));
			this.FillRect(rect.GetLeft(size));
			this.FillRect(rect.GetRight(size));
		}

		public void PlayMouseoverSounds(Rect rect, SoundDef sound)
		{
			if (!this._dragging) {
				Utils.Assert(Event.current.IsRepaint(),
					"Mouseover sounds are only valid during repaint events!");
				Utils.Assert(Utils.MouseIsOver(rect),
					"Mouseover sounds are only valid if the mouse is over the given rect!");

				var call = new MouseoverSounds.MouseoverRegionCall {
					rect = rect.ToScreenSpace().ToUnity(),
					sound = sound,
					mouseIsOver = true,
				};
				MouseoverSounds.frameCalls.Add(call);
				MouseoverSounds.lastUsedCallInd = -1;
			}
		}

		public void PlayMouseoverSounds(Rect rect) => this.PlayMouseoverSounds(rect, SoundDefOf.Mouseover_Standard);

		public void Pop()
		{
			var frame = this._frames.Pop();
			frame.Context?.Dispose();
			Text.Anchor = frame.Anchor;
			GUI.color = frame.Color;
			Text.Font = frame.Font;
		}

		public void Push(Palette palette)
		{
			this._frames.Push(new() {
				Anchor = Text.Anchor,
				Color = GUI.color,
				Context = palette.Context,
				Font = Text.Font,
			});
			Text.Anchor = palette.Anchor ?? Text.Anchor;
			GUI.color *= palette.Color ?? Color.white;
			Text.Font = palette.Font ?? Text.Font;
		}

		public string TextField(Rect rect, string text, ref int? id)
		{
			var content = new GUIContent(text);
			id ??= GUIUtility.GetControlID(
				focus: FocusType.Keyboard,
				position: rect.ToScreenSpace().ToUnity());

			GUI.DoTextField(
				position: rect.ToUnity(),
				id: id.Value,
				content: content,
				multiline: false,
				maxLength: -1,
				style: Text.CurTextFieldStyle,
				secureText: null,
				maskChar: '\0');

			return content.text;
		}

		private struct Frame
		{
			public TextAnchor Anchor = default;

			public Color Color = default;

			public IDisposable? Context = null;

			public GameFont Font = default;

			public Frame() { }
		}
	}
}
