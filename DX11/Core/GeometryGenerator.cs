using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace Core {
    public class GeometryGenerator {
        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex {
            public Vector3 Position { get; set; }
            public Vector3 Normal { get; set; }
            public Vector3 TangentU { get; set; }
            public Vector2 TexC { get; set; }
            public Vertex(Vector3 pos, Vector3 norm, Vector3 tan, Vector2 uv) : this() {
                Position = pos;
                Normal = norm;
                TangentU = tan;
                TexC = uv;
            }
        }
        public class MeshData {
            public List<Vertex> Vertices = new List<Vertex>();
            public List<int> Indices = new List<int>(); 
        }

        public static MeshData CreateBox(float width, float height, float depth) {
            throw new NotImplementedException();
        }

        public static MeshData CreateGrid(float width, float depth, int m, int n) {
            var ret = new MeshData();

            var halfWidth = width*0.5f;
            var halfDepth = width*0.5f;

            var dx = width/(n - 1);
            var dz = depth/(m - 1);

            var du = 1.0f/(n - 1);
            var dv = 1.0f/(m - 1);

            for (var i = 0; i < m; i++) {
                var z = halfDepth - i*dz;
                for (var j = 0; j < n; j++) {
                    var x = -halfWidth*j*dx;
                   ret.Vertices.Add(new Vertex(new Vector3(x, 0, z), new Vector3(0,1,0), new Vector3(1, 0, 0), new Vector2(j*du, i*dv)));
                }
            }
            for (var i = 0; i < m-1; i++) {
                for (var j = 0; j < n-1; j++) {
                    ret.Indices.Add(i*n+j);
                    ret.Indices.Add(i*n+j+1);
                    ret.Indices.Add((i+1)*n+j);

                    ret.Indices.Add((i+1)*n+j);
                    ret.Indices.Add(i*n+j+1);
                    ret.Indices.Add((i+1)*n+j+1);
                }
            }

            return ret;
        }

    }
}
