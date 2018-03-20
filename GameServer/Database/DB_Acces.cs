using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.IO;

namespace GameServer.Database
{
    static class DB_Acces
    {
        public static List<Player> GetPlayerList(int UID, Connection con)
        {
            List<Player> pList = new List<Player>();
            Player p;
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT * FROM PLAYER WHERE UID = @uid";
                cm.Parameters.Add("@uid", SqlDbType.Int).Value = UID;
                cn.Open();
                SqlDataReader rdr = null;
                rdr = cm.ExecuteReader();
                while (rdr.Read())
                {
                    p = new Player(con);
                    p.PlayerUID = rdr.GetInt32(0);
                    p.PlayerPID = rdr.GetInt32(1);
                    p.PlayerName = rdr.GetString(2);
                    p.Strength = rdr.GetInt32(3);
                    p.Health = rdr.GetInt32(4);
                    p.Intel = rdr.GetInt32(5);
                    p.Wisdom = rdr.GetInt32(6);
                    p.Agility = rdr.GetInt32(7);
                    p.PosX = rdr.GetInt32(8);
                    p.PosY = rdr.GetInt32(9);
                    p.PosZ = rdr.GetInt32(10);
                    p.Race = rdr.GetInt32(11);
                    p.Job = rdr.GetInt32(12);
                    p.Level = rdr.GetInt32(13);
                    p.FaceType = rdr.GetInt32(14);
                    p.HairType = rdr.GetInt32(15);
                    p.Experience = rdr.GetInt32(16);
                    p.ActHealth = rdr.GetInt32(17);
                    p.ActMana = rdr.GetInt32(18);
                    p.ActRage = rdr.GetInt32(19);
                    p.HeadArmor = rdr.GetInt32(20);
                    p.GlovesArmor = rdr.GetInt32(21);
                    p.ChestArmor = rdr.GetInt32(22);
                    p.ShortsArmor = rdr.GetInt32(23);
                    p.BootsArmor = rdr.GetInt32(24);
                    p.LeftHand = rdr.GetInt32(25);
                    p.RightHand = rdr.GetInt32(26);
                    pList.Add(p);
                }
                rdr.Close();
                cn.Close();
                rdr = null;
                cm = null;
            }
            return pList;
        }

        public static Player GetPlayer(int PID, Connection con)
        {
            Player p = null;
            byte[] playerSkills = new byte[164];
            Array.Clear(playerSkills, 0, playerSkills.Length);
            int count = 0;
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT * FROM PLAYER WHERE PID = @pid";
                cm.Parameters.Add("@pid", SqlDbType.Int).Value = PID;
                cn.Open();
                SqlDataReader rdr = null;
                rdr = cm.ExecuteReader();
                while (rdr.Read())
                {
                    p = new Player(con);
                    p.PlayerUID = rdr.GetInt32(0);
                    p.PlayerPID = rdr.GetInt32(1);
                    p.PlayerName = rdr.GetString(2);
                    p.Strength = rdr.GetInt32(3);
                    p.Health = rdr.GetInt32(4);
                    p.Intel = rdr.GetInt32(5);
                    p.Wisdom = rdr.GetInt32(6);
                    p.Agility = rdr.GetInt32(7);
                    p.PosX = rdr.GetInt32(8);
                    p.PosY = rdr.GetInt32(9);
                    p.PosZ = rdr.GetInt32(10);
                    p.Race = rdr.GetInt32(11);
                    p.Job = rdr.GetInt32(12);
                    p.Level = rdr.GetInt32(13);
                    p.FaceType = rdr.GetInt32(14);
                    p.HairType = rdr.GetInt32(15);
                    p.Experience = rdr.GetInt32(16);
                    p.ActHealth = rdr.GetInt32(17);
                    p.ActMana = rdr.GetInt32(18);
                    p.ActRage = rdr.GetInt32(19);
                    p.HeadArmor = rdr.GetInt32(20);
                    p.GlovesArmor = rdr.GetInt32(21);
                    p.ChestArmor = rdr.GetInt32(22);
                    p.ShortsArmor = rdr.GetInt32(23);
                    p.BootsArmor = rdr.GetInt32(24);
                    p.LeftHand = rdr.GetInt32(25);
                    p.RightHand = rdr.GetInt32(26);
                    rdr.GetBytes(27, 0, playerSkills, 0, 164);
                    count++;
                }
                rdr.Close();
                cn.Close();
                rdr = null;
                cm = null;
            }

