using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entity
{
    public class Stats
    {
        private byte _strength;
        private byte _health;
        private byte _intelligence;
        private byte _wisdom;
        private byte _agility;

        public byte Strength { get { return this._strength; } set { this._strength = value; } }
        public byte Health { get { return this._health; } set { this._health = value; } }
        public byte Intelligence { get { return this._intelligence; } set { this._intelligence = value; } }
        public byte Wisdom { get { return this._wisdom; } set { this._wisdom = value; } }
        public byte Agility { get { return this._agility; } set { this._agility = value; } }
    }
}
