using ANCA_HMI.SystemInfo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dianyongkong4._7._2
{
    internal static class Program
    {

        public static string ModbusDianBiaoIP = string.Empty;


        public static string ModbusDianBiaoPort = string.Empty;
        
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            ModbusDianBiaoIP = ConfigurationManager.AppSettings["ModbusDianBiaoIP"];
            ModbusDianBiaoPort = ConfigurationManager.AppSettings["ModbusDianBiaoPort"];
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
