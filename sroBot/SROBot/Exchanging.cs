using SilkroadSecurityApi;
using sroBot.SROBot.Spawn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public class Exchanging
    {
        private SROBot.Bot _bot;
        private Player _player = null;
        private Packet _exchangeItemsDataToMe;
        private Packet _exchangeItemsDataFromMe;

        public Exchanging(Bot bot)
        {
            _bot = bot;
        }

        private bool Accept()
        {
            return _bot.Config.Exchanging.AutoAccept && (!_bot.Config.Exchanging.OnlyFromList || (_player != null && _bot.Config.Exchanging.Players.Contains(_player.Name)));
        }

        public void Request(Packet packet)
        {
            var playerId = packet.ReadUInt32();
            _player = _bot.Spawns.Player.Get(playerId);

            if (_player == null)
            {
                _bot.Log("exchange from unknown player!!");
                return;
            }

            _bot.Log("exchange request from {0}", _player?.Name ?? playerId.ToString());

            if (Accept())
            {
                _bot.Log("-> accepted!");
                Actions.AcceptPartyRequest(_bot, true);
            }
        }

        public void AcceptedStep1(Packet packet)
        {
            _bot.Log("the other1 has accepted - (first)");
            if (_bot.Config.Exchanging.AutoAccept)
            {
                Actions.AcceptExchange(_bot, true, true);
            }
        }

        public void AcceptedStep2(Packet packet)
        {
            _bot.Log("accepted step 1? (second)");
            if (_bot.Config.Exchanging.AutoAccept)
            {
                Actions.AcceptExchange(_bot, true, false);
            }
        }

        public void Started(Packet packet)
        {
            _exchangeItemsDataToMe = null;
            _exchangeItemsDataFromMe = null;

            var playerId = packet.ReadUInt32();
            _player = _bot.Spawns.Player.Get(playerId);

            if (_player != null)
            {
                _bot.Log("exchange started with: {0}", _player.Name);
            }
            else
            {
                _bot.Log("exchange started with playerid: {0}", playerId);
            }
        }

        public void GoldChanged(Packet packet)
        {
            var type = packet.ReadUInt8();
            switch (type)
            {
                case 2:
                    {
                        var gold = packet.ReadUInt64();
                        _bot.Log("exchange: gold changed to: {0:N0}", gold);
                    }
                    break;

                default:
                    _bot.Log("exchange gold changed: {0}", String.Join("", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                    break;
            }
        }

        public void ItemsGained(Packet packet)
        {
            var playerId = packet.ReadUInt32();
            packet.SeekRead(0, System.IO.SeekOrigin.Begin);

            if (playerId == _bot.Char.CharId || playerId == _bot.Char.AccountId)
            {
                //Debug("exchange items gained: my items: {0}", string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
                _exchangeItemsDataFromMe = packet;
                return;
            }

            _exchangeItemsDataToMe = packet;
            //Debug("exchange items gained: {0}", string.Join(", ", packet.GetBytes().Select(b => "0x" + b.ToString("X2"))));
        }

        public void Done(Packet packet)
        {
            _bot.Log("exchange: done.");

            if (_exchangeItemsDataToMe != null)
            {
                var playerId = _exchangeItemsDataToMe.ReadUInt32();
                var items = _exchangeItemsDataToMe.ReadUInt8();
                _player = _bot.Spawns.Player.Get(playerId);

                if (_player != null)
                {
                    _bot.Log("exchange(getting): got {0} items from {1}", items, _player.Name);
                }
                else
                {
                    _bot.Log("exchange(getting): got {0} items from {1}", items, playerId);
                }

                while (items-- > 0)
                {
                    var itemCnt = _exchangeItemsDataToMe.ReadUInt8();
                    //var unk1 = exchangeItemsDataToMe.ReadUInt32();
                    //var itemmodel = exchangeItemsDataToMe.ReadUInt32();
                    //var count = exchangeItemsDataToMe.ReadUInt16();

                    //var iteminfo = ItemInfos.GetById(itemmodel);
                    //if (iteminfo != null)
                    //{
                    //    Debug("exchange(getting): got item: {0} with count: {1}", iteminfo.Type, count);
                    //}
                    //else
                    //{
                    //    Debug("exchange(getting): could not find item with model: {0}\r\n{1}", itemmodel, String.Join(", ", exchangeItemsDataToMe.GetBytes().Select(b => b.ToString("X2"))));
                    //}

                    var firstFreeSlot = _bot.Inventory.FirstFreeSlot();
                    //Debug("exchange(getting): firstFreeSlot would be: {0}", firstFreeSlot);
                    //if (firstFreeSlot >= 0)
                    //{
                    //    var item = new InventoryItem((byte)firstFreeSlot, itemmodel, iteminfo, count);
                    //    Inventory.Add(item);
                    //}

                    var item = SROBot.Inventory.ParseItem(_exchangeItemsDataToMe, _bot, firstFreeSlot);
                    if (item != null)
                    {
                        _bot.Inventory.Add(item);
                        _bot.Log($"exchange(getting, {firstFreeSlot}): got item: {item.Iteminfo.Type} with count: {item.Count}");
                    }
                    else
                    {
                        _bot.Log("exchange(getting): error parsing packet..\r\n{0}", String.Join(", ", _exchangeItemsDataToMe.GetBytes().Select(b => b.ToString("X2"))));
                    }
                }
            }
            else
            {
                _bot.Log("exchange(getting): invalid items packet ..");
            }

            _bot.Log();

            if (_exchangeItemsDataFromMe != null)
            {
                var playerId = _exchangeItemsDataFromMe.ReadUInt32();
                int items = _exchangeItemsDataFromMe.ReadUInt8();
                var player = _bot.Spawns.Player.Get(playerId);

                if (player != null)
                {
                    _bot.Log("exchange(putting): put {0} items", items, player.Name);
                }
                else
                {
                    _bot.Log("exchange(putting): put {0} items", items, playerId);
                }

                while (items-- > 0)
                {
                    var slot = _exchangeItemsDataFromMe.ReadUInt8();
                    var exchangeSlot = _exchangeItemsDataFromMe.ReadUInt8();
                    var item = SROBot.Inventory.ParseItem(_exchangeItemsDataFromMe, _bot, slot);

                    if (item != null)
                    {
                        _bot.Inventory.Remove(slot);
                        _bot.Log("exchange(putting): remove item from slot: {0} -> {1}", item.Slot, item.Iteminfo.Type);
                    }
                    else
                    {
                        _bot.Log("exchange(putting): error parsing packet..");
                    }
                }
            }
            else
            {
                _bot.Log("exchange(putting): invalid items packet ..");
            }

            _bot.Inventory.RefreshInventoryViews();
            _player = null;
        }

        public void Canceled (Packet packet)
        {
            _bot.Log("exchange: canceled.");
            _exchangeItemsDataFromMe = null; // war nicht, wieso?
            _exchangeItemsDataToMe = null;
        }
    }
}
