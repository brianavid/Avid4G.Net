using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UsbUirt;

namespace Avid.Desktop
{
    public static class UsbService
    {
        public static bool SendIR(
            string irCode)
        {
            if (Controller.DriverVersion != 0x0100)
            {
                Console.WriteLine("ERROR: Invalid uuirtdrv version!\n");
                return false;
            }

            try
            {
                using (Controller mc = new Controller())
                {
                    mc.Transmit(irCode, CodeFormat.Pronto, 1, TimeSpan.Zero);
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
    }
}
