using Assimp;
using SoulsFormats;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using FromAssimp.Extensions.Numerics;
using FromAssimp.Extensions.Common;
using FromAssimp.Extensions.Assimp;

namespace FromAssimp
{
    public static class Smd4Import
    {
        private static void CollectBones(Node rootNode, IReadOnlyList<SMD4.Bone> bones, out Bone[] newBones)
        {
            int count = bones.Count;
            Node[] newBoneNodes = new Node[count];
            newBones = new Bone[count];

            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                var bone = bones[boneIndex];
                Node parentNode;
                if (bone.ParentIndex == -1)
                {
                    parentNode = rootNode;
                }
                else
                {
                    parentNode = newBoneNodes[bone.ParentIndex];
                }

                Node newNode = new Node(bone.Name, parentNode);
                Bone newBone = new Bone();
                NumericsMatrix4x4 worldTransform = bone.ComputeWorldTransform(bones);
                NumericsMatrix4x4.Invert(worldTransform, out NumericsMatrix4x4 inverseWorldTransform);

                newNode.Transform = bone.ComputeLocalTransform().ToAssimpMatrix4x4();
                newBone.Name = bone.Name;
                newBone.OffsetMatrix = inverseWorldTransform.ToAssimpMatrix4x4();

                parentNode.Children.Add(newNode);
                newBoneNodes[boneIndex] = newNode;
                newBones[boneIndex] = newBone;
            }
        }

