using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Advanced_Combat_Tracker;

namespace ACT.TPMonitor
{
    public static class FFXIVPluginHelper
    {
        private static object lockObject = new object();
        private static object plugin;
        private static object pluginMemory;
        private static dynamic pluginConfig;
        private static dynamic pluginScancombat;

        public static object Instance
        {
            get
            {
                try
                {
                    Initialize();
                    return plugin;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static Regex regVersion = new Regex(@"FileVersion: (?<version>\d+\.\d+\.\d+\.\d+)\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Version Version { get; private set; }

        public static void Initialize()
        {
            lock (lockObject)
            {
                if (!ActGlobals.oFormActMain.Visible)
                {
                    return;
                }

                if (plugin == null)
                {
                    foreach (var item in ActGlobals.oFormActMain.ActPlugins)
                    {
                        if (item.pluginFile.Name.ToUpper() == "FFXIV_ACT_Plugin.dll".ToUpper() &&
                            item.lblPluginStatus.Text.ToUpper() == "FFXIV Plugin Started.".ToUpper())
                        {
                            plugin = item.pluginObj;
                            Version = new Version(regVersion.Match(item.pluginVersion).Groups["version"].ToString());
                            break;
                        }
                    }
                }

                if (plugin != null)
                {
                    FieldInfo fi;

                    if (pluginMemory == null)
                    {
                        fi = plugin.GetType().GetField("_Memory", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                        pluginMemory = fi.GetValue(plugin);
                    }

                    if (pluginMemory == null)
                    {
                        return;
                    }

                    if (pluginConfig == null)
                    {
                        fi = pluginMemory.GetType().GetField("_config", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                        pluginConfig = fi.GetValue(pluginMemory);
                    }

                    if (pluginConfig == null)
                    {
                        return;
                    }

                    if (pluginScancombat == null)
                    {
                        fi = pluginConfig.GetType().GetField("ScanCombatants", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                        pluginScancombat = fi.GetValue(pluginConfig);
                    }
                }
            }
        }

        public static Process GetFFXIVProcess
        {
            get
            {
                try
                {
                    Initialize();

                    if (pluginConfig == null)
                    {
                        return null;
                    }

                    var process = pluginConfig.Process;

                    return (Process)process;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static Combatant GetPlayerData()
        {
            Initialize();

            var result = new Combatant();

            if (plugin == null)
            {
                return result;
            }

            if (GetFFXIVProcess == null)
            {
                return result;
            }

            if (pluginScancombat == null)
            {
                return result;
            }

            dynamic player = pluginScancombat.GetPlayerData();

            result.Job = (int)player.JobID;

            return result;
        }

        public static List<Combatant> GetCombatantList()
        {
            return GetCombatantList(TYPE.Player);
        }

        public static List<Combatant> GetCombatantList(params TYPE[] types)
        {
            Initialize();

            var result = new List<Combatant>();

            if (plugin == null)
            {
                return result;
            }

            if (GetFFXIVProcess == null)
            {
                return result;
            }

            if (pluginScancombat == null)
            {
                return result;
            }

            dynamic list = pluginScancombat.GetCombatantList();
            foreach (dynamic item in list.ToArray())
            {
                if (item == null)
                {
                    continue;
                }

                if (types.Contains((TYPE)((byte)item.type)))
                {
                    var combatant = new Combatant();

                    combatant.ID = (uint)item.ID;
                    combatant.OwnerID = (uint)item.OwnerID;
                    combatant.Job = (int)item.Job;
                    combatant.Name = (string)item.Name;
                    combatant.type = (byte)item.type;
                    combatant.Level = (int)item.Level;
                    combatant.CurrentHP = (int)item.CurrentHP;
                    combatant.MaxHP = (int)item.MaxHP;
                    combatant.CurrentMP = (int)item.CurrentMP;
                    combatant.MaxMP = (int)item.MaxMP;
                    combatant.CurrentTP = (int)item.CurrentTP;

                    combatant.Role = GetRole((JOB)combatant.Job);
                    combatant.JobOrder = GetJobOrder((JOB)combatant.Job);

                    result.Add(combatant);
                }                    
            }

            return result;
        }

        public static List<uint> GetCurrentPartyList(out int partyCount)
        {
            Initialize();

            var partyList = new List<uint>();
            partyCount = 0;

            if (plugin == null)
            {
                return partyList;
            }

            if (GetFFXIVProcess == null)
            {
                return partyList;
            }

            if (pluginScancombat == null)
            {
                return partyList;
            }

            partyList = pluginScancombat.GetCurrentPartyList(
                out partyCount) as List<uint>;

            return partyList;
        }


        public static List<Combatant> GePartyList(out int partyCount)
        {
            List<Combatant> result = new List<Combatant>();
            partyCount = 0;
            try
            {
                var partyList = GetCurrentPartyList(out partyCount);

                var playerList = GetCombatantList(TYPE.Player, TYPE.Mob);
                var player = playerList[0];

                foreach (var memberID in partyList)
                {
                    if (memberID == 0 || memberID == player.ID) continue;

                    var partyMember = (from x in playerList where x.ID == memberID select x).FirstOrDefault();
                    if (partyMember != null)
                    {
                        partyMember.SortOrder = GetSortOrder((Role)player.Role, (Role)partyMember.Role);
                        result.Add(partyMember);
                    }
                }
                result = result.OrderBy(x => x.SortOrder).ThenBy(x => x.JobOrder).ThenByDescending(x => x.ID).ToList();
                result.Insert(0, player);
            }
            catch { }
            return result;
        }

        private static int GetRole(JOB job)
        {
            switch (job)
            {
                case JOB.GLA:
                case JOB.PLD:
                case JOB.MRD:
                case JOB.WAR:
                    return (int)Role.TANK;
                case JOB.PGL:
                case JOB.LNC:
                case JOB.ARC:
                case JOB.THM:
                case JOB.MNK:
                case JOB.DRG:
                case JOB.BRD:
                case JOB.BLM:
                case JOB.ACN:
                case JOB.SMN:
                case JOB.ROG:
                case JOB.NIN:
                    return (int)Role.DPS;
                case JOB.CNJ:
                case JOB.WHM:
                case JOB.SCH:
                    return (int)Role.HEALER;
                case JOB.CPT:
                case JOB.BSM:
                case JOB.ARM:
                case JOB.GSM:
                case JOB.LTW:
                case JOB.WVR:
                case JOB.ALC:
                case JOB.CUL:
                case JOB.MIN:
                case JOB.BOT:
                case JOB.FSH:
                    return (int)Role.OTHER;
                default:
                    return -1;
            }
        }

        private static int GetSortOrder(Role myRole, Role role)
        {
            var sortOrder = Util.SortOrder[myRole];
            switch (sortOrder)
            {
                case 0:
                    // TANK HEALER DPS
                    switch (role)
                    {
                        case Role.TANK:
                            return 0;
                        case Role.HEALER:
                            return 1;
                        case Role.DPS:
                            return 2;
                        case Role.OTHER:
                            return 3;
                        default:
                            return 9;
                    }
                case 1:
                    // TANK DPS HEALER
                    switch (role)
                    {
                        case Role.TANK:
                            return 0;
                        case Role.HEALER:
                            return 2;
                        case Role.DPS:
                            return 1;
                        case Role.OTHER:
                            return 3;
                        default:
                            return 9;
                    }
                case 2:
                    // HEALER TANK DPS
                    switch (role)
                    {
                        case Role.TANK:
                            return 1;
                        case Role.HEALER:
                            return 0;
                        case Role.DPS:
                            return 2;
                        case Role.OTHER:
                            return 3;
                        default:
                            return 9;
                    }
                case 3:
                    // HEALER DPS TANK
                    switch (role)
                    {
                        case Role.TANK:
                            return 2;
                        case Role.HEALER:
                            return 0;
                        case Role.DPS:
                            return 1;
                        case Role.OTHER:
                            return 3;
                        default:
                            return 9;
                    }
                case 4:
                    // DPS TANK HEALER
                    switch (role)
                    {
                        case Role.TANK:
                            return 1;
                        case Role.HEALER:
                            return 2;
                        case Role.DPS:
                            return 0;
                        case Role.OTHER:
                            return 3;
                        default:
                            return 9;
                    }
                case 5:
                    // DPS HEALER TANK
                    switch (role)
                    {
                        case Role.TANK:
                            return 2;
                        case Role.HEALER:
                            return 1;
                        case Role.DPS:
                            return 0;
                        case Role.OTHER:
                            return 3;
                        default:
                            return 9;
                    }
                default:
                    return 0;
            }
        }

        private static int GetJobOrder(JOB job)
        {
            switch (job)
            {
                case JOB.PLD:
                    return 0;
                case JOB.WAR:
                    return 1;
                case JOB.MNK:
                    return 10;
                case JOB.DRG:
                    return 11;
                case JOB.NIN:
                    return 12;
                case JOB.BRD:
                    return 13;
                case JOB.BLM:
                    return 14;
                case JOB.SMN:
                    return 15;
                case JOB.WHM:
                    return 20;
                case JOB.SCH:
                    return 21;
                case JOB.GLA:
                    return 30;
                case JOB.MRD:
                    return 31;
                case JOB.PGL:
                    return 32;
                case JOB.LNC:
                    return 33;
                case JOB.ROG:
                    return 34;
                case JOB.ARC:
                    return 35;
                case JOB.THM:
                    return 36;
                case JOB.ACN:
                    return 37;
                case JOB.CNJ:
                    return 38;
                default:
                    return 999;
            }
        }
    }

    public class Combatant
    {
        public uint ID;
        public uint OwnerID;
        public int Order;
        public byte type;
        public int Job;
        public int Level;
        public string Name;
        public int CurrentHP;
        public int MaxHP;
        public int CurrentMP;
        public int MaxMP;
        public int CurrentTP;
        public int Role;
        public int JobOrder;
        public int SortOrder;
    }
}
