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
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Common.AOP
{
    [Aspect(Scope.Global)]
    [Injection(typeof(TeaceMethodAspectAttribute))]
    public class TeaceMethodAspectAttribute : Attribute
    {
        NLog.Logger logger = LogManager.GetCurrentClassLogger();
        StopWatchPool stopWatchPool = new StopWatchPool();
        const string CALL_CONTEXT_KEY_STOPWATCH = "CALL_CONTEXT_KEY_STOPWATCH";
        [Advice(Kind.Before, Targets = Target.Method)]
        public void Before([Argument(Source.Name)] string name, [Argument(Source.Arguments)] object[] arguments)
        {
            //logger.Debug($"Before,:{}");
            var sw = stopWatchPool.GetObject();
            sw.Restart();
            CallContext.SetData(CALL_CONTEXT_KEY_STOPWATCH, sw);
        }

        [Advice(Kind.After, Targets = Target.Method)]
        public void After([Argument(Source.Name)] string name, [Argument(Source.Arguments)] object[] arguments, [Argument(Source.ReturnValue)] object returnValue)
        {
            var sw = System.Runtime.Remoting.Messaging.CallContext.GetData(CALL_CONTEXT_KEY_STOPWATCH) as Stopwatch;
            if (sw != null)
            {
                sw.Stop();
                logger.Debug();
                stopWatchPool.PutObject(sw);
            }
        }

        private class StopWatchPool
        {
            private ConcurrentBag<Stopwatch> swObjects;
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
