using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI
{
    abstract class AIEvent
    {
        protected long _lastcalled;
        protected long _delay;

        public long LastCalled { get { return this._lastcalled; } }
        public long Delay { get { return this._delay; } }

        public abstract void Run();
    }
}
