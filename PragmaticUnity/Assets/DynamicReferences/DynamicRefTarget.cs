using System.Collections;
using UnityEngine;

namespace Com.Duffy.DynamicReferences
{
    public class DynamicRefTarget : MonoBehaviour, IDynamicRefTarget
    {
        [SerializeField] private string _id;

        public string Id
        {
            get { return _id; }
        }

        public Component Ref;

        public object Target
        {
            get { return Ref; }
        }

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            yield return new WaitUntil(()=>Ref != null);
#endif
            DynamicRefService.Register(this);
        }

        private void OnDestroy()
        {
            DynamicRefService.Unregister(this);
        }
    }
}