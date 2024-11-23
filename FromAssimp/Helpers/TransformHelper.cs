using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Helpers
{
    internal static class TransformHelper
    {
        public static NumericsMatrix4x4 MirrorMatrixX
            = new NumericsMatrix4x4(-1, 0, 0, 0,
                                    0, 1, 0, 0,
                                    0, 0, 1, 0,
                                    0, 0, 0, 1);

        public static NumericsMatrix4x4 MirrorMatrixY
            = new NumericsMatrix4x4(1, 0, 0, 0,
                                    0, -1, 0, 0,
                                    0, 0, 1, 0,
                                    0, 0, 0, 1);

        public static NumericsMatrix4x4 MirrorMatrixZ
            = new NumericsMatrix4x4(1, 0, 0, 0,
                                    0, 1, 0, 0,
                                    0, 0, -1, 0,
                                    0, 0, 0, 1);

        public static NumericsMatrix4x4 MirrorMatrixXY
            = new NumericsMatrix4x4(-1, 0, 0, 0,
                                    0, -1, 0, 0,
                                    0, 0, 1, 0,
                                    0, 0, 0, 1);

        public static NumericsMatrix4x4 MirrorMatrixXZ
            = new NumericsMatrix4x4(-1, 0, 0, 0,
                                    0, 1, 0, 0,
                                    0, 0, -1, 0,
                                    0, 0, 0, 1);

        public static NumericsMatrix4x4 MirrorMatrixYZ
            = new NumericsMatrix4x4(1, 0, 0, 0,
                                    0, -1, 0, 0,
                                    0, 0, -1, 0,
                                    0, 0, 0, 1);

        public static NumericsMatrix4x4 MirrorMatrixXYZ
            = new NumericsMatrix4x4(-1, 0, 0, 0,
                                    0, -1, 0, 0,
                                    0, 0, -1, 0,
                                    0, 0, 0, 1);

        public const float FbxUnitMeters = 100.0f;
        public const double FbxUnitMetersDouble = 100.0d;

        public static NumericsMatrix4x4 GetMirrorMatrix(bool mirrorX, bool mirrorY, bool mirrorZ)
        {
            if (mirrorX && mirrorY && mirrorZ)
            {
                return MirrorMatrixXYZ;
            }
            else if (mirrorX && mirrorY)
            {
                return MirrorMatrixXY;
            }
            else if (mirrorX && mirrorZ)
            {
                return MirrorMatrixXZ;
            }
            else if (mirrorY && mirrorZ)
            {
                return MirrorMatrixYZ;
            }
            else if (mirrorX)
            {
                return MirrorMatrixX;
            }
            else if (mirrorY)
            {
                return MirrorMatrixY;
            }
            else if (mirrorZ)
            {
                return MirrorMatrixZ;
            }

            return NumericsMatrix4x4.Identity;
        }
    }
}
