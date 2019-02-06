using System.Collections;
using System.Collections.Generic;
using Com.Duffy.DynamicReferences;
using UnityEngine;

public class ExampleUsage : MonoBehaviour
{
    private readonly DynamicRef<ExampleViewModel> _exampleViewModel = DynamicRef.SearchByType<ExampleViewModel>();

    IEnumerator Start()
    {
        yield return _exampleViewModel;

        var errorRequest = _exampleViewModel.Value.GetError();

        yield return errorRequest;

        Debug.Log($"was error: {errorRequest.IsFaulted}");
    }
}