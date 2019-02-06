using System;
using UnityEngine;

namespace Com.Duffy.Logging
{
    public interface ILogger
    {
        void Info(string s);
        void Warning(string s);
        void Error(string s);
        void LogException(Exception e);
    }

    /// <summary>
    /// Logger proxy so users can swap out logging solutions
    /// </summary>
    public static class Log
    {
        public static ILogger Logger = new SimpleUnityLogger();

        public static void Assert(bool assertion, string s, params object[] args)
        {
            if (!assertion) throw new Exception(string.Format(s, args));
        }

        public static void Info(string s)
        {
            Logger.Info(s);
        }

        public static void Warning(string s)
        {
            Logger.Warning(s);
        }

        public static void Error(string s)
        {
            Logger.Error(s);
        }

        public static void Exception(Exception e)
        {
            Logger.LogException(e);
        }
    }

    public class SimpleUnityLogger : ILogger
    {
        public void Info(string s)
        {
            Debug.Log(s);
        }

        public void Warning(string s)
        {
            Debug.LogWarning(s);
        }

        public void Error(string s)
        {
            Debug.LogError(s);
        }

        public void LogException(Exception e)
        {
            Debug.LogException(e);
        }
    }
}