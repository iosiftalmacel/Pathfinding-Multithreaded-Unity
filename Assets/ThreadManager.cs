using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadManager : MonoBehaviour {
    public static ThreadManager instance;

    private List<Action> mainActions;
    private List<Action> childActions;
    private List<Thread> childThreads;
    private List<ManualResetEvent> manualResetEvents;

    private const int maxThreadCount = 10;

    private void Awake()
    {
        instance = this;
        mainActions = new List<Action>();
        childActions = new List<Action>();
        childThreads = new List<Thread>();
        manualResetEvents = new List<ManualResetEvent>();

        for (int j = 0; j < maxThreadCount; j++)
        {
            manualResetEvents.Add(new ManualResetEvent(false));
        }

        for (int i = 0; i < maxThreadCount; i++)
        {
            childThreads.Add(new Thread(() => { ChildThreadLoop(childThreads.IndexOf(Thread.CurrentThread)); }));
            childThreads[i].Start();
        }
    }

    void ChildThreadLoop(int i)
    {
        while (true)
        {
            manualResetEvents[i].WaitOne();
            Debug.LogError(i + " isRunning");
            Action action = null;
            do
            {
                action = null;

                lock (childActions)
                {
                    if (childActions.Count > 0)
                    {
                        action = childActions[0];
                        childActions.RemoveAt(0);
                    }
                }

                if (action != null)
                {
                    action.Invoke();
                }

                action = null;
            } while (action != null);
            manualResetEvents[i].Reset();
        }
    }

    public void RunOnMainThread(Action action) {
        lock (mainActions)
        {
            mainActions.Add(action);
        }
    }

    public void RunOnChildThread (Action action) {
        lock (childActions)
        {
            childActions.Add(action);
        }
        //TriggerThreads();
    }

    void TriggerThreads()
    {
        for (int j = 0; j < childActions.Count; j++)
        {
            for (int i = 0; i < childThreads.Count; i++)
            {
                if (!manualResetEvents[i].WaitOne(0))
                {
                    manualResetEvents[i].Set();
                    break;
                }
            }
        }

    }

    private void Update()
    {
        lock (mainActions)
        {
            while (mainActions.Count > 0)
            {
                Action action = mainActions[0];
                mainActions.RemoveAt(0);
                action.Invoke();
            }
        }
        if(childActions.Count > 0)
        {
            TriggerThreads();
        }
    }
}
