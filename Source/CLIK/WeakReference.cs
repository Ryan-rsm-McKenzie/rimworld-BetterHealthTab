#nullable enable

using System;

namespace CLIK
{
	internal class WeakReference<T> : WeakReference
		where T : class
	{
		public WeakReference() :
			base(null)
		{ }

		public WeakReference(T target) :
			base(target)
		{ }

		public new T? Target {
			get => (T?)base.Target;
			set => base.Target = value;
		}
	}
}
