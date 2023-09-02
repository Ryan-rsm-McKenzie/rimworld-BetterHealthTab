#nullable enable

using HotSwap;
using Verse;

namespace BetterHealthTab.HealthTab.Hediffs.Bars
{
	[HotSwappable]
	internal sealed class Disappears : BasicBar
	{
		private readonly HediffComp_Disappears _comp;

		public Disappears(HediffComp_Disappears comp)
		{
			this._comp = comp;
			this.Primary = comp.parent.LabelColor;
		}

		protected override double Fill => 1 - this._comp.Progress;
	}
}
