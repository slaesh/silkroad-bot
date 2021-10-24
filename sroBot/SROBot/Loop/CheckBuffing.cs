using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Loop
    {
        public static bool CheckWeapon(InventoryItem item, SkillInfo skill)
        {
            //Wrap our function inside a catcher
            try
            {
                if (SROBot.Inventory.IsItemEmpty(item)) return false;
                if (skill == null) return false;

                if (skill.WeaponType1 == 255)
                    return true;

                var weapons = new byte[2];

                weapons[0] = skill.WeaponType1;
                weapons[1] = skill.WeaponType2;

                if (weapons[1] == 255)
                    weapons[1] = 6;

                foreach (byte b in weapons)
                {
                    if (b == item.Iteminfo.TypeId4)
                        return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        public long checkBuffingTimer = 100;

        public void CheckBuffing()
        {
            if (checkBuffingTimer > 0)
            {
                checkBuffingTimer -= 100;
                if (checkBuffingTimer > 0) return;
            }

            if (bot.Char.ZerkInUse)
            {
                checkBuffingTimer = 100;
                return; // dont buf during ZERK .. use it !! :)
            }

            var buffingSkills = bot.Config.GetBuffingSkills().ToArray();
            var activeBuffs = bot.GetActiveBuffs().ToArray();

            var myItems = bot.Inventory.GetItems(i => i.Slot < 13);
            
            if (true) // use dmg scrolls ..
            {
                var dmgScroll = bot.Inventory.GetItems().FirstOrDefault(ii => ii.Iteminfo.Name.Contains("20% damage increase"));
                if (dmgScroll != null && !activeBuffs.Any(b => b.Name.Contains("20% damage")))
                {
                    bot.Log($"Use Scroll on Slot {dmgScroll.Slot} ({dmgScroll.Iteminfo.Name}) ..");
                    Actions.UseDmgScroll(dmgScroll, bot);

                    checkBuffingTimer = 1000;
                    return;
                }
            }

            if (true) // use trigger scrolls ..
            {
                var triggerScroll = bot.Inventory.GetItems().FirstOrDefault(ii => ii.Iteminfo.Type.Contains("AGILITY_SCROLL"));
                if (triggerScroll != null && !activeBuffs.Any(b => b.Type.Contains("AGILITY_SCROLL")))
                {
                    bot.Log($"Use Scroll on Slot {triggerScroll.Slot} ({triggerScroll.Iteminfo.Name}) ..");
                    Actions.UseTriggerScroll(triggerScroll.Slot, bot);

                    checkBuffingTimer = 1000;
                    return;
                }
            }

            foreach (var buff in buffingSkills.Where(b => b.CooldownTimer <= 0))
            {
                if (buff.MP > bot.Char.CurMP)
                {
                    continue;
                }

                if (activeBuffs.Any(ab => (ab?.Attributes?.ContainsKey("hste")) ?? false) && (buff?.Attributes?.ContainsKey("hste") ?? false)) // got already a "speed buff" !
                {
                    bot.Debug("already got a speed buf!");
                    continue;
                }
                
                //if (buff.RequiredItems.Any(ri => !myItems.Any(i => i.Iteminfo.ItemType == ri)))
                //{
                //    var requiredItem = buff.RequiredItems.FirstOrDefault(ri => !myItems.Any(i => i.Iteminfo.ItemType == ri));
                //    bot.Debug("Need to wear this item: {0}", requiredItem);
                //    continue;
                //}

                //if (!myItems.Any(i => i.Iteminfo.ItemType == buff.WeaponToUse))
                if (!(CheckWeapon(bot.Inventory.GetItem(6), buff) || CheckWeapon(bot.Inventory.GetItem(7), buff)))
                {
                    bot.Debug("Need this item for skilling: {0}", buff.WeaponToUse);
                    var items = bot.Inventory.GetItems(i => i.Slot >= 13 && i.Iteminfo.ItemType == buff.WeaponToUse).OrderByDescending(i => i.Iteminfo.GetVirtualPlus() + i.Iteminfo.Level);
                    if (!items.Any())
                    {
                        Console.WriteLine("..dont have this item..");
                        continue;
                    }

                    Console.WriteLine("swap items NOW! {0}", items.First().Slot);
                }

                if (!activeBuffs.Any(ab => ab.Name.Equals(buff.Name) || ab.HasSameCooldownId(buff))) // same buff active..
                {
                    //Console.WriteLine("{0} | buff not found .. cast it! -> {1}/{2}", DateTime.Now.ToString("HH:mm:ss.fff"), buff.Name, buff.Id);

                    buff.CooldownTimer = 3500; // lets try other buffs if this will not work..
                    Actions.CastBuff(buff.Model, bot);
                    
                    checkBuffingTimer = 3000;
                    return;
                }
            }

            checkBuffingTimer = 100;
        }
    }
}
