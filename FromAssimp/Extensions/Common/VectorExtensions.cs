using System.Numerics;

namespace FromAssimp.Extensions.Common
{
    internal static class VectorExtensions
    {
        internal static Vector3 ToNumericsVector3(this Vector4 vector)
            => new(vector.X, vector.Y, vector.Z);
    }
}
