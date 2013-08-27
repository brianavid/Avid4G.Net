using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.ServiceModel;

namespace Avid.Desktop
{
    [ServiceContract(Namespace="Avid")]
    public interface IDesktopService
    {
        [OperationContract]
        bool LaunchProgram(string name, string args);
        [OperationContract]
        bool LaunchNewProgram(string name, string args);
        [OperationContract]
        bool ExitProgram(string name);
        [OperationContract]
        bool ExitAllPrograms();
        [OperationContract]
        bool ForegroundProgram(string name);
        [OperationContract]
        bool SendKeys(string keys);
        [OperationContract]
        bool MouseMoveRelative(int dx, int dy);
        [OperationContract]
        bool MouseClick(bool rightButton);
        [OperationContract]
        bool SendIR(string irCode, string description);
        [OperationContract]
        bool SendSpecialkey(string keyName);
        [OperationContract]
        string FetchCoreTempInfoXml();
        [OperationContract]
        bool EnsureRemotePotatoRunning(bool recycle);
    }
}
