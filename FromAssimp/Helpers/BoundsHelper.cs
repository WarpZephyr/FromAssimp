using System.Numerics;

namespace FromAssimp.Helpers
{
    internal static class BoundsHelper
    {
        public static Vector3 MinVector3 = new Vector3(float.MinValue);
        public static Vector3 MaxVector3 = new Vector3(float.MaxValue);

        public static Vector3 CorrectBounds(Vector3 bounds, bool min)
        {
            var target = min ? MaxVector3 : MinVector3;
            var correct = min ? MinVector3 : MaxVector3;

            if (bounds == target)
                return correct;

            var targetF = min ? float.MaxValue : float.MinValue;
            var correctF = min ? float.MinValue : float.MaxValue;
            var x = bounds.X;
            var y = bounds.Y;
            var z = bounds.Z;

            bool changed = false;
            if (x == targetF)
            {
                x = correctF;
                changed = true;
            }

            if (y == targetF)
            {
                y = correctF;
                changed = true;
            }

            if (z == targetF)
            {
                z = correctF;
                changed = true;
            }

            if (changed)
            {
                return new Vector3(x, y, z);
            }

            return bounds;
        }

        public static Vector3 GetMin(Vector3 min, Vector3 pos)
        {
            var minX = min.X;
            var minY = min.Y;
            var minZ = min.Z;

            if (pos.X < minX)
                minX = pos.X;
            if (pos.Y < minY)
                minY = pos.Y;
            if (pos.Z < minZ)
                minZ = pos.Z;

            if (minX != min.X || minY != min.Y || minZ != min.Z)
                return new Vector3(minX, minY, minZ);

            return min;
        }

        public static Vector3 GetMax(Vector3 max, Vector3 pos)
        {
            var maxX = max.X;
            var maxY = max.Y;
            var maxZ = max.Z;

            if (pos.X > maxX)
                maxX = pos.X;
            if (pos.Y > maxY)
                maxY = pos.Y;
            if (pos.Z > maxZ)
                maxZ = pos.Z;

            if (maxX != max.X || maxY != max.Y || maxZ != max.Z)
                return new Vector3(maxX, maxY, maxZ);

            return max;
        }

        public static void GetMin(ref float minX, ref float minY, ref float minZ, Vector3 pos)
        {
            if (pos.X < minX)
                minX = pos.X;
            if (pos.Y < minY)
                minY = pos.Y;
            if (pos.Z < minZ)
                minZ = pos.Z;
        }

        public static void GetMax(ref float maxX, ref float maxY, ref float maxZ, Vector3 pos)
        {
            if (pos.X > maxX)
                maxX = pos.X;
            if (pos.Y > maxY)
                maxY = pos.Y;
            if (pos.Z > maxZ)
                maxZ = pos.Z;
        }
    }
}
