using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public partial class Bot
    {
        public EventHandler<ulong> GoldAmountChanged;
        private void goldAmountChanged(ulong gold)
        {
            GoldAmountChanged?.Invoke(this, gold);
        }

        public EventHandler<String> CharSelected;
        private void charSelected(String charname)
        {
            CharSelected?.Invoke(this, charname);
        }

        public EventHandler Reconnected;
        private void reconnected()
        {
            Log("-- reconnected --");
            Reconnected?.Invoke(this, null);
        }

        public EventHandler ConnectedFirstTime;
        private void connectedFirstTime()
        {
            Log("-- connected first time --");
            ConnectedFirstTime?.Invoke(this, null);
        }

        public EventHandler Disconnected;
        private void disconnected()
        {
            var isARealDisconnect = ConnectionTimes.Any() && ConnectionTimes.Last().Type == ConnectionInfo.CONNECTION_TYPE.CONNECTED;
            if (isARealDisconnect)
            {
                Log("-- disconnected --");
            }
            else
            {
                Log("-- not REALLY disconnected? :) --");
            }
            
            Disconnected?.Invoke(this, null);

            if (isARealDisconnect)
            {
                try
                {
                    App.Current.Dispatcher.Invoke(() => ConnectionTimes.Add(new ConnectionInfo(ConnectionInfo.CONNECTION_TYPE.DISCONNECTED)));
                }
                catch { }
            }
        }
    }
}
