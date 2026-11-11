namespace HexMapEditor {
    // Compressed source bytes, serialized files and decoded working buffers are separate budgets.
    internal static class GraphicLimits {
        internal const int SourceBytes=32*1024*1024, SourceSide=4096, SourceCount=64;
        internal const long DocumentSources=96L*1024*1024, LibrarySources=192L*1024*1024;
        internal const int Definitions=256, LibraryEntries=128, Pool=32, CropPreviewSide=1024, BackgroundSide=2048;
        internal const int TileSide=256, TileCacheEntries=256;
        internal const int MapBytes=256*1024*1024, PresetBytes=160*1024*1024;
        internal const long SourceJsonBytes=(SourceBytes+2L)/3*4+65536;
        internal static long Encoded(long bytes){return (bytes+2)/3*4;}
    }
}
