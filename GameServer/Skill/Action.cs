using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Skill
{
    class Action
    {
        Skill skill;
        ActiveSkill actSkill;
        Database.Entity attacker;
        int targetID;
        Map.Nod position;

        //skill
        public Action(ActiveSkill actSkill, Skill skill, Database.Entity attacker, int target)
        {
            this.skill = skill;
            this.actSkill = actSkill;
            this.attacker = attacker;
            this.targetID = target;
            this.position = new Map.Nod();
        }
        //skill but on place
        public Action(ActiveSkill actSkill, Skill skill, Database.Entity attacker, Map.Nod pos)
        {
            this.skill = skill;
            this.actSkill = actSkill;
            this.attacker = attacker;
            this.position = pos;
        }
        //physicall attack
        public Action(Database.Entity attacker, int target)
        {
            this.attacker = attacker;
            this.targetID = target;
            this.position = new Map.Nod();
        }

        public void DoAction()
        {
            BroadcastPacket bPacket = null;
            //skill
            if (actSkill != null)
            {
                if(skill == null)
                {
                    Output.WriteLine("Action::DoAction skill can't be null");
                    return;
                }
                Output.WriteLine("Action::DoAction Skill attack");
                //check if target is correct one and is in skill range ect.
                //distance..
                switch (skill.demageFlag)
                {
                    //skills that need to give target entity
                    case SKILL_DEMAGE_FLAG.SINGLE_TARGET_FLAG:
                    case SKILL_DEMAGE_FLAG.MULTI_TARGET_FLAG:
                        {
                            int range = GameServer.world.GetDistance(attacker.PosX, attacker.PosY, targetID);
                            Output.WriteLine("Action::DoAction Range from attacker to target = " + range.ToString());
                            if(range > skill.range)
                            {
                                Output.WriteLine("Action::DoAction Target outside skill range! (" + range.ToString() + "->" + skill.range.ToString() + ")");
                                break;
                            }
                            Database.Entity entity = GameServer.world.GetEntity(targetID);
                            if (entity == null) break;
                            Database.Entity.Attacker att = new Database.Entity.Attacker();
                            att.dmgDeal = CalculateMagicDmg(attacker, entity);
                            att.attackerID = attacker.InternalID;
                            entity.IsAttacked = true;
                            entity.AddAttacker(att);
                            position.X = (uint)entity.PosX;
                            position.Y = (uint)entity.PosY;
                            bPacket = new BroadcastPacket((uint)attacker.PosX, (uint)attacker.PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.AttackMag(attacker.InternalID, targetID, actSkill.skillID, actSkill.skillLvl));
                            entity.GetDemage(att.dmgDeal);
                            Output.WriteLine("Action::DoAction Attacker demage: " + att.dmgDeal.ToString());
                        }
                        break;
                    //skills that are place related not target entity
                    case SKILL_DEMAGE_FLAG.POSITION_TARGET_FLAG:
                    case SKILL_DEMAGE_FLAG.CON_45_FLAG:
                    case SKILL_DEMAGE_FLAG.CON_90_FLAG:
                    case SKILL_DEMAGE_FLAG.AOE_TARGET_FLAG:
                        {
                            int range = GameServer.world.GetDistance(attacker.PosX, attacker.PosY, (int)position.X, (int)position.Y);
                            Output.WriteLine("Action::DoAction Range from attacker to target = " + range.ToString());
                            if (range > skill.range)
                            {
                                Output.WriteLine("Action::DoAction Target outside skill range! (" + range.ToString() + "->" + skill.range.ToString() + ")");
                                break;
                            }
                            //GameServer.world.get
                            bPacket = new BroadcastPacket((uint)attacker.PosX, (uint)attacker.PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.AttackMag(attacker.InternalID, targetID, actSkill.skillID, actSkill.skillLvl));
                        }
                        break;
                    default:
                        Output.WriteLine("Skill has inncorrect DEMAGE FLAG SET");
                        break;
                }
                if(bPacket != null) GameServer.world.BroadcastPacket((int)bPacket.X, (int)bPacket.Y, (uint)bPacket.Range, bPacket.Packet);
                //GameServer.world.broadcastQueue.Enqueue(bPacket);
            }
            else//physical attack
            {
                Output.WriteLine("Action::DoAction Normall attack");
            }
        }

        int CalculateMagicDmg(Database.Entity attacker, Database.Entity target)
        {
            int dmg = 0;
            int fromInt = attacker.Intel / 2;
            int fromWiz = attacker.Wisdom / 8;
            int minDmg = skill.minDmg + (((fromInt + fromWiz) * actSkill.skillLvl) * 5);// skill.minDmg + (((attacker.Intel / 2 + attacker.Wisdom / 8)* actSkill.skillLvl) * 5);
            int maxDmg = skill.maxDmg + (((fromInt + fromWiz) * actSkill.skillLvl) * 5);// skill.maxDmg + (((attacker.Intel / 2 + attacker.Wisdom / 8) * actSkill.skillLvl) * 5);
            Output.WriteLine(ConsoleColor.Yellow, "Action::CalculateMagicDmg Att int: " + attacker.Intel.ToString() + " Att wiz: " + attacker.Wisdom.ToString() + " Skill lvl: " + actSkill.skillLvl.ToString());
            Output.WriteLine(ConsoleColor.Yellow, "Action::CalculateMagicDmg From int: " + fromInt.ToString() + " From wiz: " + fromWiz.ToString() + " min/max : " + skill.minDmg.ToString() + "/" + skill.maxDmg.ToString());

            switch (skill.magicType)
            {
                case SKILL_MAGIC_TYPE_FLAG.LIGHTENING_FLAG:
                    int fromSkill = Program.random.Next(minDmg, maxDmg);
                    int fromWeapo = Program.random.Next(attacker.WeaponMinMagDmg, attacker.WeaponMaxMagDmg);
                    int resist = target.ResistLight;
                    Output.WriteLine("Action::CalculateMagicDmg From skill: " + fromSkill.ToString() + " From weapon: " + fromWeapo.ToString() + " Resist: " + resist.ToString());
                    dmg = fromSkill + fromWeapo - resist;
                    //dmg = Program.random.Next(minDmg, maxDmg) + Program.random.Next(attacker.WeaponMinMagDmg, attacker.WeaponMaxMagDmg) - target.ResistLight;
                    if (dmg < 0) dmg = 0;
                    break;
                case SKILL_MAGIC_TYPE_FLAG.ICE_FLAG:
                    dmg = Program.random.Next(minDmg, maxDmg) + Program.random.Next(attacker.WeaponMinMagDmg, attacker.WeaponMaxMagDmg) - target.ResistIce;
                    if (dmg < 0) dmg = 0;
                    break;
                case SKILL_MAGIC_TYPE_FLAG.FIRE_FLAG:
                    dmg = Program.random.Next(minDmg, maxDmg) + Program.random.Next(attacker.WeaponMinMagDmg, attacker.WeaponMaxMagDmg) - target.ResistFire;
                    if (dmg < 0) dmg = 0;
                    break;
                case SKILL_MAGIC_TYPE_FLAG.EARTH_FLAG:
                    dmg = Program.random.Next(minDmg, maxDmg) + Program.random.Next(attacker.WeaponMinMagDmg, attacker.WeaponMaxMagDmg) - target.ResistEarth;
                    if (dmg < 0) dmg = 0;
                    break;
                case SKILL_MAGIC_TYPE_FLAG.DEATH_FLAG:
                    dmg = Program.random.Next(minDmg, maxDmg) + Program.random.Next(attacker.WeaponMinMagDmg, attacker.WeaponMaxMagDmg) - target.ResistDeath;
                    if (dmg < 0) dmg = 0;
                    break;
                case SKILL_MAGIC_TYPE_FLAG.NULL_FLAG:
                    break;
                default:
                    break;
            }
            return dmg;
        }

    }
}
