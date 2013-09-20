using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;

/// <summary>
/// Summary description for Receiver
/// </summary>
public static class Receiver
{
    static string Url = null;

    static XDocument GetXml(
        string body)
    {
        if (Url == null)
        {
            if (Config.ReceiverAddress != null)
            {
                Url = "http://" + Config.ReceiverAddress + "/YamahaRemoteControl/ctrl";
            }
            else
            {
                return null;
            }
        }

        Uri requestUri = new Uri(Url);

        HttpWebRequest request =
            (HttpWebRequest)HttpWebRequest.Create(requestUri);
        request.Method = WebRequestMethods.Http.Post;
        StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), Encoding.UTF8);
        requestWriter.Write(body);
        requestWriter.Close();

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        XDocument xDoc =
            XDocument.Load(new StreamReader(response.GetResponseStream()));

        return xDoc;
    }

    static int volumeLevel;
    static bool volumeMute = false;
    static bool mainZone;
    static bool switchedOn;

    static string MainZoneInput { get; set; }

    static string ZoneName { get { return mainZone ? "Main_Zone" : "Zone_2"; } }

    public static string SelectedInput { get; private set; }
    public static string SelectedOutput { get; private set; }
    public static int SelectedOutputIndex { get; private set; }
    public static string VolumeDisplay { get { return !switchedOn ? "Off" : String.Format(volumeMute ? "({0}%)" : "{0}%", volumeLevel); } }

    public static void Initialize()
    {
        XDocument state = GetXml(String.Format(
            "<YAMAHA_AV cmd=\"GET\"><{0}><Basic_Status>GetParam</Basic_Status></{0}></YAMAHA_AV>",
            "Main_Zone"));

        if (state != null)
        {
            var basicStatus = state.Element("YAMAHA_AV").Element("Main_Zone").Element("Basic_Status");
            string powerString = basicStatus.Element("Power_Control").Element("Power").Value;

            switchedOn = powerString == "On";

            MainZoneInput = basicStatus.Element("Input").Element("Input_Sel").Value;
        }

        if (switchedOn && MainZoneInput == "HDMI3")
        {
            SelectSkyInput();
        }
        else
        {
            SelectComputerInput();
        }
    }

    public static void GetState()
    {
        if (switchedOn)
        {
            XDocument state = GetXml(String.Format(
                "<YAMAHA_AV cmd=\"GET\"><{0}><Basic_Status>GetParam</Basic_Status></{0}></YAMAHA_AV>",
                ZoneName));

            if (state == null)
            {
                return;
            }

            var basicStatus = state.Element("YAMAHA_AV").Element(ZoneName).Element("Basic_Status");
            string volumeString = basicStatus.Element("Volume").Element("Lvl").Element("Val").Value;

            volumeLevel = (Convert.ToInt32(volumeString) + 850) / 10;
            //volumeMute = basicStatus.Element("Volume").Element("Mute").Value == "On";

            if (mainZone)
            {
                var audioProgram = basicStatus.Element("Surround").Element("Program_Sel").Element("Current");
                if (audioProgram.Element("Straight").Value == "On")
                {
                    SelectedOutputIndex = 1;
                }
                else
                {
                    switch (audioProgram.Element("Sound_Program").Value)
                    {
                        case "2ch Stereo":
                            SelectedOutputIndex = 2;
                            break;
                        case "7ch Stereo":
                            SelectedOutputIndex = 3;
                            break;
                        case "Surround Decoder":
                            SelectedOutputIndex = 4;
                            break;
                        case "Drama":
                            SelectedOutputIndex = 5;
                            break;
                    }
                }
            }

            if (mainZone)
            {
                if (string.IsNullOrEmpty(MainZoneInput))
                {
                    MainZoneInput = basicStatus.Element("Input").Element("Input_Sel").Value;
                }
                if (MainZoneInput != basicStatus.Element("Input").Element("Input_Sel").Value)
                {
                    if (SelectedInput == "Sky")
                    {
                        SelectSkyInput();
                    }
                    else
                    {
                        SelectComputerInput();
                    }
                    SetMute(volumeMute);
                }
            }
        }
    }

    public static void SelectComputerInput()
    {
        SelectedInput = "Computer";
        MainZoneInput = "HDMI2";
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Input><Input_Sel>HDMI2</Input_Sel></Input></Main_Zone></YAMAHA_AV>");
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Input><Input_Sel>AV6</Input_Sel></Input></Zone_2></YAMAHA_AV>");
    }

    public static void SelectSkyInput()
    {
        SelectedInput = "Sky";
        MainZoneInput = "HDMI3";
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Input><Input_Sel>HDMI3</Input_Sel></Input></Main_Zone></YAMAHA_AV>");
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Input><Input_Sel>AV5</Input_Sel></Input></Zone_2></YAMAHA_AV>");
    }

    public static void ReselectInput()
    {
        if (!string.IsNullOrEmpty(MainZoneInput))
        {
            GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Input><Input_Sel>" + MainZoneInput + "</Input_Sel></Input></Main_Zone></YAMAHA_AV>");
        }
    }

    public static void SelectTVOutput(string menuIndex = null, bool unmute = true)
    {
        if (!switchedOn || SelectedOutput != "TV")
        {
            MainZoneOn();
            ReselectInput();
            Zone2Off();
        }

        SelectedOutput = "TV";
        SelectedOutputIndex = menuIndex == null ? 1 : Convert.ToInt32(menuIndex);
        mainZone = true;
        switchedOn = true;

        switch (SelectedOutputIndex)
        {
            default:
                break;
            case 1:
                GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Surround><Program_Sel><Current><Straight>On</Straight></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>");
                break;
            case 2:
                GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Surround><Program_Sel><Current><Straight>Off</Straight><Sound_Program>2ch Stereo</Sound_Program></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>");
                break;
            case 3:
                GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Surround><Program_Sel><Current><Straight>Off</Straight><Sound_Program>7ch Stereo</Sound_Program></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>");
                break;
            case 4:
                GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Surround><Program_Sel><Current><Straight>Off</Straight><Sound_Program>Surround Decoder</Sound_Program></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>");
                break;
            case 5:
                GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Surround><Program_Sel><Current><Straight>Off</Straight><Sound_Program>Drama</Sound_Program></Current></Program_Sel></Surround></Main_Zone></YAMAHA_AV>");
                break;
        }

        if (volumeMute && unmute)
        {
            ToggleMute();
        }

        GetState();
    }

    public static void SelectRoomsOutput()
    {
        if (!switchedOn || SelectedOutput != "Rooms")
        {
            Zone2On();
            MainZoneOff();
        }

        SelectedOutput = "Rooms";
        SelectedOutputIndex = 0;
        mainZone = false;
        switchedOn = true;

        if (volumeMute)
        {
            ToggleMute();
        }

        GetState();
    }

    public static void TurnOff()
    {
        MainZoneOff();
        Zone2Off();

        switchedOn = false;
    }

    public static void IncreaseVolume()
    {
        volumeLevel++;
        SetVolume();
    }

    public static void DecreaseVolume()
    {
        volumeLevel--;
        SetVolume();
    }

    static void SetVolume()
    {
        ReselectInput();
        GetXml(String.Format(
            "<YAMAHA_AV cmd=\"PUT\"><{0}><Volume><Lvl><Val>{1}</Val><Exp>1</Exp><Unit>dB</Unit></Lvl></Volume></{0}></YAMAHA_AV>",
            ZoneName,
            volumeLevel * 10 - 850));
    }

    public static void ToggleMute()
    {
        SetMute(!volumeMute);
    }

    public static void SetMute(
        bool muted)
    {
        ReselectInput();
        volumeMute = muted;
        GetXml(String.Format(
            "<YAMAHA_AV cmd=\"PUT\"><{0}><Volume><Mute>{1}</Mute></Volume></{0}></YAMAHA_AV>",
            ZoneName,
            volumeMute ? "On" : "Off"));
    }

    static void MainZoneOff()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Power_Control><Power>Standby</Power></Power_Control></Main_Zone></YAMAHA_AV>");
    }

    static void Zone2Off()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Power_Control><Power>Standby</Power></Power_Control></Zone_2></YAMAHA_AV>");
    }

    static void MainZoneOn()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Main_Zone><Power_Control><Power>On</Power></Power_Control></Main_Zone></YAMAHA_AV>");
    }

    static void Zone2On()
    {
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Power_Control><Power>On</Power></Power_Control></Zone_2></YAMAHA_AV>");
        GetXml("<YAMAHA_AV cmd=\"PUT\"><Zone_2><Input><Input_Sel>" + ((SelectedInput == "Computer") ? "AV6" : "AV5") + "</Input_Sel></Input></Zone_2></YAMAHA_AV>");
    }

    public static bool IsOn()
    {
        return switchedOn;
    }
}