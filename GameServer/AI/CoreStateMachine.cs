using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI
{
    class CoreStateMachine
    {
        public enum State
        {
            STAND,
            WONDER,
            RUN,
            ATTACK
        }

        State initialState;
        State currentstate;

        private const int MIN_DELAY = 500;  /* Minimal delay for between events */

        protected long _lastcalled;
        protected long _delay;

        public long LastCalled { get { return this._lastcalled; } }
        public long Delay { get { return this._delay; } }
        public State SetState { set { currentstate = value; } }
        public State CurrentState { get { return currentstate; } }

        public CoreStateMachine()
        {
            initialState = currentstate = State.STAND;
            this._delay = Environment.TickCount;
        }

        public void Update(Ism entitySM)
        {
            this._delay = Environment.TickCount;
            if (Delay - LastCalled < MIN_DELAY)
            {
                return;// no time yet for change state
            }
            this._lastcalled = _delay;

            switch (currentstate)
            {
                case State.ATTACK:
                    entitySM.Attack();
                    break;
                case State.RUN:
                    entitySM.Run();
                    break;
                case State.STAND:
                    entitySM.Stand();
                    break;
                case State.WONDER:
                    entitySM.Wonder();
                    break;
                default:
                    break;
            }
        }
    }
}
