using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entity
{
    class Player : Entity
    {
        Connection con;

        public Player(Connection connection, Map.Nod start) : base(start, null, 0, connection.client.PlayerID)
        {
            this.con = connection;
        }

        public Connection UserConnection
        {
            get { return con; }
        }
    }
}
