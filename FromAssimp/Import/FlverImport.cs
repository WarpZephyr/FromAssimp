using Assimp;
using SoulsFormats;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using FromAssimp.Extensions.Numerics;
using FromAssimp.Extensions.Common;
using FromAssimp.Extensions.Assimp;

namespace FromAssimp
{
    public static class FlverImport
    {
        private static void CollectBones(Node rootNode, IReadOnlyList<FLVER.Bone> bones, out Bone[] newBones)
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

        private static void CollectVertices(List<FLVER.Vertex> vertices, Mesh newMesh, IList<int> boneIndices, Bone[] newBones, byte dynamic, out Dictionary<int, Bone> boneMap)
        {
            // Prepare a bone map
            boneMap = new Dictionary<int, Bone>(boneIndices.Count);
            bool hasBones = boneIndices.Count > 0;

            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                // Add Position
                var vertex = vertices[vertexIndex];
                if (hasBones && dynamic == 0)
                {
                    int boneIndex = boneIndices[vertex.NormalW];
                    NumericsMatrix4x4.Invert(newBones[boneIndex].OffsetMatrix.ToNumericsMatrix4x4(), out NumericsMatrix4x4 worldTransform);
                    newMesh.Vertices.Add(vertex.Position.ToAssimpVector3D(worldTransform));

                    // If the bone map does not already have the bone add it
                    if (!boneMap.ContainsKey(boneIndex))
                    {
                        var aiBone = new Bone();
                        aiBone.Name = newBones[boneIndex].Name;
                        aiBone.OffsetMatrix = newBones[boneIndex].OffsetMatrix;
                        boneMap.Add(boneIndex, aiBone);
                    }

                    boneMap[boneIndex].VertexWeights.Add(new VertexWeight(vertexIndex, 1f));
                }
                else if (hasBones && dynamic == 1)
                {
                    newMesh.Vertices.Add(vertex.Position.ToAssimpVector3D());

                    for (int i = 0; i < 4; i++)
                    {
                        int boneIndex = vertex.BoneIndices[i];
                        float boneWeight = vertex.BoneWeights[i];
                        if (boneWeight > 0)
                        {
                            // If the bone map does not already have the bone add it
                            if (!boneMap.ContainsKey(boneIndex))
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
                else
                {
                    newMesh.Vertices.Add(vertex.Position.ToAssimpVector3D());
                }

                // Add Normal, BiTangent, and Tangents
                newMesh.Normals.Add(vertex.Normal.ToAssimpVector3D());
                newMesh.BiTangents.Add(vertex.Bitangent.ToAssimpVector3D());
                foreach (var tangent in vertex.Tangents)
                {
                    newMesh.Tangents.Add(tangent.ToAssimpVector3D());
                }

                // Add UVs
                int uvCount = vertex.UVs.Count;
                for (int i = 0; i < 4; i++)
                {
                    Vector3D textureCoordinate;
                    if (i < uvCount)
                    {
                        var uv = vertex.UVs[i];
                        textureCoordinate = new Vector3D(uv.X, uv.Y, 0f);
                    }
                    else
                    {
                        textureCoordinate = new Vector3D(1f, 1f, 1f);
                    }
                    newMesh.TextureCoordinateChannels[i].Add(textureCoordinate);
                }

                // Add Colors
                int colorCount = vertex.Colors.Count;
                for (int i = 0; i < 2; i++)
                {
                    if (i < colorCount)
                    {
                        var color = vertex.Colors[i];
                        newMesh.VertexColorChannels[i].Add(new Color4D(color.R, color.G, color.B, color.A));
                    }
                }
            }
        }

        private static void CollectMeshes(Scene scene, FLVER0 model, Bone[] newBones, out int[] consumedBoneIndices)
        {
            List<int> consumedBoneIndicesList = new List<int>(model.Bones.Count);
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var meshNode = new Node($"Mesh_{meshIndex}", scene.RootNode);
                var newMesh = new Mesh($"Mesh_M{meshIndex}", PrimitiveType.Triangle);

                // Collect Faces
                var faceIndices = mesh.GetFaceIndices(model.Header.Version);
                foreach (int[] indices in faceIndices)
                {
                    newMesh.Faces.Add(new Face(indices));
                }

                // Each UV is only X and Y so set the component count to 2
                for (int i = 0; i < newMesh.TextureCoordinateChannelCount; i++)
                {
                    newMesh.UVComponentCount[i] = 2;
                }

                CollectVertices(mesh.Vertices, newMesh, mesh.BoneIndices.ToIntArray(), newBones, mesh.Dynamic, out Dictionary<int, Bone> boneMap);
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
                newMesh.MaterialIndex = mesh.MaterialIndex;

                scene.RootNode.Children.Add(meshNode);
                scene.Meshes.Add(newMesh);
            }
            consumedBoneIndices = consumedBoneIndicesList.ToArray();
        }

        private static void CollectMeshes(Scene scene, FLVER2 model, Bone[] newBones, out int[] consumedBoneIndices)
        {
            List<int> consumedBoneIndicesList = new List<int>(model.Bones.Count);
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var meshNode = new Node($"Mesh_{meshIndex}", scene.RootNode);
                var newMesh = new Mesh($"Mesh_M{meshIndex}", PrimitiveType.Triangle);

                // Collect Faces
                foreach (var faceset in mesh.FaceSets)
                {
                    var indices = faceset.Triangulate(mesh.Vertices.Count < ushort.MaxValue);
                    for (int i = 0; i < indices.Count - 2; i += 3)
                    {
                        newMesh.Faces.Add(new Face(new int[] { indices[i], indices[i + 1], indices[i + 2] }));
                    }
                }

                // Each UV is only X and Y so set the component count to 2
                for (int i = 0; i < newMesh.TextureCoordinateChannelCount; i++)
                {
                    newMesh.UVComponentCount[i] = 2;
                }

                CollectVertices(mesh.Vertices, newMesh, mesh.BoneIndices, newBones, mesh.Dynamic, out Dictionary<int, Bone> boneMap);

                // Add Bone references holding bone weights to the mesh
                newMesh.Bones.AddRange(boneMap.Values);
                consumedBoneIndicesList.AddRange(boneMap.Keys);

                // Collect Bones referenced in this mesh, but not in it's vertices.
                for (int i = 0; i < mesh.BoneIndices.Count; i++)
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
                newMesh.MaterialIndex = mesh.MaterialIndex;

                scene.RootNode.Children.Add(meshNode);
                scene.Meshes.Add(newMesh);
            }
            consumedBoneIndices = consumedBoneIndicesList.ToArray();
        }

        public static Scene ToAssimpScene(this IFlver model)
        {
            var scene = new Scene();
            scene.RootNode = new Node();

            // Collect Materials
            foreach (var material in model.Materials)
            {
                Material newMaterial = new Material();
                newMaterial.Name = material.Name;
                scene.Materials.Add(newMaterial);
            }

            CollectBones(scene.RootNode, model.Bones, out Bone[] newBones);

            int[] consumedBoneIndices = Array.Empty<int>();
            if (model is FLVER0 flver0)
            {
                CollectMeshes(scene, flver0, newBones, out consumedBoneIndices);
            }
            else if (model is FLVER2 flver2)
            {
                CollectMeshes(scene, flver2, newBones, out consumedBoneIndices);
            }

            // Collect Bones not referenced in Meshes or Vertices.
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

                        if (scene.MaterialCount == 0)
                        {
                            Material newMaterial = new()
                            {
                                Name = "default"
                            };
                            scene.Materials.Add(newMaterial);
                        }

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
