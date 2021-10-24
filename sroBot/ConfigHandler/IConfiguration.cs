using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.ConfigHandler
{
    interface IConfiguration<T>
    {
        T Load();
        bool Save (T config);
    }
}
