using System;
using System.Collections.Generic;
using Com.Duffy.Logging;

namespace Com.Duffy.DynamicReferences
{
    public static class DynamicRefService
    {
   
        private static readonly Dictionary<string, IDynamicRefTarget> _refsById = new Dictionary<string, IDynamicRefTarget>();

        private static readonly Dictionary<Type, List<IDynamicRefTarget>> _refsByType = new Dictionary<Type, List<IDynamicRefTarget>>();

        public static bool GetById<T>(string id, out T referencedItem)
        {
            IDynamicRefTarget dynamicRefTarget;
            if (_refsById.TryGetValue(id, out dynamicRefTarget))
            {
                Log.Assert(dynamicRefTarget.Target is T, "Could not cast reference");
                referencedItem = (T)dynamicRefTarget.Target;
                return true;
            }
        
            referencedItem = default(T);
            return false;
        }

        public static bool GetByType<T>(out T referencedItem)
        {
            List<IDynamicRefTarget> dynamicReferences;
            if (_refsByType.TryGetValue(typeof(T), out dynamicReferences))
            {
                Log.Assert(dynamicReferences != null && dynamicReferences.Count <= 1, "Could not find DynamicRef {0}", typeof(T));

                if (dynamicReferences == null || dynamicReferences.Count < 1)
                {
                    referencedItem =  default(T);
                    return false;
                }
                Log.Assert(dynamicReferences[0].Target is T, "Could not cast reference");
                referencedItem = (T)dynamicReferences[0].Target;
                return true;
            }

            referencedItem =  default(T);
            return false;
        }

        public static void Register(IDynamicRefTarget dynamicRefTarget)
        {
            //add by id
            if (!string.IsNullOrEmpty(dynamicRefTarget.Id))
            {
                _refsById[dynamicRefTarget.Id] = dynamicRefTarget;
            }

            //add by type
            Type refType = dynamicRefTarget.Target.GetType();
            List<IDynamicRefTarget> dynamicReferences;
            if (!_refsByType.TryGetValue(refType, out dynamicReferences))
            {
                dynamicReferences = new List<IDynamicRefTarget>();
                _refsByType.Add(refType, dynamicReferences);
            }

            dynamicReferences.Add(dynamicRefTarget);
        }

        public static void Unregister(IDynamicRefTarget dynamicRefTarget)
        {
            //remove by id
            if (!string.IsNullOrEmpty(dynamicRefTarget.Id))
            {
                _refsById.Remove(dynamicRefTarget.Id);
            }

            //remove by type
            Type refType = dynamicRefTarget.Target.GetType();
            List<IDynamicRefTarget> dynamicReferences;
            if (_refsByType.TryGetValue(refType, out dynamicReferences))
            {
                dynamicReferences.Remove(dynamicRefTarget);
            }
        }
    }
}