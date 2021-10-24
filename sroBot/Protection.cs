using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{
    public class Protection
    {
        public static void UseHP(SROBot.Bot bot)
        {
            if (bot.Char.IsAlive)
            {
                var invitem = bot.Inventory.GetItemByType("ITEM_EVENT_HP_SUPERSET_");
                if (SROBot.Inventory.IsItemEmpty(invitem))
                {
                    invitem = bot.Inventory.GetItem("HP Recovery Potion");

                    if (SROBot.Inventory.IsItemEmpty(invitem)) return;
                }

                var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
                packet.WriteUInt8(invitem.Slot);
                packet.WriteUInt16(0x08EC);

                bot.SendToSilkroadServer(packet);
            }
        }

        public static void UseMP(SROBot.Bot bot)
        {
            if (bot.Char.IsAlive)
            {
                var invitem = bot.Inventory.GetItemByType("ITEM_EVENT_MP_SUPERSET_");
                if (SROBot.Inventory.IsItemEmpty(invitem))
                {
                    invitem = bot.Inventory.GetItem("MP Recovery Potion");

                    if (SROBot.Inventory.IsItemEmpty(invitem)) return;
                }

                var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
                packet.WriteUInt8(invitem.Slot);
                packet.WriteUInt16(0x10EC);

                bot.SendToSilkroadServer(packet);
            }
        }

        public static void UseUniversalPill(SROBot.Bot bot)
        {
            if (bot.Char.IsAlive)
            {
                var invitem = bot.Inventory.GetItem("Universal Pill");
                if (SROBot.Inventory.IsItemEmpty(invitem) || !invitem.Iteminfo.Type.StartsWith("ITEM_ETC_CURE_ALL")) return;

                var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
                packet.WriteUInt8(invitem.Slot);
                packet.WriteUInt16(0x316C);

                bot.SendToSilkroadServer(packet);
            }
        }

        public static void UsePetHP(SROBot.Bot bot, uint petId)
        {
            if (bot == null) return;

            var invitem = bot.Inventory.GetItemByType("ITEM_ETC_COS_HP_POTION");
            if (SROBot.Inventory.IsItemEmpty(invitem)) return;

            var packet = new Packet((ushort)SROData.Opcodes.CLIENT.INVENTORYUSE, true);
            packet.WriteUInt8(invitem.Slot);
            packet.WriteUInt16(0x20EC);
            packet.WriteUInt32(petId);

            bot.SendToSilkroadServer(packet);
        }
    }
}
