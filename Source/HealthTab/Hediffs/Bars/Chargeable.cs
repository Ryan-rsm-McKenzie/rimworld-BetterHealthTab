#nullable enable

using HotSwap;
using Verse;

namespace BetterHealthTab.HealthTab.Hediffs.Bars
{
	[HotSwappable]
	internal sealed class Chargeable : BasicBar
	{
		private readonly HediffComp_Chargeable _comp;

		public Chargeable(HediffComp_Chargeable comp) => this._comp = comp;

		protected override double Fill => CLIK.Math.Lerp(0, this._comp.Props.fullChargeAmount, this._comp.Charge);
	}
}
