namespace Core.Terrain {
    public struct InitInfo {
        // RAW heightmap image file or null for random terrain
        public string HeightMapFilename;
        // Heightmap maximum height
        public float HeightScale;
        // Heightmap dimensions
        public int HeightMapWidth;
        public int HeightMapHeight;
        // terrain diffuse textures
        public string LayerMapFilename0;
        public string LayerMapFilename1;
        public string LayerMapFilename2;
        public string LayerMapFilename3;
        public string LayerMapFilename4;
        // Blend map which indicates which diffuse map is
        // applied to which portions of the terrain
        // null if the blendmap should be generated
        public string BlendMapFilename;
        // The distance between vertices in the generated mesh
        public float CellSpacing;
        public Material? Material;
        // Random heightmap parameters
        public float NoiseSize1;
        public float NoiseSize2;
        public float Persistence1;
        public float Persistence2;
        public int Octaves1;
        public int Octaves2;
        public int Seed;
    }
}