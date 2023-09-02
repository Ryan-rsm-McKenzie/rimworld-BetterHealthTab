#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Iterator;
using Verse;

namespace BetterHealthTab
{
	[StaticConstructorOnStartup]
	internal static class BodyMapper
	{
		private static readonly Dictionary<BodyDef, Dictionary<BodyPartRecord, PartInfo>> s_map;

		static BodyMapper()
		{
			s_map = DefDatabase<BodyDef>.AllDefs.ToDictionary(x => x, GraphBodyParts);
		}

		public static IReadOnlyDictionary<BodyPartRecord, PartInfo> BodyInfo(this BodyDef body)
		{
			return s_map[body];
		}

		private static Dictionary<BodyPartRecord, PartInfo> GraphBodyParts(BodyDef body)
		{
			var result = new Dictionary<BodyPartRecord, PartInfo>();
			body.AllParts.ForEach((record) => result[record] = new PartInfo());
			result[body.corePart].Depth = 0;

			var queue = new Queue<BodyPartRecord>();
			queue.Enqueue(body.corePart);

			while (queue.Count > 0) {
				var record = queue.Dequeue();

				var current = result[record];
				if (record.parent is not null) {
					current.Depth = Math.Max(current.Depth, result[record.parent].Depth + 1);
				} else {
					current.Depth = 0;
				}

				record.parts.ForEach(queue.Enqueue);
			}

			result
				.Filter(x => x.Value.Depth == -1)
				.ForEach(x => x.Value.Depth = 0);

			return result;
		}
	}

	internal sealed class PartInfo
	{
		public int Depth = -1;
	}
}
