using System;
using Core;
using SlimDX;

namespace VoronoiMap {
    public delegate bool IslandShape(Vector2 p);
    public static class IslandShapes {
        private static int _seed    ;
        private static ParkerMillerPnrg _islandRandom;
        private static int _bumps;
        private static float _startAngle;
        private static float _dipAngle;
        private static float _dipWidth;
        private const float IslandFactor = 1.07f;

        public static IslandShape Get(string type, int seed) {
            _seed = seed;
            switch (type) {
                case "radial":
                    _islandRandom = new ParkerMillerPnrg() {
                        Seed = seed
                    };
                    _bumps = _islandRandom.NextIntRange(1, 6);
                    _startAngle = _islandRandom.NextFloatRange(0, MathF.PI*2);
                    _dipAngle = _islandRandom.NextFloatRange(0, MathF.PI*2);
                    _dipWidth = _islandRandom.NextFloatRange(0.2f, 0.7f);
                    return Radial;
                case "perlin":
                    return Perlin;
                case "square":
                    return Square;
                case "blob":
                    return Blob;
            }
            return Blob;
        }

        private static bool Perlin(Vector2 q) {
            throw new NotImplementedException();
        }

        private static bool Square(Vector2 q) {
            return true;
        }

        private static bool Blob(Vector2 q) {
            var eye1 = new Vector2(q.X - 0.2f, q.Y/2 + 0.2f).Length() < 0.05f;
            var eye2 = new Vector2(q.X + 0.2f, q.Y/2 + 0.2f).Length() < 0.05f;
            var body = q.Length() < 0.8f - 0.18*MathF.Sin(5*(float)Math.Atan2(q.Y, q.X));
            return body && !eye1 && !eye2;

        }

        private static bool Radial(Vector2 p) {
            var angle = (float)Math.Atan2(p.Y, p.X);
            var length = 0.5f*(Math.Max(Math.Abs(p.X), Math.Abs(p.Y)) + p.Length());
            var r1 = 0.5f + 0.4f*MathF.Sin(_startAngle + _bumps*angle + MathF.Cos((_bumps + 3)*angle));
            var r2 = 0.7f - 0.2f*MathF.Sin(_startAngle + _bumps*angle - MathF.Sin((_bumps + 2)*angle));

            if (Math.Abs(angle - _dipAngle) < _dipWidth || Math.Abs(angle - _dipAngle + 2*MathF.PI) < _dipWidth  || Math.Abs(angle - _dipAngle - 2*MathF.PI) < _dipWidth) {
                r1 = r2 = 0.2f;
            }
            return (length < r1 || (length > r1*IslandFactor && length < r2));
        }
    }
}