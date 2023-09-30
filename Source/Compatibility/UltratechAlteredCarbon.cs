#nullable enable

using System;
using System.Reflection;
using BetterHealthTab.HealthTab.Hediffs.Bars;
using Extensions;
using HotSwap;
using Iterator;
using Verse;

namespace Compatibility
{
	[StaticConstructorOnStartup]
	internal static class UltratechAlteredCarbon
	{
		private static readonly Type? s_hediffStackDegradation = null;

		private static readonly IsEmptySleeve_t? s_isEmptySleeve = null;

		private static readonly FieldInfo? s_stackDegradation = null;

		static UltratechAlteredCarbon()
		{
			if (ModsConfig.IsActive("hlx.UltratechAlteredCarbon")) {
				var assembly = AppDomain
					.CurrentDomain
					.GetAssemblies()
					.Filter(x => x.GetName().Name == "AlteredCarbon")
					.Nth(0);
				s_isEmptySleeve = assembly?
					.GetType("AlteredCarbon.ACUtils")?
					.GetMethod("IsEmptySleeve", new Type[] { typeof(Pawn) })?
					.CreateDelegate<IsEmptySleeve_t>();
				s_hediffStackDegradation = assembly?.GetType("AlteredCarbon.Hediff_StackDegradation");
				s_stackDegradation = s_hediffStackDegradation?.GetField("stackDegradation");
			}
		}

		private delegate bool IsEmptySleeve_t(Pawn pawn);

		public static Bar? Create(HediffWithComps hediff)
		{
			return s_hediffStackDegradation is not null && hediff.GetType() == s_hediffStackDegradation ?
				new Degradation(hediff) :
				null;
		}

		public static bool ShouldAllowOperations(Pawn pawn) => s_isEmptySleeve?.Invoke(pawn) ?? false;

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
