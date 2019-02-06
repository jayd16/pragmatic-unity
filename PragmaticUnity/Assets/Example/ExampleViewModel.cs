using System;
using System.Collections;
using Com.Duffy;
using UnityEngine;

public class ExampleViewModel : MonoBehaviour
{
    #region Simple Async Getter/Setter Example

    public AsyncRequest<int> ExampleGetInt()
    {
        return EnumeratingAsyncRequest<int>.Start(this, GetInt);
    }

    public AsyncRequest ExampleSetInt(int i)
    {
        return EnumeratingAsyncRequest.Start(this, SetInt(i));
    }

    private IEnumerator GetInt(Action<int> result)
    {
        yield return new WaitForSeconds(1);
        result(123);
    }

    private IEnumerator SetInt(int i)
    {
        yield return new WaitForSeconds(1);
    }

    #endregion

    #region Cached Async Request

    private AsyncRequest<int> _cachedGetInt;

    public AsyncRequest<int> ExampleCachedGetInt()
    {
        //using simple async request here but this pattern works with any kind
        return _cachedGetInt ?? (_cachedGetInt = new SimpleAsyncRequest<int>(123));
    }

    #endregion


    #region Example Error Handling

    public AsyncRequest<string> GetError()
    {
        return EnumeratingAsyncRequest<string>.Start(this, GetError);
    }

    private IEnumerator GetError(Action<string> result)
    {
        yield return null;
        throw new Exception("example error");
    }
    

    #endregion
}