using NvTriStripDotNet;
using System.Diagnostics.CodeAnalysis;

namespace FromAssimp.Helpers
{
    internal static class IndexHelper
    {
        public const int DefaultPrimitiveRestart = ushort.MaxValue;
        public const int DefaultCacheSize = NvStripifier.CACHESIZE_GEFORCE1_2;
        public const int Ps3CacheSize = NvStripifier.CACHESIZE_RSX;

        public static bool TryStripify(List<int> indices, [NotNullWhen(true)] out List<int>? triangleStrip, int restart = DefaultPrimitiveRestart, int cacheSize = DefaultCacheSize)
        {
            ushort[] indexBuffer = new ushort[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                if (indices[i] > ushort.MaxValue)
                    throw new Exception();

                indexBuffer[i] = (ushort)indices[i];
            }

            var stripifier = new NvStripifier();
            stripifier.UseRestart = true;
            stripifier.RestartValue = restart;
            stripifier.CacheSize = cacheSize;
            stripifier.ListsOnly = false;
            stripifier.MinStripSize = 0; // minimum triangle count in a strip is 0
            stripifier.StitchStrips = true;
            if (stripifier.GenerateStrips(indexBuffer, out var result, false))
            {
                var group = result[0];
                triangleStrip = new List<int>(group.Indices.Length);
                for (int i = 0; i < group.Indices.Length; i++)
                {
                    triangleStrip.Add(group.Indices[i]);
                }

                return true;
            }

            triangleStrip = null;
            return false;
        }
    }
}
