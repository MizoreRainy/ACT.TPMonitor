using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ACT.TPMonitor
{
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
                case JOB.BRD:
                    return 12;
                case JOB.BLM:
                    return 13;
                case JOB.SMN:
                    return 14;
                case JOB.NIN:
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
                case JOB.ARC:
                    return 34;
                case JOB.THM:
                    return 35;
                case JOB.ACN:
                    return 36;
                case JOB.CNJ:
                    return 40;
                default:
                    return 999;
            }
        }
    }
}
