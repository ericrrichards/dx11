using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Core;
using Core.Model;
using Core.Terrain;
using SlimDX;
using SlimDX.Direct3D11;

namespace _33_Pathfinding {
    public class Unit : DisposableClass {
        // offset from the terrain surface to render the model
        private const float HeightOffset = 0.1f;
        private bool _disposed;

        // 3D model instance for this entity
        private readonly BasicModelInstance _modelInstance;

        // current MapTile this entity is occupying
        private MapTile MapTile { get; set; }

        // current path the entity is following
        private List<MapTile> _path = new List<MapTile>();
        // world-positions of the MapTiles the entity is traveling between
        private Vector3 _lastWP, _nextWP;
        // index of the current node in the path the entity is following
        private int _activeWP;

        // world-space position of the entity
        private Vector3 _position;

        private readonly Terrain _terrain;

        // movement related properties
        private bool Moving { get; set; }
        private float MovePrc { get; set; }
        private float Time { get; set; }
        private float Speed { get; set; }
        public Point Position { get { return MapTile.MapPosition; } }
        public MapTile Destination {
            get {
                if (_path.Any()) {
                    return _path.Last();
                } else {
                    return MapTile;
                }
            }
        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {

                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public Unit(BasicModelInstance model, MapTile mp, Terrain terrain) {
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
            Time += dt * Speed;

            if (Moving) {
                if (MovePrc < 1.0f) {
                    MovePrc += dt * Speed;
                }
                if (MovePrc > 1.0f) {
                    MovePrc = 1.0f;
                }
                if (Math.Abs(MovePrc - 1.0f) < float.Epsilon) {
                    if (_activeWP + 1 >= _path.Count) {
                        // done following path
                        Moving = false;
                    } else {
                        // move to the next leg of the path
                        _activeWP++;
                        MoveUnit(_path[_activeWP]);
                    }
                }
                // move the unit towards the next waypoint on the path
                _position = Vector3.Lerp(_lastWP, _nextWP, MovePrc);
            }
            // set the world position of the model for rendering
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
                if (_path.Count <= 0) {
                    // unit is already at goal position
                    return;
                }
                Moving = true;
                MoveUnit(_path[_activeWP]);
            }
        }

        private void MoveUnit(MapTile to) {
            // set the unit's last position to its current position
            _lastWP = MapTile.WorldPos;
            _lastWP.Y = _terrain.Height(_lastWP.X, _lastWP.Z) + HeightOffset;
            // set the unit's position to the next leg in the path
            MapTile = to;
            MovePrc = 0.0f;
            // set the next position to the next leg's position
            _nextWP = MapTile.WorldPos;
            _nextWP.Y = _terrain.Height(_nextWP.X, _nextWP.Z) + HeightOffset;
        }
    }
}