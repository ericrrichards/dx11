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
        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref BasicFX);
            Util.ReleaseCom(ref TreeSpriteFX);


        }

        public static BasicEffect BasicFX;
        public static TreeSpriteEffect TreeSpriteFX;
    }
}
