# FromAssimp
Imports FLVER0, FLVER2, MDL4, and SMD4 FromSoftware models into Assimp to export to common formats such as FBX.  

Currently has issues with normals and UVs but should work for everything else it manages to read.  
Some FLVER2 models have strange faceset issues that need to be resolved, and later PS3 FLVER2 models have edge index compression on faceset indices.  
In the case a game has PS3 edge index compression, simply go for the Xbox 360 version of the game if possible.  

Textures are not added in currently, but can be found nearby usually in TPF containers.  
Xbox 360 TPFs are currently problematic to be read by tools, PS3 textures may need to be used.

# Building
1. [Download SoulsFormatsExtended][0]  
2. Place SoulsFormatsExtended's repo into the folder containing this repo's folder.  
3. Build this repo in VS 2022.  

[0]: https://www.github.com/WarpZephyr/SoulsFormatsExtended