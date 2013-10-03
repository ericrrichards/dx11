using System;
using Core.FX;
using SlimDX.Direct3D11;

namespace Core {
    public static class InputLayouts {
        public static void InitAll(Device device) {
            var bl1 = Effects.BasicFX;
            if (bl1 != null) {
                try {
                    var passDesc = bl1.Light1Tech.GetPassByIndex(0).Description;
                    if (passDesc.Signature != null) PosNormal = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.PosNormal);
                } catch (Exception dex) {
                    Console.WriteLine(dex.Message );
                    PosNormal = null;
                }
                try {
                    var passDesc = bl1.Light1Tech.GetPassByIndex(0).Description;
                    Basic32 = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.Basic32);
                } catch (Exception dex) {
                    Console.WriteLine(dex.Message );
                    Basic32 = null;
                }
            }
            try {
                var ibl1 = Effects.InstancedBasicFX;
                if (ibl1 != null) {
                    var shaderSignature = ibl1.Light1Tech.GetPassByIndex(0).Description.Signature;
                    InstancedBasic32 = new InputLayout(device, shaderSignature, InputLayoutDescriptions.InstancedBasic32);
                }
            } catch (Exception dex) {
                Console.WriteLine(dex.Message + dex.StackTrace);
                InstancedBasic32 = null;
            }
            try {
                var tsl3 = Effects.TreeSpriteFX;
                if (tsl3 != null) {
                    var passDesc = tsl3.Light3Tech.GetPassByIndex(0).Description;
                    TreePointSprite = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.TreePointSprite);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message + ex.StackTrace);
                TreePointSprite = null;
            }
            try {
                var skyTech = Effects.SkyFX;
                if (skyTech != null) {
                    var passDesc = skyTech.SkyTech.GetPassByIndex(0).Description;
                    Pos = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.Pos);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message + ex.StackTrace);
                Pos = null;
            }
            try {
                var tech = Effects.NormalMapFX;
                if (tech != null) {
                    var passDesc = tech.Light1Tech.GetPassByIndex(0).Description;
                    PosNormalTexTan = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.PosNormalTexTan);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message + ex.StackTrace);
                PosNormalTexTan = null;
            }
            try {
                var tech = Effects.TerrainFX;
                if (tech != null) {
                    var passDesc = tech.Light1Tech.GetPassByIndex(0).Description;
                    TerrainCP = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.TerrainCP);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                TerrainCP = null;
            }
            try {
                var tech = Effects.ColorFX;
                if (tech != null) {
                    var passDesc = tech.ColorTech.GetPassByIndex(0).Description;
                    PosColor = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.PosColor);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                PosColor = null;
            }
            try {
                var tech = Effects.BasicFX;
                if (tech != null) {
                    var passDesc = tech.Light1SkinnedTech.GetPassByIndex(0).Description;
                    PosNormalTexTanSkinned = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.PosNormalTexTanSkinned);
                } else if ((tech = Effects.NormalMapFX) != null) {
                    var passDesc = tech.Light1SkinnedTech.GetPassByIndex(0).Description;
                    PosNormalTexTanSkinned = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.PosNormalTexTanSkinned);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                PosNormalTexTanSkinned = null;
            }
            try {
                var tech = Effects.InstancedNormalMapFX;
                if (tech != null) {
                    var passDesc = tech.Light1Tech.GetPassByIndex(0).Description;
                    InstancedPosNormalTexTan = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.InstancedPosNormalTexTan);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message + ex.StackTrace);
                InstancedPosNormalTexTan = null;
            }
            try {
                var tech = Effects.FireFX;
                if (tech != null) {
                    var passDesc = tech.StreamOutTech.GetPassByIndex(0).Description;
                    Particle = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.Particle);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message + ex.StackTrace);
                Particle = null;
            }
            
        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref Pos);
            Util.ReleaseCom(ref PosNormal);
            Util.ReleaseCom(ref Basic32);
            Util.ReleaseCom(ref TreePointSprite);
            Util.ReleaseCom(ref InstancedBasic32);
            Util.ReleaseCom(ref PosNormalTexTan);
            Util.ReleaseCom(ref TerrainCP);
            Util.ReleaseCom(ref PosColor);
            Util.ReleaseCom(ref PosNormalTexTanSkinned);
            Util.ReleaseCom(ref InstancedPosNormalTexTan);
            Util.ReleaseCom(ref Particle);
        }

        public static InputLayout PosNormal;
        public static InputLayout Basic32;
        public static InputLayout TreePointSprite;
        public static InputLayout InstancedBasic32;
        public static InputLayout Pos;
        public static InputLayout PosNormalTexTan;
        public static InputLayout TerrainCP;
        public static InputLayout PosColor;
        public static InputLayout PosNormalTexTanSkinned;
        public static InputLayout InstancedPosNormalTexTan;
        public static InputLayout Particle;
    }
}