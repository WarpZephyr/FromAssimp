using Assimp;
using System.Numerics;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Extensions.Numerics
{
    public static class VectorExtensions
    {
        public static Vector2D ToAssimpVector2D(this Vector2 vector)
        {
            return new Vector2D(vector.X, vector.Y);
        }

        public static Vector2D ToAssimpVector2D(this Vector2 vector, NumericsMatrix4x4 transform)
        {
            vector = Vector2.Transform(vector, transform);
            return new Vector2D(vector.X, vector.Y);
        }

        public static Vector2D ToAssimpVector2DNormal(this Vector2 vector, NumericsMatrix4x4 transform)
        {
            vector = Vector2.TransformNormal(vector, transform);
            return new Vector2D(vector.X, vector.Y);
        }

        public static Vector3D ToAssimpVector3D(this Vector3 vector)
        {
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector3D ToAssimpVector3D(this Vector3 vector, NumericsMatrix4x4 transform)
        {
            vector = Vector3.Transform(vector, transform);
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector3D ToAssimpVector3DNormal(this Vector3 vector, NumericsMatrix4x4 transform)
        {
            vector = Vector3.TransformNormal(vector, transform);
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector3D ToAssimpVector3D(this Vector4 vector)
        {
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector3D ToAssimpVector3D(this Vector4 vector, NumericsMatrix4x4 transform)
        {
            vector = Vector4.Transform(vector, transform);
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector3D ToAssimpVector3DNormal(this Vector4 vector, NumericsMatrix4x4 transform)
        {
            vector = Vector4.Transform(vector, transform);
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Vector3 NegateX(this Vector3 vector)
        {
            return new Vector3(-vector.X, vector.Y, vector.Z);
        }

        public static Vector3 NegateY(this Vector3 vector)
        {
            return new Vector3(vector.X, -vector.Y, vector.Z);
        }

        public static Vector3 NegateZ(this Vector3 vector)
        {
            return new Vector3(vector.X, vector.Y, -vector.Z);
        }
    }
}
