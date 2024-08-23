using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IQuickLoggable
{
    int GetScriptID();

    string GetScriptName();
}

public static class QuickLogger
{
    private static string WriteScriptOrigin(IQuickLoggable script)
    {
        return $"(Name: '{script.GetScriptName()}', ID: '{script.GetScriptID()}')";
    }


    public static void Log(IQuickLoggable script, string statement)
    {
        Debug.Log($"{WriteScriptOrigin(script)} \n {statement}");
    }

    public static void ConditionalLog(bool condition,IQuickLoggable script, string statement)
    {
        if (condition)
            Log(script, statement);
    }

    public static void Warn(IQuickLoggable script, string warning)
    {
        Debug.LogWarning($"{WriteScriptOrigin(script)} \n {warning}");
    }

    public static void Error(IQuickLoggable script, string error)
    {
        Debug.LogError($"{WriteScriptOrigin(script)} \n {error}");
    }


}


