using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GameServer.Database
{
    class Entity
    {
        public struct Attacker
        {
            public int attackerID;
            public int dmgDeal;
        }

        protected static int nextID = 101;
        protected int uniqueID;

        private byte entityType = 0;
        private Map.Nod myNode;
        private Map.Nod oldNode;
        private Map.Spawn mySpawn;
        private Map.Nod start;
        private Map.Spawn spawn;

        private bool isKilled;
        int Str;
        int Hp;
        int Int;
        int Wis;
        int Agi;
        int Px;
        int Py;
        int Pz;
        int Class;
        int SubClass;
        int Lvl;
        int curHealth;
        int maxHealth;
        int curMana;
        int maxMana;

        int resistLight;
        int resistFire;
        int resistIce;
        int resistDeath;
        int resistEarth;
        int resistPhysical;

        int weaponMinMagDmg;
        int weaponMaxMagDmg;
        int weaponMinPhyDmg;
        int weaponMaxPhyDmg;

        public int newX = 0;
        public int newY = 0;
        public int newZ = 0;

        int attackRange = 10;//default att range for all entitys
        bool isAttacked = false;
        object attLock = new object();
        List<Attacker> attackers = new List<Attacker>();

        public int AttackRange { get { return attackRange; } set { attackRange = value; } }
        public Map.Spawn Spawn { get { return this.mySpawn; } set { this.mySpawn = value; } }
        public bool IsKilled { get { return this.isKilled; } set { this.isKilled = value; } }
        public Map.Nod MapPosition { get { return this.myNode; } set { this.myNode = value; } }
        public Map.Nod OldMapPosition { get { return this.oldNode; } set { this.oldNode = value; } }
        public byte Type { get { return this.entityType; } set { this.entityType = value; } }
        public int InternalID { get { return this.uniqueID; } }
        public int Strength { get { return this.Str; } set { this.Str = value; } }
        public int Health { get { return this.Hp; } set { this.Hp = value; } }
        public int Intel { get { return this.Int; } set { this.Int = value; } }
        public int Wisdom { get { return this.Wis; } set { this.Wis = value; } }
        public int Agility { get { return this.Agi; } set { this.Agi = value; } }
        public int PosX { get { return this.Px; } set { this.Px = value; } }
        public int PosY { get { return this.Py; } set { this.Py = value; } }
        public int PosZ { get { return this.Pz; } set { this.Pz = value; } }
        public int Race { get { return this.Class; } set { this.Class = value; } }
        public int Job { get { return this.SubClass; } set { this.SubClass = value; } }
        public int Level { get { return this.Lvl; } set { this.Lvl = value; } }
        public int ActHealth { get { return this.curHealth; } set { this.curHealth = value; } }
        public int ActMana { get { return this.curMana; } set { this.curMana = value; } }
        public int MaxHealth { get { return this.maxHealth; } set { this.maxHealth = value; } }
        public int MaxMana { get { return this.maxMana; } set { this.maxMana = value; } }

        public int ResistLight { get { return resistLight; } set { resistLight = value; } }
        public int ResistFire { get { return resistFire; } set { resistFire = value; } }
        public int ResistIce { get { return resistIce; } set { resistIce = value; } }
        public int ResistDeath { get { return resistDeath; } set { resistDeath = value; } }
        public int ResistEarth { get { return resistEarth; } set { resistEarth = value; } }
        public int ResistPhysical { get { return resistPhysical; } set { resistPhysical = value; } }

        public int WeaponMinMagDmg { get { return weaponMinMagDmg; } set { weaponMinMagDmg = value; } }
        public int WeaponMaxMagDmg { get { return weaponMaxMagDmg; } set { weaponMaxMagDmg = value; } }
        public int WeaponMinPhyDmg { get { return weaponMinPhyDmg; } set { weaponMinPhyDmg = value; } }
        public int WeaponMaxPhyDmg { get { return weaponMaxPhyDmg; } set { weaponMaxPhyDmg = value; } }

        internal AI.EntityState.STATE currentState = AI.EntityState.STATE.STAND;
        internal AI.EntityState.State state = new AI.EntityState.Idle(null);

        public bool IsAttacked
        {
            get { return isAttacked; }
            set
            {
                isAttacked = value;
                if (!isAttacked)// if we set it to false then clear attackers list
                {
                    lock (attLock)
                    {
                        attackers.Clear();
                    }
                }
            }
        }

        public int AttackerID
        {
            get
            {
                Attacker att = new Attacker();
                att.attackerID = -1;
                int dmg = 0;
                lock (attLock)
                {
                    foreach (Attacker at in attackers)
                    {
                        if (at.dmgDeal > dmg)
                        {
                            att = at;
                            dmg = at.dmgDeal;
                        }
                    }
                }
                return att.attackerID;
            }
        }

        public void RemoveAttacker(int attackerID)
        {
            lock (attLock)
            {
                foreach (Attacker at in attackers)
                {
                    if (at.attackerID == attackerID)
                    {
                        attackers.Remove(at);
                        break;
                    }
                }
            }
        }

        public void AddAttacker(Attacker att)
        {
            lock (attLock)
            {
                attackers.Add(att);
            }
        }

        public Entity()
        {
            this.uniqueID = Interlocked.Increment(ref nextID);
        }

        public Entity(Map.Nod start, Map.Spawn pSpawn, byte type, int id)
        {
            this.mySpawn = pSpawn;
            this.myNode = start;
            if (myNode != null)
            {
                this.oldNode = new Map.Nod(myNode.X, myNode.Y);
            }
            else
            {
                this.oldNode = new Map.Nod(0,0);
            }
            this.entityType = type;
            this.uniqueID = id;
        }

        public Entity(Map.Nod start, Map.Spawn pSpawn, byte type)
        {
            this.mySpawn = pSpawn;
            this.myNode = start;
            if (myNode != null)
            {
                this.oldNode = new Map.Nod(myNode.X, myNode.Y);
            }
            else
            {
                this.oldNode = new Map.Nod(0,0);
            }
            this.entityType = type;
            this.uniqueID = Interlocked.Increment(ref nextID);
        }

        public Entity(Map.Nod start, Map.Spawn spawn)
        {
            this.start = start;
            this.spawn = spawn;
            this.uniqueID = Interlocked.Increment(ref nextID);
        }

        public void GetDemage(int demage)
        {
            curHealth = curHealth - demage;
            if(curHealth <= 0)
            {
                isKilled = true;
                Output.WriteLine("Entity::GetDemage Entity is killed");
            }
        }
    }
}
