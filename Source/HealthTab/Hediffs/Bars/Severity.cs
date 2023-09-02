#nullable enable

using HotSwap;
using RimWorld;
using Verse;

namespace BetterHealthTab.HealthTab.Hediffs.Bars
{
  [HotSwappable]
	internal sealed class Severity : BasicBar
	{
		private readonly Hediff _hediff;

		private readonly bool _inverse;

		private readonly double _max;

		private readonly double _min;

		public Severity(Hediff hediff, double max, bool inverse)
		{
			this._hediff = hediff;
			this._min = hediff.def.minSeverity;
			this._max = max;
			this._inverse = inverse;
			this.Primary = this._hediff.def == HediffDefOf.BloodLoss ?
				HealthUtility.RedColor :
				this._hediff.LabelColor;
		}

		protected sealed override double Fill {
			get {
				double value = CLIK.Math.InverseLerp(this._min, this._max, this._hediff.Severity);
				return this._inverse ? 1 - value : value;
			}
		}
	}
}
