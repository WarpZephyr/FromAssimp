using Assimp;
using SoulsFormats;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using FromAssimp.Extensions.Numerics;
using FromAssimp.Extensions.Common;
using FromAssimp.Extensions.Assimp;
using System.Numerics;

namespace FromAssimp
{
    public static class Mdl4Import
    {
        private static void CollectBones(Node rootNode, IReadOnlyList<MDL4.Bone> bones, out Bone[] newBones)
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

        private static void CollectVertices(List<MDL4.Vertex> vertices, Mesh newMesh, IList<int> boneIndices, Bone[] newBones, byte vertexFormat, out Dictionary<int, Bone> boneMap)
        {
            // Prepare a bone map
            boneMap = new Dictionary<int, Bone>(boneIndices.Count);
            bool hasBones = boneIndices.Count > 0;

            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                var vertex = vertices[vertexIndex];
                var position = vertex.Position;
                var normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                var normalW = (int)vertex.Normal.W;
                var tangent = new Vector3(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z);
                var bitangent = new Vector3(vertex.Bitangent.X, vertex.Bitangent.Y, vertex.Bitangent.Z);

                bool transformPosition = false;
                int boneTransformationIndex = -1;
                if (hasBones)
                {
                    if (vertexFormat == 0)
                    {
                        transformPosition = true;
                        boneTransformationIndex = boneIndices[(int)vertex.Normal.W];
                        if (boneTransformationIndex > -1)
                        {
                            // If the bone map does not already have the bone add it
                            if (!boneMap.ContainsKey(boneTransformationIndex))
                            {
                                var newBone = new Bone();
                                newBone.Name = newBones[boneTransformationIndex].Name;
                                newBone.OffsetMatrix = newBones[boneTransformationIndex].OffsetMatrix;
                                boneMap.Add(boneTransformationIndex, newBone);
                            }

                            // Add this vertex weight to it's bone
                            boneMap[boneTransformationIndex].VertexWeights.Add(new VertexWeight(vertexIndex, 1f));
                        }
                    }
                    else if (vertexFormat != 0)
                    {
                        if (vertex.BoneIndices[0] == vertex.BoneIndices[1] && vertex.BoneIndices[0] == vertex.BoneIndices[2] && vertex.BoneIndices[0] == vertex.BoneIndices[3])
                        {
                            boneTransformationIndex = boneIndices[vertex.BoneIndices[0]];
                        }

                        for (int i = 0; i < 4; i++)
                        {
                            int boneIndex = vertex.BoneIndices[i];
                            float boneWeight = vertex.BoneWeights[i];
                            if (boneWeight > 0)
                            {
                                // If the bone map does not already have the bone add it
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

                }

                // Transform Position, Normal, Tangents, and BiTangent
                if (boneTransformationIndex > -1)
                {
                    NumericsMatrix4x4.Invert(newBones[boneTransformationIndex].OffsetMatrix.ToNumericsMatrix4x4(), out NumericsMatrix4x4 worldTransform);
                    if (transformPosition)
                        position = Vector3.Transform(position, worldTransform);

                    normal = Vector3.TransformNormal(normal, worldTransform);
                    bitangent = Vector3.TransformNormal(bitangent, worldTransform);
                    tangent = Vector3.TransformNormal(tangent, worldTransform);
                }

                // Add Position, Normal, Tangents, and BiTangent
                newMesh.Vertices.Add(position.ToAssimpVector3D());
                newMesh.Normals.Add(normal.ToAssimpVector3D());
                newMesh.BiTangents.Add(bitangent.ToAssimpVector3D());
                newMesh.Tangents.Add(tangent.ToAssimpVector3D());

                // Add UVs
                int uvCount = vertex.UVs.Count;
                for (int i = 0; i < 4; i++)
                {
                    Vector3D textureCoordinate;
                    if (uvCount > i)
                    {
                        var uv = vertex.UVs[i];
                        textureCoordinate = new Vector3D(uv.X, 1 - uv.Y, 0f);
                    }
                    else
                    {
                        textureCoordinate = new Vector3D(1f, 1f, 1f);
                    }
                    newMesh.TextureCoordinateChannels[i].Add(textureCoordinate);
                }

                // Each UV is only X and Y so set the component count to 2
                for (int i = 0; i < newMesh.TextureCoordinateChannelCount; i++)
                {
                    if (i < 4)
                    {
                        newMesh.UVComponentCount[i] = 2;
                    }
                    else
                    {
                        // There are only 4 channels for these models
                        newMesh.UVComponentCount[i] = 0;
                    }
                }

                // Add Colors
                var color = vertex.Color;
                newMesh.VertexColorChannels[0].Add(new Color4D(color.R, color.G, color.B, color.A));
            }
        }

        private static void CollectMeshes(Scene scene, MDL4 model, Bone[] newBones, out List<int> consumedBoneIndices)
        {
            consumedBoneIndices = new List<int>(model.Bones.Count);
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var meshNode = new Node($"Mesh_{meshIndex}", scene.RootNode);
                var newMesh = new Mesh($"Mesh_M{meshIndex}", PrimitiveType.Triangle);

                // Collect faces
                var faceIndices = mesh.GetFaceIndices(true, true);
                foreach (ushort[] indices in faceIndices)
                {
                    newMesh.Faces.Add(new Face([indices[2], indices[1], indices[0]]));
                }

                CollectVertices(mesh.Vertices, newMesh, mesh.BoneIndices.ToIntArray(), newBones, mesh.VertexFormat, out Dictionary<int, Bone> boneMap);

                // Add Bone references holding bone weights to the mesh
                newMesh.Bones.AddRange(boneMap.Values);
                consumedBoneIndices.AddRange(boneMap.Keys);

                // Collect Bones referenced in this mesh, but not in it's vertices.
                for (int i = 0; i < mesh.BoneIndices.Length; i++)
                {
                    int boneIndex = mesh.BoneIndices[i];
                    if (boneIndex >= 0 && !boneMap.ContainsKey(boneIndex))
                    {
                        var bone = model.Bones[boneIndex];
                        NumericsMatrix4x4.Invert(bone.ComputeWorldTransform(model.Bones), out NumericsMatrix4x4 inverseWorldTransform);
                        newMesh.Bones.Add(new Bone(bone.Name, inverseWorldTransform.ToAssimpMatrix4x4(), Array.Empty<VertexWeight>()));
                        consumedBoneIndices.Add(boneIndex);
                    }
                }

                meshNode.Transform = AssimpMatrix4x4.Identity;
                meshNode.MeshIndices.Add(meshIndex);
                newMesh.MaterialIndex = mesh.MaterialIndex;

                scene.RootNode.Children.Add(meshNode);
                scene.Meshes.Add(newMesh);
            }
        }

        public static Scene ToAssimpScene(this MDL4 model)
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
            CollectMeshes(scene, model, newBones, out List<int> consumedBoneIndices);

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
