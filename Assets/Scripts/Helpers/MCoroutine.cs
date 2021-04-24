using UnityEngine;
using System.Collections;
using System;

public static class MCoroutineHelper {

    public static MCoroutine StartCoroutine (this MCoroutine _mCoroutine, MonoBehaviour _caller, 
                                             IEnumerator _coroutine, bool _doEndAction = false) {

        return MCoroutine.StartCoroutine(_caller,_mCoroutine,_coroutine,_doEndAction);

    }

}

public class MCoroutine {

    public bool running { get; protected set; }
    public bool paused;

    public Action onEndAction;
    public bool doEndAction;

    // ---

    protected IEnumerator wrapper;
    protected MonoBehaviour caller;

    // ---

    public static MCoroutine StartCoroutine (MonoBehaviour _caller, MCoroutine _mCoroutine, 
                                             IEnumerator _coroutine, bool _doEndAction = false) {

        if (_mCoroutine == null) _mCoroutine = new MCoroutine(_caller);
        _mCoroutine.Start(_coroutine,_doEndAction);
        return _mCoroutine;

    }

    public MCoroutine (MonoBehaviour _caller) {
        caller = _caller;
    }

    public void Start (IEnumerator _coroutine, bool _doEndAction = false) {

        Stop();

        // ---

        running = true;
        paused = false;
        doEndAction = _doEndAction;

        wrapper = WrapperCoroutine(_coroutine);
        caller.StartCoroutine(wrapper);

    }

    IEnumerator WrapperCoroutine (IEnumerator _coroutine) {

        if (_coroutine == null) yield break;

        // yield return null;

        while (running) {

            if (paused) {

                yield return null;
            
            } else {

                if (caller != null && _coroutine.MoveNext()) {
                    yield return _coroutine.Current;
                } else {
                    running = false;
                }

            }
        }

        // --- end

        if (doEndAction) onEndAction?.Invoke();

    }

    public void Stop () {

        if (!running) return;

        running = false;
        caller.StopCoroutine(wrapper);

    }

}