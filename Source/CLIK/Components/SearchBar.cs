#nullable enable

using System;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Rect = CLIK.Painting.Rect;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class SearchBar : UIComponent
	{
		private readonly ButtonTexture _clear;

		private readonly CIcons _icons;

		private readonly Icon _search;

		private readonly CSS.TextStyle _textStyle;

		private Rect _canvas = Rect.Zero;

		private int? _id = null;

		private string _text = string.Empty;

		public SearchBar()
		{
			this.HasPalette = true;
			this.ListensForInput = true;

			this._icons = new(this.InvalidateSize);
			this._textStyle = new(this.InvalidateSize);

			this._search = new() {
				Parent = this,
				Visible = false,
			};
			this._clear = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
				},
				OnPress = () => this.Text = "",
				Sound = SoundDefOf.CancelMode,
				Visible = false,
			};
		}

		public Color Color { get; set; } = Color.white;

		public CIcons Icons {
			get => this._icons;
			set => this._icons.Copy(value);
		}

		public bool NoMatches { get; set; } = false;

		public Action<string>? OnChanged { get; set; } = null;

		public string Text {
			get => this._text;
			set {
				if (this._text != value) {
					this._text = value;
					this.OnChanged?.Invoke(value);
				}
			}
		}

		public CSS.TextStyle TextStyle {
			get => this._textStyle;
			set => this._textStyle.Copy(value);
		}

		public override double HeightFor(double width)
		{
			var font = this._textStyle.Font;
			return font.HasValue ?
				Verse.Text.LineHeightOf(font.Value) :
				Verse.Text.LineHeight;
		}

		public bool Matches(string? value)
		{
			return (value?.IndexOf(this._text, StringComparison.InvariantCultureIgnoreCase) ?? -1) >= 0;
		}

		public override Palette Palette() => new() { Color = this.Color };

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void InputNow(Painter painter)
		{
			bool focused = this.HasControlFocus();
			if (focused && Event.current.IsKeyDown(KeyCode.Escape)) {
				Event.current.Use();
				this.KillControlFocus();
			} else if (focused && OriginalEventUtility.EventType == EventType.MouseDown && !Utils.MouseIsOver(this._canvas)) {
				this.KillControlFocus();
			} else {
				this.RepaintNow(painter);
			}
		}

		protected override void RepaintNow(Painter painter)
		{
			var textColor = this._textStyle.Color ?? Color.white;
			var color =
				this.NoMatches && !this._text.IsEmpty() ? ColorLibrary.RedReadable :
				this._text.IsEmpty() && !this.HasControlFocus() ? textColor with { a = 0.60f } :
				textColor;
			using var _ = new Context.Palette(
				painter,
				new() {
					Anchor = this.TextStyle.Anchor,
					Color = color,
					Font = this.TextStyle.Font,
				});
			this.Text = painter.TextField(this._canvas, this._text, ref this._id);
		}

		protected override void ResizeNow()
		{
			const double Scale = 0.90;
			using var _ = new Context.GUIStyle(this._textStyle);
			var rect = this.Rect;
			var margins = (Margins)Verse.Text.CurTextFieldStyle.margin;

			if (this._icons.Search is not null) {
				this._search.Visible = true;
				this._search.Texture = this._icons.Search;
				var geometry = rect.CutLeft(this._search.WidthFor(rect.Height * Scale));
				this._search.Geometry = geometry with {
					Height = geometry.Height * Scale,
					Center = geometry.Center,
				};
				rect.CutLeft(margins.Left);
			} else {
				this._search.Visible = false;
				this._search.Geometry = Rect.Zero;
				rect.CutLeft(1);
			}

			if (this._icons.Clear is not null) {
				this._clear.Visible = true;
				this._clear.Texture = this._icons.Clear;
				var geometry = rect.CutRight(this._clear.WidthFor(rect.Height * Scale));
				this._clear.Geometry = geometry with {
					Height = geometry.Height * Scale,
					Center = geometry.Center,
				};
				rect.CutRight(margins.Right);
			} else {
				this._clear.Visible = false;
				this._clear.Geometry = Rect.Zero;
				rect.CutRight(1);
			}

			this._canvas = rect;
		}

		private bool HasControlFocus() => this._id.HasValue && GUIUtility.keyboardControl == this._id.Value;

		private void KillControlFocus()
		{
			Utils.Assert(this.HasControlFocus(),
				"Asked self to kill control focus, but self does not have focus in the first place!");
			GUIUtility.keyboardControl = 0;
			this._id = null;
		}

		[HotSwappable]
		public sealed class CIcons
		{
			private readonly Action? _onChanged = null;

			private Texture2D? _clear = TexButton.CloseXSmall;

			private Texture2D? _search = TexButton.Search;

			public CIcons(Action onChanged) => this._onChanged = onChanged;

			public CIcons() { }

			public Texture2D? Clear {
				get => this._clear;
				set {
					if (this._clear != value) {
						this._clear = value;
						this._onChanged?.Invoke();
					}
				}
			}

			public Texture2D? Search {
				get => this._search;
				set {
					if (this._search != value) {
						this._search = value;
						this._onChanged?.Invoke();
					}
				}
			}

			public void Copy(CIcons other)
			{
				this.Clear = other.Clear;
				this.Search = other.Search;
			}
		}
	}
}
