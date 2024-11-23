using Assimp;
using System.Numerics;

namespace FromAssimp.Extensions
{
    internal static class VectorExtensions
    {
        public static Vector3D ToAssimpVector3D(this Vector3 vector)
        {
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector3D ToAssimpVector3D(this Vector4 vector)
        {
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector2 ToNumericsVector2(this Vector2D vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static Vector3 ToNumericsVector3(this Vector3D vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector3 ToNumericsVector3(this Vector4 vector)
            => new(vector.X, vector.Y, vector.Z);
    }
}
