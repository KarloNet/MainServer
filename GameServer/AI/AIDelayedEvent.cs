using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI
{
    class AIDelayedEvent : AIEvent
    {
        private Callback __callback;
        private Object _cbParam;

        public AIDelayedEvent(long ms, Callback callback, Object cbParam)
        {
            this.__callback = callback;
            this._cbParam = cbParam;
            this._lastcalled = Environment.TickCount;
            this._delay = ms;
        }

        public override void Run()
        {
            __callback(this._cbParam);
        }
    }
}
