#nullable enable

using System;
using UnityEngine;
using Verse;

namespace CLIK.Painting
{
	internal struct Palette
	{
		public TextAnchor? Anchor = null;

		public Color? Color = null;

		public IDisposable? Context = null;

		public GameFont? Font = null;

		public Palette() { }

		public CSS.TextStyle TextStyle {
			set {
				this.Anchor = value.Anchor;
				this.Color = value.Color;
				this.Font = value.Font;
			}
		}
	}
}
