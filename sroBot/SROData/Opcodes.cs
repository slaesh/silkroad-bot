using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROData
{
    class Opcodes
    {
        public enum SERVER
        {
            SINGLEDESPAWN = 0x3016,
            CHARDATA = 0x3013,
            MOVE = 0xB021,
            GROUPSPAWNB = 0x3017,
            GROUPSPAWNEND = 0x3018,
            SINGLESPAWN = 0x3015,
            GROUPESPAWN = 0x3019,
            CHARACTERINFO = 0x303D,
            SPEEDUPDATE = 0x30D0, //??
            CHARACTERLISTING = 0xB007,
            OBJECTDIE = 0x30BF,
            ANGLECHANGE = 0xB024,
            BUFFINFO = 0xB0BD,
            SKILLADD = 0xB070,
            SKILLCASTED = 0xB071,
            BUFFDELL = 0xB072,
            INVENTORYMOVEMENT = 0xB034,
            EXPSPUPDATE = 0x3056,
            LVLUP = 0x3054,
            STUCK = 0xB023,
            HPMPUPDATE = 0x3057,
            OBJECTSELECT = 0xB045,
            CHAT = 0x3026,
            CHATCOUNT = 0xB025,
            NPCSELECT = 0xB046,
            NPCDESELECT = 0xB04B,
            CONFIRMSPAWN = 0x3020,
            INVENTORYUSE = 0xB04C,
            STUFFUPDATE = 0x304E,
            PARTYINVITATION = 0x3080,

            STORAGEGOLD = 0x3047,
            STORAGEOK = 0x3048,
            STORAGEITEMS = 0x3049,

            GUILDSTORAGEGOLD = 0x3253,
            GUILDSTORAGEOK = 0x3254,
            GUILDSTORAGEITEMS = 0x3255,

            PARTYMATCHING = 0xB06C,
            ITEMFIXED = 0xB03E,
            OBJECTACTION = 0xB074,
            DURABILITYCHANGE = 0x3052,
            SKILLUPDATE = 0xB0A1,
            MASTERYUPDATE = 0xB0A2, //NEW
            GUILDINFO = 0x3101,
            PETINFO = 0x30C8,
            HORSEACTION = 0xB0CB,
            PETSTATS = 0x30C9,

            EXCHANGESTARTED = 0x3085,
            EXCHANGEGOLDCHANGED = 0x3089,
            EXCHANGEITEMSGAINED = 0x308c,
            EXCHANGEDONE = 0x3087,
            EXCHANGECANCELED = 0x3088,

            ALCHEMYRESULT = 0xb150,
        }

        public enum CLIENT
        {
            DISCONNECT = 0x7005,
            TELEPORT = 0x705A,
            GETSTORAGEITEMS = 0x703C,
            NPCDESELECT = 0x704B,
            CHARACTERLISTING = 0x7007,
            INVENTORYUSE = 0x704C,
            SELECTCHARACTER = 0x7001,
            OBJECTACTION = 0x7074,
            NPCSELECT = 0x7046,
            OBJECTSELECT = 0x7045,
            INVENTORYMOVEMENT = 0x7034,
            CONFIRMSPAWN = 0x34C5,
            REPAIR = 0x703E,
            KILLHORSE = 0x70C6,
            PETACTION = 0x70C5,
            SITDOWN = 0x704F,
            CHAT = 0x7025,
            DROPGOLD = 0x7034,
            PARTY = 0x3080,
            PARTYLEAVE = 0x7061,
            MOVEMENT = 0x7021,
            JOINPARTY = 0x706D,
            ACCEPTDEAD = 0x3053,
            ZERK = 0x70A7,
            INCSTR = 0x7050,
            INCINT = 0x7051,
            UPSKILL = 0x70A1,
            UPMASTERY = 0x70A2,
        }
    }
}
