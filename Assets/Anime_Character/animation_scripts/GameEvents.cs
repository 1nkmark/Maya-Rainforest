using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class GameEvents
{
    // 胖达开始带路
    public static event Action OnPandaCruiseStarted;

    public static void TriggerPandaCruiseStarted()
    {
        OnPandaCruiseStarted?.Invoke();
    }
}