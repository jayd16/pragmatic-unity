public class BoundStringDataBinder : DataBinder<string> {
    
    [UnityEngine.SerializeField()]
    protected BoundStringUnityEvent _onGet = new BoundStringUnityEvent();
    
    protected override UnityEngine.Events.UnityEvent<string> OnGet {
        get {
            return _onGet;
        }
    }
    
    private void Reset() {
        _dataBindingId = "BoundString";
    }
    
    [System.SerializableAttribute()]
    public class BoundStringUnityEvent : UnityEngine.Events.UnityEvent<string> {
    }
}
