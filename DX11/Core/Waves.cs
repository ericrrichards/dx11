using System.Diagnostics;
using SlimDX;

namespace Core {
    public class Waves {
        private float _k1;
        private float _k2;
        private float _k3;

        private float _timeStep;
        private float _spatialStep;

        private Vector3[] _currentSolution;
        private Vector3[] _prevSolution;
        private Vector3[] _normals;
        private Vector3[] _tangentX;

        public int VertexCount { get; private set; }
        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }
        public int TriangleCount { get; private set; }
        public float Width { get { return ColumnCount*_spatialStep; } }
        public float Depth { get { return RowCount*_spatialStep; } }

        public Vector3 Normal(int i) {
            return _normals[i];
        } 
        public Vector3 TangentX(int i) {
            return _tangentX[i];
        }

        public Vector3 this[int i] {
            get { return _currentSolution[i]; }
        }

        public void Init(int m, int n, float dx, float dt, float speed, float damping) {
            RowCount = m;
            ColumnCount = n;
            VertexCount = m * n;
            TriangleCount = (m - 1) * (n - 1) * 2;

            _timeStep = dt;
            _spatialStep = dx;

            var d = damping * dt + 2.0f;
            var e = (speed * speed) * (dt * dt) / (dx * dx);
            _k1 = (damping * dt - 2.0f) / d;
            _k2 = (4.0f - 8.0f * e) / d;
            _k3 = (2.0f * e) / d;

            _currentSolution = new Vector3[m*n];
            _prevSolution = new Vector3[m*n];
            _normals = new Vector3[m*n];
            _tangentX = new Vector3[m*n];

            var w2 = (n - 1) * dx * 0.5f;
            var d2 = (m - 1) * dx * 0.5f;

            for (var i = 0; i < m; i++) {
                var z = d2 - i * dx;
                for (var j = 0; j < n; j++) {
                    var x = -w2 + j * dx;
                    _prevSolution[i*n+j] = new Vector3(x, 0, z);
                    _currentSolution[i*n+j] = new Vector3(x, 0, z);
                    _normals[i*n+j] = new Vector3(0,1,0);
                    _tangentX[i*n+j] = new Vector3(1.0f, 0, 0);
                }
            }
        }

        private float _t;
        public void Update(float dt) {
            _t += dt;

            if (!(_t >= _timeStep)) {
                return;
            }
            for (int i = 1; i < RowCount-1; i++) {
                for (int j = 1; j < ColumnCount-1; j++) {
                    var n =
                        _k1 * _prevSolution[i * ColumnCount + j].Y +
                        _k2 * _currentSolution[i * ColumnCount + j].Y +
                        _k3 * (_currentSolution[(i + 1) * ColumnCount + j].Y +
                               _currentSolution[(i - 1) * ColumnCount + j].Y +
                               _currentSolution[i * ColumnCount + j + 1].Y +
                               _currentSolution[i * ColumnCount + j - 1].Y);

                    _prevSolution[i * ColumnCount + j].Y = n;
                }
            }
            var temp = _prevSolution;
            _prevSolution = _currentSolution;
            _currentSolution = temp;
            _t = 0.0f;
            for (int i = 1; i < RowCount - 1; i++) {
                for (int j = 1; j < ColumnCount - 1; j++) {
                    var l = _currentSolution[i*ColumnCount + j - 1].Y;
                    var r = _currentSolution[i*ColumnCount + j + 1].Y;
                    var t = _currentSolution[(i - 1)*ColumnCount + j].Y;
                    var b = _currentSolution[(i + 1)*ColumnCount + j].Y;
                    _normals[i*ColumnCount + j] = Vector3.Normalize(new Vector3(-r+l, 2.0f*_spatialStep, b-t));
                    
                    _tangentX[i*ColumnCount + j]  = Vector3.Normalize(new Vector3(2.0f*_spatialStep, r-l, 0.0f));

                }
            }
        }

        public void Disturb(int i, int j, float magnitude) {
            Debug.Assert(i > 1 && i < RowCount-2);
            Debug.Assert(j > 1 && j < ColumnCount -2);

            var m2 = 0.5f * magnitude;

            _currentSolution[i * ColumnCount + j].Y += magnitude;
            _currentSolution[i * ColumnCount + j+1].Y += m2;
            _currentSolution[i * ColumnCount + j-1].Y += m2;
            _currentSolution[(i + 1) * ColumnCount + j].Y += m2;
            _currentSolution[(i - 1) * ColumnCount + j].Y += m2;
        }
    }
}