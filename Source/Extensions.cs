#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CLIK.Extensions;
using Compatibility;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;

namespace Extensions
{
	[HotSwappable]
	internal static class Extensions
	{
		public static double BleedRateTotal(this IEnumerable<Hediff> self)
		{
			if (!self.Any()) {
				return 0;
			}

			var pawn = self.Nth(0)!.pawn;
			if (!pawn.RaceProps.IsFlesh || pawn.health.Dead || pawn.Deathresting) {
				return 0;
			}

			double factor = self
				.FilterMap(x => x.CurStage?.totalBleedFactor)
				.Reduce((l, r) => l * r) ?? 1;
			double rate = self
				.Map(x => x.BleedRate)
				.Reduce((l, r) => l + r) ?? 0;
			return rate * factor / pawn.HealthScale;
		}

		public static T CreateDelegate<T>(this MethodInfo self)
			where T : Delegate
		{
			return (T)self.CreateDelegate(typeof(T));
		}

		public static void Deconstruct<T1, T2>(this Pair<T1, T2> self, out T1 t1, out T2 t2)
		{
			t1 = self.first;
			t2 = self.second;
		}

		public static double GetMaxHealth(this BodyPartRecord self, Pawn pawn) => EBFramework.GetMaxHealth(self, pawn);

		public static double GetPartDamage(this IEnumerable<Hediff> self)
		{
			double damage = 0;

			foreach (var hediff in self) {
				if (hediff is Hediff_MissingPart) {
					return double.PositiveInfinity;
				} else if (hediff is Hediff_Injury injury) {
					damage += injury.Severity;
				}
			}

			return damage;
		}

		public static IEnumerable<Hediff_MissingPart> MissingPartsCommonAncestors(this IEnumerable<Hediff> self)
		{
			var replaced = self
				.FilterMap(x => {
					if (x.Part is not null && x is Hediff_AddedPart added) {
						return added;
					} else {
						return null;
					}
				})
				.ToDictionary(x => x.Part);
			var (good, bad) = self
				.FilterMap(x => x as Hediff_MissingPart)
				.Partition(x => x.Part is not null);
			var removed = good.ToDictionary(x => x.Part);
			return good
				.Filter(x => {
					var parent = x.Part.parent;
					if (replaced.ContainsKey(x.Part)) {
						return false;
					} else if (parent is not null && (removed.ContainsKey(parent) || replaced.ContainsKey(parent))) {
						return false;
					} else {
						return true;
					}
				})
				.Chain(bad);
		}

		public static bool OperableOn(this Pawn self)
		{
			return self.Faction == Faction.OfPlayer ||
				self.IsPrisonerOfColony ||
				(self.HostFaction == Faction.OfPlayer && !self.health.capacities.CapableOf(PawnCapacityDefOf.Moving)) ||
				self.OperableOnNonHumanlike();
		}

		public static bool PawnCanEverDo(this PawnCapacityDef self, Pawn pawn)
		{
			return PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, self);
		}

		public static Pawn? PawnForHealth(this Thing self)
		{
			if (self is Pawn pawn) {
				return pawn;
			} else if (self is Corpse corpse) {
				return corpse.InnerPawn;
			} else {
				return null;
			}
		}

		public static IEnumerable<string> Split<T>(this string self)
			where T : struct
		{
			if (!self.IsEmpty()) {
				int i = 0;
				var acc = new StringBuilder();
				while (true) {
					acc.Clear();
					while (i < self.Length && self[i].IsWhiteSpace()) { ++i; }
					while (i < self.Length && !self[i].IsWhiteSpace()) { acc.Append(self[i++]); }
					if (acc.Length > 0) {
						yield return acc.ToString();
					} else {
						break;
					}
				}
			}
		}

		public static int? TicksUntilDeathDueToBloodLoss(this IEnumerable<Hediff> self, double bleedRateTotal)
		{
			if (bleedRateTotal < 1E-4) {
				return null;
			}

			var bloodLoss = self.Filter(x => x.def == HediffDefOf.BloodLoss).Nth(0);
			double severity = bloodLoss?.Severity ?? 0;
			return (int)((1 - severity) / bleedRateTotal * GenDate.TicksPerDay);
		}

		public static string ToHexString(this Color self)
		{
			var builder = new StringBuilder(9);
			builder.Append('#');
			foreach (double x in new double[] { self.r, self.g, self.b, self.a }) {
				builder.Append($"{(int)CLIK.Math.Lerp(0, 255, x):X2}");
			}
			return builder.ToString();
		}

		public static string ToStringPercent(this double self) => ((float)self).ToStringPercent();

		private static bool OperableOnNonHumanlike(this Pawn self)
		{
			return (!self.RaceProps.IsFlesh || self.Faction is null || !self.Faction.HostileTo(Faction.OfPlayer)) &&
				!self.RaceProps.Humanlike &&
				self.Downed;
		}
	}
}
