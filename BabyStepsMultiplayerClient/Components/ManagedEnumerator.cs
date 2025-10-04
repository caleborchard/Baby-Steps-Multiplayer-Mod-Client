using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections;

namespace BabyStepsMultiplayerClient.Components
{
    public class ManagedEnumerator : Il2CppSystem.Object
    {
        private readonly IEnumerator enumerator;

        internal static bool Register()
            => Core.RegisterComponent<ManagedEnumerator>(typeof(Il2CppSystem.Collections.IEnumerator));

        public ManagedEnumerator(IntPtr ptr) : base(ptr) { }
        public ManagedEnumerator(IEnumerator _enumerator)
            : base(ClassInjector.DerivedConstructorPointer<ManagedEnumerator>())
        {
            ClassInjector.DerivedConstructorBody(this);
            enumerator = _enumerator ?? throw new NullReferenceException("routine is null");
        }

        public Il2CppSystem.Object Current
        {
            get => enumerator.Current switch
            {
                IEnumerator next => new ManagedEnumerator(next),
                Il2CppSystem.Object il2cppObject => il2cppObject,
                null => null,
                _ => throw new NotSupportedException($"{enumerator.GetType()}: Unsupported type {enumerator.Current.GetType()}"),
            };
        }

        public bool MoveNext()
        {
            try
            {
                return enumerator.MoveNext();
            }
            catch (Exception e)
            {
                Core.logger.Error("Unhandled exception in coroutine. It will not continue executing.", e);
                return false;
            }
        }

        public void Reset() => enumerator.Reset();
    }
}
