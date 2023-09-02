#pragma warning disable IDE1006 // Naming Styles
#nullable enable

using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterHealthTab
{
	[HarmonyPatch(typeof(DefGenerator))]
	[HarmonyPatch("GenerateImpliedDefs_PreResolve")]
	[HarmonyPatch(new Type[] { })]
	internal class DefGenerator_GenerateImpliedDefs_PreResolve
	{
		public static void Postfix()
		{
			foreach (var thing in DefDatabase<ThingDef>.AllDefs) {
				if (thing.category == ThingCategory.Pawn) {
					int pos = thing.inspectorTabs.FindIndex((tab) => tab == typeof(ITab_Pawn_Health));
					if (pos != -1) {
						thing.inspectorTabs[pos] = typeof(HealthTab.Tab);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker))]
	[HarmonyPatch("AddHediff")]
	[HarmonyPatch(new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
	internal class Pawn_HealthTracker_AddHediff
	{
		public static void Postfix(Pawn_HealthTracker __instance)
		{
			HealthTab.Tab.Instance!.InvalidateHediffs(__instance.pawn);
		}
	}

	[HarmonyPatch(typeof(Pawn_HealthTracker))]
	[HarmonyPatch("Notify_HediffChanged")]
	[HarmonyPatch(new Type[] { typeof(Hediff) })]
	internal class Pawn_HealthTracker_Notify_HediffChanged
	{
		public static void Postfix(Pawn_HealthTracker __instance)
		{
			HealthTab.Tab.Instance!.InvalidateHediffs(__instance.pawn);
		}
	}
}
