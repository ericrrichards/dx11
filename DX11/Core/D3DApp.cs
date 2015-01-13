using System;
using Core.Controls;
using SlimDX.Direct2D;
using SlimDX.Direct3D11;
using FeatureLevel = SlimDX.Direct3D11.FeatureLevel;

namespace Core {
    using System.Drawing;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;

    using Core.Vertex;

    using SlimDX;
    using SlimDX.DXGI;

    using SpriteTextRenderer;

    using Buffer = SlimDX.Direct3D11.Buffer;
    using Device = Device;
    using Debug = System.Diagnostics.Debug;

    public class D3DApp : DisposableClass {
        public static D3DApp GD3DApp;
        private bool _disposed;

        public Form Window { get; protected set; }
        public IntPtr AppInst { get; protected set; }
        public float AspectRatio { get { return (float)ClientWidth / ClientHeight; } }
        public bool GammaCorrectedBackBuffer { get; set; }


        protected bool AppPaused;
        protected bool Minimized;
        protected bool Maximized;
        protected bool Resizing;
        protected int Msaa4XQuality;
        protected GameTimer Timer;

        protected Device Device;
        protected DeviceContext ImmediateContext;
        protected SwapChain SwapChain;
        protected Texture2D DepthStencilBuffer;
        protected RenderTargetView RenderTargetView;
        protected DepthStencilView DepthStencilView;
        protected Viewport Viewport;
        protected string MainWindowCaption;
        protected DriverType DriverType;
        protected int ClientWidth;
        protected int ClientHeight;
        protected bool Enable4XMsaa;
        private bool _running;
        private int _frameCount;
        private float _timeElapsed;
        private WindowRenderTarget _dxWRT;
        internal WindowRenderTarget DxWrt {
            get { return _dxWRT; }
        }
        private ProgressUpdate _progressUpdate;

        protected Buffer _screenQuadVB;
        protected Buffer _screenQuadIB;

        protected SpriteRenderer Sprite;
        protected FontCache FontCache;



        public ProgressUpdate ProgressUpdate { get { return _progressUpdate; } }

        public Buffer ScreenQuadVB {
            get { return _screenQuadVB; }
            set { _screenQuadVB = value; }
        }

        public Buffer ScreenQuadIB {
            get { return _screenQuadIB; }
            set { _screenQuadIB = value; }
        }

        protected bool InitMainWindow() {
            try {
                Window = new D3DForm {
                    Text = MainWindowCaption,
                    Name = "D3DWndClassName",
                    FormBorderStyle = FormBorderStyle.Sizable,
                    ClientSize = new Size(ClientWidth, ClientHeight),
                    StartPosition = FormStartPosition.CenterScreen,
                    MyWndProc = WndProc,
                    MinimumSize = new Size(200, 200),
                };
                Window.MouseDown += OnMouseDown;
                Window.MouseUp += OnMouseUp;
                Window.MouseMove += OnMouseMove;
                Window.MouseWheel += OnMouseWheel;
                Window.ResizeBegin += (sender, args) => {
                    AppPaused = true;
                    Resizing = true;
                    Timer.Stop();
                };
                Window.ResizeEnd += (sender, args) => {
                    AppPaused = false;
                    Resizing = false;
                    Timer.Start();
                    OnResize();
                };


                Window.Show();
                Window.Update();
                return true;
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "Error");
                return false;
            }
        }

        protected virtual void OnMouseWheel(object sender, MouseEventArgs e) { }

        protected virtual void OnMouseMove(object sender, MouseEventArgs e) {

        }

        protected virtual void OnMouseUp(object sender, MouseEventArgs e) {

        }

        protected virtual void OnMouseDown(object sender, MouseEventArgs e) {
        }

        // ReSharper disable InconsistentNaming
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_SIZE = 0x0005;
        private const int WM_DESTROY = 0x0002;
        // ReSharper restore InconsistentNaming

