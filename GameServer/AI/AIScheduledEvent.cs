using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI
{
    delegate void Callback(Object parameter);

    class AIScheduledEvent : AIEvent
    {
        private const int MIN_DELAY = 500;  /* Minimal delay for between events */
        private Callback __callback;

        /* Calls CallbackFunc every delay milliseconds
        */
        public AIScheduledEvent(Callback cbFunc, long delay)
        {
            __callback = cbFunc;
            if (delay < MIN_DELAY)
            {
                delay = MIN_DELAY;
            }
            this._delay = delay;
        }

        public override void Run()
        {
            this._lastcalled = Environment.TickCount;
            __callback(null);
        }
    }
}
