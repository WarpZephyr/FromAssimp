using System.Numerics;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Extensions.Numerics
{
    public static class MatrixExtensions
    {
        /// <summary>
        /// Convert a <see cref="NumericsMatrix4x4"/> into an <see cref="AssimpMatrix4x4"/>.
        /// </summary>
        /// <param name="mat4">A <see cref="NumericsMatrix4x4"/>.</param>
        /// <returns>An <see cref="AssimpMatrix4x4"/>.</returns>
        public static AssimpMatrix4x4 ToAssimpMatrix4x4(this NumericsMatrix4x4 mat4)
        {
            return new AssimpMatrix4x4(mat4.M11, mat4.M21, mat4.M31, mat4.M41,
                                        mat4.M12, mat4.M22, mat4.M32, mat4.M42,
                                        mat4.M13, mat4.M23, mat4.M33, mat4.M43,
                                        mat4.M14, mat4.M24, mat4.M34, mat4.M44);
        }

        public static Vector3 ToEulerXZY(this NumericsMatrix4x4 m)
        {
            Vector3 ret;
            ret.Z = MathF.Asin(-Math.Clamp(-m.M12, -1, 1));

            if (Math.Abs(m.M12) < 0.9999999)
            {
                ret.X = MathF.Atan2(-m.M32, m.M22);
                ret.Y = MathF.Atan2(-m.M13, m.M11);
            }
            else
            {
                ret.X = MathF.Atan2(m.M23, m.M33);
                ret.Y = 0;
            }

            float pi = MathHelper.Pi;
            float negPi = -pi;
            float doublePi = 2 * pi;

            ret.X = ret.X <= negPi ? ret.X + doublePi : ret.X;
            ret.Y = ret.Y <= negPi ? ret.Y + doublePi : ret.Y;
            ret.Z = ret.Z <= negPi ? ret.Z + doublePi : ret.Z;
            return ret;
        }
    }
}
