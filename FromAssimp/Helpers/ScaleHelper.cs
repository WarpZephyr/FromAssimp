using Assimp;

namespace FromAssimp.Helpers
{
    internal static class ScaleHelper
    {
        public static Matrix4x4 ScaleMatrix4x4(Matrix4x4 matrix, float scaleX, float scaleY, float scaleZ)
        {
            matrix.A4 *= scaleX;
            matrix.B4 *= scaleY;
            matrix.C4 *= scaleZ;
            return matrix;
        }

        public static Vector3D ScaleVector3D(Vector3D vector, float scaleX, float scaleY, float scaleZ)
        {
            vector.X *= scaleX;
            vector.Y *= scaleY;
            vector.Z *= scaleZ;
            return vector;
        }

        public static void ScaleNode(Node node, float scaleX, float scaleY, float scaleZ)
        {
            node.Transform = ScaleMatrix4x4(node.Transform, scaleX, scaleY, scaleZ);
            foreach (var child in node.Children)
            {
                ScaleNode(child, scaleX, scaleY, scaleZ);
            }
        }

        public static void ScaleBone(Bone bone, float scaleX, float scaleY, float scaleZ)
        {
            bone.OffsetMatrix = ScaleMatrix4x4(bone.OffsetMatrix, scaleX, scaleY, scaleZ);
        }

        public static void ScaleMesh(Mesh mesh, float scaleX, float scaleY, float scaleZ)
        {
            foreach (var bone in mesh.Bones)
            {
                ScaleBone(bone, scaleX, scaleY, scaleZ);
            }

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                mesh.Vertices[i] = ScaleVector3D(mesh.Vertices[i], scaleX, scaleY, scaleZ);
            }
        }

        public static void ScaleScene(Scene scene, float scaleX, float scaleY, float scaleZ)
        {
            if (scaleX == 1.0f && scaleY == 1.0f && scaleZ == 1.0f)
                return;

            ScaleNode(scene.RootNode, scaleX, scaleY, scaleZ);
            foreach (var mesh in scene.Meshes)
            {
                ScaleMesh(mesh, scaleX, scaleY, scaleZ);
            }
        }

        public static void ScaleSceneUniform(Scene scene, float scale)
        {
            if (scale == 1.0f)
                return;

            ScaleScene(scene, scale, scale, scale);
        }
    }
}
