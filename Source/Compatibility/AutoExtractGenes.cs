#nullable enable

using System;
using System.Reflection;
using CLIK;
using CLIK.Components;
using CLIK.Painting;
using HarmonyLib;
using HotSwap;
using Iterator;
using Verse;

namespace Compatibility
{
	[StaticConstructorOnStartup]
	internal static class AutoExtractGenes
	{
		private static readonly MethodInfo? s_getAutoExtractGenesComponent = null;

		private static readonly FieldInfo? s_isEnabled = null;

		static AutoExtractGenes()
		{
			if (ModsConfig.IsActive("Nibato.AutoExtractGenes")) {
				var assembly = AppDomain
					.CurrentDomain
					.GetAssemblies()
					.Filter(x => x.GetName().Name == "AutoExtractGenes")
					.Nth(0);
				s_getAutoExtractGenesComponent = AccessTools.Method(
						assembly?.GetType("AutoExtractGenes.Utils"),
						"GetAutoExtractGenesComponent",
						new Type[] { typeof(Pawn) });
				s_isEnabled = assembly?
					.GetType("AutoExtractGenes.AutoExtractGenesComp")?
					.GetField("isEnabled");
			}
		}

		public static UIComponent? Create(Pawn pawn)
		{
			var comp = s_getAutoExtractGenesComponent?
				.Invoke(null, new object[] { pawn })
				as ThingComp;
			return comp is not null ? new Item(comp) : null;
		}

		[HotSwappable]
		private sealed class Item : UIComponent
		{
			private readonly CheckBox _checkbox;

			private readonly Label _label;

			private readonly TipSignal _tooltip = new("nibato.AutoExtractGenes.AutoExtractGenes.Tooltip".Translate());

			public Item(ThingComp comp)
			{
				this._label = new() {
					Parent = this,
					Text = "nibato.AutoExtractGenes.AutoExtractGenes".Translate(),
				};

				bool on = (bool)s_isEnabled!.GetValue(comp);
				this._checkbox = new(on) {
					Parent = this,
					Mouseover = new() {
						Sounds = true,
					},
					OnChanged = (value) => s_isEnabled!.SetValue(comp, value),
				};
			}

			public override double HeightFor(double width) => Text.LineHeight;

			public override double WidthFor(double height) => throw new NotImplementedException();

			protected override void RepaintNow(Painter painter)
			{
				if (this.Focused) {
					Utils.TooltipRegion(this.Rect, this._tooltip);
				}
			}

			protected override void ResizeNow()
			{
				var rect = this.Rect;
				this._checkbox.Geometry =
					rect.CutRight(this._checkbox.WidthFor(rect.Height));
				this._label.Geometry = rect;
			}
		}
	}
}
