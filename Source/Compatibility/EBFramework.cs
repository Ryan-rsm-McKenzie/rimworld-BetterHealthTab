#nullable enable

using System;
using CLIK.Extensions;
using HarmonyLib;
using Iterator;
using Verse;

namespace Compatibility
{
	[StaticConstructorOnStartup]
	internal static class EBFramework
	{
		private static readonly GetMaxHealth_t? s_getMaxHealth = null;

		static EBFramework()
		{
			if (ModsConfig.IsActive("V1024.EBFramework")) {
				var type = AppDomain
					.CurrentDomain
					.GetAssemblies()
					.Filter(x => x.GetName().Name == "EliteBionicsFramework")
					.Nth(0)?
					.GetType("EBF.VanillaExtender");
				var method = AccessTools.Method(
						type,
						"GetMaxHealth",
						new Type[] { typeof(BodyPartDef), typeof(Pawn), typeof(BodyPartRecord) });
				s_getMaxHealth = (GetMaxHealth_t?)method?.CreateDelegate(typeof(GetMaxHealth_t));
			}
		}

		private delegate float GetMaxHealth_t(BodyPartDef def, Pawn pawn, BodyPartRecord record);

		public static double GetMaxHealth(BodyPartRecord record, Pawn pawn) =>
			s_getMaxHealth?.Invoke(record.def, pawn, record) ?? record.def.GetMaxHealth(pawn);
	}
}