        private bool WndProc(ref Message m) {
            switch (m.Msg) {
                case WM_ACTIVATE:
                    if (m.WParam.ToInt32().LowWord() == 0) {
                        AppPaused = true;
                        Timer.Stop();
                    } else {
                        AppPaused = false;
                        Timer.Start();
                    }
                    return true;
                case WM_SIZE:
                    ClientWidth = m.LParam.ToInt32().LowWord();
                    ClientHeight = m.LParam.ToInt32().HighWord();
                    if (Device != null) {
                        if (m.WParam.ToInt32() == 1) { // SIZE_MINIMIZED
                            AppPaused = true;
                            Minimized = true;
                            Maximized = false;
                        } else if (m.WParam.ToInt32() == 2) { // SIZE_MAXIMIZED
                            AppPaused = false;
                            Minimized = false;
                            Maximized = true;
                            OnResize();
                        } else if (m.WParam.ToInt32() == 0) { // SIZE_RESTORED
                            if (Minimized) {
                                AppPaused = false;
                                Minimized = false;
                                OnResize();
                            } else if (Maximized) {
                                AppPaused = false;
                                Maximized = false;
                                OnResize();
                            } else if (Resizing) {

                            } else {
                                OnResize();
                            }
                        }
                    }
                    return true;
                case WM_DESTROY:
                    _running = false;
                    return true;
            }
            return false;
        }

        protected bool InitDirect3D() {
            var creationFlags = DeviceCreationFlags.None;
#if DEBUG
            creationFlags |= DeviceCreationFlags.Debug;
#endif
            try {
                Device = new Device(DriverType, creationFlags);
            } catch (Exception ex) {
                MessageBox.Show("D3D11Device creation failed\n" + ex.Message + "\n" + ex.StackTrace, "Error");
                return false;
            }
            ImmediateContext = Device.ImmediateContext;
            if (Device.FeatureLevel != FeatureLevel.Level_11_0) {
                Console.WriteLine("Direct3D Feature Level 11 unsupported\nSupported feature level: " + Enum.GetName(Device.FeatureLevel.GetType(), Device.FeatureLevel));
                //return false;
            }
            //Debug.Assert((Msaa4XQuality = Device.CheckMultisampleQualityLevels(Format.R8G8B8A8_UNorm, 4)) > 0);
            try {
                var format = GammaCorrectedBackBuffer ? Format.R8G8B8A8_UNorm_SRGB : Format.R8G8B8A8_UNorm;
                var sd = new SwapChainDescription {
                    ModeDescription = new ModeDescription(ClientWidth, ClientHeight, new Rational(60, 1), format) {
                        ScanlineOrdering = DisplayModeScanlineOrdering.Unspecified,
                        Scaling = DisplayModeScaling.Unspecified
                    },
                    SampleDescription = Enable4XMsaa && Device.FeatureLevel >= FeatureLevel.Level_10_1 ? new SampleDescription(4, Msaa4XQuality - 1) : new SampleDescription(1, 0),
                    Usage = Usage.RenderTargetOutput,
                    BufferCount = 1,
                    OutputHandle = Window.Handle,
                    IsWindowed = true,
                    SwapEffect = SwapEffect.Discard,
                    Flags = SwapChainFlags.None

                };
                SwapChain = new SwapChain(Device.Factory, Device, sd);
            } catch (Exception ex) {
                MessageBox.Show("SwapChain creation failed\n" + ex.Message + "\n" + ex.StackTrace, "Error");
                return false;
            }
            OnResize();

            Sprite = new SpriteRenderer(Device);
            FontCache = new FontCache(Sprite);
            return true;
        }


        protected void CalculateFrameRateStats() {
            _frameCount++;
            if ((Timer.TotalTime - _timeElapsed) >= 1.0f) {
                var fps = (float)_frameCount;
                var mspf = 1000.0f / fps;

                var s = string.Format("{0}    FPS: {1}    Frame Time: {2} (ms)", MainWindowCaption, fps, mspf);
                Window.Text = s;
                _frameCount = 0;
                _timeElapsed += 1.0f;
            }
        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref RenderTargetView);
                    Util.ReleaseCom(ref DepthStencilView);
                    Util.ReleaseCom(ref _screenQuadIB);
                    Util.ReleaseCom(ref _screenQuadVB);

                    Util.ReleaseCom(ref DepthStencilBuffer);
                    if (ImmediateContext != null) {
                        ImmediateContext.ClearState();
                    }

                    if (SwapChain.IsFullScreen) {
                        SwapChain.SetFullScreenState(false, null);
                    }
                    Util.ReleaseCom(ref SwapChain);
                    Util.ReleaseCom(ref ImmediateContext);
                    Util.ReleaseCom(ref Device);

                    Util.ReleaseCom(ref _progressUpdate);

