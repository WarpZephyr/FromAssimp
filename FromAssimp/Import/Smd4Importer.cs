using Assimp;
using FromAssimp.Extensions;
using SoulsFormats;
using System.Numerics;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;

namespace FromAssimp.Import
{
    internal static class Smd4Importer
    {
        public static Scene ImportSmd4(SMD4 model)
        {
            var scene = new Scene();
            var rootNode = new Node("Root");
            scene.RootNode = rootNode;

            // Add material placeholders
            // SMD4 isn't known to have materials
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var newMaterial = new Material();
                newMaterial.Name = $"MeshMaterialPlaceholder_{meshIndex}";
                scene.Materials.Add(newMaterial);
            }

            // Add bone nodes
            // In Assimp every bone needs a node with the same name
            // To have bones transform properly you need to set the transform of the bone relative to its parent in the node's Transform.
            // This is also known as the local transform.
            var boneRootNode = rootNode;
            Node[] newBoneNodes = new Node[model.Nodes.Count];
            NumericsMatrix4x4[] localTransforms = new NumericsMatrix4x4[model.Nodes.Count];
            for (int boneIndex = 0; boneIndex < model.Nodes.Count; boneIndex++)
            {
                var bone = model.Nodes[boneIndex];
                var localTransform = bone.ComputeLocalTransform();
                Node parentNode = bone.ParentIndex > -1 ? newBoneNodes[bone.ParentIndex] : boneRootNode;

                localTransforms[boneIndex] = localTransform;
                var newBoneNode = new Node(bone.Name, parentNode);
                newBoneNode.Transform = localTransform.ToAssimpMatrix4x4();
                newBoneNodes[boneIndex] = newBoneNode;
                parentNode.Children.Add(newBoneNode);
            }

            // Get full bone transforms
            // These will be a full world transform for every bone
            // These will be used to transform vertices that aren't already in bind pose,
            // As well as be used in Assimp Bone OffsetMatrix values.
            NumericsMatrix4x4[] transforms = new NumericsMatrix4x4[model.Nodes.Count];
            void GetTransforms(int index, NumericsMatrix4x4 parentTransform)
            {
                if (index > -1)
                {
                    var bone = model.Nodes[index];
                    var localTransform = localTransforms[index];
                    var transform = localTransform * parentTransform; // Get world
                    transforms[index] = transform; // Store world

                    // Move onto this bone's next sibling
                    GetTransforms(bone.NextSiblingIndex, parentTransform);

                    // Move onto this bone's children
                    GetTransforms(bone.FirstChildIndex, transform);
                }
            }

            // Go from the top of the hiearchy down, enabling getting world transforms for every bone only once
            for (int boneIndex = 0; boneIndex < model.Nodes.Count; boneIndex++)
            {
                var bone = model.Nodes[boneIndex];
                if (bone.ParentIndex != -1) // Skip bones that have parents
                    continue;

                var transform = localTransforms[boneIndex];
                transforms[boneIndex] = transform; // Store world

                // Move onto this bone's children
                GetTransforms(bone.FirstChildIndex, transform);
            }

            // Get inverse transforms
            // Used for the offset matrix in bones stored in meshes for assimp
            // Assimp stores bone references per mesh, along with the weights of vertices this bone affects in that mesh
            // This means you will need to duplicate bone references per mesh, because they cannot share the same vertex IDs
            AssimpMatrix4x4[] offsetTransforms = new AssimpMatrix4x4[model.Nodes.Count];
            for (int boneIndex = 0; boneIndex < model.Nodes.Count; boneIndex++)
            {
                var transform = transforms[boneIndex];
                NumericsMatrix4x4.Invert(transform, out NumericsMatrix4x4 inverseTransform);
                offsetTransforms[boneIndex] = inverseTransform.ToAssimpMatrix4x4();
            }

            // Allocate for indices and weights to be used
            Span<int> boneIndicesAlloc = stackalloc int[4];
            Span<float> boneWeightsAlloc = stackalloc float[4];

            // Store what bones are referenced for later
            // This is so we know which ones aren't referenced and can make a decision on whether or not to export them
            HashSet<int> referencedBoneIndices = [];

