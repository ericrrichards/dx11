using System;
using System.Windows.Forms;

namespace VoronoiMap {
    static class Program {
        [STAThread]
        static void Main() {
            log4net.Config.XmlConfigurator.Configure();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            var form = new MainForm();
            

            Application.Run(form);
        }
    }
}
