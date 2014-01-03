using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Class to render the inclusion of a JavaScript file such that the file is cached only until the source file changes
/// </summary>
/// <remarks>
/// This copes with the development cycle when the JavaScript file may change at any time, while still benefiting
/// from the caching of an unchanged file in normal operation.
/// </remarks>
public static class Render
{
    /// <summary>
    /// Generate a script tag for a local URL of the specified script name, which will be in the "Scripts" directory.
    /// Append an unused argument whose value is the timestamp of the real source file for the script URL, so that 
    /// changes in the source file result in different (separately cached) URLs.
    /// </summary>
    /// <param name="scriptName"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string Script(
        string scriptName,
        HttpRequestBase request)
    {
        return string.Format("<script src='/Scripts/{0}.js?x={1}' type='text/javascript'></script>",
            scriptName, (new System.IO.FileInfo(request.PhysicalApplicationPath + "\\Scripts\\" + scriptName + ".js")).LastWriteTime.ToString("HHmmss"));
    }
}
