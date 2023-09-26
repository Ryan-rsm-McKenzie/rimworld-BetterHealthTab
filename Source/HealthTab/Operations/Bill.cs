#nullable enable

using System;
using System.Linq;
using CLIK.Components;
using CLIK.Extensions;
using CLIK.Painting;
using HotSwap;
using Iterator;
using RimWorld;
using UnityEngine;
using Verse;
using Context = CLIK.Context;

namespace BetterHealthTab.HealthTab.Operations
{
	[HotSwappable]
	internal sealed class Bill : UIComponent, IListItem
	{
		private readonly Background _bg;

		private readonly RimWorld.Bill _bill;

		private readonly ButtonTexture _delete;

		private readonly ButtonTexture _info;

		private readonly Label _label;

		private readonly ButtonTexture _moveDown;

		private readonly ButtonTexture _moveUp;

		private readonly ButtonTexture _suspend;

		private readonly Suspended _suspended;

		private Color _color;

		private int _index = -1;

		public Bill(RimWorld.Bill bill)
		{
			this.HasPalette = true;
			this.InvalidateCache();

			this._bill = bill;
			this._color = bill.BaseColor;
			this._moveUp = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
					Tooltip = new("ReorderBillUpTip".Translate()),
				},
				OnPress = () => {
					var list = this.MyList;
					if (Event.current.shift) {
						this.Index = int.MinValue;
					} else {
						var other = list.Data[this.Index - 1];
						(this.Index, other.Index) = (other.Index, this.Index);
					}
					list.InvalidateSize();
					this.ValidateBillStack();
				},
				Sound = SoundDefOf.Tick_High,
				Texture = TexButton.ReorderUp,
				Visible = false,
			};
			this._moveDown = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
					Tooltip = new("ReorderBillDownTip".Translate()),
				},
				OnPress = () => {
					var list = this.MyList;
					if (Event.current.shift) {
						this.Index = int.MaxValue;
					} else {
						var other = list.Data[this.Index + 1];
						(this.Index, other.Index) = (other.Index, this.Index);
					}
					list.InvalidateSize();
					this.ValidateBillStack();
				},
				Sound = SoundDefOf.Tick_Low,
				Texture = TexButton.ReorderDown,
				Visible = false,
			};
			this._label = new() {
				Parent = this,
				Text = bill.LabelCap,
			};
			this._delete = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
					Tooltip = new("DeleteBillTip".Translate()),
				},
				OnPress = () => this._bill.billStack.Delete(this._bill),
				Sound = SoundDefOf.Click,
				Texture = TexButton.DeleteX,
			};
			this._info = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
					Tooltip = new("DefInfoTip".Translate()),
				},
				OnPress = () => Find.WindowStack.Add(new Dialog_InfoCard(this._bill.recipe)),
				Texture = TexButton.Info,
			};
			this._suspend = new() {
				Parent = this,
				Mouseover = new() {
					Sounds = true,
					Tooltip = new("SuspendBillTip".Translate()),
				},
				OnPress = () =>
					this.QueueAction(() => {
						bool suspended = !this._bill.suspended;
						this._bill.suspended = suspended;
						this._suspended!.Visible = suspended;
						this._color = bill.BaseColor;
					}),
				Sound = SoundDefOf.Click,
				Texture = TexButton.Suspend,
			};
			this._suspended = new() {
				Parent = this,
				Visible = bill.suspended,
			};
			this._bg = new() { Parent = this };
		}

		public int Index {
			get => this._index;
			set {
				this._index = value;
				this.InvalidateCache();
			}
		}

		private ScrollingList MyList => (ScrollingList)this.Parent!;

		public override double HeightFor(double width) => RimWorld.Bill.InterfaceBaseHeight;

		public override Palette Palette() => new() { Color = this._color };

		public override double WidthFor(double height) => throw new NotImplementedException();

		protected override void RecacheNow()
		{
			var data = this.MyList.Data;
			this._moveUp.Visible = this != data.First();
			this._moveDown.Visible = this != data.Last();
		}

		protected override void RepaintNow(Painter painter)
		{
			if (this.Focused) {
				using var _ = new Context.Palette(painter, new() { Color = this._color });
				painter.DrawHighlight(this.Rect);
			}
		}

		protected override void ResizeNow()
		{
			const double Size = RimWorld.Bill.ButSize;
			var rect = this.Rect;
			this._suspended.Geometry = rect;
			this._bg.Geometry = rect;
			rect = rect.Contract(1);

			var left = rect.CutLeft(Size);
			this._moveUp.Geometry = left.CutTop(Size);
			this._moveDown.Geometry = left.CutBottom(Size);

			var righter = rect.CutRight(Size);
			this._delete.Geometry = righter.CutTop(Size);
			this._info.Geometry = righter.CutBottom(Size);

			var right = rect.CutRight(Size);
			this._suspend.Geometry = right.CutTop(Size);

			this._label.Geometry = rect;
		}

		private void Drag() => this.MyList.DragItem(this.Index, this.Rect.TopLeft, this.ValidateBillStack);

		private void ValidateBillStack()
		{
			var range = this
				.MyList!
				.Data
				.Cast<Bill>()
				.OrderBy(x => x.Index)
				.Map(x => x._bill);
			var bills = this._bill.billStack.bills;
			bills.Clear();
			bills.AddRange(range);
		}

		[HotSwappable]
		private sealed class Background : UIComponent
		{
			public Background() => this.ListensForInput = true;

			private Bill MyParent => (Bill)this.Parent!;

			public override double HeightFor(double width) => throw new NotImplementedException();

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void InputNow(Painter painter)
			{
				if (this.Focused && Event.current.IsMouseDown(0)) {
					Event.current.Use();
					this.MyParent.Drag();
				}
			}

			protected override void RepaintNow(Painter painter)
			{
				if (this.Focused) {
					painter.PlayMouseoverSounds(this.Rect);
				}
			}
		}

		[HotSwappable]
		private sealed class Suspended : UIComponent
		{
			private readonly string _label = "SuspendedCaps".Translate();

			public override double HeightFor(double width) => throw new NotImplementedException();

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RepaintNow(Painter painter)
			{
				using var _ = new Context.Palette(
					painter,
					new() {
						Anchor = TextAnchor.MiddleCenter,
						Font = GameFont.Medium,
					});
				painter.DrawTexture(this.Rect, TexUI.GrayTextBG);
				painter.Label(this.Rect, this._label);
			}
		}
	}
}