        private static void CollectVertices(List<SMD4.Vertex> vertices, Mesh newMesh, IList<int> boneIndices, Bone[] newBones, byte vertexFormat, out Dictionary<int, Bone> boneMap)
        {
            // Prepare a bone map
            boneMap = new Dictionary<int, Bone>(boneIndices.Count);
            bool hasBones = boneIndices.Count > 0;

            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                // Add Position
                var vertex = vertices[vertexIndex];
                if (hasBones && vertexFormat > 0) // Multiple bones per vertex.
                {
                    newMesh.Vertices.Add(vertex.Position.ToAssimpVector3D());

                    for (int i = 0; i < 4; i++)
                    {
                        int boneIndex = vertex.BoneIndices[i];
                        float boneWeight = vertex.BoneWeights[i];
                        if (boneWeight > 0)
                        {
                            // If the bone map does not already have the bone add it.
                            if (boneIndex >= 0 && !boneMap.ContainsKey(boneIndex))
                            {
                                var newBone = new Bone();
                                newBone.Name = newBones[boneIndex].Name;
                                newBone.OffsetMatrix = newBones[boneIndex].OffsetMatrix;
                                boneMap.Add(boneIndex, newBone);
                            }

                            if (!boneMap[boneIndex].VertexWeights.Any(x => x.VertexID == vertexIndex))
                            {
                                boneMap[boneIndex].VertexWeights.Add(new VertexWeight(vertexIndex, boneWeight));
                            }
                        }
                    }
                }
                else if (hasBones && vertexFormat == 0) // Single bone per vertex.
                {
                    // Get the local bone index from the only bone index, then the final bone index from the mesh.
                    var boneIndex = boneIndices[vertex.BoneIndices[0]];
                    var bone = newBones[boneIndex];
                    NumericsMatrix4x4.Invert(bone.OffsetMatrix.ToNumericsMatrix4x4(), out NumericsMatrix4x4 worldTransform);

                    newMesh.Vertices.Add(vertex.Position.ToAssimpVector3D(worldTransform));

                    // If the bone map does not already have the bone add it.
                    if (boneIndex >= 0 && !boneMap.ContainsKey(boneIndex))
                    {
                        var newBone = new Bone();
                        newBone.Name = newBones[boneIndex].Name;
                        newBone.OffsetMatrix = newBones[boneIndex].OffsetMatrix;
                        boneMap.Add(boneIndex, newBone);
                    }

                    // Add this vertex weight to it's bone
                    boneMap[boneIndex].VertexWeights.Add(new VertexWeight(vertexIndex, 1f));
                }
                else // No bones
                {
                    newMesh.Vertices.Add(vertex.Position.ToAssimpVector3D());
                }
            }
        }

        private static void CollectMeshes(Scene scene, SMD4 model, Bone[] newBones, out int[] consumedBoneIndices)
        {
            List<int> consumedBoneIndicesList = new List<int>(model.Bones.Count);
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var meshNode = new Node($"Mesh_{meshIndex}", scene.RootNode);
                var newMesh = new Mesh($"Mesh_M{meshIndex}", PrimitiveType.Triangle);

                // Collect faces
                var faceIndices = mesh.GetFaceIndices(true, true);
                foreach (ushort[] indices in faceIndices)
                {
                    newMesh.Faces.Add(new Face(new int [] { indices[2], indices[1], indices[0] }));
                }

                CollectVertices(mesh.Vertices, newMesh, mesh.BoneIndices.ToIntArray(), newBones, mesh.VertexFormat, out Dictionary<int, Bone> boneMap);

                // Add Bone references holding bone weights to the mesh
                newMesh.Bones.AddRange(boneMap.Values);
                consumedBoneIndicesList.AddRange(boneMap.Keys);

                // Collect Bones referenced in this mesh, but not in it's vertices.
                for (int i = 0; i < mesh.BoneIndices.Length; i++)
                {
                    int boneIndex = mesh.BoneIndices[i];
                    if (boneIndex >= 0 && !boneMap.ContainsKey(boneIndex))
                    {
                        var bone = model.Bones[boneIndex];
                        NumericsMatrix4x4.Invert(bone.ComputeWorldTransform(model.Bones), out NumericsMatrix4x4 inverseWorldTransform);
                        newMesh.Bones.Add(new Bone(bone.Name, inverseWorldTransform.ToAssimpMatrix4x4(), Array.Empty<VertexWeight>()));
                        consumedBoneIndicesList.Add(boneIndex);
                    }
                }

                meshNode.Transform = AssimpMatrix4x4.Identity;
                meshNode.MeshIndices.Add(meshIndex);

                scene.RootNode.Children.Add(meshNode);
                scene.Meshes.Add(newMesh);
            }
            consumedBoneIndices = consumedBoneIndicesList.ToArray();
        }

        public static Scene ToAssimpScene(this SMD4 model)
        {
            var scene = new Scene();
            scene.RootNode = new Node();

            // Add materials
            Material newMaterial = new()
            {
                Name = "default"
            };
            scene.Materials.Add(newMaterial);

            CollectBones(scene.RootNode, model.Bones, out Bone[] newBones);

            int[] consumedBoneIndices = Array.Empty<int>();
            CollectMeshes(scene, model, newBones, out consumedBoneIndices);

            for (int i = 0; i < model.Bones.Count; i++)
            {
                if (!consumedBoneIndices.Contains(i))
                {
                    var bone = model.Bones[i];
                    NumericsMatrix4x4.Invert(bone.ComputeWorldTransform(model.Bones), out NumericsMatrix4x4 inverseWorldTransform);

                    if (scene.MeshCount == 0)
                    {
                        string newMeshName = "ASSIMP_MESH_PLACEHOLDER";
                        Node newMeshNode = new Node(newMeshName, scene.RootNode);
                        newMeshNode.Transform = AssimpMatrix4x4.Identity;
                        newMeshNode.MeshIndices.Add(0);
                        scene.RootNode.Children.Add(newMeshNode);

                        Mesh newMesh = new Mesh(newMeshName, PrimitiveType.Triangle);
                        newMesh.MaterialIndex = 0;
                        scene.Meshes.Add(newMesh);
                    }

                    scene.Meshes[0].Bones.Add(new Bone(bone.Name, inverseWorldTransform.ToAssimpMatrix4x4(), Array.Empty<VertexWeight>()));
                }
            }

            return scene;
        }
    }
}
