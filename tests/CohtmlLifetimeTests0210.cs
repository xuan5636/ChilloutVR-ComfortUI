using System;
using System.Runtime.InteropServices;
using ComfortUI;

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static bool operator ==(Object a, Object b) { return (ReferenceEquals(a, null) || a.Destroyed) ? ReferenceEquals(b, null) || b.Destroyed : ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !(a == b); }
        public override bool Equals(object obj) { return ReferenceEquals(this, obj); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }
}
namespace cohtml.Net
{
    public class View
    {
        private HandleRef swigCPtr;
        public uint Id;
        public int NativeCalls, Scripts;
        public bool Alive = true, Ready = true, ThrowManaged;
        public View(uint id) { Id = id; swigCPtr = new HandleRef(this, new IntPtr(id + 1)); }
        private void Enter() { NativeCalls++; if (!Alive) throw new InvalidOperationException("Regression: entered native API through a stale view"); }
        public void ZeroPointer() { swigCPtr = new HandleRef(this, IntPtr.Zero); Alive = false; }
        public uint GetId() { Enter(); return Id; }
        public bool IsReadyForBindings() { Enter(); return Ready; }
        public void ExecuteScript(string script) { Enter(); if (ThrowManaged) throw new InvalidOperationException("Managed script failure"); Scripts++; }
    }
}
namespace cohtml { public class CohtmlView : UnityEngine.Object { public Net.View View; } }
namespace ABI_RC.Core.UI
{
    public class CohtmlControlledView : cohtml.CohtmlView
    {
        public bool FinishedLoading = true;
        public new CohtmlControlledViewWrapper View;
    }
    public class CohtmlControlledViewWrapper
    {
        private cohtml.Net.View _view;
        private bool _disposed { get; set; }
        public CohtmlControlledViewWrapper(cohtml.Net.View view) { _view = view; }
        public void Retire(bool clear) { _disposed = true; if (clear) _view = null; }
        public void ReplaceInner(cohtml.Net.View view) { _view = view; }
    }
}
class CohtmlLifetimeTests0210
{
    private static int passed;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    private static ABI_RC.Core.UI.CohtmlControlledView Owner(cohtml.Net.View native)
    {
        var owner = new ABI_RC.Core.UI.CohtmlControlledView { View = new ABI_RC.Core.UI.CohtmlControlledViewWrapper(native) };
        ((cohtml.CohtmlView)owner).View = native;
        return owner;
    }
    static int Main()
    {
        try
        {
            var native = new cohtml.Net.View(1); var owner = Owner(native); CohtmlViewTarget target;
            Check(CohtmlViewTarget.TryCreate(owner, out target) && target.Id == 1 && target.Execute("pendingChat") && native.Scripts == 1, "A loaded current page receives the translation notice");
            owner.Destroyed = true; native.Alive = false; var calls = native.NativeCalls;
            Check(!target.Execute("pendingChat") && !CohtmlViewTarget.TryCreate(owner, out _) && native.NativeCalls == calls, "Destroyed login owner is rejected before any native call (reported F8 crash path)");
            native = new cohtml.Net.View(2); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out target); owner.View.Retire(false); native.Alive = false; calls = native.NativeCalls;
            Check(!target.Execute("pendingChat") && native.NativeCalls == calls, "Disposed wrapper is rejected even if it retains the old native reference");
            native = new cohtml.Net.View(3); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out target); native.ZeroPointer(); calls = native.NativeCalls;
            Check(!target.Execute("restored") && !CohtmlViewTarget.TryCreate(owner, out _) && native.NativeCalls == calls, "Zero SWIG handle is rejected before ExecuteScript, GetId or IsReadyForBindings");
            native = new cohtml.Net.View(4); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out target); var replacement = new cohtml.Net.View(4); owner.View = new ABI_RC.Core.UI.CohtmlControlledViewWrapper(replacement); ((cohtml.CohtmlView)owner).View = replacement; native.Alive = false; calls = native.NativeCalls;
            Check(!target.Execute("late translation") && native.NativeCalls == calls && CohtmlViewTarget.TryCreate(owner, out var fresh) && fresh.Execute("new page"), "Reused native ID cannot redirect an old response into the replacement document");
            native = new cohtml.Net.View(5); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out target); owner.View.ReplaceInner(new cohtml.Net.View(55)); native.Alive = false; calls = native.NativeCalls;
            Check(!target.Execute("preferenceFailed") && native.NativeCalls == calls, "Wrapper inner-view replacement invalidates its old target");
            native = new cohtml.Net.View(6); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out target); ((cohtml.CohtmlView)owner).View = null; native.Alive = false; calls = native.NativeCalls;
            Check(!target.TryUse(view => view.ExecuteScript("unbind")) && native.NativeCalls == calls, "Released base view cannot be used for callbacks or unbinding");
            native = new cohtml.Net.View(7); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out target); owner.FinishedLoading = false; calls = native.NativeCalls;
            Check(!target.Execute("preferences") && !CohtmlViewTarget.TryCreate(owner, out _) && native.NativeCalls == calls, "Reloading pages receive neither script delivery nor native readiness queries");
            owner.FinishedLoading = true; Check(target.Execute("preferences"), "Same live view accepts delivery after loading completes");
            native.ThrowManaged = true; Check(!target.Execute("managed failure"), "Ordinary managed script failures are contained without retry loops");
            native = new cohtml.Net.View(8); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out var live); native = new cohtml.Net.View(9); owner = Owner(native); CohtmlViewTarget.TryCreate(owner, out var dead); owner.Destroyed = true; native.Alive = false; calls = native.NativeCalls;
            for (var i = 0; i < 1000; i++) { dead.Execute("pendingChat"); live.Execute("pendingChat"); }
            Check(native.NativeCalls == calls, "Repeated notices skip stale pages while current pages remain usable");
            native = new cohtml.Net.View(10) { Ready = false }; owner = Owner(native);
            Check(!CohtmlViewTarget.TryCreate(owner, out _) && native.Scripts == 0, "Unready pages are not bound or injected");
            Console.WriteLine("TOTAL " + passed + " checks; managed lifetime simulation, no actual Cohtml process or VR session"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
