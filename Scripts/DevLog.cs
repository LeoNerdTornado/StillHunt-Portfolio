using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.Diagnostics;
public static class DevLog
{
    private const bool ENABLED = false;
    public static void Log(string message)
    {
        if (!ENABLED) { return; }

        #if UNITY_EDITOR
        UnityEngine.Debug.Log(message);
        #endif

    }

}
