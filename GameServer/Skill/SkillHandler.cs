using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Skill
{
    internal delegate void OnSkillRequest(int skillID, Database.Player.RACE race, Database.Player pAttacker, int targetID = 0, Map.Nod pos = null);
    internal delegate void OnSkillExecute(int skillID, Database.Player.RACE race, Database.Player pAttacker, int targetID = 0, Map.Nod pos = null);

    /* This is the skill-handler. It keeps the IDs of all skills for all classes.
     * You can register two methods to be called for each skill. (see RegisterSkillRequest()
     * and RegisterSkillExecute() ) 
     * 
     * When a client wants to attack a monster it REQUESTS the skill. After the request is 
     * received by the server some preparations are made for the skill to be executed (eg. 
     * play an animation, spawn some wicked things, whatever you wanna do).
     * 
     * After the warm-up time of a skill the client sends an EXECUTE command. This would be 
     * where you calculate the damage, let the mob recalculate its range, or something like
     * that.
    */

    class ActiveSkill
    {
        public int skillID;
        public int skillLvl;
        DateTime _startCast;
        DateTime _startCoolDown;

        public ActiveSkill()
        {
            _startCast = new DateTime();
            _startCoolDown = new DateTime();
        }

        public DateTime startCast { get { return _startCast; } set { _startCast = value; } }
        public void StartCast() { startCast = DateTime.UtcNow; }
        public int ElapsedCast() { TimeSpan t = DateTime.UtcNow - _startCast; return (int)t.TotalMilliseconds; }

        public DateTime startCoolDown { get { return _startCoolDown; } set { _startCoolDown = value; } }
        public void StartCoolDown() { startCoolDown = DateTime.UtcNow; }
        public int ElapsedCoolDown() { TimeSpan t = DateTime.UtcNow - _startCoolDown; return (int)t.TotalMilliseconds; }
    }

    enum SKILL_MAGIC_TYPE_FLAG
    {
        NULL_FLAG           = 0x00000000,
        FIRE_FLAG           = 0x00000001,
        ICE_FLAG            = 0x00000002,
        LIGHTENING_FLAG     = 0x00000004,
        EARTH_FLAG          = 0x00000008,
        DEATH_FLAG          = 0x00000010
    }

    enum SKILL_EFFECT_FLAG
    {
        NULL_FLAG               = 0x00000000,
        DEMAGE_TARGET_FLAG      = 0x00000001,
        BUFF_TARGET_FLAG        = 0x00000002,
        BUFF_SELF_FLAG          = 0x00000004,
        JUMP_TO_TARGET_FLAG     = 0x00000008,
        BRING_TO_CASTER_FLAG    = 0x00000010
    }

    enum SKILL_DEMAGE_FLAG
    {
        NULL_FLAG               = 0x00000000,
        SINGLE_TARGET_FLAG      = 0x00000001,
        AOE_TARGET_FLAG         = 0x00000002,
        MULTI_TARGET_FLAG       = 0x00000004,
        POSITION_TARGET_FLAG    = 0x00000008,
        CON_45_FLAG             = 0x00000010,
        CON_90_FLAG             = 0x00000020
    }

    class Skill
    {
        public int skillID;
        public int race;
        public string name;
        public int lvl;
        public int coolDown;
        public int castTime;
        public string info;
        public int range;
        public int manaCost;
        public OnSkillRequest Request;
        public OnSkillExecute Execute;
        public SKILL_EFFECT_FLAG effectFlag;
        public SKILL_DEMAGE_FLAG demageFlag;
        public SKILL_MAGIC_TYPE_FLAG magicType;
        public int aoeRadius;
        public int maxMultiTargetHits;
        public int minDmg;
        public int maxDmg;
    }

    class SkillHandler
    {
        public static Dictionary<int, Skill> mageSkillList = new Dictionary<int, Skill>();
        public static Dictionary<int, Skill> knightSkillList = new Dictionary<int, Skill>();
        public static Dictionary<int, Skill> archerSkillList = new Dictionary<int, Skill>();

        public static bool Init()
        {
            ReadIniFile("data//knightSkills.ini", Database.Player.RACE.KNIGHT);
            ReadIniFile("data//mageSkills.ini", Database.Player.RACE.MAGE);
            ReadIniFile("data//archerSkills.ini", Database.Player.RACE.ARCHER);
            return true;
        }
        //Load all skills for every race
        static bool ReadIniFile(string fileName, Database.Player.RACE race)
        {
            //its only to force static constructor to execute
            Dictionary<int, Skill> skillList = null;
            switch (race)
            {
                case Database.Player.RACE.KNIGHT:
                    skillList = knightSkillList;
                    Output.WriteLine( ConsoleColor.Cyan, "Begin register skills for KNIGHT");
                    break;
                case Database.Player.RACE.MAGE:
                    skillList = mageSkillList;
                    Output.WriteLine(ConsoleColor.Cyan, "Begin register skills for MAGE");
                    break;
                case Database.Player.RACE.ARCHER:
                    skillList = archerSkillList;
                    Output.WriteLine(ConsoleColor.Cyan, "Begin register skills for ARCHER");
                    break;
            }
            IniFile configServerFile = new IniFile(fileName);
            foreach (string sec in configServerFile.Sections)
            {
                Skill sk = new Skill();
                string tmp;
                sk.name = sec;
                sk.skillID = configServerFile.GetInteger(sec, "id", 0);
                sk.lvl = configServerFile.GetInteger(sec, "lvl", 1);
                sk.coolDown = configServerFile.GetInteger(sec, "coolDown", 0);
                sk.castTime = configServerFile.GetInteger(sec, "castTime", 0);
                sk.info = configServerFile.GetValue(sec, "info", "null");
                sk.range = configServerFile.GetInteger(sec, "range", 1);
                //to faster the math in cast range skills we store internally range as r^2 to avoid using later Sqr() in distance math
                sk.range = sk.range * sk.range;
                sk.manaCost = configServerFile.GetInteger(sec, "mana", 0);
                sk.minDmg = configServerFile.GetInteger(sec, "minDmg", 0);
                sk.maxDmg = configServerFile.GetInteger(sec, "maxDmg", 0);
                sk.race = (int)race;
                tmp = configServerFile.GetValue(sec, "effectFlag", "");
                if(tmp == "")
                {
                    Output.WriteLine("Corrupted data in skill effect " + sec + ", skipping");
                    continue;
                }
                else
                {
                    sk.effectFlag = GetSkillEffectFlag(tmp);
                }
                tmp = configServerFile.GetValue(sec, "demageFlag", "");
                if (tmp == "")
                {
                    Output.WriteLine("Corrupted data in skill demage " + sec + ", skipping");
                    continue;
                }
                else
                {
                    sk.demageFlag = GetSkillDemageFlag(tmp);
                }
                tmp = configServerFile.GetValue(sec, "magicTypeFlag", "");
                if (tmp == "")
                {
                    Output.WriteLine("Corrupted data in skill magic type " + sec + ", skipping");
                    continue;
                }
                else
                {
                    sk.magicType = GetSkillMagicTypeFlag(tmp);
                }
                sk.aoeRadius = configServerFile.GetInteger(sec, "aoeRadius", 0);
                sk.maxMultiTargetHits = configServerFile.GetInteger(sec, "maxMultiTargetHits", 0);
                sk.Request = SkillRequest;
                sk.Execute = SkillExecute;
                try
                {
                    skillList.Add(sk.skillID, sk);
                    Output.WriteLine("Register skill: " + sk.name + " ID: " + sk.skillID);
                }
                catch (ArgumentException)
                {
                    Output.WriteLine("SkillHandler::ReadIniFile " + "Wrong skill ID [" + sk.skillID + "] - " + sk.name + " skipping...");
                }
            }
            Output.WriteLine(ConsoleColor.DarkCyan, "END register skills");
            return true;
        }

        static SKILL_EFFECT_FLAG GetSkillEffectFlag(string data)
        {
            bool isFirst = true;
            SKILL_EFFECT_FLAG effectFlag = SKILL_EFFECT_FLAG.NULL_FLAG;
            string[] subStrings = data.Split(',');
            foreach (string str in subStrings)
            {
                SKILL_EFFECT_FLAG flag;
                if(Enum.TryParse(str, true, out flag))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        effectFlag = flag;
                    }
                    else
                    {
                        effectFlag |= flag;
                    }
                }
            }
            return effectFlag;
        }

        static SKILL_DEMAGE_FLAG GetSkillDemageFlag(string data)
        {
            bool isFirst = true;
            SKILL_DEMAGE_FLAG flag = SKILL_DEMAGE_FLAG.NULL_FLAG;
            string[] subStrings = data.Split(',');
            foreach (string str in subStrings)
            {
                SKILL_DEMAGE_FLAG tmpFlag;
                if (Enum.TryParse(str, true, out tmpFlag))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        flag = tmpFlag;
                    }
                    else
                    {
                        flag |= tmpFlag;
                    }
                }
            }
            return flag;
        }

        static SKILL_MAGIC_TYPE_FLAG GetSkillMagicTypeFlag(string data)
        {
            bool isFirst = true;
            SKILL_MAGIC_TYPE_FLAG flag = SKILL_MAGIC_TYPE_FLAG.NULL_FLAG;
            string[] subStrings = data.Split(',');
            foreach (string str in subStrings)
            {
                SKILL_MAGIC_TYPE_FLAG tmpFlag;
                if (Enum.TryParse(str, true, out tmpFlag))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        flag = tmpFlag;
                    }
                    else
                    {
                        flag |= tmpFlag;
                    }
                }
            }
            return flag;
        }

        //Check if requested skill is in current player skill list
        public static OnSkillRequest GetSkillRequestHandler(int skillID, Database.Player player)
        {
            try
            {
                ActiveSkill ask;
                Dictionary<int, Skill> dict = null;
                if (!player.activeSkillList.TryGetValue(skillID, out ask))
                {
                    throw new KeyNotFoundException();
                }
                switch ((Database.Player.RACE)player.Race)
                {
                    case Database.Player.RACE.KNIGHT:
                        dict = knightSkillList;
                        break;
                    case Database.Player.RACE.MAGE:
                        dict = mageSkillList;
                        break;
                    case Database.Player.RACE.ARCHER:
                        dict = archerSkillList;
                        break;
                    default:
                        throw new NullReferenceException();
                        break;
                }
                OnSkillRequest requestHandler = dict[skillID].Request;
                return requestHandler;
            }
            catch (KeyNotFoundException e)
            {
                Output.WriteLine("SkillHandler::GetSkillRequestHandler " + "Someone tried to request a skill for that no handler exists. " + e.Source.ToString() + " : " + e.TargetSite.ToString());
            }
            catch (NullReferenceException)
            {
                // Should never ever happen -.-
                Output.WriteLine("SkillHandler::GetSkillRequestHandler " + "Someone tried to request a skill for an unknown race!");
            }
            return null;
        }
        //Check if requested skill is in current player skill list
        public static OnSkillExecute GetSkillExecuteHandler(int skillID, Database.Player player)
        {
            try
            {
                ActiveSkill ask;
                Dictionary<int, Skill> dict = null;
                if (!player.activeSkillList.TryGetValue(skillID, out ask))
                {
                    throw new KeyNotFoundException();
                }
                switch ((Database.Player.RACE)player.Race)
                {
                    case Database.Player.RACE.KNIGHT:
                        dict = knightSkillList;
                        break;
                    case Database.Player.RACE.MAGE:
                        dict = mageSkillList;
                        break;
                    case Database.Player.RACE.ARCHER:
                        dict = archerSkillList;
                        break;
                    default:
                        throw new NullReferenceException();
                        break;
                }
                OnSkillExecute executeHandler = dict[skillID].Execute;
                return executeHandler;
            }
            catch (KeyNotFoundException e)
            {
                Output.WriteLine("SkillHandler::GetSkillExecuteHandler " + "Someone tried to execute a skill for that no handler exists. " + e.Source.ToString() + " : " + e.TargetSite.ToString());
            }
            catch (NullReferenceException)
            {
                // Should never ever happen -.-
                Output.WriteLine("SkillHandler::GetSkillExecuteHandler " + "Someone tried to request a skill for an unknown race!");
            }
            return null;
        }

        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * 
         * Here we go. Let's define the functions that get called whenever a skill is executed.     
         * This is the place where you wanna start to create your own custom skills. Perhaps some   
         * kind of scripting support will be added in the far future.   
         * 
        */
        public static void SkillRequest(int skillID, Database.Player.RACE race, Database.Player pAttacker, int targetID = 0, Map.Nod position = null)
        {
            Skill skill = null;
            Dictionary<int, Skill> dict = null;
            ActiveSkill activeSkill;
            if(!pAttacker.activeSkillList.TryGetValue(skillID, out activeSkill))
            {
                throw new KeyNotFoundException();
            } 
            try
            {
                switch (race)
                {
                    case Database.Player.RACE.KNIGHT:
                        dict = knightSkillList;
                        break;
                    case Database.Player.RACE.MAGE:
                        dict = mageSkillList;
                        break;
                    case Database.Player.RACE.ARCHER:
                        dict = archerSkillList;
                        break;
                    default:
                        throw new NullReferenceException();
                        break;
                }
                skill = dict[skillID];
            }
            catch (KeyNotFoundException e)
            {
                Output.WriteLine("SkillHandler::GetSkillExecuteHandler " + "Someone tried to execute a skill for that no handler exists. " + e.Source.ToString() + " : " + e.TargetSite.ToString());
            }
            catch (NullReferenceException)
            {
                // Should never ever happen -.-
                Output.WriteLine("SkillHandler::GetSkillExecuteHandler " + "Someone tried to request a skill for an unknown race!");
            }
            //This is place where we do actions for requested skill
            Output.WriteLine("SkillRequest: " + skill.name);
            if (activeSkill.ElapsedCoolDown() > 0 && activeSkill.ElapsedCoolDown() < skill.coolDown)
            {
                Output.WriteLine(ConsoleColor.Red, "SkillHandler::SkillRequest - can't cast skill (still on CoolDown)");
                Output.WriteLine(ConsoleColor.Red, "SkillHandler::SkillRequest CoolDown: " + activeSkill.ElapsedCoolDown().ToString());
                return;
            }
            Output.WriteLine(ConsoleColor.DarkGreen, "SkillHandler::SkillRequest CoolDown: " + activeSkill.ElapsedCoolDown().ToString());
            pAttacker.isCastingSkill = true;
            pAttacker.currentCastingSkillID = skillID;
            activeSkill.StartCast();
            BroadcastPacket bPacket = new BroadcastPacket((uint)pAttacker.PosX, (uint)pAttacker.PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.SkillAnim(pAttacker.PlayerPID, targetID, activeSkill.skillID, activeSkill.skillLvl));
            GameServer.world.broadcastQueue.Enqueue(bPacket);
        }

        public static void SkillExecute(int skillID , Database.Player.RACE race, Database.Player pAttacker, int targetID = 0, Map.Nod position = null)
        {
            if (!pAttacker.isCastingSkill || pAttacker.currentCastingSkillID != skillID)
            {
                Output.WriteLine("SkillHandler::SkillExecute " + " Try to cast skill but isCastingSkill is FALSE or skill ID missmatch");
                return; // if no Skillrequest executed or player break the skill execute then return
            }
            Skill skill = null;
            Dictionary<int, Skill> dict = null;
            ActiveSkill activeSkill;
            if (!pAttacker.activeSkillList.TryGetValue(skillID, out activeSkill))
            {
                throw new KeyNotFoundException();
            }
            try
            {
                switch (race)
                {
                    case Database.Player.RACE.KNIGHT:
                        dict = knightSkillList;
                        break;
                    case Database.Player.RACE.MAGE:
                        dict = mageSkillList;
                        break;
                    case Database.Player.RACE.ARCHER:
                        dict = archerSkillList;
                        break;
                    default:
                        throw new NullReferenceException();
                        break;
                }
                skill = dict[skillID];
            }
            catch (KeyNotFoundException e)
            {
                Output.WriteLine("SkillHandler::SkillExecute " + "Someone tried to execute a skill for that no handler exists. " + e.Source.ToString() + " : " + e.TargetSite.ToString());
            }
            catch (NullReferenceException)
            {
                // Should never ever happen -.-
                Output.WriteLine("SkillHandler::SkillExecute " + "Someone tried to request a skill for an unknown race!");
            }
            if (activeSkill.ElapsedCast() > 0 && activeSkill.ElapsedCast() < skill.castTime)
            {
                Output.WriteLine(ConsoleColor.Red, "SkillHandler::SkillExecute - can't cast skill (still in cast time)");
                Output.WriteLine(ConsoleColor.Red, "SkillHandler::SkillExecute cast time: " + activeSkill.ElapsedCast().ToString());
                return;
            }
            Output.WriteLine(ConsoleColor.DarkGreen, "SkillHandler::SkillExecute cast time: " + activeSkill.ElapsedCast().ToString());
            //this is place where we do actions for execution the skill
            Output.WriteLine("SkillExecute: " + skill.name);
            Output.WriteLine("cast time: " + skill.castTime);
            Output.WriteLine("cool down: " + skill.coolDown);
            Output.WriteLine("info: " + skill.info);
            Output.WriteLine("lvl: " + skill.lvl);
            Output.WriteLine("mana cost: " + skill.manaCost);
            Output.WriteLine("race: " + skill.race);
            Output.WriteLine("range: " + skill.range);
            activeSkill.StartCoolDown();
            activeSkill.startCast = DateTime.UtcNow;
            pAttacker.isCastingSkill = false;
            pAttacker.currentCastingSkillID = -1;
            //BroadcastPacket bPacket = new BroadcastPacket((uint)pAttacker.PosX, (uint)pAttacker.PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.AttackMag(pAttacker.PlayerPID, targetID, activeSkill.skillID, activeSkill.skillLvl));
            //GameServer.world.broadcastQueue.Enqueue(bPacket);
            switch (skill.demageFlag)
            {
                //skills that need to give target entity
                case SKILL_DEMAGE_FLAG.SINGLE_TARGET_FLAG:
                case SKILL_DEMAGE_FLAG.MULTI_TARGET_FLAG:
                    {
                        Action act = new Action(activeSkill, skill, pAttacker, targetID);
                        GameServer.world.actions.Enqueue(act);
                        Output.WriteLine("Skill enqueue as target skill");
                    }
                    break;
                    //skills that are place related not target entity
                case SKILL_DEMAGE_FLAG.POSITION_TARGET_FLAG:
                case SKILL_DEMAGE_FLAG.CON_45_FLAG:
                case SKILL_DEMAGE_FLAG.CON_90_FLAG:
                case SKILL_DEMAGE_FLAG.AOE_TARGET_FLAG:
                    {
                        Action act = new Action(activeSkill, skill, pAttacker, position);
                        GameServer.world.actions.Enqueue(act);
                        Output.WriteLine("Skill enqueue as position skill");
                    }
                    break;
                default:
                    Output.WriteLine("Skill has inncorrect DEMAGE FLAG SET");
                    break;
            }
        }





        //
        //    OLD WAY TO CALL SKILLS
        //
        //
        public static void BeheadSkillRequest(Database.Player pAttacker, int targetID)
        {
            // no implementation here :)
            Output.WriteLine("SkillHandler::BeheadSkillRequest");
        }

        public static void BeheadSkillExecute(Database.Player pAttacker, int targetID)
        {
            Output.WriteLine("SkillHandler::BeheadSkillExecute");
            //Database.Mob attackedMob = World.Monsters[mobID];

            //Packet attackPacket = new ExecuteSkill(pAttacker.UniqueID, attackedMob.UniqueID, 1, 1, 0, 0, 0);
            //attackedMob.broadcastPacket(attackPacket);

            //Packet aniPack = new PlayAnimation(mobID, 10);
            //attackedMob.broadcastPacket(aniPack);

            //attackedMob.OnBehead(pAttacker);
        }

        public static void LightningSkillRequest(Database.Player pAttacker, int targetID)
        {
            Output.WriteLine("SkillHandler::LightningSkillRequest");
            BroadcastPacket bPacket = new BroadcastPacket((uint)pAttacker.PosX, (uint)pAttacker.PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.SkillAnim(pAttacker.PlayerPID, targetID, 2, 2));
            GameServer.world.broadcastQueue.Enqueue(bPacket);
            //Monster attackedMob = World.Monsters[mobID];
            //attackedMob.broadcastPacket(new PlayAnimation(mobID, pAttacker.UniqueID, 4));
        }

        public static void LightningSkillExecute(Database.Player pAttacker, int targetID)
        {
            Output.WriteLine("SkillHandler::LightningSkillExecute");
            //GameServer.world.BroadcastPacket(pConn.client.GetPlayer(), new Packet.SendPacketHandlers.AttackMag(casterID, targetID, skill_id, skill_lvl));
            BroadcastPacket bPacket = new BroadcastPacket((uint)pAttacker.PosX, (uint)pAttacker.PosY, (int)World.DEBUG_SIGHT_RANGE, new Packet.SendPacketHandlers.AttackMag(pAttacker.PlayerPID, targetID, 2, 2));
            GameServer.world.broadcastQueue.Enqueue(bPacket);

            //Monster attackedMob = World.Monsters[mobID];
            //Packet attackPacket = new ExecuteSkill(pAttacker.UniqueID, attackedMob.UniqueID, 4, 1, 1, 31, 0);
            //attackedMob.broadcastPacket(attackPacket);
            //attackedMob.getDamage((ushort)Server.rand.Next(100), pAttacker);
        }
    }
}
