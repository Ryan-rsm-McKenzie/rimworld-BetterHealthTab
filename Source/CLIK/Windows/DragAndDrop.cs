#nullable enable

using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using UnityEngine;
using Verse;

namespace CLIK.Windows
{
	internal interface IDraggableTarget
	{
		public abstract void OnDrag(Point where);

		public abstract void OnDragBegin();

		public abstract void OnDragEnd();
	}

	[HotSwappable]
	internal sealed class DragAndDrop : Window
	{
		private readonly IDraggableTarget _target;

		private Point _where;

		private DragAndDrop(IDraggableTarget target, Point where)
		{
			this._target = target;
			this._where = where.ToScreenSpace();

			this.closeOnClickedOutside = false;
			this.doWindowBackground = false;
			this.drawShadow = false;
			this.layer = WindowLayer.Super;
			this.soundAppear = null;
			this.soundClose = null;
		}

		protected override float Margin => 0;

		public static void Start(IDraggableTarget target, Point where)
		{
			Utils.Assert(OriginalEventUtility.EventType == EventType.MouseDown,
				"Drag and drop only works if the mouse is currently dragging!");
			Find.WindowStack.Add(new DragAndDrop(target, where));
		}

		public static void Stop() =>
			Find.WindowStack.RemoveWindowsOfType<DragAndDrop>();

		public override void DoWindowContents(UnityEngine.Rect inRect)
		{
			switch (Event.current.type) {
				case EventType.MouseUp:
					if (Event.current.button == 0) {
						Event.current.Use();
						this.Close(false);
					}
					break;
				case EventType.MouseDrag:
					if (Event.current.button == 0) {
						Event.current.Use();
						this._where += (Point)Event.current.delta;
					}
					break;
				case EventType.Repaint:
					this._target.OnDrag(this._where);
					break;
			}
		}

		public override void PostOpen() => this._target.OnDragBegin();

		public override void PreClose() => this._target.OnDragEnd();

		protected override void SetInitialSizeAndPosition() =>
			this.windowRect = new UnityEngine.Rect(0, 0, UI.screenWidth, UI.screenHeight);
	}
}
