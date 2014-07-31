using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Algorithms.Voronoi;

namespace VoronoiExample1 {
    class Program {
        [STAThread]
        static void Main(string[] args) {

            var form = new MainForm();
            Application.Run(form);


        }
    }
}