            p.activeSkillList = new Dictionary<int, Skill.ActiveSkill>();

            MemoryStream stream = new MemoryStream(playerSkills);
            BinaryReader br;
            using (br = new BinaryReader(stream))
            {
                int skillCount;
                long cdTime;
                stream.Position = 0;
                skillCount = br.ReadInt32();
                for (int i = 0; i < skillCount; i++)
                {
                    Skill.ActiveSkill sk = new Skill.ActiveSkill();
                    sk.skillID = (int)br.ReadInt16();
                    sk.skillLvl = (int)br.ReadInt16();
                    cdTime = br.ReadInt64();
                    sk.startCoolDown = DateTime.FromBinary(cdTime);
                    p.activeSkillList.Add(sk.skillID, sk);
                }
            }
            Output.WriteLine(ConsoleColor.Green, "DB_Acces::GetPlayer Player have: " + p.activeSkillList.Count.ToString() + " skills");
            Output.WriteLine(ConsoleColor.Red, "DB_Acces::GetPlayer DEBUG - ALL PLAYERS HAS ADDED SKILL ID 1!");
            if (!p.activeSkillList.ContainsKey(1))
            {
                Skill.ActiveSkill sk = new Skill.ActiveSkill();
                sk.skillID = 1;
                sk.skillLvl = 1;
                //sk.startCoolDown = DateTime();
                p.activeSkillList.Add(sk.skillID, sk);
            }
            if (count > 1) Output.WriteLine(ConsoleColor.Red, "DB_Acces::GetPlayer Read multiply player info! [PID: " + PID.ToString() + "]");
            return p;
        }

        public static Inventory PlayerInventory(int PlayerID)
        {
            Item tempItem;
            ItemTemplate template;
            Inventory inv = new Inventory();
            // Load inventory from database
            using (SqlConnection cn = new SqlConnection(Program.dbConnStr))
            {
                SqlCommand cm = cn.CreateCommand();
                cm.CommandText = "SELECT * FROM INVENTORY WHERE PlayerID = @id";
                cm.Parameters.Add("@id", SqlDbType.Int).Value = PlayerID;
                cn.Open();
                SqlDataReader rdr = null;
                rdr = cm.ExecuteReader();
                while (rdr.Read())
                {
                    tempItem = new Item();
                    tempItem.DBID = rdr.GetInt32(0);
                    tempItem.Index = rdr.GetInt32(2);
                    tempItem.Count = rdr.GetInt32(3);
                    tempItem.Prefix = rdr.GetInt32(4);
                    tempItem.Info = rdr.GetInt32(5);
                    tempItem.MaxEndurance = rdr.GetInt32(6);
                    tempItem.CurrentEndurance = rdr.GetInt32(7);
                    tempItem.SetGem = rdr.GetInt32(8);
                    tempItem.AttackTalis = rdr.GetInt32(9);
                    tempItem.MagicTalis = rdr.GetInt32(10);
                    tempItem.Defense = rdr.GetInt32(11);
                    tempItem.OnTargetPoint = rdr.GetInt32(12);
                    tempItem.Dodge = rdr.GetInt32(13);
                    tempItem.Protect = rdr.GetInt32(14);
                    tempItem.EBLevel = rdr.GetInt32(15);
                    tempItem.EBRate = rdr.GetInt32(16);

                    //if this item is worn by player ( info == 1 )
                    if (tempItem.Info == 1)
                    {
                        template = TemplateManager.GetItemTemplate(tempItem.Index);
                        if (template.Class == ItemClass.Weapon) inv.Weapon = tempItem;
                        if (template.Class == ItemClass.Defense)
                        {
                            switch (template.Subclass)
                            {
                                case ItemSubclass.Chest:
                                    inv.Chest = tempItem;
                                    break;

                                case ItemSubclass.Helmet:
                                    inv.Helmet = tempItem;
                                    break;

                                case ItemSubclass.Gloves:
                                    inv.Gloves = tempItem;
                                    break;

                                case ItemSubclass.Boots:
                                    inv.Boots = tempItem;
                                    break;

                                case ItemSubclass.Shorts:
                                    inv.Shorts = tempItem;
                                    break;

                                case ItemSubclass.Shield:
                                    inv.Shield = tempItem;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        inv.AddToInventory(tempItem);
                    }
                }
                rdr.Close();
                cn.Close();
                rdr = null;
                cm = null;
            }
            return inv;
        }

        public static void Monster()
        {

        }

        public static void Item()
        {

        }
    }
}
