 ```
                                         _   _                    _ _         
  _ __  _ __ __ _  __ _ _ __ ___   __ _| |_(_) ___   _   _ _ __ (_) |_ _   _ 
 | '_ \| '__/ _` |/ _` | '_ ` _ \ / _` | __| |/ __| | | | | '_ \| | __| | | |
 | |_) | | | (_| | (_| | | | | | | (_| | |_| | (__  | |_| | | | | | |_| |_| |
 | .__/|_|  \__,_|\__, |_| |_| |_|\__,_|\__|_|\___|  \__,_|_| |_|_|\__|\__, |
 |_|              |___/                                                |___/ 
```
This repository is a gathering of opinionated utilities meant to fill in the gaps left by the base Unity offering.

The main design goal is to lean into the official Unity workflow.  We attempt to work within the Unity lifecycle instead of abandoning it and substituting our own.


# Class Breakdown

## AsyncRequest
Async Request is meant to add proper error and type checked results to asynchronous requests.
This is meant as a compromise between Unity's coroutine system and async/await 

Unlike base IEnumerators and Coroutines, AsyncRequests are designed to be awaited on by two or more coroutines without issue.  This allows a reduction in wasted work as well as a natural result caching system.

Here we can see the base example of how to build asynchronous calls with properly typed results
```
public AsyncRequest<int> ExampleGetInt()
{
    return EnumeratingAsyncRequest<int>.Start(this, GetInt);
}

//example yielding coroutine
private IEnumerator GetInt(Action<int> result)
{
    yield return new WaitForSeconds(1);
    result(123);
}
```

Wrapping web and disk IO in a similar fashion is just as simple.

## Dynamic References

DynamicRefService et el is a dependency injection alternative designed around Unity's MonoBehavior actor model.  It supports asynchronous dependency and service discovery through a coroutine yielding. 

DynamicRefs can be searched by class, base class, or a string Id.  DynamicRef types act as promises that will be fulfilled by as they come online.  Dependecies and services are registered and unregistered through the static DynamicRefService class.  

DynamicRefTarget is a helper MonoBehaviour that will register and unregister a target component along with the Unity Start and Destroy lifecycle.


```private readonly DynamicRef<SomeType> _myReference = DynamicRef.SearchByType<SomeType>();```

These DynamicRefs are YieldInstructions that will wait until the dependency is fulfilled.

```
IEnumerator Start()
{
    //this will yield until _myReference has a value to return
    yield return _myReference;

    Log.Assert(_myReference.HasValue, "this will be ture");
    
    //The SomeType instance can be used now
    _myReference.Value.SomeTypeMethod();
}
```

## Data Binding (Very Experimental)

Code gen to facilitate auto generated data binding behaviors.  This allows for MVVM style reactive data binding.

A view model behavior can mark up setter and getter methods, and invalidation events to have binding behaviors generated.  These binding behaviors have type properly types UnityEvents that can be used to build type safe but loosely coupled interactions between the view and the view model.

Example DataBinding Markup
```
[DataBinding("SomeBindingId")] public event Action OnStringUpdated = () => { };

private AsyncRequest<string> _cachedBoundStringExample = new SimpleAsyncRequest<string>("default");

[DataBinding("SomeBindingId")]
public AsyncRequest SetString(string val)
{
    _cachedBoundStringExample = new SimpleAsyncRequest<string>(val);
    OnStringUpdated();
    return _cachedBoundStringExample;
}

[DataBinding("SomeBindingId")]
public AsyncRequest<string> GetString()
{
    return _cachedBoundStringExample;
}
```

Here we can see two InputTexts bound to the same data through the generated DataBinding code.  
Notice that:

 - Each InputText has the <BindingId>DataBinder Behavior (In this case BoundStringDataBinder).
 - The InputText onEdit event is wired to the DataBinder.Set() method
 - The DataBinder.OnGet event is wired to the InputText.text field
 - The InputTexts do **NOT** reference each other

 ![Bound Input Text Exmaple][bound_input_text_gif]




[bound_input_text_gif]:https://github.com/jayd16/pragmatic-unity/blob/master/ReadMeContent/boundInputTexts.gif "Bound Input Text Exmaple"
