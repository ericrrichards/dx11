namespace Core.FX {
    using System;
    using System.Text;

    using SlimDX.Direct3D11;

    public static class Effects {
        public static void InitAll(Device device) {
            try {
                BasicFX = new BasicEffect(device, "FX/Basic.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                TreeSpriteFX = new TreeSpriteEffect(device, "FX/TreeSprite.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                InstancedBasicFX = new InstancedBasicEffect(device, "FX/InstancedBasic.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                SkyFX = new SkyEffect(device, "FX/Sky.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                NormalMapFX = new NormalMapEffect(device, "FX/NormalMap.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                DisplacementMapFX = new DisplacementMapEffect(device, "FX/DisplacementMap.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                TerrainFX = new TerrainEffect(device, "FX/Terrain.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                ColorFX = new ColorEffect(device, "FX/color.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
               InstancedNormalMapFX = new InstancedNormalMapEffect(device, "FX/InstancedNormalMap.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            try {
                FireFX = new ParticleEffect(device, "FX/Fire.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                RainFX = new ParticleEffect(device, "FX/Rain.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref BasicFX);
            Util.ReleaseCom(ref TreeSpriteFX);
            Util.ReleaseCom(ref InstancedBasicFX);
            Util.ReleaseCom(ref SkyFX);
            Util.ReleaseCom(ref NormalMapFX);
            Util.ReleaseCom(ref DisplacementMapFX);
            Util.ReleaseCom(ref TerrainFX);
            Util.ReleaseCom(ref ColorFX);
            Util.ReleaseCom(ref InstancedNormalMapFX);

            Util.ReleaseCom(ref FireFX);
            Util.ReleaseCom(ref RainFX);
        }

        public static BasicEffect BasicFX;
        public static TreeSpriteEffect TreeSpriteFX;
        public static InstancedBasicEffect InstancedBasicFX;
        public static SkyEffect SkyFX;
        public static NormalMapEffect NormalMapFX;
        public static DisplacementMapEffect DisplacementMapFX;
        public static TerrainEffect TerrainFX;
        public static ColorEffect ColorFX;
        public static InstancedNormalMapEffect InstancedNormalMapFX;

        public static ParticleEffect FireFX;
        public static ParticleEffect RainFX;
    }
}
