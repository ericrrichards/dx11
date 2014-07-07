
using System;
using System.Windows.Forms;
using Fortune.FromJS;

namespace Fortune {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            log4net.Config.XmlConfigurator.Configure();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            var form = new MainForm();
            

            Application.Run(form);
        }
    }
}
