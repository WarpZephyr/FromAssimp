using Assimp;
using System.Numerics;
using AssimpMatrix4x4 = Assimp.Matrix4x4;

namespace FromAssimp.Extensions.Assimp
{
    public static class VectorExtensions
    {
        public static Vector2 ToNumericsVector2(this Vector2D vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static Vector2 ToNumericsVector2(this Vector2D vector, AssimpMatrix4x4 transform)
        {
            var newVector = new Vector2(vector.X, vector.Y);
            return Vector2.Transform(newVector, transform.ToNumericsMatrix4x4());
        }

        public static Vector3 ToNumericsVector3(this Vector3D vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }
    }
}
