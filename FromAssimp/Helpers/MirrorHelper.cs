using Assimp;

namespace FromAssimp.Helpers
{
    internal static class MirrorHelper
    {
        public static Vector3D MirrorVector3D(Vector3D vector, Matrix4x4 mirror)
        {
            vector.X *= mirror.A1;
            vector.Y *= mirror.B2;
            vector.Z *= mirror.C3;
            return vector;
        }

        public static void MirrorNode(Scene scene, Node node, Matrix4x4 mirror)
        {
            static bool IsBone(Scene scene, Node node)
                => scene.Meshes.Find(m => m.Bones.Find(b => b.Name == node.Name) != null) != null;

            if (IsBone(scene, node))
            {
                node.Transform *= mirror;
            }
            else
            {
                foreach (var child in node.Children)
                {
                    MirrorNode(scene, child, mirror);
                }
            }
        }

        public static void MirrorBone(Bone bone, Matrix4x4 mirror)
        {
            bone.OffsetMatrix *= mirror;
        }

        public static void MirrorMesh(Mesh mesh, Matrix4x4 mirror)
        {
            foreach (var bone in mesh.Bones)
            {
                MirrorBone(bone, mirror);
            }

            var inverseTransposeMirror = mirror;
            inverseTransposeMirror.Inverse();
            inverseTransposeMirror.Transpose();

            bool hasNormals = mesh.Normals.Count > 0;
            bool hasTangents = mesh.Tangents.Count > 0;
            bool hasBitangents = mesh.BiTangents.Count > 0;
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                mesh.Vertices[i] = MirrorVector3D(mesh.Vertices[i], mirror);

                if (hasNormals)
                {
                    var normal = MirrorVector3D(mesh.Normals[i], inverseTransposeMirror);
                    normal.Normalize();
                    mesh.Normals[i] = normal;
                }

                if (hasTangents)
                {
                    var tangent = MirrorVector3D(mesh.Tangents[i], inverseTransposeMirror);
                    tangent.Normalize();
                    mesh.Tangents[i] = tangent;
                }

                if (hasBitangents)
                {
                    var bitangent = MirrorVector3D(mesh.BiTangents[i], inverseTransposeMirror);
                    bitangent.Normalize();
                    mesh.BiTangents[i] = bitangent;
                }
            }

            foreach (var face in mesh.Faces)
            {
                face.Indices.Reverse();
            }
        }

        public static void MirrorScene(Scene scene, Matrix4x4 mirror)
        {
            if (mirror == Matrix4x4.Identity)
                return;

            MirrorNode(scene, scene.RootNode, mirror);
            foreach (var mesh in scene.Meshes)
            {
                MirrorMesh(mesh, mirror);
            }
        }
    }
}
