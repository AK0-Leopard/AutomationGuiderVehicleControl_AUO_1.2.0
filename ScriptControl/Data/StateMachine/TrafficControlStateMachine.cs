using Stateless.Graph;
using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace com.mirle.ibg3k0.sc.Data.StateMachine
{
    public class TrafficControlStateMachine
    {
        public enum State
        {
            NotEntry,
            AGVRequest,
            WaitReply,
            AllowedEntry
        }
        public enum Trigger
        {
            AGVRepuestEntry,
            AGVCRequestForRightOfWay,
            AcquireRightOfWay,
            ReturnRightOfWay,
            CancelRequestForRightOfWay
        }


        bool isNeedAutoTesting = true;
        StateMachine<State, Trigger> _machine;
        public State TrafficControlState { get; private set; } = State.NotEntry;
        Stopwatch Stopwatch { get; set; }


        public TrafficControlStateMachine()
        {
            _machine = new StateMachine<State, Trigger>(() => TrafficControlState, s => TrafficControlState = s);
            Stopwatch = new Stopwatch();
            TrafficControlStateMachineConfigInitial();
        }

        private void TrafficControlStateMachineConfigInitial()
        {
            _machine.Configure(State.NotEntry)
                .Permit(Trigger.AGVRepuestEntry, State.AGVRequest);

            _machine.Configure(State.AGVRequest)
                .Permit(Trigger.AGVCRequestForRightOfWay, State.WaitReply)
                .Permit(Trigger.CancelRequestForRightOfWay, State.NotEntry);

            _machine.Configure(State.WaitReply)
                .Permit(Trigger.AcquireRightOfWay, State.AllowedEntry)
                .Permit(Trigger.CancelRequestForRightOfWay, State.NotEntry);

            _machine.Configure(State.AllowedEntry)
                .Permit(Trigger.ReturnRightOfWay, State.NotEntry)
                .Permit(Trigger.CancelRequestForRightOfWay, State.NotEntry);

            _machine.OnTransitioned(t => Console.WriteLine($"OnTransitioned: {t.Source} -> {t.Destination} via {t.Trigger}"));
            _machine.OnUnhandledTrigger((s, t) => Console.WriteLine($"unhandled trigger happend ,source state:{s} trigger:{t}"));
        }
        private void Print()
        {
            Console.WriteLine($"[vehicle state:{_machine.State}]");
        }

        public void AGVRepuestEntry()
        {
            _machine.Fire(Trigger.AGVRepuestEntry);
        }

        public void AGVCRequestForRightOfWay()
        {
            _machine.Fire(Trigger.AGVCRequestForRightOfWay);
        }

        public void AcquireRightOfWay()
        {
            _machine.Fire(Trigger.AcquireRightOfWay);
        }
        public void ReturnRightOfWay()
        {
            _machine.Fire(Trigger.ReturnRightOfWay);
        }
        public void CancelRequestForRightOfWay()
        {
            _machine.Fire(Trigger.CancelRequestForRightOfWay);
        }
        public bool canFire(Trigger trigger)
        {
            return _machine.CanFire(trigger);
        }

        public bool IsNotEntry => _machine.IsInState(State.NotEntry);

        public bool IsAGVRequest => _machine.IsInState(State.AGVRequest);

        public bool IsWaitReply => _machine.IsInState(State.WaitReply);

        public bool IsAllowedEntry => _machine.IsInState(State.AllowedEntry);

        public string CurrentContrlState => _machine.State.ToString();



    }
}
