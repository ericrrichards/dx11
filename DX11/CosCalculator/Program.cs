using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosCalculator {
    using System.IO;

    class Program {
        static void Main(string[] args) {
            using (var writer = new StreamWriter("cos.cs")) {
                writer.WriteLine("private static float[] CosLookup = new []{ ");
                for (decimal i = 0; i < (decimal)Math.PI * 2; i += (decimal)0.001) {
                    writer.WriteLine("\t{0}f,", (float)Math.Cos((double)i));
                }
                writer.WriteLine("};");
            }
        }
    }
}
