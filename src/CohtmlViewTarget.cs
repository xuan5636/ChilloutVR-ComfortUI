using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ABI_RC.Core.UI;

namespace ComfortUI
{
    // Main-thread access only. A retained SWIG View can outlive its Unity owner.
    // GetId/IsReadyForBindings/ExecuteScript are native calls, not validity tests.
    internal sealed class CohtmlViewTarget
    {
        private static readonly FieldInfo Inner = typeof(CohtmlControlledViewWrapper).GetField("_view", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo Disposed = typeof(CohtmlControlledViewWrapper).GetField("<_disposed>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Pointer = typeof(cohtml.Net.View).GetField("swigCPtr", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly CohtmlControlledView owner;
        private readonly CohtmlControlledViewWrapper wrapper;
        private readonly cohtml.Net.View view;
        internal uint Id { get; private set; }

        private CohtmlViewTarget(CohtmlControlledView owner, CohtmlControlledViewWrapper wrapper, cohtml.Net.View view)
        { this.owner = owner; this.wrapper = wrapper; this.view = view; }

        internal bool IsCurrent
        {
            get
            {
                // Check managed state before crossing into Cohtml. Fail closed if
                // a future game update changes any of these lifetime contracts.
                if (Inner == null || Disposed == null || Pointer == null || owner == null || !owner.FinishedLoading || wrapper == null || view == null) return false;
                try
                {
                    return ReferenceEquals(owner.View, wrapper) && !(bool)Disposed.GetValue(wrapper) &&
                        ReferenceEquals(((cohtml.CohtmlView)owner).View, view) && ReferenceEquals(Inner.GetValue(wrapper), view) &&
                        ((HandleRef)Pointer.GetValue(view)).Handle != IntPtr.Zero;
                }
                catch { return false; }
            }
        }

        internal static bool TryCreate(CohtmlControlledView owner, out CohtmlViewTarget target)
        {
            target = null;
            if (owner == null || !owner.FinishedLoading) return false;
            var current = new CohtmlViewTarget(owner, owner.View, ((cohtml.CohtmlView)owner).View);
            if (!current.IsCurrent) return false;
            try
            {
                if (!current.view.IsReadyForBindings()) return false;
                current.Id = current.view.GetId();
                target = current;
                return true;
            }
            catch { return false; }
        }

        internal bool TryUse(Action<cohtml.Net.View> action)
        {
            if (!IsCurrent) return false;
            try { action(view); return true; }
            catch { return false; }
        }

        internal bool Execute(string script) { return TryUse(current => current.ExecuteScript(script)); }
    }
}
