using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Til.GodotCoroutine;

public partial class CoroutineNode : Node {
    private readonly IList<CoroutineContext> onProcessCoroutineContext = new List<CoroutineContext>();
    private readonly IList<CoroutineContext> onPhysicsProcessCoroutineContext = new List<CoroutineContext>();

    /// <summary>
    /// k 阻塞者 v 被阻塞者
    /// </summary>
    private readonly IList<KeyValuePair<CoroutineContext, CoroutineContext>> chokeUpCoroutineContext = new List<KeyValuePair<CoroutineContext, CoroutineContext>>();

    private readonly IList<CoroutineContext> addCoroutineContextCache = new List<CoroutineContext>();
    private readonly IList<CoroutineContext> stopCoroutineContextCache = new List<CoroutineContext>();
    private readonly IList<CoroutineContext> changePathCoroutineContextCache = new List<CoroutineContext>();
    private readonly IList<KeyValuePair<CoroutineContext, CoroutineContext>> chokeUpCoroutineContextCache = new List<KeyValuePair<CoroutineContext, CoroutineContext>>();

    public CoroutineContext startCoroutine(IEnumerator enumerator, CoroutinePath coroutinePath = CoroutinePath.onProcess) {
        CoroutineContext coroutineContext = new CoroutineContext(enumerator, coroutinePath);
        startCoroutine(coroutineContext);
        return coroutineContext;
    }

    // ReSharper disable once NotDisposedResource
    public CoroutineContext startCoroutine(IEnumerable enumerable, CoroutinePath coroutinePath = CoroutinePath.onProcess) => startCoroutine(enumerable.GetEnumerator(), coroutinePath);

    public void startCoroutine(CoroutineContext coroutineContext) => addCoroutineContextCache.Add(coroutineContext);

    protected void stopCoroutine(CoroutineContext coroutineContext) => stopCoroutineContextCache.Add(coroutineContext);

    protected void _stopCoroutineContext(CoroutineContext coroutineContext) {
        switch (coroutineContext.coroutinePath) {
            case CoroutinePath.onProcess:
                onProcessCoroutineContext.Remove(coroutineContext);
                break;
            case CoroutinePath.onPhysicsProcess:
                onPhysicsProcessCoroutineContext.Remove(coroutineContext);
                break;
        }

        for (var i = 0; i < chokeUpCoroutineContext.Count; i++) {
            if (chokeUpCoroutineContext[i].Value.Equals(coroutineContext)) {
                chokeUpCoroutineContext.RemoveAt(i);
                i--;
                continue;
            }
            if (chokeUpCoroutineContext[i].Key.Equals(coroutineContext)) {
                chokeUpCoroutineContext.RemoveAt(i);
                startCoroutine(chokeUpCoroutineContext[i].Value);
                i--;
                continue;
            }
        }
    }

    protected void _changePathCoroutine(CoroutineContext coroutineContext) {
        switch (coroutineContext.coroutinePath) {
            case CoroutinePath.onProcess:
                onPhysicsProcessCoroutineContext.Remove(coroutineContext);
                onProcessCoroutineContext.Add(coroutineContext);
                break;
            case CoroutinePath.onPhysicsProcess:
                onProcessCoroutineContext.Remove(coroutineContext);
                onPhysicsProcessCoroutineContext.Add(coroutineContext);
                break;
        }
    }

    protected void _chokeUpCoroutineContext(KeyValuePair<CoroutineContext, CoroutineContext> keyValuePair) {
        switch (keyValuePair.Value.coroutinePath) {
            case CoroutinePath.onProcess:
                onProcessCoroutineContext.Remove(keyValuePair.Value);
                break;
            case CoroutinePath.onPhysicsProcess:
                onPhysicsProcessCoroutineContext.Remove(keyValuePair.Value);
                break;
        }
        chokeUpCoroutineContext.Add(keyValuePair);
    }

    protected void _addCoroutineContext(CoroutineContext coroutineContext) {
        switch (coroutineContext.coroutinePath) {
            case CoroutinePath.onProcess:
                onProcessCoroutineContext.Add(coroutineContext);
                break;
            case CoroutinePath.onPhysicsProcess:
                onPhysicsProcessCoroutineContext.Add(coroutineContext);
                break;
        }
    }

    protected void completeParagraph(CoroutineContext coroutineContext, double time) {
        coroutineContext.delay -= time;
        if (coroutineContext.delay > 0) {
            return;
        }
        if (!coroutineContext.routine.MoveNext()) {
            stopCoroutine(coroutineContext);
            (coroutineContext.routine as IDisposable)?.Dispose();
        }

        object? routineCurrent = coroutineContext.routine.Current;

        switch (routineCurrent) {
            case float f:
                coroutineContext.delay = f;
                break;
            case CoroutinePath coroutinePath:
                coroutineContext.coroutinePath = coroutinePath;
                changePathCoroutineContextCache.Add(coroutineContext);
                completeParagraph(coroutineContext, 0);
                break;
            case CoroutineContext _coroutineContext:
                chokeUpCoroutineContextCache.Add(new KeyValuePair<CoroutineContext, CoroutineContext>(_coroutineContext, coroutineContext));
                break;
        }
    }

    protected void settleAnAccount() {
        if (stopCoroutineContextCache.Count > 0) {
            stopCoroutineContextCache.forEach(_stopCoroutineContext);
            stopCoroutineContextCache.Clear();
        }
        if (changePathCoroutineContextCache.Count > 0) {
            changePathCoroutineContextCache.forEach(_changePathCoroutine);
            changePathCoroutineContextCache.Clear();
        }
        if (chokeUpCoroutineContextCache.Count > 0) {
            chokeUpCoroutineContextCache.forEach(_chokeUpCoroutineContext);
            chokeUpCoroutineContextCache.Clear();
        }
        if (addCoroutineContextCache.Count > 0) {
            addCoroutineContextCache.forEach(_addCoroutineContext);
            addCoroutineContextCache.Clear();
        }
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
        settleAnAccount();
        onProcessCoroutineContext.forEach(v => completeParagraph(v, delta));
        settleAnAccount();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        settleAnAccount();
        onPhysicsProcessCoroutineContext.forEach(v => completeParagraph(v, delta));
        settleAnAccount();
    }
}

public class CoroutineContext {
    protected internal double delay;
    protected internal CoroutinePath coroutinePath;
    protected internal IEnumerator routine;
    protected internal bool stop;

    public CoroutineContext(IEnumerator routine, CoroutinePath coroutinePath) {
        this.routine = routine;
        this.coroutinePath = coroutinePath;
    }
}

public enum CoroutinePath {
    onProcess,
    onPhysicsProcess
}

public static class EnumerableExtendMethod {
    public static void forEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
        foreach (var x1 in enumerable) {
            action(x1);
        }
    }
}