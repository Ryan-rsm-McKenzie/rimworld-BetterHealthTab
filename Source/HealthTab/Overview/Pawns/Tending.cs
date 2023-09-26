#nullable enable

using System;
using CLIK;
using CLIK.Components;
using Extensions;
using HotSwap;
using RimWorld;
using Verse;

namespace BetterHealthTab.HealthTab.Overview.Pawns
{
	[HotSwappable]
	internal sealed class Tending : UIComponent
	{
		private readonly CheckBox _checkbox;

		private readonly Label _label;

		private readonly Pawn _pawn;

		private readonly TipSignal _tooltip = new("SelfTendTip".Translate(Faction.OfPlayer.def.pawnsPlural, 0.70.ToStringPercent()).CapitalizeFirst());

		public Tending(Pawn pawn)
		{
			this._pawn = pawn;
			this._label = new() {
				Parent = this,
				Text = "SelfTend".Translate(),
			};
			this._checkbox = new(pawn.playerSettings.selfTend) {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
				},
				OnChanged = this.OnChanged,
			};
		}

		public static UIComponent? Create(Pawn pawn)
		{
			return pawn.IsColonist &&
				!pawn.Dead &&
				!pawn.DevelopmentalStage.Baby() ?
				new Tending(pawn) : null;
		}

		public override double HeightFor(double width) => Text.LineHeight;

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RepaintNow(CLIK.Painting.Painter painter)
		{
			this._checkbox.Checked = this._pawn.playerSettings.selfTend;
			if (this.Focused) {
				Utils.TooltipRegion(this.Rect, this._tooltip);
			}
		}

		protected override void ResizeNow()
		{
			var rect = this.Rect;
			double checkbox = this._checkbox.WidthFor(rect.Height);
			this._checkbox.Geometry = rect.CutRight(checkbox);
			this._label.Geometry = rect;
		}

		private void OnChanged(bool enabled)
		{
			var pawn = this._pawn;
			pawn.playerSettings.selfTend = enabled;

			if (enabled) {
				if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor)) {
					this._checkbox.Checked = false;
					Messages.Message("MessageCannotSelfTendEver".Translate(pawn.LabelShort, pawn), MessageTypeDefOf.RejectInput, false);
				} else if (this._pawn.workSettings.GetPriority(WorkTypeDefOf.Doctor) == 0) {
					Messages.Message("MessageSelfTendUnsatisfied".Translate(pawn.LabelShort, pawn), MessageTypeDefOf.CautionInput, false);
				}
			}
		}
	}
}
