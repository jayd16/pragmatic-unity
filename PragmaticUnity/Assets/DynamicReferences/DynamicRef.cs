using UnityEngine;

namespace Com.Duffy.DynamicReferences
{
    public static class DynamicRef
    {
        public static DynamicRef<T> SearchById<T>(string id)
        {
            return new IdDynamicRef<T>(id);
        }

        public static DynamicRef<T> SearchByType<T>()
        {
            return new TypeDynamicRef<T>();
        }

        private class IdDynamicRef<T> : DynamicRef<T>
        {
            private readonly string _id;

            public IdDynamicRef(string id)
            {
                _id = id;
            }

            public override bool HasValue
            {
                get
                {
                    T reference;
                    return DynamicRefService.GetById(_id, out reference);
                }
            }

            public override T Value
            {
                get
                {
                    T reference;
                    DynamicRefService.GetById(_id, out reference);
                    return reference;
                }
            }
        }

        private class TypeDynamicRef<T> : DynamicRef<T>
        {
            public override bool HasValue
            {
                get
                {
                    T reference;
                    return DynamicRefService.GetByType(out reference);
                }
            }

            public override T Value
            {
                get
                {
                    T reference;
                    DynamicRefService.GetByType(out reference);
                    return reference;
                }
            }
        }
    }

    public abstract class DynamicRef<TValue> : CustomYieldInstruction
    {
        private readonly WaitUntil _wait;

        protected DynamicRef()
        {
            _wait = new WaitUntil(() => HasValue);
        }

        public virtual CustomYieldInstruction Wait => _wait;

        public override bool keepWaiting => !HasValue;
        
        public abstract bool HasValue { get; }

        public abstract TValue Value { get; }
    }
}