using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D9;

namespace HemisphericalAmbient {
    

    class Program {
        static void Main(string[] args) {
            var filename = "Meshes/bunny.sdkmesh";

            var mesh = new SdkMesh(filename);
            Console.WriteLine(mesh);
        }
    }
}
