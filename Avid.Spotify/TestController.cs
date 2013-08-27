using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;

namespace Avid.Spotify
{
    public class TestController : ApiController
    {
        [HttpGet]
        public int Test()
        {
            return 0;
        }
    }
}
