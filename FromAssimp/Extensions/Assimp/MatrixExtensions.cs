using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Extensions.Assimp
{
    public static class MatrixExtensions
    {
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
    }
}
