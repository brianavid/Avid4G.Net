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
    class DesktopProcess
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetActiveWindow(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam);

        public DesktopProcess(
            string name,
            string path,
            string args)
        {
            Name = name;
            Path = path;
            Args = args;

            p = new Process();
            p.StartInfo.FileName = path;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
        }

        public string Name { get; private set; }
        public string Path { get; private set; }
        public string Args { get; private set; }
        public bool Running { get { return running && !p.HasExited; } }
        public Process p { get; private set; }
        bool running;
        string processName;

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

        public bool Start(string args)
        {
            p.StartInfo.Arguments = (args != null) ? args : Args;

            if (!p.Start())
            {
                return false;
            }

            processName = p.ProcessName;
            Trace.WriteLine(String.Format("- Service '{0}' started ID {1}", processName, p.Id));

            running = true;
            return p.WaitForInputIdle() && Foreground();
        }

        public bool Foreground()
        {
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

        public bool Exit()
        {
            ReloadProcess();

            if (!running || p.HasExited)
            {
                Trace.WriteLine(String.Format("- Service '{0}' not running", processName));
                running = false;
                return true;
            }

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

            p.Kill();

            Trace.WriteLine(String.Format("- Service '{0}' stopped", processName));
            running = false;
            return true;
        }
    }

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

        static Dictionary<string, DesktopProcess> processes = null;
        public DesktopService()
        {
            Trace.WriteLine(String.Format("New DesktopService"));
            XDocument doc = XDocument.Load(@"C:\Avid.Net\AvidConfig.xml");

            if (processes == null)
            {
                processes = new Dictionary<string, DesktopProcess>();
                foreach (var program in doc.Root.Element("Programs").Elements("Program"))
                {
                    string name = program.Attribute("name").Value;
                    string path = program.Attribute("path").Value;
                    string args = program.Attribute("args") == null ? "" : program.Attribute("args").Value;
                    processes[name] = new DesktopProcess(name, path, args);
                    Trace.WriteLine(String.Format("Service '{0}': {1}", name, System.IO.File.Exists(path) ? "OK" : "Not Found"));
                }
            }
        }

        public bool LaunchProgram(string name, string args)
        {
            bool result;
            Trace.WriteLine(String.Format("{0} LaunchProgram {1} '{2}'", DateTime.Now.ToLongTimeString(), name, args == null ? "" : args));

            if (!processes.ContainsKey(name))
            {
                Trace.WriteLine(String.Format("\tUnknown {0}", name));
                return false;
            }

            DesktopProcess process = processes[name];

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

        public bool LaunchNewProgram(string name, string args)
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

        public bool ExitProgram(string name)
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

        public bool ForegroundProgram(string name)
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

        public bool SendKeys(string keys)
        {
            Trace.WriteLine(String.Format("{0} SendKeys {1}", DateTime.Now.ToLongTimeString(), keys));
            System.Windows.Forms.SendKeys.SendWait(keys);
            return true;
        }

        public bool MouseMoveRelative(int dx, int dy)
        {
            Trace.WriteLine(String.Format("{0} Mouse move {1},{2}", DateTime.Now.ToLongTimeString(), dx, dy));
            mouse_event((uint)(MouseEventFlags.MOVE), dx, dy, 0, 0);
            return false;
        }

        public bool MouseClick(bool rightButton)
        {
            Trace.WriteLine(String.Format("{0} Mouse click {1}", DateTime.Now.ToLongTimeString(), rightButton ? "right" : "left"));
            mouse_event((uint)(rightButton ? MouseEventFlags.RIGHTDOWN : MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            mouse_event((uint)(rightButton ? MouseEventFlags.RIGHTUP : MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            return false;
        }

        public bool SendIR(string irCode, string description)
        {
            Trace.WriteLine(String.Format("{0} Send IR {1}", DateTime.Now.ToLongTimeString(), description));
            return UsbService.SendIR(irCode);
        }

        public bool SendSpecialkey(string keyName)
        {
            Trace.WriteLine(String.Format("{0} SendSpecialkey {1}", DateTime.Now.ToLongTimeString(), keyName));
            switch (keyName)
            {
                default:
                    return false;
                case "ClearDesktop":
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.LWIN, VirtualKeyCode.VK_M);
                    return true;
                case "ContextMenu":
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.APPS);
                    return true;
            }
        }

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
