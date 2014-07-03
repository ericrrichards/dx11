
using System;
using System.Windows.Forms;
using Fortune.FromJS;

namespace Fortune {
    class Program {
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            var form = new MainForm();
            

            Application.Run(form);
        }
    }
}
