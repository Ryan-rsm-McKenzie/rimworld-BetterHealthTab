#nullable enable

using CLIK.Components;
using Verse;

namespace BetterHealthTab.HealthTab
{
	internal interface IWindow : IUIComponent
	{
		public abstract (Thing? Thing, Pawn? Pawn) SelectedThings { set; }

		public abstract void OnOpen();
	}
}
