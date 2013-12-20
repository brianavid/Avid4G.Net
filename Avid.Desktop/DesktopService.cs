using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.ServiceModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.ServiceProcess;
using WindowsInput;
using GetCoreTempInfoNET;

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
        /// The Avis name of the process
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
    /// DesktopService is a WCF service implementing IDesktopService for a collection of disparate methods that
    /// must be run in the context of a desktop
    /// </summary>
    [ServiceBehavior(IncludeExceptionDetailInFaults=true)]
    class DesktopService : IDesktopService
    {
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
        public DesktopService()
        {
            Trace.WriteLine(String.Format("New DesktopService"));

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

        /// <summary>
        /// Launch the named application at the path defined in AvidConfig, 
        /// either with provided arguments or those defined in AvidConfig
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool LaunchProgram(
            string name, 
            string args)
        {
            bool result;
            Trace.WriteLine(String.Format("{0} LaunchProgram {1} '{2}'", DateTime.Now.ToLongTimeString(), name, args == null ? "" : args));

            if (!processes.ContainsKey(name))
            {
                Trace.WriteLine(String.Format("\tUnknown {0}", name));
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
                    Trace.WriteLine(String.Format("\tExit {0} {1}", name, result ? "OK" : "Fail"));
                }
                else
                {
                    result = process.Foreground();
                    Trace.WriteLine(String.Format("\tForeground {0} {1}", name, result ? "OK" : "Fail"));
                    return result;
                }
            }

            //  Stop all other desktop applications - only one will run at at a time
            foreach (var otherProcess in processes.Values)
            {
                if (otherProcess.Name != name && otherProcess.Running)
                {
                    result = otherProcess.Exit();
                    Trace.WriteLine(String.Format("\tExit {0} {1}", otherProcess.Name, result ? "OK" : "Fail"));
                }
            }

            result = process.Start(args);
            Trace.WriteLine(String.Format("\tStart {0} {1}", name, result ? "OK" : "Fail"));
            return result;
        }

        /// <summary>
        /// Launch a new instance of the named program with specified arguments
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool LaunchNewProgram(
            string name, 
            string args)
        {
            Trace.WriteLine(String.Format("{0} LaunchNewProgram {1} {2}", DateTime.Now.ToLongTimeString(), name, args));

            if (!processes.ContainsKey(name))
            {
                Trace.WriteLine(String.Format("\tUnknown {0}", name));
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

            Trace.WriteLine(String.Format("\tStart {0} {1}", name, p != null ? "OK" : "Fail"));
            return p != null;
        }

        /// <summary>
        /// Exit a named program we have launched if it is still running
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ExitProgram(
            string name)
        {
            bool result;
            Trace.WriteLine(String.Format("{0} ExitProgram {1}", DateTime.Now.ToLongTimeString(), name));

            if (!processes.ContainsKey(name))
            {
                Trace.WriteLine(String.Format("\tUnknown {0}", name));
                return false;
            }

            DesktopProcess process = processes[name];

            if (!process.Running)
            {
                Trace.WriteLine(String.Format("\tStopped {0}", name));
                return true;
            }

            result = process.Exit();
            Trace.WriteLine(String.Format("\tExit {0} {1}", name, result ? "OK" : "Fail"));
            return result;
        }

        /// <summary>
        /// Exits all running programs we have launched
        /// </summary>
        /// <returns></returns>
        public bool ExitAllPrograms()
        {
            bool result;
            Trace.WriteLine(String.Format("{0} ExitAllPrograms", DateTime.Now.ToLongTimeString()));

            foreach (var process in processes.Values)
            {
                if (process.Running)
                {
                    result = process.Exit();
                    Trace.WriteLine(String.Format("\tExit {0} {1}", process.Name, result ? "OK" : "Fail"));
                }
            }

            return true;
        }

        /// <summary>
        /// Bring the named program to the foreground if it is running
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ForegroundProgram(
            string name)
        {
            bool result;
            Trace.WriteLine(String.Format("{0} ForegroundProgram {1}", DateTime.Now.ToLongTimeString(), name));

            if (!processes.ContainsKey(name))
            {
                Trace.WriteLine(String.Format("\tUnknown {0}", name));
                return false;
            }

            DesktopProcess process = processes[name];

            if (!process.Running)
            {
                Trace.WriteLine(String.Format("\tStopped {0}", name));
                return false;
            }

            result = process.Foreground();
            Trace.WriteLine(String.Format("\tForeground {0} {1}", name, result ? "OK" : "Fail"));
            return result;
        }

        /// <summary>
        /// Send an emulated keyboard sequence of key presses to the foreground application
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public bool SendKeys(
            string keys)
        {
            Trace.WriteLine(String.Format("{0} SendKeys {1}", DateTime.Now.ToLongTimeString(), keys));
            System.Windows.Forms.SendKeys.SendWait(keys);
            return true;
        }

        /// <summary>
        /// Move the mouse cursor on screen by a relative amount
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        public bool MouseMoveRelative(
            int dx, 
            int dy)
        {
            Trace.WriteLine(String.Format("{0} Mouse move {1},{2}", DateTime.Now.ToLongTimeString(), dx, dy));
            mouse_event((uint)(MouseEventFlags.MOVE), dx, dy, 0, 0);
            return false;
        }

        /// <summary>
        /// Send an emulated mouse click at the current cursor location
        /// </summary>
        /// <param name="rightButton">True if an emulated right mouse click; otherwise a left mouse click</param>
        /// <returns></returns>
        public bool MouseClick(
            bool rightButton)
        {
            Trace.WriteLine(String.Format("{0} Mouse click {1}", DateTime.Now.ToLongTimeString(), rightButton ? "right" : "left"));
            mouse_event((uint)(rightButton ? MouseEventFlags.RIGHTDOWN : MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((uint)(rightButton ? MouseEventFlags.RIGHTUP : MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            return false;
        }

        /// <summary>
        /// Send An IR code through the USB IIRT transmitter
        /// </summary>
        /// <param name="irCode"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public bool SendIR(
            string irCode, 
            string description)
        {
            Trace.WriteLine(String.Format("{0} Send IR {1}", DateTime.Now.ToLongTimeString(), description));
            return UsbService.SendIR(irCode);
        }

        /// <summary>
        /// Send special keys to the desktop
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public bool SendSpecialkey(
            string keyName)
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

        /// <summary>
        /// Fetch CPU and GPU temperature and load statistics as XML
        /// </summary>
        /// <returns></returns> 
        public string FetchCoreTempInfoXml()
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

        /// <summary>
        /// Ensure that the RemotePotato service is running and has not died, starting the service if it is not running
        /// </summary>
        /// <param name="recycle">If true; unconditionally stops and restarts the service</param>
        /// <returns>True if the service is now running</returns>
        public bool EnsureRemotePotatoRunning(
            bool recycle)
        {
            const int timeoutMilliseconds = 5000;
            ServiceController service = new ServiceController("Remote Potato Service");
            try
            {
                int millisec1 = Environment.TickCount;
                TimeSpan timeout;
                if (recycle && service.Status == ServiceControllerStatus.Running)
                {
                    timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                }

                // count the rest of the timeout
                int millisec2 = Environment.TickCount;
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                }

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

    }
}
