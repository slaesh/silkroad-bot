using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {

        public void CheckInventory(bool full = false)
        {
            if (!IsStarted) return;

            if (!full) full = bot.Inventory.IsFull();

            if (full)
            {
                bot.Log("INVENTORY FULL -- back town!");
                bot.UseReturnScroll();
            }
        }
    }
}
