using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entity
{
    public class Resistances
    {
        private byte _Fresi = 10;
        private byte _Iresi = 10;
        private byte _Lresi = 10;
        private byte _Cresi = 25;
        private byte _NEresi = 29;

        public byte Fire { get { return this._Fresi; } set { this._Fresi = value; } }
        public byte Light { get { return this._Lresi; } set { this._Lresi = value; } }
        public byte Ice { get { return this._Iresi; } set { this._Iresi = value; } }
        public byte Curse { get { return this._Cresi; } set { this._Cresi = value; } }
        public byte NonElemental { get { return this._NEresi; } set { this._NEresi = value; } }
    }
}
