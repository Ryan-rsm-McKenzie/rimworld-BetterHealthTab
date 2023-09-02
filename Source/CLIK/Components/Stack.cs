#nullable enable

using CLIK.Painting;
using HotSwap;

namespace CLIK.Components
{
	[HotSwappable]
	internal sealed class Stack : CoreList
	{
		public Stack()
			: base((Side)Flows.TopToBottom)
		{ }

		public enum Flows : byte
		{
			TopToBottom = Side.Top,

			BottomToTop = Side.Bottom,

			LeftToRight = Side.Left,

			RightToLeft = Side.Right,
		}

		public Flows Flow {
			get => (Flows)this.Side;
			set {
				var val = (Side)value;
				if (this.Side != val) {
					this.Side = val;
					this.InvalidateSize();
				}
			}
		}
	}
}
