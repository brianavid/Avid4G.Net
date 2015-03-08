using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UsbUirt;

namespace Avid.Desktop
{
    /// <summary>
    /// A wrapper class for the USB UIRT device
    /// </summary>
    public static class UsbService
    {
        /// <summary>
        /// The only method is to Send an IR code sequence
        /// </summary>
        /// <param name="irCode"></param>
        /// <returns></returns>
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
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
