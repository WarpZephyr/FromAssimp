using System.Numerics;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Extensions
{
    internal static class MatrixExtensions
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

        /// <summary>
        /// Convert an <see cref="AssimpMatrix4x4"/> into a <see cref="NumericsMatrix4x4"/>.
        /// </summary>
        /// <param name="mat4">An <see cref="AssimpMatrix4x4"/>.</param>
        /// <returns>A <see cref="NumericsMatrix4x4"/>.</returns>
        public static NumericsMatrix4x4 ToNumericsMatrix4x4(this AssimpMatrix4x4 mat4)
        {
            return new NumericsMatrix4x4(mat4.A1, mat4.B1, mat4.C1, mat4.D1,
                                 mat4.A2, mat4.B2, mat4.C2, mat4.D2,
                                 mat4.A3, mat4.B3, mat4.C3, mat4.D3,
                                 mat4.A4, mat4.B4, mat4.C4, mat4.D4);
        }

        /// <summary>
        /// Convert a <see cref="NumericsMatrix4x4"/>'s rotation to a euler XZY rotation vector.
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>A euler XZY rotation vector.</returns>
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
