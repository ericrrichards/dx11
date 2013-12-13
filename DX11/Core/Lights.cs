using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace Core {
    [StructLayout(LayoutKind.Sequential)]
    public struct DirectionalLight {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Vector3 Direction;
        public float Pad;

        public static int Stride = Marshal.SizeOf(typeof(DirectionalLight));
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PointLight {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Vector3 Position;
        public float Range;
        public Vector3 Attenuation;
        public float Pad;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SpotLight {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Vector3 Position;
        public float Range;
        public Vector3 Direction;
        public float Spot;
        public Vector3 Attenuation;
        public float Pad;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Material {
        public Color4 Ambient;
        public Color4 Diffuse;
        public Color4 Specular;
        public Color4 Reflect;
        public static int Stride = Marshal.SizeOf(typeof(Material));

    }
}
