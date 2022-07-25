using AspectInjector.Broker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Common.AOP
{
    [Aspect(Scope.Global)]
    [Injection(typeof(TeaceMethodAspectAttribute))]
    public class TeaceMethodAspectAttribute : Attribute
    {
        public TeaceMethodAspectAttribute()
        {

        }

        NLog.Logger logger = LogManager.GetLogger("AOP_MethodExecuteInfo");
        StopWatchPool stopWatchPool = new StopWatchPool();
        ConcurrentDictionary<string, Stopwatch> StopWatchDictionary { get; set; } = new ConcurrentDictionary<string, Stopwatch>();

        //const string CALL_CONTEXT_KEY_STOPWATCH = "CALL_CONTEXT_KEY_STOPWATCH";
        [Advice(Kind.Before, Targets = Target.Method)]
        public void Before([Argument(Source.Name)] string name, [Argument(Source.Metadata)] System.Reflection.MethodBase methodBase, [Argument(Source.Arguments)] object[] arguments)
        {
            LogDebug("On Before", name, methodBase);
            var sw = stopWatchPool.GetObject();
            sw.Restart();

            string thread_id = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
            string name_full_name = tryGetNameSpace(methodBase);
            string stop_watch_key = $"{thread_id}#{name_full_name}#{name}";
            StopWatchDictionary.TryAdd(stop_watch_key, sw);
            //CallContext.SetData(CALL_CONTEXT_KEY_STOPWATCH, sw);
        }

        private void LogDebug(string action, string name, MethodBase methodBase)
        {
            string name_full_name = tryGetNameSpace(methodBase);
            string thread_id = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
            logger.Debug($"{action},full name:[{name_full_name}], method name:[{name}] , thread id:[{thread_id}]");
        }
        private void LogWarn(string action, string name, MethodBase methodBase, long processTime)
        {
            string name_full_name = tryGetNameSpace(methodBase);
            string thread_id = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
            logger.Warn($"{action},full name:[{name_full_name}], method name:[{name}] , thread id:[{thread_id}] ,process time:[{processTime}]");
        }
        private string tryGetNameSpace(MethodBase methodBase)
        {
            if (methodBase == null)
                return "";
            return methodBase.DeclaringType.FullName;
        }
        const int MAX_ALLOW_PROCESS_TIME_ms = 1_000;
        [Advice(Kind.After, Targets = Target.Method)]
        public void After([Argument(Source.Name)] string name, [Argument(Source.Metadata)] System.Reflection.MethodBase methodBase, [Argument(Source.Arguments)] object[] arguments, [Argument(Source.ReturnValue)] object returnValue)
        {
            LogDebug("On After", name, methodBase);
            //var sw = System.Runtime.Remoting.Messaging.CallContext.GetData(CALL_CONTEXT_KEY_STOPWATCH) as Stopwatch;
            string thread_id = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
            string name_full_name = tryGetNameSpace(methodBase);
            string stop_watch_key = $"{thread_id}#{name_full_name}#{name}";
            bool is_exist = StopWatchDictionary.TryRemove(stop_watch_key, out Stopwatch sw);
            if (is_exist)
            {
                sw.Stop();

                if (sw.ElapsedMilliseconds > MAX_ALLOW_PROCESS_TIME_ms)
                {
                    LogWarn("On After", name, methodBase, sw.ElapsedMilliseconds);
                }
                sw.Reset();
                stopWatchPool.PutObject(sw);
            }
        }


        [Advice(Kind.Around, Targets = Target.Method)]
        public object Around(
            [Argument(Source.Name)] string name,
            [Argument(Source.Arguments)] object[] arguments,
            [Argument(Source.Target)] Func<object[], object> target)
        {

            var result = target(arguments);


            return result;
        }

        private class StopWatchPool
        {
            private ConcurrentBag<Stopwatch> swObjects = new ConcurrentBag<Stopwatch>();
            public Stopwatch GetObject()
            {

                Stopwatch item;
                if (swObjects.TryTake(out item)) return item;
                return new Stopwatch();
            }
            public void PutObject(Stopwatch item)
            {
                if (item == null)
                    return;

                if (!(item is Stopwatch))
                    return;
                {
                    Stopwatch sw = item;
                    if (sw.IsRunning) sw.Stop();
                    sw.Reset();
                }
            }
        }

    }
}
