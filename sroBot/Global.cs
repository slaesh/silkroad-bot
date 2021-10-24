using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot
{
    public static class Global
    {
        public static Random Random = new Random();

        public static T Copy<T> (this T me)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(me));
        }
    }
}
