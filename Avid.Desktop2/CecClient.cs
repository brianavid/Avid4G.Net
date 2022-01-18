using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using CecSharp;

namespace Avid.Desktop
{
    /// <summary>
    /// A class to use the Pulse-Eight CEC library and device to turn the screen 
    /// on and off and determine the screen state. 
    /// </summary>
    class CecClient: CecCallbackMethods
    {
        /// <summary>
        /// The one and only configuration of the library
        /// </summary>
        LibCECConfiguration CecConfig;

        /// <summary>
        /// The one and only CEC library instance
        /// </summary>
        LibCecSharp CecLib;

        /// <summary>
        /// Constructor
        /// </summary>
        public CecClient()
        {

            CecConfig = new LibCECConfiguration();
            CecConfig.DeviceTypes.Types[0] = CecDeviceType.RecordingDevice;
            CecConfig.DeviceName = "Avid TV Switch";
            CecConfig.ClientVersion = LibCECConfiguration.CurrentVersion;
            CecConfig.ActivateSource = false;
            CecConfig.SetCallbacks(this);
            
            CecLib = new LibCecSharp(CecConfig);
            CecLib.InitVideoStandalone();
        }

        /// <summary>
        /// Handler for commands from the TV - does nothing
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public override int ReceiveCommand(CecCommand command)
        {
            return 1;
        }

        /// <summary>
        /// Handler for commands from the remote control via the TV - does nothing
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public override int ReceiveKeypress(CecKeypress key)
        {
            return 1;
        }

        /// <summary>
        /// Handler for CEC library event logging - does nothing
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override int ReceiveLogMessage(CecLogMessage message)
        {
            //Console.WriteLine("{0}:   {1,16} {2}", message.Level, message.Time, message.Message);

            return 1;
        }

        /// <summary>
        /// Establish a connection to the CEC devices
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Connect(int timeout)
        {
            CecAdapter[] adapters = CecLib.FindAdapters(string.Empty);
            if (adapters.Length > 0)
                return Connect(adapters[0].ComPort, timeout);
            else
            {
                //Console.WriteLine("Did not find any CEC adapters");
                return false;
            }
        }

        /// <summary>
        /// Establish a connection to the CEC devices
        /// </summary>
        /// <param name="port"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Connect(string port, int timeout)
        {
            return CecLib.Open(port, timeout);
        }

        /// <summary>
        /// Close the connection to the CEC devices
        /// </summary>
        public void Close()
        {
            CecLib.Close();
        }

        /// <summary>
        /// Turn the TV screen on
        /// </summary>
        public void TvScreenOn()
        {
            CecLib.PowerOnDevices(CecLogicalAddress.Tv);
        }

        /// <summary>
        /// Turn the TV screen off (standby)
        /// </summary>
        public void TvScreenOff()
        {
            CecLib.StandbyDevices(CecLogicalAddress.Tv);
        }

        /// <summary>
        /// Is the TV screen on?
        /// </summary>
        /// <returns></returns>
        public bool TvScreenIsOn()
        {
            return CecLib.GetDevicePowerStatus(CecLogicalAddress.Tv) == CecPowerStatus.On;
        }

        /// <summary>
        /// Send a remote key press to the TV
        /// </summary>
        public bool TvSendKey(int keyNum)
        {
            var res = CecLib.SendKeypress(CecLogicalAddress.Tv, (CecUserControlCode)keyNum, true);
            //CecLib.SendKeyRelease(CecLogicalAddress.Tv, true);
            return res;
        }

        /// <summary>
        /// Select an HDMI input port
        /// </summary>
        public void TvSelectHdmi(byte port)
        {
            CecLib.SetHDMIPort(CecLogicalAddress.Tv, port);
        }

    }
}
