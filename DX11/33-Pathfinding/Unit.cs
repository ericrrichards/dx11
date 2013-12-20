using System;
using System.Collections.Generic;
using Core;
using Core.Model;
using Core.Terrain;
using SlimDX;
using SlimDX.Direct3D11;

namespace _33_Pathfinding {
    public class Unit : DisposableClass {
        private const float HeightOffset = 0.1f;
        private bool _disposed;
        private readonly BasicModelInstance _modelInstance;

        public MapTile MapTile { get; set; }

        private List<MapTile> _path = new List<MapTile>();
        private Vector3 _lastWP, _nextWP;
        private Vector3 _position;
        private int _activeWP;
        private readonly Terrain _terrain;

        public bool Moving { get; private set; }
        public float MovePrc { get; private set; }
        public float Time { get; set; }
        public float Speed { get; set; }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {

                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public Unit( BasicModelInstance model, MapTile mp, Terrain terrain ) {
            _modelInstance = model;
            MapTile = mp;
            _terrain = terrain;
            _position = mp.WorldPos;
            _position.Y += HeightOffset;
            Time = 0.0f;
            _activeWP = 0;
            Moving = false;
            MovePrc = 0;

            Speed = 1.0f;

        }

        public void Update(float dt) {
            Time += dt*0.8f*Speed;

            if (Moving) {
                if (MovePrc < 1.0f) {
                    MovePrc += dt*Speed;
                }
                if (MovePrc > 1.0f) {
                    MovePrc = 1.0f;
                }
                if (Math.Abs(MovePrc - 1.0f) < float.Epsilon) {
                    if (_activeWP + 1 >= _path.Count) {
                        Moving = false;
                    } else {
                        _activeWP++;
                        MoveUnit(_path[_activeWP]);
                    }
                }
                _position = Vector3.Lerp(_lastWP, _nextWP, MovePrc);
                //_position.Y = _terrain.Height(_position.X, _position.Z) + HeightOffset;
            }
            _modelInstance.World = Matrix.Translation(_position);
        }

        public void Render(DeviceContext dc, EffectPass effectPass, Matrix view, Matrix proj) {
            _modelInstance.Draw(dc, effectPass, view, proj, RenderMode.Basic);
        }

        public void Goto(MapTile mp) {
            if (_terrain == null) return;

            _path.Clear();
            _activeWP = 0;

            if (Moving) {
                _path.Add(MapTile);
                var tmpPath = _terrain.GetPath(MapTile.MapPosition, mp.MapPosition);
                _path.AddRange(tmpPath);
            } else {
                _path = _terrain.GetPath(MapTile.MapPosition, mp.MapPosition);
                if (_path.Count > 0) {
                    Moving = true;
                    MoveUnit(_path[_activeWP]);

                }
            }
        }

        private void MoveUnit(MapTile to) {
            _lastWP = MapTile.WorldPos;
            _lastWP.Y = _terrain.Height(_lastWP.X, _lastWP.Z) + HeightOffset;
            MapTile = to;
            MovePrc = 0.0f;
            _nextWP = MapTile.WorldPos;
            _nextWP.Y = _terrain.Height(_nextWP.X, _nextWP.Z)+ HeightOffset;
        }
    }
}