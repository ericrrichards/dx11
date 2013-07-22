using System.Diagnostics;
using SlimDX;

namespace Core {
    public class Waves {
        private int _vertexCount;
        private int _triangleCount;

        private int _rowCount;
        private int _columnCount;

        private float _k1;
        private float _k2;
        private float _k3;

        private float _timeStep;
        private float _spatialStep;

        private Vector3[] _currentSolution;
        private Vector3[] _prevSolution;

        public int VertexCount {get { return _vertexCount; }}
        public int RowCount {get { return _rowCount; }}
        public int ColumnCount {get { return _columnCount; }}
        public int TriangleCount {get { return _triangleCount; }}

        public Vector3 this[int i] {
            get { return _currentSolution[i]; }
        }

        public void Init(int m, int n, float dx, float dt, float speed, float damping) {
            _rowCount = m;
            _columnCount = n;
            _vertexCount = m * n;
            _triangleCount = (m - 1) * (n - 1) * 2;

            _timeStep = dt;
            _spatialStep = dx;

            var d = damping * dt + 2.0f;
            var e = (speed * speed) * (dt * dt) / (dx * dx);
            _k1 = (damping * dt - 2.0f) / d;
            _k2 = (4.0f - 8.0f * e) / d;
            _k3 = (2.0f * e) / d;

            _currentSolution = new Vector3[m*n];
            _prevSolution = new Vector3[m*n];

            var w2 = (n - 1) * dx * 0.5f;
            var d2 = (m - 1) * dx * 0.5f;

            for (var i = 0; i < m; i++) {
                var z = d2 - i * dx;
                for (var j = 0; j < n; j++) {
                    var x = -w2 + j * dx;
                    _prevSolution[i*n+j] = new Vector3(x, 0, z);
                    _currentSolution[i*n+j] = new Vector3(x, 0, z);
                }
            }
        }

        private float _t;
        public void Update(float dt) {
            _t += dt;

            if (!(_t >= _timeStep)) {
                return;
            }
            for (int i = 1; i < _rowCount-1; i++) {
                for (int j = 1; j < _columnCount-1; j++) {
                    var n =
                        _k1 * _prevSolution[i * _columnCount + j].Y +
                        _k2 * _currentSolution[i * _columnCount + j].Y +
                        _k3 * (_currentSolution[(i + 1) * _columnCount + j].Y +
                               _currentSolution[(i - 1) * _columnCount + j].Y +
                               _currentSolution[i * _columnCount + j + 1].Y +
                               _currentSolution[i * _columnCount + j - 1].Y);

                    _prevSolution[i * _columnCount + j].Y = n;
                }
            }
            var temp = _prevSolution;
            _prevSolution = _currentSolution;
            _currentSolution = temp;
            _t = 0.0f;
        }

        public void Disturb(int i, int j, float magnitude) {
            Debug.Assert(i > 1 && i < _rowCount-2);
            Debug.Assert(j > 1 && j < _columnCount -2);

            var m2 = 0.5f * magnitude;

            _currentSolution[i * _columnCount + j].Y += magnitude;
            _currentSolution[i * _columnCount + j+1].Y += m2;
            _currentSolution[i * _columnCount + j-1].Y += m2;
            _currentSolution[(i + 1) * _columnCount + j].Y += m2;
            _currentSolution[(i - 1) * _columnCount + j].Y += m2;
        }
    }
}