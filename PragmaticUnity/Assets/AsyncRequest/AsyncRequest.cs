using System;
using System.Collections;
using UnityEngine;

namespace Com.Duffy
{
    /// <summary>
    /// Async Request is meant to add proper error to asynchronous requests.
    /// This is meant as a compromise between Unity's coroutine system and async/await 
    /// </summary>
    public interface AsyncRequest : IEnumerator
    {
        bool IsFaulted { get; }
    }

    /// <summary>
    /// Adds a typed result to AsyncRequest
    /// </summary>
    public interface AsyncRequest<out T> : AsyncRequest
    {
        T Result { get; }
    }

    public class SimpleAsyncRequest<T> : AsyncRequest<T>
    {
        public SimpleAsyncRequest(T result)
        {
            Result = result;
        }

        public bool IsFaulted => false;

        public T Result { get; }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }

        public object Current => null;
    }

    public class EnumeratingAsyncRequest : AsyncRequest
    {
        private Exception _exception;
        private bool _isFaulted;
        private bool _isExecuting = true;

        public static EnumeratingAsyncRequest Start(MonoBehaviour owner, IEnumerator routine)
        {
            var request = new EnumeratingAsyncRequest();

            owner.StartCoroutine(request.InternalRoutine(owner, routine));

            return request;
        }

        protected EnumeratingAsyncRequest()
        {
        }

        protected IEnumerator InternalRoutine(MonoBehaviour owner, IEnumerator iter)
        {
            yield return InternalRoutineRecursion(owner, iter);
            _isExecuting = false;
        }

        private IEnumerator InternalRoutineRecursion(MonoBehaviour owner, IEnumerator iter)
        {
            while (true)
            {
                try
                {
                    if (!iter.MoveNext()) break;
                }
                catch (Exception caught)
                {
                    _exception = caught;
                }

                if (_exception != null)
                {
                    Debug.unityLogger.LogException(_exception);
                    break;
                }

                IEnumerator recursiveRoutine = iter.Current as IEnumerator;
                if (recursiveRoutine != null)
                {
                    //recurse so we can continue to capture exceptions
                    yield return owner.StartCoroutine(InternalRoutineRecursion(owner, recursiveRoutine));
                }
                else
                {
                    yield return iter.Current;
                }
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public object Current => null;

        public bool MoveNext()
        {
            return _isExecuting;
        }

        public bool IsFaulted => _exception != null;

        public void ThrowIfFaulted()
        {
            if (IsFaulted) throw new Exception("AsyncRequest is faulted.  Rethrowing.", _exception);
            if (_isExecuting) throw new InvalidOperationException("Result of async task read before task is complete");
        }
    }

    public class EnumeratingAsyncRequest<T> : EnumeratingAsyncRequest, AsyncRequest<T>
    {
        private T _result;

        public T Result
        {
            get
            {
                ThrowIfFaulted();
                return _result;
            }
        }

        public static EnumeratingAsyncRequest<T> Start(MonoBehaviour owner, Func<Action<T>, IEnumerator> routine)
        {
            var request = new EnumeratingAsyncRequest<T>();

            owner.StartCoroutine(request.InternalRoutine(owner, routine(request.SetResult)));

            return request;
        }

        private void SetResult(T res)
        {
            _result = res;
        }
    }
}