using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public static class Render
{
    public static string Script(
        string scriptName,
        HttpRequestBase request)
    {
        return string.Format("<script src='/Scripts/{0}.js?x={1}' type='text/javascript'></script>",
            scriptName, (new System.IO.FileInfo(request.PhysicalApplicationPath + "\\Scripts\\" + scriptName + ".js")).LastWriteTime.ToString("HHmmss"));
    }
}
