using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using log4net;

namespace sroBot.ConfigHandler
{
    class JsonConfiguration<T> : IConfiguration<T>
    {
        private static ILog log = LogManager.GetLogger(typeof(JsonConfiguration<T>));

        private String m_sConfigFile = "";

        public JsonConfiguration(String sFile)
        {
            m_sConfigFile = sFile;
        }

        public T Load()
        {
            if (!File.Exists(m_sConfigFile)) return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(m_sConfigFile), new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore });
            }
            catch (Exception ex)
            {
                log.ErrorFormat("JsonConfiguration<{0}>.Load(): {1} => {2}", typeof(T).Name, ex.Message, ex.StackTrace);
            }

            return default(T);
        }

        public bool Save(T config)
        {
            try
            {
                File.WriteAllText(m_sConfigFile, JsonConvert.SerializeObject(config, Formatting.Indented));
                return true;
            }
            catch (Exception ex) { log.ErrorFormat("JsonConfiguration<{0}>.Save(): {1} => {2}", typeof(T).Name, ex.Message, ex.StackTrace); }

            return false;
        }
    }
}