            // Add meshes
            var meshRootNode = rootNode;
            for (int meshIndex = 0; meshIndex < model.Meshes.Count; meshIndex++)
            {
                var mesh = model.Meshes[meshIndex];
                var parentNode = meshRootNode;
                var newMeshNode = new Node($"MeshNode_{meshIndex}", parentNode);
                var newMesh = new Mesh($"Mesh_{meshIndex}", PrimitiveType.Triangle);
                newMesh.MaterialIndex = meshIndex; // SMD4 isn't known to have materials

                // Add faces
                List<ushort> faces = mesh.Triangulate(true, true);
                for (int i = 0; i < faces.Count - 2; i += 3)
                {
                    newMesh.Faces.Add(new Face([faces[i + 2], faces[i + 1], faces[i]]));
                }

                // Get info
                // This is so we know what to export for each vertex ahead of time
                bool hasPosition = false;
                bool hasUV = false;
                bool hasBone = false;
                bool hasBones = false;
                var meshBoneIndices = mesh.BoneIndices;
                if (model.Header.Version == 0x40001)
                {
                    if (mesh.VertexFormat == 0)
                    {
                        hasPosition = true;

                        // Only one for this vertex format
                        hasBone = true;
                    }
                    else if (mesh.VertexFormat == 1)
                    {
                        hasPosition = true;
                        hasUV = true;

                        // Only one for this vertex format?
                        hasBone = true;
                    }
                    else if (mesh.VertexFormat == 2)
                    {
                        hasPosition = true;
                        hasBones = true;
                    }
                }

                // Reserve bone map for vertex weights
                Dictionary<int, Bone> meshBoneMap = new Dictionary<int, Bone>(meshBoneIndices.Length);
                bool ValidMeshBoneIndex(int index)
                        => index > -1 && index < meshBoneIndices.Length // Make sure mesh bone index is valid
                        && meshBoneIndices[index] > -1 && meshBoneIndices[index] < model.Nodes.Count; // Make sure bone index is valid;

                // Add vertices
                bool doTransform = true;
                bool doBoneIndexTransform = hasBone; // Assuming multiple bones means already in bind pose for now
                for (int vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
                {
                    // Gather elements
                    var vertex = mesh.Vertices[vertexIndex];
                    var pos = vertex.Position;
                    var uv = vertex.UV;
                    var boneIndices = vertex.BoneIndices;
                    var boneWeights = vertex.BoneWeights;
                    var validBoneIndex = ValidMeshBoneIndex(boneIndices[0]);
                    var validBoneIndices = validBoneIndex
                        && ValidMeshBoneIndex(boneIndices[1])
                        && ValidMeshBoneIndex(boneIndices[2])
                        && ValidMeshBoneIndex(boneIndices[3]);

                    // Transform elements
                    if (doTransform)
                    {
                        int boneTransformIndex = -1;
                        if (doBoneIndexTransform && validBoneIndex)
                        {
                            boneTransformIndex = meshBoneIndices[boneIndices[0]];
                        }

                        // Bone index found and valid
                        if (boneTransformIndex > -1)
                        {
                            var transform = transforms[boneTransformIndex];
                            if (hasPosition)
                                pos = Vector3.Transform(pos, transform);
                        }
                    }

                    // Add elements
                    if (hasPosition)
                        newMesh.Vertices.Add(pos.ToAssimpVector3D());

                    if (hasUV)
                    {
                        var u = uv.X;
                        var v = 1 - uv.Y;
                        newMesh.TextureCoordinateChannels[0].Add(new Vector3D(u, v, 0f));
                        newMesh.UVComponentCount[0] = 2;
                    }

                    // Setup bone indices and bone weights
                    boneIndicesAlloc[0] = -1;
                    boneIndicesAlloc[1] = -1;
                    boneIndicesAlloc[2] = -1;
                    boneIndicesAlloc[3] = -1;
                    boneWeightsAlloc[0] = 0f;
                    boneWeightsAlloc[1] = 0f;
                    boneWeightsAlloc[2] = 0f;
                    boneWeightsAlloc[3] = 0f;
                    if (validBoneIndices)
                    {
                        boneIndicesAlloc[0] = meshBoneIndices[boneIndices[0]];
                        boneIndicesAlloc[1] = meshBoneIndices[boneIndices[1]];
                        boneIndicesAlloc[2] = meshBoneIndices[boneIndices[2]];
                        boneIndicesAlloc[3] = meshBoneIndices[boneIndices[3]];
                        boneWeightsAlloc[0] = boneWeights[0];
                        boneWeightsAlloc[1] = boneWeights[1];
                        boneWeightsAlloc[2] = boneWeights[2];
                        boneWeightsAlloc[3] = boneWeights[3];
                    }
                    else if (validBoneIndex)
                    {
                        boneIndicesAlloc[0] = meshBoneIndices[boneIndices[0]];
                        boneIndicesAlloc[1] = -1;
                        boneIndicesAlloc[2] = -1;
                        boneIndicesAlloc[3] = -1;
                        boneWeightsAlloc[0] = boneWeights[0];
                        boneWeightsAlloc[1] = 0f;
                        boneWeightsAlloc[2] = 0f;
                        boneWeightsAlloc[3] = 0f;
                    }

                    // Is a local function to allow for recursion and easier to read code
                    // Needed to add parent bones that no vertices ever reference themselves
                    Bone AddMeshBone(int boneIndex)
                    {
                        // Add the bone if we couldn't find it
                        if (!meshBoneMap.TryGetValue(boneIndex, out Bone? meshBone))
                        {
                            var bone = model.Nodes[boneIndex];
                            var offsetTransform = offsetTransforms[boneIndex];

                            // The name of an assimp mesh bone must be the same as an existing Node added in the scene node tree
                            meshBone = new Bone
                            {
                                Name = bone.Name,
                                OffsetMatrix = offsetTransform,
                            };

                            newMesh.Bones.Add(meshBone);
                            meshBoneMap.Add(boneIndex, meshBone);
                            referencedBoneIndices.Add(boneIndex);

                            // Add this bone's parents
                            if (bone.ParentIndex > -1)
                                AddMeshBone(bone.ParentIndex);
                        }

                        return meshBone;
                    }

                    // Add bone weights
                    for (int i = 0; i < 4; i++)
                    {
                        int boneIndex = boneIndicesAlloc[i];
                        float boneWeight = boneWeightsAlloc[i];
                        if (boneIndex > -1 && boneIndex < model.Nodes.Count && boneWeight > 0f)
                        {
                            // Add a mesh bone and all it's parents, or find an existing one we already added
                            var meshBone = AddMeshBone(boneIndex);
                            meshBone.VertexWeights.Add(new VertexWeight(vertexIndex, boneWeight));
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                scene.Meshes.Add(newMesh);
                newMeshNode.MeshIndices.Add(meshIndex);
                parentNode.Children.Add(newMeshNode);
            }

            // Find unused bones
            var unusedBoneIndices = new List<int>();
            for (int i = 0; i < model.Nodes.Count; i++)
            {
                if (!referencedBoneIndices.Contains(i))
                    unusedBoneIndices.Add(i);
            }

            // Add unused bones into unused bones mesh
            if (unusedBoneIndices.Count > 0)
            {
                Mesh unusedBonesMesh = new Mesh("UnusedBonesMesh");
                Node unusedBonesMeshNode = new Node("UnusedBonesMeshNode", meshRootNode);
                Material unusedBonesMeshMaterial = new Material();
                unusedBonesMeshMaterial.Name = "UnusedBonesMeshMaterial";

                foreach (var boneIndex in unusedBoneIndices)
                {
                    var newBone = new Bone(model.Nodes[boneIndex].Name, offsetTransforms[boneIndex], []);
                    unusedBonesMesh.Bones.Add(newBone);
                }

                // Count now will be last index once added
                unusedBonesMesh.MaterialIndex = scene.MaterialCount;
                unusedBonesMeshNode.MeshIndices.Add(scene.MeshCount);
                meshRootNode.Children.Add(unusedBonesMeshNode);
                scene.Materials.Add(unusedBonesMeshMaterial);
                scene.Meshes.Add(unusedBonesMesh);
            }

            return scene;
        }
    }
}
