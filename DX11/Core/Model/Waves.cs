using Core.FX;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    public class Waves : DisposableClass {
        // wave geometry
        private BasicModel _gridModel;
        private BasicModelInstance _grid;

        // offsets to scroll the wave textures for displacement mapping
        private Vector2 _wavesDispOffset0;
        private Vector2 _wavesDispOffset1;

        // offsets to scroll the wave textures for normal mapping
        private Vector2 _wavesNormalOffset0;
        private Vector2 _wavesNormalOffset1;
        
        // transform matrices to convert offsets into transformations we can feed into the shader
        private Matrix _wavesDispTexTransform0;
        private Matrix _wavesDispTexTransform1;
        private Matrix _wavesNormalTexTransform0;
        private Matrix _wavesNormalTexTransform1;

        private bool _disposed;

        // provides access to model material
        public Material Material {
            get { return _gridModel.Materials[0]; }
            set { _gridModel.Materials[0] = value; }
        }
        // provides access to model world transform
        public Matrix World {
            get { return _grid.World; }
            set { _grid.World = value; }
        }

        public BoundingBox BoundingBox { get { return _grid.BoundingBox; } }

        // normal/heightmap textures
        public ShaderResourceView NormalMap0 {get; set; }
        public ShaderResourceView NormalMap1 { get; set; }

        // parameters to modify texture scrolling rates
        public Vector2 DispFactor0 { get; private set; }
        public Vector2 DispFactor1 { get; private set; }
        public Vector2 NormalFactor0 { get; private set; }
        public Vector2 NormalFactor1 { get; private set; }

        // parameters to modify texture tiling
        public Vector3 DispScale0 { get; private set; }
        public Vector3 DispScale1 { get; private set; }
        public Vector3 NormalScale0 { get; private set; }
        public Vector3 NormalScale1 { get; private set; }

        // provides access to model diffuse map
        public ShaderResourceView DiffuseMap {
            get { return _gridModel.DiffuseMapSRV[0]; }
            set { _gridModel.DiffuseMapSRV[0] = value; }
        }
        // provides access to model diffuse texture transform
        public Matrix TexTransform {
            get {return _grid.TexTransform;}
            set { _grid.TexTransform = value; }
        }
        
        public Waves() {
            _wavesDispOffset0 = new Vector2();
            _wavesDispOffset1 = new Vector2();
            _wavesNormalOffset0 = new Vector2();
            _wavesNormalOffset1 = new Vector2();

            DispFactor0 = new Vector2(0.01f, 0.03f);
            DispFactor1 = new Vector2(0.01f, 0.03f);
            NormalFactor0 = new Vector2(0.05f, 0.02f);
            NormalFactor1 = new Vector2(0.02f, 0.05f);

            DispScale0 = new Vector3(2,2, 1);
            DispScale1 = new Vector3(1,1, 1);
            NormalScale0 = new Vector3(22,22,1 );
            NormalScale1 = new Vector3(16,16, 1);
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _gridModel);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public void Init(Device device, TextureManager texMgr, float width, float depth, string texture1 = "Textures/waves0.dds", string texture2 = "textures/waves1.dds") {
            NormalMap0 = texMgr.CreateTexture(texture1);
            NormalMap1 = texMgr.CreateTexture(texture2);

            _gridModel = new BasicModel();
            _gridModel.CreateGrid(device, width, depth, ((int)width) * 2, ((int)depth) * 2);
                
            //    BasicModel.CreateGrid(device, width, depth, ((int)width) * 2, ((int)depth) * 2);
            Material = new Material {
                Ambient = new Color4(0.1f, 0.1f, 0.3f),
                Diffuse = new Color4(0.4f, 0.4f, 0.7f),
                Specular = new Color4(128f, 0.8f, 0.8f, 0.8f),
                Reflect = new Color4(0.4f, 0.4f, 0.4f)
            };

            _grid = new BasicModelInstance(_gridModel);
            TexTransform = Matrix.Identity;
            World = Matrix.Translation(0, -0.2f, 0);
        }

        public void Update(float dt) {
            _wavesDispOffset0.X += DispFactor0.X * dt;
            _wavesDispOffset0.Y += DispFactor0.Y * dt;

            _wavesDispOffset1.X += DispFactor1.X * dt;
            _wavesDispOffset1.Y += DispFactor1.Y * dt;

            _wavesDispTexTransform0 = Matrix.Scaling(DispScale0) *
                                      Matrix.Translation(_wavesDispOffset0.X, _wavesDispOffset0.Y, 0);

            _wavesDispTexTransform1 = Matrix.Scaling(DispScale1) *
                                      Matrix.Translation(_wavesDispOffset1.X, _wavesDispOffset1.Y, 0);

            _wavesNormalOffset0.X += NormalFactor0.X * dt;
            _wavesNormalOffset0.Y += NormalFactor0.Y * dt;

            _wavesNormalOffset1.X += NormalFactor1.X* dt;
            _wavesNormalOffset1.Y += NormalFactor1.Y * dt;

            _wavesNormalTexTransform0 = Matrix.Scaling(NormalScale0 ) *
                                        Matrix.Translation(_wavesNormalOffset0.X, _wavesNormalOffset0.Y, 0);
            _wavesNormalTexTransform1 = Matrix.Scaling(NormalScale1) *
                                        Matrix.Translation(_wavesNormalOffset1.X, _wavesNormalOffset1.Y, 0);
        }

        public void Draw(DeviceContext dc, EffectTechnique waveTech, Matrix viewProj) {
            for (var p = 0; p < waveTech.Description.PassCount; p++) {
                var world = _grid.World;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.WavesFX.SetWorld(world);
                Effects.WavesFX.SetWorldInvTranspose(wit);
                Effects.WavesFX.SetViewProj(viewProj);
                Effects.WavesFX.SetWorldViewProj(wvp);
                Effects.WavesFX.SetTexTransform(_grid.TexTransform);
                Effects.WavesFX.SetWaveDispTexTransform0(_wavesDispTexTransform0);
                Effects.WavesFX.SetWaveDispTexTransform1(_wavesDispTexTransform1);
                Effects.WavesFX.SetWaveNormalTexTransform0(_wavesNormalTexTransform0);
                Effects.WavesFX.SetWaveNormalTexTransform1(_wavesNormalTexTransform1);
                Effects.WavesFX.SetMaterial(_grid.Model.Materials[0]);
                Effects.WavesFX.SetDiffuseMap(_grid.Model.DiffuseMapSRV[0]);
                Effects.WavesFX.SetNormalMap0(NormalMap0);
                Effects.WavesFX.SetNormalMap1(NormalMap1);

                waveTech.GetPassByIndex(p).Apply(dc);
                _grid.Model.ModelMesh.Draw(dc, 0);
            }
        }
    }
}