                    Util.ReleaseCom(ref _dxWRT);
                    Util.ReleaseCom(ref Sprite);
                    Util.ReleaseCom(ref FontCache);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        protected D3DApp(IntPtr hInstance) {
            AppInst = hInstance;
            MainWindowCaption = "D3D11 Application";
            DriverType = DriverType.Hardware;
            ClientWidth = 800;
            ClientHeight = 600;
            Enable4XMsaa = false;
            Window = null;
            AppPaused = false;
            Minimized = false;
            Maximized = false;
            Resizing = false;
            Msaa4XQuality = 0;
            Device = null;
            ImmediateContext = null;
            SwapChain = null;
            DepthStencilBuffer = null;
            RenderTargetView = null;
            DepthStencilView = null;
            Viewport = new Viewport();
            Timer = new GameTimer();

            GD3DApp = this;
        }
        public virtual bool Init() {
            if (!InitMainWindow()) {
                return false;
            }
            if (!InitDirect3D()) {
                return false;
            }
            if (!InitDirect2D()) {
                return false;
            }
            BuildScreenQuadGeometryBuffers();

            _running = true;
            return true;
        }
        private void BuildScreenQuadGeometryBuffers() {
            var quad = GeometryGenerator.CreateFullScreenQuad();

            var verts = quad.Vertices.Select(v => new Basic32(v.Position, v.Normal, v.TexC)).ToList();
            var vbd = new BufferDescription(Basic32.Stride * verts.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            ScreenQuadVB = new Buffer(Device, new DataStream(verts.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof(int) * quad.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            ScreenQuadIB = new Buffer(Device, new DataStream(quad.Indices.ToArray(), false, false), ibd);
        }

        private bool InitDirect2D() {
            try {
                var factory = new SlimDX.Direct2D.Factory(FactoryType.SingleThreaded);

                _dxWRT = new WindowRenderTarget(factory, new WindowRenderTargetProperties {
                    Handle = Window.Handle,
                    PixelSize = Window.ClientSize,
                    PresentOptions = PresentOptions.Immediately
                });
                Util.ReleaseCom(ref factory);
                _progressUpdate = new ProgressUpdate(_dxWRT);


            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public virtual void OnResize() {
            Debug.Assert(ImmediateContext != null);
            Debug.Assert(Device != null);
            Debug.Assert(SwapChain != null);

            Util.ReleaseCom(ref RenderTargetView);
            Util.ReleaseCom(ref DepthStencilView);
            Util.ReleaseCom(ref DepthStencilBuffer);

            var format = GammaCorrectedBackBuffer ? Format.R8G8B8A8_UNorm_SRGB : Format.R8G8B8A8_UNorm;
            SwapChain.ResizeBuffers(1, ClientWidth, ClientHeight, format, SwapChainFlags.None);
            using (var resource = SlimDX.Direct3D11.Resource.FromSwapChain<Texture2D>(SwapChain, 0)) {
                RenderTargetView = new RenderTargetView(Device, resource);
                RenderTargetView.Resource.DebugName = "main render target";
            }

            var depthStencilDesc = new Texture2DDescription {
                Width = ClientWidth,
                Height = ClientHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D24_UNorm_S8_UInt,
                SampleDescription = (Enable4XMsaa && Device.FeatureLevel >= FeatureLevel.Level_10_1) ? new SampleDescription(4, Msaa4XQuality - 1) : new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            DepthStencilBuffer = new Texture2D(Device, depthStencilDesc) { DebugName = "DepthStencilBuffer" };
            DepthStencilView = new DepthStencilView(Device, DepthStencilBuffer);

            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);

            Viewport = new Viewport(0, 0, ClientWidth, ClientHeight, 0.0f, 1.0f);

            ImmediateContext.Rasterizer.SetViewports(Viewport);
        }
        public virtual void UpdateScene(float dt) { }
        public virtual void DrawScene() { }

        protected virtual void EndFrame() {
            Sprite.Flush();
            SwapChain.Present(0, PresentFlags.None);
        }

        public void Run() {
            Timer.Reset();
            Timer.FrameTime = 1.0f / 60.0f;
            while (_running) {
                Application.DoEvents();
                Timer.Tick();

                if (!AppPaused) {
                    CalculateFrameRateStats();
                    UpdateScene(Timer.DeltaTime);
                    DrawScene();


                } else {
                    Thread.Sleep(100);
                }
            }
            Dispose();
        }

    }
}
