#nullable enable

using System;
using System.Reflection;
using BetterHealthTab.HealthTab.Hediffs.Bars;
using HotSwap;
using Iterator;
using Verse;

namespace Compatibility
{
	[StaticConstructorOnStartup]
	internal static class UltratechAlteredCarbon
	{
		private static readonly Type? s_hediffStackDegradation = null;

		private static readonly FieldInfo? s_stackDegradation = null;

		static UltratechAlteredCarbon()
		{
			if (ModsConfig.IsActive("hlx.UltratechAlteredCarbon")) {
				s_hediffStackDegradation = AppDomain
					.CurrentDomain
					.GetAssemblies()
					.Filter(x => x.GetName().Name == "AlteredCarbon")
					.Nth(0)?
					.GetType("AlteredCarbon.Hediff_StackDegradation");
				s_stackDegradation = s_hediffStackDegradation?.GetField("stackDegradation");
			}
		}

		public static Bar? Create(HediffWithComps hediff)
		{
			return s_hediffStackDegradation is not null && hediff.GetType() == s_hediffStackDegradation ?
				new Degradation(hediff) :
				null;
		}

		[HotSwappable]
		private sealed class Degradation : BasicBar
		{
			private readonly HediffWithComps _hediff;

			public Degradation(HediffWithComps hediff)
			{
				this.Primary = hediff.LabelColor;
				this._hediff = hediff;
			}

			protected override double Fill => (float)s_stackDegradation!.GetValue(this._hediff);
		}
	}
}
