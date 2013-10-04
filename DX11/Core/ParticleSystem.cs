using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Camera;
using Core.FX;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;


namespace Core {
    public class ParticleSystem:DisposableClass {
        private bool _disposed;

        private int _maxParticles;
        private bool _firstRun;

        private float _gameTime;
        private float _timeStep;
        public float Age { get; private set; }

        public Vector3 EyePosW { get;  set; }
        public Vector3 EmitPosW { get;  set; }
        public Vector3 EmitDirW { get;  set; }

        private ParticleEffect _fx;

        private Buffer _initVB;
        private Buffer _drawVB;
        private Buffer _streamOutVB;

        private ShaderResourceView _texArraySRV;
        private ShaderResourceView _randomTexSRV;

        public ParticleSystem() {
            _firstRun = true;
            EmitDirW = new Vector3(0,1, 0);
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _initVB);
                    Util.ReleaseCom(ref _drawVB);
                    Util.ReleaseCom(ref _streamOutVB);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public void Init(Device device, ParticleEffect fx, ShaderResourceView texArraySRV, ShaderResourceView randomTexSRV, int maxParticles) {
            _maxParticles = maxParticles;
            _fx = fx;
            _texArraySRV = texArraySRV;
            _randomTexSRV = randomTexSRV;

            BuildVB(device);
        }
        public void Reset() {
            _firstRun = true;
            Age = 0;
        }
        public void Update(float dt, float gameTime) {
            _gameTime = gameTime;
            _timeStep = dt;

            Age += dt;
        }
        public void Draw(DeviceContext dc, CameraBase camera) {
            var vp = camera.ViewProj;

            _fx.SetViewProj(vp);
            _fx.SetGameTime(_gameTime);
            _fx.SetTimeStep(_timeStep);
            _fx.SetEyePosW(EyePosW);
            _fx.SetEmitPosW(EmitPosW);
            _fx.SetEmitDirW(EmitDirW);
            _fx.SetTexArray(_texArraySRV);
            _fx.SetRandomTex(_randomTexSRV);

            dc.InputAssembler.InputLayout = InputLayouts.Particle;
            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

            var stride = Particle.Stride;
            const int Offset = 0;

           
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_firstRun ? _initVB : _drawVB, stride, Offset));
            
            dc.StreamOutput.SetTargets(new StreamOutputBufferBinding(_streamOutVB, Offset));

            var techDesc = _fx.StreamOutTech.Description;
            for (int p = 0; p < techDesc.PassCount; p++) {
                _fx.StreamOutTech.GetPassByIndex(p).Apply(dc);
                if (_firstRun) {
                    dc.Draw(1, 0);
                    _firstRun = false;
                } else {
                    dc.DrawAuto();
                }
            }
            dc.StreamOutput.SetTargets(null);
            
            var temp = _drawVB;
            _drawVB = _streamOutVB;
            _streamOutVB = temp;

            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_drawVB, stride, Offset));

            techDesc = _fx.DrawTech.Description;
            for (int p = 0; p < techDesc.PassCount; p++) {
                _fx.DrawTech.GetPassByIndex(p).Apply(dc);
                dc.DrawAuto();
            }

        }

        private void BuildVB(Device device) {
            var vbd = new BufferDescription(Particle.Stride, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            var p = new Particle {
                Age = 0,
                Type = 0
            };

            _initVB = new Buffer(device, new DataStream(new[]{p}, true, true), vbd);

            vbd.SizeInBytes = Particle.Stride*_maxParticles;
            vbd.BindFlags = BindFlags.VertexBuffer | BindFlags.StreamOutput;
 
            _drawVB = new Buffer(device, vbd);
            _streamOutVB = new Buffer(device, vbd);
        }
    }
}
