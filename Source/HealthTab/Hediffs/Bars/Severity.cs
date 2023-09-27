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

		protected override double Fill {
			get {
				double value = CLIK.Math.InverseLerp(this._min, this._max, this._hediff.Severity);
				return this._inverse ? 1 - value : value;
			}
		}

		public static Severity? Create(HediffComp_SeverityModifierBase comp)
		{
			return comp is not HediffComp_TendDuration && !comp.parent.IsPermanent() ?
				Create(comp, inverse: false) :
				null;
		}

		public static Severity? Create(HediffComp_SeverityFromHemogen comp) => Create(comp, inverse: false);

		public static Severity? Create(HediffComp_Pollution comp) => Create(comp, inverse: false);

		public static Severity? Create(HediffComp_SelfHeal comp) => Create(comp, inverse: true);

		public static Severity? Create(HediffComp_SeverityFromGas comp) => Create(comp, inverse: false);

		public static Severity? Create(HediffComp_RandomizeSeverityPhases comp) => Create(comp, inverse: false);

		private static Severity? Create(HediffComp comp, bool inverse)
		{
			var def = comp.Def;
			if (def.initialSeverity > 0 && (def.maxSeverity < float.MaxValue || def.lethalSeverity > 0)) {
				double max =
					def.maxSeverity < float.MaxValue ? def.maxSeverity :
					def.lethalSeverity > 0 ? def.lethalSeverity :
					1.00;
				return new Severity(comp.parent, max, inverse);
			} else {
				return null;
			}
		}
	}
}
