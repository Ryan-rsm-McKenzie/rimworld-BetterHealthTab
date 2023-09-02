#nullable enable

using System;
using System.Collections.Generic;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using UnityEngine;
using Verse;
using Rect = CLIK.Painting.Rect;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class ButtonText : UIComponent, IButton
	{
		private static readonly Rect[] s_uvs = CutUVs();

		private readonly List<(Texture2D Atlas, Rect[] Scale9)> _states = new();

		private readonly CSS.TextStyle _textStyle;

		private double _padding = GenUI.GapWide;

		private string _text = string.Empty;

		public ButtonText()
		{
			this.ListensForInput = true;
			this._textStyle = new(this.InvalidateSize);
		}

		private enum State
		{
			Out,

			Over,

			Down,
		}

		public ButtonGroup? ButtonGroup { get; set; } = null;

		public Color Color { get; set; } = Color.white;

		public CSS.Mouseover Mouseover { get; set; } = new();

		public Action? OnPress { get; set; } = null;

		public double Padding {
			get => this._padding;
			set {
				if (this._padding != value) {
					this._padding = value;
					this.InvalidateSize();
				}
			}
		}

		public string Text {
			get => this._text;
			set {
				if (this._text != value) {
					this._text = value;
					this.InvalidateSize();
				}
			}
		}

		public CSS.TextStyle TextStyle {
			get => this._textStyle;
			set => this._textStyle.Copy(value);
		}

		public override double HeightFor(double width)
		{
			var font = this.TextStyle.Font;
			return font.HasValue ?
				this._text.DisplayHeight(width, font.Value) :
				this._text.DisplayHeight(width);
		}

		public void OnButtonDown() => this.OnPress?.Invoke();

		public override double WidthFor(double height)
		{
			var font = this.TextStyle.Font;
			double width = font.HasValue ?
				this._text.DisplayWidth(font.Value) :
				this._text.DisplayWidth();
			return width + this._padding;
		}

		protected override void InputNow(Painter painter)
		{
			bool focused = this.Focused;
			this.ButtonGroup?.HandleSelection(this, focused);
			if (focused && Event.current.IsMouseDown(0)) {
				Event.current.Use();
				this.OnButtonDown();
			}
		}

		protected override void RepaintNow(Painter painter)
		{
			var rect = this.Rect;
			var state = State.Out;
			if (this.Focused) {
				this.Mouseover.Repaint(painter, rect);
				state = State.Over;
				if (Input.GetMouseButton(0)) {
					state = State.Down;
				}
			}

			var (atlas, parts) = this._states[(int)state];
			foreach (var (part, uv) in parts.Zip(s_uvs)) {
				painter.DrawAtlas(part, atlas, uv);
			}

			if (!this._text.IsEmpty()) {
				using var _ = new Context.Palette(painter, new() { TextStyle = this.TextStyle });
				painter.Label(rect, this._text);
			}
		}

		protected override void ResizeNow()
		{
			var textures = new Texture2D[] {
				Widgets.ButtonBGAtlas,
				Widgets.ButtonBGAtlasMouseover,
				Widgets.ButtonBGAtlasClick,
			};

			var rect = this.Rect;
			this._states.Clear();
			this._states.AddRange(textures.Map(x => (x, CutParts(rect, x))));
		}

		private static Rect[] CutParts(Rect rect, Texture2D texture)
		{
			double margin = Math.Min(texture.width / 4.0, rect.Width / 2.0, rect.Height / 2.0);

			var l = rect.CutLeft(margin);
			l.Right += 1;
			var tl = l.CutTop(margin);
			var bl = l.CutBottom(margin);
			var cl = l;

			var r = rect.CutRight(margin);
			r.Left -= 1;
			var tr = r.CutTop(margin);
			var br = r.CutBottom(margin);
			var cr = r;

			var m = rect;
			var tm = m.CutTop(margin);
			var bm = m.CutBottom(margin);
			var cm = m;

			return new Rect[] {
				tl, tm, tr,
				cl, cm, cr,
				bl, bm, br,
			};
		}

		private static Rect[] CutUVs()
		{
			const double Margin = 0.25;
			var rect = new Rect(0, 0, 1, 1);

			var l = rect.CutLeft(Margin);
			var tl = l.CutTop(Margin);
			var bl = l.CutBottom(Margin);
			var cl = l;

			var r = rect.CutRight(Margin);
			var tr = r.CutTop(Margin);
			var br = r.CutBottom(Margin);
			var cr = r;

			var tm = rect.CutTop(Margin);
			var bm = rect.CutBottom(Margin);
			var cm = rect;

			return new Rect[] {
					tl, tm, tr,
					cl, cm, cr,
					bl, bm, br,
				};
		}
	}
}
