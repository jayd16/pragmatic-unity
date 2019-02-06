public partial class ExampleViewModel {
    
    private DataBinding<string> _BoundStringBinding;
    
    partial void _Awake();
    partial void _OnDestroy();
    private void Awake()
    {
        _Awake();
        ExposeBindings();
    }
    private void OnDestroy()
    {
        _OnDestroy();
        DestroyBindings();
    }
    
    private void ExposeBindings() {
        _BoundStringBinding = new DataBinding<string>("BoundString", SetString, GetString, BoundStringAddListeners, BoundStringRemoveListeners);
        _BoundStringBinding.Register();
    }
    
    private void DestroyBindings() {
        _BoundStringBinding.Dispose();
    }
    
    private void BoundStringAddListeners(System.Action l) {
        OnStringUpdated += l;
    }
    
    private void BoundStringRemoveListeners(System.Action l) {
        OnStringUpdated -= l;
    }
}
