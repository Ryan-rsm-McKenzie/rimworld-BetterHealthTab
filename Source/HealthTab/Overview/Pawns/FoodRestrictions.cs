#nullable enable

using System;
using System.Collections.Generic;
using CLIK.Components;
using CLIK.Extensions;
using CLIK.Painting;
using CLIK.Windows;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterHealthTab.HealthTab.Overview.Pawns
{
	[HotSwappable]
	internal sealed class FoodRestrictions : UIComponent
	{
		private readonly ButtonText _dropdown;

		private readonly Label _label;

		private readonly List<Option> _options = new();

		private readonly Pawn _pawn;

		public FoodRestrictions(Pawn pawn)
		{
			this._pawn = pawn;
			this._label = new() {
				Parent = this,
				Text = "FoodRestrictionShort".Translate(),
			};

			this._dropdown = new() {
				Parent = this,
				OnPress = () => {
					if (Dropdown.IsOpen()) {
						Dropdown.Stop();
					} else {
						Dropdown.Start(this._options);
					}
				},
				Text = pawn.foodRestriction.CurrentFoodRestriction.label,
				TextStyle = new() {
					Anchor = TextAnchor.MiddleCenter,
				},
			};

			this._options.AddRange(
				Current
					.Game
					.foodRestrictionDatabase
					.AllFoodRestrictions
					.Map(x => new Option(
						x.label,
						() => {
							pawn.foodRestriction.CurrentFoodRestriction = x;
							this._dropdown.Text = x.label;
							Dropdown.Stop();
						}))
					.Chain(Iter.Once(new Option(
						"ManageFoodRestrictions".Translate(),
						() => {
							Find.WindowStack.Add(new Dialog_ManageFoodRestrictions(null));
							Dropdown.Stop();
						}))));
		}

		public static UIComponent? Create(Pawn pawn)
		{
			return (pawn.foodRestriction?.Configurable ?? false) &&
				!pawn.DevelopmentalStage.Baby() ?
				new FoodRestrictions(pawn) : null;
		}

		public override double HeightFor(double width) => Text.LineHeight;

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RepaintNow(Painter painter)
		{
			this._dropdown.Text = this._pawn.foodRestriction.CurrentFoodRestriction.label;
		}

		protected override void ResizeNow()
		{
			var rect = this.Rect;
			this._dropdown.Geometry = rect.CutRight(rect.Width / 2);
			this._label.Geometry = rect;
		}

		[HotSwappable]
		private sealed class Option : UIComponent
		{
			private readonly Label _label;

			private readonly Action _onPress;

			public Option(string label, Action onPress)
			{
				this.ListensForInput = true;

				this._onPress = onPress;
				this._label = new() {
					Parent = this,
					Text = label,
				};
			}

			public override double HeightFor(double width) => this._label.HeightFor(width);

			public override double WidthFor(double height) => this._label.WidthFor(height);

			protected override void InputNow(Painter painter)
			{
				if (this.Focused && Event.current.IsMouseDown(0)) {
					Event.current.Use();
					this._onPress.Invoke();
				}
			}

			protected override void RepaintNow(Painter painter) { }

			protected override void ResizeNow() => this._label.Geometry = this.Rect;
		}
	}
}
