#nullable enable

using System;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class CheckBox : UIComponent, IButton
	{
		private bool _checked;

		public CheckBox(bool on)
		{
			this.ListensForInput = true;
			this._checked = on;
		}

		public ButtonGroup? ButtonGroup { get; set; } = null;

		public bool Checked {
			get => this._checked;
			set {
				if (this._checked != value) {
					this._checked = value;
					this.OnChanged?.Invoke(value);
				}
			}
		}

		public Color Color { get; set; } = Color.white;

		public CSS.Mouseover Mouseover { get; set; } = new();

		public Action<bool>? OnChanged { get; set; } = null;

		public override double HeightFor(double width) => width;

		public void OnButtonDown()
		{
			this.Checked = !this._checked;
			var sound = this._checked ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff;
			sound.PlayOneShotOnCamera();
		}

		public override double WidthFor(double height) => height;

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
			if (this.Focused) {
				this.Mouseover.Repaint(painter, rect);
			}

			var texture = this.Checked ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
			using var _ = new Context.Palette(painter, new() { Color = this.Color });
			painter.DrawTexture(rect, texture);
		}
	}
}
