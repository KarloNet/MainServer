using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI
{
    interface Ism
    {
        void Wonder();
        void Stand();
        void Attack();
        void Run();
        void Update(Ism entityIsm);
        EntityState.STATE GetCurrentState();
        void UpdateFrame(EntityState.State state);
    }
}
