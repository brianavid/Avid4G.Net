using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using NLog;

public static class Roku
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The HTTP Url of the Roku service
    /// </summary>
    public static string Url
    {
        get { return "http://" + Config.RokuAddress + ":8060/"; }
    }

    /// <summary>
    /// Send an HTTP GET request to the Roku service, expecting an XML response, which is returned
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    static XDocument GetXml(
        string url)
    {
        try
        {
            Uri requestUri = new Uri(Url + url);

            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Method = WebRequestMethods.Http.Get;
            request.ContentType = "text/xml";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            XDocument xDoc =
                XDocument.Load(new StreamReader(response.GetResponseStream()));

            return xDoc;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Send an HTTP POST request to the Roku service with no body, expecting no response
    /// </summary>
    /// <param name="url"></param>
    /// <param name="paramValue"></param>
    /// <returns></returns>
    static void PostRequest(
        string url,
        string paramValue)
    {
        try
        {
            Uri requestUri = new Uri(Url + url + (paramValue ?? ""));

            HttpWebRequest request =
                (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Method = WebRequestMethods.Http.Post;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        }
        catch
        {
        }
    }

    public static Dictionary<string, string> Apps
    {
        get
        {
            var xApps = GetXml("query/apps");
            if (xApps == null)
            {
                return null;
            }

            return xApps.Element("apps").Elements("app").ToDictionary(x => x.Attribute("id").Value, x => x.Value);
        }
    }

    public static void RunApp(
        string appId,
        Dictionary<string,string> args = null)
    {
        if (args != null && args.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var arg in args)
            {
                sb.AppendFormat("&{0}={1}", arg.Key, HttpUtility.UrlEncode(arg.Value));
            }

            appId += "?" + sb.ToString().Substring(1);

        }

        PostRequest("launch/", appId);
    }

    public static void KeyDown(
        string key)
    {
        PostRequest("keydown/", key);
    }

    public static void KeyUp(
        string key)
    {
        PostRequest("keyup/", key);
    }

    public static void KeyPress(
        string key)
    {
        PostRequest("keypress/", key);
    }

    public static void SendText(
        string text)
    {
        foreach (var c in text)
        {
            PostRequest("keypress/", "Lit_" + HttpUtility.UrlEncode(c.ToString()));
        }
        PostRequest("keypress/", "Enter");
    }

    public static string AppImageUrl(
        string id)
    {
        return Url + "query/icon/" + id;
    }

}