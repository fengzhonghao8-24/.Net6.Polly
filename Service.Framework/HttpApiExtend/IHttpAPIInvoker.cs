using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Framework.HttpApiExtend
{
    public interface IHttpAPIInvoker
    {
        string InvokeApi(string url);
    }
}
