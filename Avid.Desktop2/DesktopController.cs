using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Http;
using NLog;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.ServiceProcess;
using GetCoreTempInfoNET;
using WindowsInput;

namespace Avid.Desktop
{
    /// <summary>
    /// A class to encapsulate the control of a single desktop application process
    /// </summary>
    class DesktopProcess
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetActiveWindow(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="args"></param>
        /// <param name="mode"></param>
        public DesktopProcess(
            string name,
            string path,
            string args,
            string mode)
        {
            Name = name;
            Path = path;
            Args = args;

            //  Create, but do not yet run, the process
            p = new Process();
            p.StartInfo.FileName = path;
            p.StartInfo.WindowStyle = mode == null || mode == "Maximize" ? ProcessWindowStyle.Maximized : ProcessWindowStyle.Normal;
        }

        /// <summary>
        /// The Avid name of the process
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The path of the process Exe file
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// The arguments with which the process is run
        /// </summary>
        public string Args { get; private set; }

        /// <summary>
        /// Is the process currently running?
        /// </summary>
        public bool Running { get { return running && !p.HasExited; } }

        /// <summary>
        /// The process being controlled
        /// </summary>
        public Process p { get; private set; }

        /// <summary>
        /// Is the process running?
        /// </summary>
        bool running;

        /// <summary>
        /// Process name
        /// </summary>
        string processName;

        /// <summary>
        /// Discover the process if it may have re-created itself - not under our direct control
        /// </summary>
        void ReloadProcess()
        {
            if (running && p.HasExited)
            {
                Process[] runningProcesses = Process.GetProcessesByName(processName);
                if (runningProcesses != null && runningProcesses.Length != 0)
                {
                    Trace.WriteLine(String.Format("- Service '{0}' changed process ID {1} to {2}", processName, p.Id, runningProcesses[0].Id));
                    p = runningProcesses[0];
                }
            }
        }

        /// <summary>
        /// Start the process running
        /// </summary>
        /// <param name="args">Optional arguments string</param>
        /// <returns></returns>
        public bool Start(
            string args)
        {
            p.StartInfo.Arguments = (args != null) ? args : Args;

            if (!p.Start())
            {
                return false;
            }

            processName = p.ProcessName;
            Trace.WriteLine(String.Format("- Service '{0}' started ID {1}", processName, p.Id));

            running = true;

            //  Bring the newly created process to the foreground
            return p.WaitForInputIdle() && Foreground();
        }

        /// <summary>
        /// Bring the process to the foreground
        /// </summary>
        /// <returns></returns>
        public bool Foreground()
        {
            //  In case the process has re-created itself
            ReloadProcess();

            if (!running || p.HasExited)
            {
                running = false;
                return false;
            }

            SetForegroundWindow(p.MainWindowHandle);
            SendMessage(p.MainWindowHandle, 0x0112, 0xF030, 0);

            Trace.WriteLine(String.Format("- Service '{0}' foreground ID {1}", processName, p.Id));
            return true;
        }

        /// <summary>
        /// Exit the process; Normally we will do this by communicating with the application internally
        /// </summary>
        /// <returns></returns>
        public bool Exit()
        {
            //  In case the process has re-created itself
            ReloadProcess();

            if (!running || p.HasExited)
            {
                Trace.WriteLine(String.Format("- Service '{0}' not running", processName));
                running = false;
                return true;
            }

            //  Close the process's main windows and spin-wait for five seconds for the process to die
            p.CloseMainWindow();
            for (int i = 0; i < 50; i++)
            {
                if (p.HasExited)
                {
                    running = false;
                    return true;
                }

                System.Threading.Thread.Sleep(100);
            }

            //  If is still not dead, peremptorily kill it
            p.Kill();

            Trace.WriteLine(String.Format("- Service '{0}' stopped", processName));
            running = false;
            return true;
        }
    }

