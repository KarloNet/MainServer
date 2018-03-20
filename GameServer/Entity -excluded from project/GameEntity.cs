using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entity
{
    /* Represents a character. Can be a monster or a player    */
    abstract class GameEntity : Entity
    {

        protected int curHp;
        protected int maxHp;
        protected short curMana;
        protected short maxMana;

        protected byte charLevel;
        protected short charEvasion;
        protected short charDefense;
        protected byte charAbsorption;
        protected short onTarget;
        protected short minPhysicalDMG;
        protected short maxPhysicalDMG;
        protected short minMagicalDMG;
        protected short maxMagicalDMG;

        protected Stats charStats;
        protected Map.Nod charPosition;
        protected Resistances charResistances;

        

        public int CurrentHP { get { return this.curHp; } set { this.curHp = value; } }
        public int MaxHP { get { return this.maxHp; } }
        public short CurrentMana { get { return this.curMana; } set { this.curMana = value; } }
        public short MaxMana { get { return this.maxMana; } }

        public byte Level { get { return this.charLevel; } }
        public short Evasion { get { return this.charEvasion; } }
        public short Defense { get { return this.charDefense; } }
        public byte Absorption { get { return this.charAbsorption; } }
        public short OnTarget { get { return this.onTarget; } }

        public short MinPhysicalDMG { get { return this.minPhysicalDMG; } }
        public short MaxPhysicalDMG { get { return this.maxPhysicalDMG; } }
        public short MinMagicalDMG { get { return this.minMagicalDMG; } }
        public short MaxMagicalDMG { get { return this.maxMagicalDMG; } }
        public Map.Nod Position { get { return this.MapPosition; } set { this.MapPosition = value; } }
        public Stats Stats { get { return this.charStats; } }

        public GameEntity(Map.Nod start, Map.Spawn spawn)
            : base(start, spawn)
        {
            //this.uniqueID = World.GetID();
            this.charPosition = new Map.Nod();
            this.charStats = new Stats();
            this.charResistances = new Resistances();
        }

        public void SetStrength(byte newValue)
        {
            this.charStats.Strength = newValue;
            this.CalcPhysicalDMG();
        }

        public void SetHealth(byte newValue)
        {
            this.charStats.Health = newValue;
            this.maxHp = (short)(this.charStats.Health * 30);
        }

        public void SetIntelligence(byte newValue)
        {
            this.charStats.Intelligence = newValue;
            this.CalcMagicalDMG();
        }

        public void SetWisdom(byte newValue)
        {
            this.charStats.Wisdom = newValue;
            this.maxMana = (short)(this.charStats.Wisdom * 30);
            this.CalcMagicalDMG();
        }

        public void SetAgility(byte newValue)
        {
            this.charStats.Agility = newValue;
            this.CalcPhysicalDMG();
        }

        public void SetLevel(byte newValue)
        {
            this.charLevel = newValue;
        }

        private void CalcPhysicalDMG()
        {
            this.minPhysicalDMG = (short)((this.charStats.Agility / 4) + (this.charStats.Strength / 2) + 3);
            this.maxPhysicalDMG = (short)((this.charStats.Agility / 4) + (this.charStats.Strength / 2) + 15);
        }

        private void CalcMagicalDMG()
        {
            this.minMagicalDMG = (short)((this.charStats.Wisdom / 4) + (this.charStats.Intelligence / 2) + 3);
            this.maxMagicalDMG = (short)((this.charStats.Wisdom / 4) + (this.charStats.Intelligence / 2) + 15);
        }
    }
}
