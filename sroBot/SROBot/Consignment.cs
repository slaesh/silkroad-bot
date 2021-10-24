using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public class Consignment
    {
        public enum CONSIGMENT_STATUS
        {
            SEARCHING = 0,
            COMPLETE = 1,
            ERROR = 2,
            IDLE = 3
        }

        public class ConsignmentItem
        {
            public enum CONSIG_ITEM_STATE
            {
                RUNNING = 0,
                SOLD = 1,
                EXPIRED = 2
            }

            public UInt32 ConsigId { get; set; }
            public string Player { get; set; }
            public CONSIG_ITEM_STATE State { get; set; }
            public UInt32 Model { get; set; }
            public UInt16 Count { get; set; }
            public UInt64 Price { get; set; }
            public UInt64 Deposit { get; set; }
            public UInt64 Comission { get; set; }
            public UInt32 Seconds { get; set; }

            public DateTime ExpiringAt { get; set; }
            public ItemInfo Item { get; set; }
            public byte Page { get; set; }
            public bool Running => State == CONSIG_ITEM_STATE.RUNNING;
            public bool Sold => State == CONSIG_ITEM_STATE.SOLD;
            public bool Expired => State == CONSIG_ITEM_STATE.EXPIRED;
            public bool MyItem { private set; get; }

            private ConsignmentItem() { }

            public static ConsignmentItem Create(UInt32 consigId, CONSIG_ITEM_STATE state, UInt32 itemModel, UInt16 count)
            {
                return new ConsignmentItem
                {
                    ConsigId = consigId,
                    State = state,
                    Model = itemModel,
                    Item = ItemInfos.GetById(itemModel),
                    Count = count,
                    MyItem = true
                };
            }

            public static ConsignmentItem ParseMyItem(Packet packet, Bot bot = null)
            {
                var ci = new ConsignmentItem();

                ci.ConsigId = packet.ReadUInt32();
                ci.State = (CONSIG_ITEM_STATE)packet.ReadUInt8();
                ci.Model = packet.ReadUInt32();
                ci.Item = ItemInfos.GetById(ci.Model);
                ci.Count = packet.ReadUInt16();
                packet.ReadUInt8Array(2);
                ci.Price = packet.ReadUInt64();
                ci.Deposit = packet.ReadUInt64();
                ci.Comission = packet.ReadUInt64();
                ci.Seconds = packet.ReadUInt32();
                ci.ExpiringAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ci.Seconds);

                bot?.Debug($"consignment({ci.ConsigId}): {ci.Item?.Type ?? "-UNKNOWN-"} .. COUNT: {ci.Count} -- {(ci.Expired ? "expired" : "sold")} = {(ci.Expired ? "YES" : ci.Sold ? "YES" : "NO")} -- price: {ci.Price:N0} // EXPIRING: {ci.ExpiringAt.ToString("dd.MM.yy HH:mm:ss")}");

                return ci;
            }

            public static ConsignmentItem ParseSearchResultItem(Packet packet, byte page)
            {
                var ci = new ConsignmentItem();
                
                ci.ConsigId = packet.ReadUInt32();
                ci.Player = packet.ReadAscii();
                ci.State = (CONSIG_ITEM_STATE)packet.ReadUInt8();
                ci.Model = packet.ReadUInt32();
                ci.Item = ItemInfos.GetById(ci.Model);
                ci.Count = packet.ReadUInt16();
                packet.ReadUInt8Array(2);
                ci.Price = packet.ReadUInt64();
                ci.Seconds = packet.ReadUInt32();

                ci.ExpiringAt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ci.Seconds);
                ci.Page = page;

                return ci;
            }
        }

        private Bot _bot;
        private byte _cmd = 1;
        private byte _page;
        private byte _tIdGroup;
        private byte _degree;
        private List<ConsignmentItem> _items = new List<ConsignmentItem>();
        private Action<IEnumerable<ConsignmentItem>> _cb;

        public bool SkipFromClient = false;
        public CONSIGMENT_STATUS Status = CONSIGMENT_STATUS.IDLE;

        public Consignment(Bot bot)
        {
            _bot = bot;
        }

        public void Search(byte tIdGroup, byte degree, Action<IEnumerable<ConsignmentItem>> callback = null)
        {
            _tIdGroup = tIdGroup;
            _degree = degree;

            _page = 0;
            SkipFromClient = true;
            Status = CONSIGMENT_STATUS.SEARCHING;
            _items.Clear();
            _cb = callback;

            _bot.Debug($"consignment-search(): CMD: {_cmd}, tIdGroup: {_tIdGroup}, degree: {_degree}");
            Actions.SearchConsignment(_bot, _cmd, _tIdGroup, _degree);

            _cmd = 2;
        }

        private void getNextPage()
        {
            ++_page;
            SkipFromClient = true;

            _bot.Debug($"consignment-page(): CMD: 3, tIdGroup: {_tIdGroup}, degree: {_degree}, page: {_page}");
            Actions.SearchConsignment(_bot, 3 /* paging */, _tIdGroup, _degree, _page);
        }

        public IEnumerable<ConsignmentItem> GetItems()
        {
            return _items.ToArray();
        }

        private void Finish(CONSIGMENT_STATUS state)
        {
            Status = state;

            _cb?.Invoke(GetItems());

            // NEED TO BE DELAYED !!
            // .. cause this packet should be skipped too !

            new Thread(() =>
            {
                Thread.Sleep(1000);

                SkipFromClient = false;

            }).Start();
        }

        public void HandlePacket(Packet packet)
        {
            var success = packet.ReadUInt8() == 1;
            if (!success)
            {
                _bot.Log("handleConsignmentSearch(): success != 1");

                Finish(CONSIGMENT_STATUS.ERROR);

                return;
            }

            var itemCnt = packet.ReadUInt8();
            var pageCnt = packet.ReadUInt8();

            while (itemCnt-- > 0)
            {
                var consigItem = ConsignmentItem.ParseSearchResultItem(packet, _page);
                
                _items.Add(consigItem);

                _bot.Debug($"consignment-search({_page + 1}/{pageCnt}): {consigItem.Item?.Type ?? "-UNKNOWN-"} .. Player: {consigItem.Player} -- price: {consigItem.Price:N0} -- {consigItem.Count} // EXPIRING: {consigItem.ExpiringAt.ToString("dd.MM.yy HH:mm:ss")}");
            }

            // this was the last page..
            if (_page >= pageCnt - 1)
            {
                Finish(CONSIGMENT_STATUS.COMPLETE);

                return;
            }

            // .. otherwise get the next page ! :)
            getNextPage();
        }
    }
}