    /// <summary>
    /// Web API Controller, with public HttpGet web methods for Controlling the NAudio player
    /// </summary>
    public class DesktopController : ApiController
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        static Process spotifyProcess = null;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, Int32 dx, Int32 dy, uint dwData,
          int dwExtraInfo);

        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        /// <summary>
        /// The processes that we have launched
        /// </summary>
        static Dictionary<string, DesktopProcess> processes = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public DesktopController()
        {
            Trace.WriteLine(String.Format("New DesktopController"));

            //  The definition of the process that we can launch is in the AvidConfig.xml file.
            //  Each defines the path and arguments for a named process
            XDocument doc = XDocument.Load(@"C:\Avid.Net\AvidConfig.xml");

            if (processes == null)
            {
                processes = new Dictionary<string, DesktopProcess>();
                foreach (var program in doc.Root.Element("Programs").Elements("Program"))
                {
                    string name = program.Attribute("name").Value;
                    string path = program.Attribute("path").Value;
                    string args = program.Attribute("args") == null ? "" : program.Attribute("args").Value;
                    string mode = program.Attribute("mode") == null ? null : program.Attribute("mode").Value;
                    processes[name] = new DesktopProcess(name, path, args, mode);
                    Trace.WriteLine(String.Format("Service '{0}': {1}", name, System.IO.File.Exists(path) ? "OK" : "Not Found"));
                }
            }
        }

        [HttpGet]
        public int Test()
        {
            logger.Info("Test");
            return 99;
        }

        /// <summary>
        /// Launch the named application at the path defined in AvidConfig, 
        /// either with provided arguments or those defined in AvidConfig
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpGet]
        public bool LaunchProgram(
            string name,
            string args)
        {
            try
            {
	            bool result;
	            logger.Info("LaunchProgram {0} '{1}'", name, args == null ? "" : args);
	
	            if (!processes.ContainsKey(name))
	            {
	                logger.Info("\tUnknown {0}", name);
	                return false;
	            }
	
	            DesktopProcess process = processes[name];
	
	            //  If the process is already running and no new arguments are specified, simply bring it to the foreground
	            //  Otherwise if it is already running with different arguments, exit the existing process instance
	            if (process.Running)
	            {
	                if (!String.IsNullOrEmpty(args))
	                {
	                    result = process.Exit();
	                    logger.Info("\tExit {0} {1}", name, result ? "OK" : "Fail");
	                }
	                else
	                {
	                    result = process.Foreground();
	                    logger.Info("\tForeground {0} {1}", name, result ? "OK" : "Fail");
	                    return result;
	                }
	            }
	
	            //  Stop all other desktop applications - only one will run at at a time
	            foreach (var otherProcess in processes.Values)
	            {
	                if (otherProcess.Name != name && otherProcess.Running)
	                {
                        DvbViewerMonitor.NothingToMonitor();
                        result = otherProcess.Exit();
	                    logger.Info("\tExit {0} {1}", otherProcess.Name, result ? "OK" : "Fail");
	                }
	            }
	
	            result = process.Start(args);
	            logger.Info("\tStart {0} {1}", name, result ? "OK" : "Fail");

                if (name == "TV")
                {
                    DvbViewerMonitor.StartMonitoring();
                }

	            return result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Launch a new instance of the named program with specified arguments
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [HttpGet]
        public bool LaunchNewProgram(
            string name,
            string args)
        {
            try
            {
	            logger.Info("LaunchNewProgram {0} '{1}'", name, args);
	
	            if (!processes.ContainsKey(name))
	            {
	                logger.Info("\tUnknown {0}", name);
	                return false;
	            }
	
	            DesktopProcess process = processes[name];
	
	            ProcessStartInfo startInfo = new ProcessStartInfo();
	            startInfo.FileName = process.Path;
	            startInfo.Arguments = args;
	            startInfo.WindowStyle = ProcessWindowStyle.Maximized;
	
	            Process p = Process.Start(startInfo);
	            if (p != null)
	            {
	                p.WaitForInputIdle();
	            }
	
	            logger.Info("\tStart {0} {1}", name, p != null ? "OK" : "Fail");
	            return p != null;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Exit a named program we have launched if it is still running
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        public bool ExitProgram(
            string name)
        {
            try
            {
	            bool result;
	            logger.Info("ExitProgram {0}", name);
	
	            if (!processes.ContainsKey(name))
	            {
	                logger.Info("\tUnknown {0}", name);
	                return false;
	            }
	
	            DesktopProcess process = processes[name];
	
	            if (!process.Running)
	            {
	                logger.Info("\tStopped {0}", name);
	                return true;
	            }
	
	            result = process.Exit();
	            logger.Info("\tExit {0} {1}", name, result ? "OK" : "Fail");


                DvbViewerMonitor.NothingToMonitor();
                return result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Exits all running programs we have launched
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public bool ExitAllPrograms()
        {
            try
            {
	            bool result;
	            logger.Info("ExitAllPrograms");
	
	            foreach (var process in processes.Values)
	            {
	                if (process.Running)
	                {
	                    result = process.Exit();
	                    logger.Info("\tExit {0} {1}", process.Name, result ? "OK" : "Fail");
	                }
	            }

                DvbViewerMonitor.NothingToMonitor();

	            return true;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Bring the named program to the foreground if it is running
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        public bool ForegroundProgram(
            string name)
        {
            try
            {
	            bool result;
	            logger.Info("ForegroundProgram {0}", name);
	
	            if (!processes.ContainsKey(name))
	            {
	                logger.Info("\tUnknown {0}", name);
	                return false;
	            }
	
	            DesktopProcess process = processes[name];
	
	            if (!process.Running)
	            {
	                logger.Info("\tStopped {0}", name);
	                return false;
	            }
	
	            result = process.Foreground();
	            logger.Info("\tForeground {0} {1}", name, result ? "OK" : "Fail");
	            return result;
            }
            catch (System.Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Send an emulated keyboard sequence of key presses to the foreground application
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        [HttpGet]
        public bool SendKeys(
            string keys)
        {
            try
            {
	            Trace.WriteLine(String.Format("{0} SendKeys {1}", DateTime.Now.ToLongTimeString(), keys));
	            System.Windows.Forms.SendKeys.SendWait(keys);
	            return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Move the mouse cursor on screen by a relative amount
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        [HttpGet]
        public bool MouseMoveRelative(
            int dx,
            int dy)
        {
            try
            {
	            Trace.WriteLine(String.Format("{0} Mouse move {1},{2}", DateTime.Now.ToLongTimeString(), dx, dy));
	            mouse_event((uint)(MouseEventFlags.MOVE), dx, dy, 0, 0);
	            return false;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Send an emulated mouse click at the current cursor location
        /// </summary>
        /// <param name="rightButton">True if an emulated right mouse click; otherwise a left mouse click</param>
        /// <returns></returns>
        [HttpGet]
        public bool MouseClick(
            bool rightButton)
        {
            try
            {
	            Trace.WriteLine(String.Format("{0} Mouse click {1}", DateTime.Now.ToLongTimeString(), rightButton ? "right" : "left"));
	            mouse_event((uint)(rightButton ? MouseEventFlags.RIGHTDOWN : MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
	            mouse_event((uint)(rightButton ? MouseEventFlags.RIGHTUP : MouseEventFlags.LEFTUP), 0, 0, 0, 0);
	            return false;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Send an IR code through the USB IIRT transmitter
        /// </summary>
        /// <param name="irCode"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        [HttpGet]
        public bool SendIR(
            string irCode,
            string description)
        {
            try
            {
	            Trace.WriteLine(String.Format("{0} Send IR {1}", DateTime.Now.ToLongTimeString(), description));
	            return UsbService.SendIR(irCode);
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Send special keys to the desktop
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        [HttpGet]
        public bool SendSpecialkey(
            string keyName)
        {
            try
            {
	            Trace.WriteLine(String.Format("{0} SendSpecialkey {1}", DateTime.Now.ToLongTimeString(), keyName));
	            switch (keyName)
	            {
	                default:
	                    return false;
	                case "ClearDesktop":
	                    //  Minimize everything on the desktop - Windows-M
	                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_M);
	                    return true;
	                case "ContextMenu":
	                    //  Press the "context menu" key
	                    InputSimulator.SimulateKeyPress(VirtualKeyCode.APPS);
	                    return true;
	            }
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Fetch CPU and GPU temperature and load statistics as XML
        /// </summary>
        /// <returns></returns> 
        [HttpGet]
        public string FetchCoreTempInfoXml()
        {
            try
            {
	            Trace.WriteLine(String.Format("{0} Fetch Core Temp Info", DateTime.Now.ToLongTimeString()));
	            //Initiate CoreTempInfo class.
	            CoreTempInfo CTInfo = new CoreTempInfo();
	
	            if (!CTInfo.GetData())
	            {
	                Trace.WriteLine(String.Format("\tNo CPU Data"));
	                return null;
	            }
	
	            XDocument result = new XDocument(new XElement("CoreTemp",
	                                                new XAttribute("count", CTInfo.GetCoreCount * CTInfo.GetCPUCount),
	                                                new XAttribute("tjmax", CTInfo.GetTjMax[0]),
	                                                new XAttribute("units", CTInfo.IsFahrenheit ? "F" : "C")));
	
	            for (int i = 0; i < CTInfo.GetCoreCount * CTInfo.GetCPUCount; i++)
	            {
	                result.Root.Add(new XElement("Core",
	                                             new XAttribute("load", CTInfo.GetCoreLoad[i]),
	                                             new XAttribute("temp", CTInfo.GetTemp[i])));
	                Trace.WriteLine(String.Format("\tCore {0}: temp {1}; load {2}", i, CTInfo.GetCoreLoad[i], CTInfo.GetTemp[i]));
	            }
	
	            var gpuz = new GPUZ.GPUZ();
	
	            if (gpuz.OpenView())
	            {
	                var data = gpuz.GetData();
	
	                var gpu = new XElement("GPU");
	                result.Root.Add(gpu);
	                foreach (var sensor in data.sensors)
	                {
	                    switch (sensor.name)
	                    {
	                        case "":
	                            break;
	                        case "GPU Load":
	                            Trace.WriteLine(String.Format("\t{0} = {1}", sensor.name, sensor.value));
	                            gpu.Add(new XAttribute("load", Convert.ToInt32(sensor.value)));
	                            continue;
	                        case "GPU Temperature":
	                            Trace.WriteLine(String.Format("\t{0} = {1}", sensor.name, sensor.value));
	                            gpu.Add(new XAttribute("temp", Convert.ToInt32(sensor.value)));
	                            continue;
	                        default:
	                            continue;
	                    }
	                }
	                gpuz.CloseView();
	            }
	            else
	            {
	                Trace.WriteLine(String.Format("\tNo GPU Data"));
	            }
	
	            return result.ToString();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Ensure that the RemotePotato service is running and has not died, starting the service if it is not running
        /// </summary>
        /// <param name="recycle">If true; unconditionally stops and restarts the service</param>
        /// <returns>True if the service is now running</returns>
        [HttpGet]
        public bool EnsureRemotePotatoRunning(
            bool recycle)
        {
            logger.Info("EnsureRemotePotatoRunning");

            try
            {
                const int timeoutMilliseconds = 5000;
                ServiceController service = new ServiceController("Remote Potato Service");
                int millisec1 = Environment.TickCount;
                TimeSpan timeout;
                if (recycle && service.Status == ServiceControllerStatus.Running)
                {
                    timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    logger.Info("Stopped RemotePotato");
                }

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    logger.Info("Started RemotePotato");
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Ensure that the Spotify Player is running and has not died
        /// </summary>
        /// <returns>True if the player is now running</returns>
        [HttpGet]
        public bool EnsureSpotifyRunning()
        {
            try
            {
                if (spotifyProcess == null || spotifyProcess.HasExited)
                {
                    logger.Info("EnsureSpotifyRunning");

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @"C:\Avid.Net\Avid.Spotify.exe";
                    startInfo.Arguments = "";
                    startInfo.WindowStyle = ProcessWindowStyle.Minimized;

                    spotifyProcess = Process.Start(startInfo);
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }

    }
}
