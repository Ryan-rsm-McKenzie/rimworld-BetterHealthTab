#nullable enable

using System;
using System.Collections.Generic;
using CLIK;
using CLIK.Components;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BetterHealthTab.HealthTab.Operations
{
	[HotSwappable]
	internal sealed class Operation : UIComponent
	{
		private readonly Icon _icon;

		private readonly Label _label;

		private readonly Thing _medBillThing;

		private readonly BodyPartRecord? _part;

		private readonly RecipeDef _recipe;

		private readonly Label _requires;

		public Operation(Pawn pawn, Thing medBillThing, RecipeDef recipe, AcceptanceReport report, IReadOnlyCollection<ThingDef> missing, BodyPartRecord? part)
		{
			this.ListensForInput = true;

			this._medBillThing = medBillThing;
			this._recipe = recipe;
			this._part = part;

			this._icon = MakeOutputIcon(recipe, part);
			this._icon.Parent = this;

			string requires = string.Concat(
				recipe
					.ingredients
					.Map(x => x.SummaryFor(recipe))
					.Filter(x => !x.IsEmpty())
					.Intersperse("\n"));
			this._requires = new() {
				Parent = this,
				Text = requires,
				Visible = !requires.IsEmpty(),
			};

			string label = recipe.Worker
				.GetLabelWhenUsedOn(pawn, part)
				.CapitalizeFirst();
			if (part is not null && !recipe.hideBodyPartNames)
				label += $"\n({part.Label})";
			if (!(report.Reason?.IsEmpty() ?? true))
				label += $" ({report.Reason})";
			this._label = new() {
				Parent = this,
				Text = label,
			};

			_ = missing;
		}

		public string Label => this._label.Text;

		public override double HeightFor(double width) => Text.LineHeight * 3;

		public bool Matches(SearchBar search)
		{
			return search.Matches(this._label.Text) || search.Matches(this._requires.Text);
		}

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void InputNow(Painter painter)
		{
			if (this.Focused && Event.current.IsMouseDown(0) && this._medBillThing is Pawn medPawn) {
				Event.current.Use();
				HealthCardUtility.CreateSurgeryBill(medPawn, this._recipe, this._part);
				SoundDefOf.Tick_Low.PlayOneShotOnCamera();
				this
					.GetAncestorByType<Tab.Component>()?
					.GetDescendantByType<Window>()?
					.InvalidateCache();
			}
		}

		protected override void RepaintNow(Painter painter)
		{
			if (this.Focused) {
				var rect = this.Rect;
				painter.DrawHighlight(rect);
				painter.PlayMouseoverSounds(rect);
			}
		}

		protected override void ResizeNow()
		{
			var rect = this.Rect.Contract(1);

			var icon = rect.CutLeft(this._icon.WidthFor(rect.Height * 0.50));
			this._icon.Geometry = icon with {
				Height = icon.Height * 0.50,
				Center = icon.Center,
			};
			rect.CutLeft(GenUI.GapTiny);

			var label = rect.CutLeft(rect.Width * 0.50 - GenUI.GapTiny / 2);
			this._label.Geometry = label;
			rect.CutLeft(GenUI.GapTiny);

			this._requires.Geometry = rect;
		}

		private static Icon MakeOutputIcon(RecipeDef recipe, BodyPartRecord? part)
		{
			var color = Color.white;
			var icon = BaseContent.BadTex;
			var thing = recipe.UIIconThing;

			if (thing is null && recipe.Worker is Recipe_RemoveBodyPart removal) {
				thing = removal.SpawnPartsWhenRemoved ?
					part?.def.spawnThingOnRemoved :
					null;
			}

			thing ??= recipe
				.ingredients
				.FilterMap(x => x.filter.AnyAllowedDef)
				.Chain(Iter.Once(recipe.defaultIngredientFilter.AnyAllowedDef))
				.Nth(0);

			if (thing is not null) {
				icon = thing.uiIcon;
				color = thing.uiIconColor;
			}

			return new() {
				Color = color,
				Texture = icon ?? BaseContent.BadTex,
			};
		}
	}
}
