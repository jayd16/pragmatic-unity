using System;
using System.Collections;
using Com.Duffy;
using Com.Duffy.DynamicReferences;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

public abstract class DataBinder<T> : MonoBehaviour
{

    [SerializeField] protected string _dataBindingId;

    [SerializeField] protected UnityEvent _onInvalidated;

    //event for when data comes in
    protected abstract UnityEvent<T> OnGet { get; }

    [NonSerialized] private DynamicRef<DataBinding<T>> _dataBinding;

    protected IEnumerator Start()
    {
        _dataBinding = DynamicRef.SearchById<DataBinding<T>>(_dataBindingId);
        yield return _dataBinding.Wait;

        _dataBinding.Value.OnInvalidated += Invalidated;
        
        Invalidated();
    }

    public void Set(T val)
    {
        StartCoroutine(_Set(val));
    }

    private IEnumerator _Set(T val)
    {
        yield return _dataBinding.Wait;
        yield return _dataBinding.Value.Setter.Invoke(val);
    }


    private void Invalidated()
    {
        StartCoroutine(_Invalidated());
    }

    private IEnumerator _Invalidated()
    {
        _onInvalidated.Invoke();

        yield return _dataBinding.Wait;
        var asyncRequest = _dataBinding.Value.Getter();
        yield return asyncRequest;

        OnGet.Invoke(asyncRequest.Result);
    }


    private void OnDestroy()
    {
        if (_dataBinding.HasValue)
        {
            _dataBinding.Value.OnInvalidated -= Invalidated;
        }
    }
    
    #if UNITY_EDITOR
    protected static void AddEvent(UnityEvent<T> unityEvent, ref int index, UnityAction<T> unityAction)
    {
        if (index == -1)
        {
            UnityEventTools.AddPersistentListener(unityEvent, unityAction);
            index = unityEvent.GetPersistentEventCount() - 1;
        }
        else
        {
            UnityEventTools.RegisterPersistentListener(unityEvent, index, unityAction);
        }
    }
    #endif
}


public class DataBinding<T> : IDisposable, IDynamicRefTarget
{
    public event Action OnInvalidated = () => { };
    public readonly Func<T, AsyncRequest> Setter;
    public readonly Func<AsyncRequest<T>> Getter;
    private Action<Action> _detachInvalidationAction;


    public void Register()
    {
        DynamicRefService.Register(this);
    }
    
    public DataBinding(string id, Func<T, AsyncRequest> setter, Func<AsyncRequest<T>> getter, 
        Action<Action> attachInvalidationAction, Action<Action> detachInvalidationAction)
    {
        Id = id;
        Setter = setter;
        Getter = getter;

        attachInvalidationAction?.Invoke(Invalidated);
        _detachInvalidationAction = detachInvalidationAction;
    }
    

    public void Dispose()
    {
        DynamicRefService.Unregister(this);
        
        _detachInvalidationAction?.Invoke(Invalidated);
        _detachInvalidationAction = null;
    }

    private void Invalidated()
    {
        Debug.Log("invalidated");
        OnInvalidated();
        
    }
    
    public string Id { get; private set; }
    public object Target => this;
}