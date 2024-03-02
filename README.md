WARNING this code sucks because it's not fun to code in VS 2022 on my computer with parts no younger than eleven years. I'll rewrite it at some point.

# Pmdview

## What is this
This is a tool to view 3D models in Mort the Chicken (slightly modified libgs PMD files with animations in the form of vertex lists). As of this commit it's messy and I can't be bothered to refactor what's needed because I have already sank two weeks into this project.
## Why
Bored
## Where are the releases
I'm too lazy to build a release right now
## The imgui panel has z testing
I know I'm keeping it like that because it's really fucking funny
# How to use
- Insert path to a TPF in the `Texture bundle path (TPF)` field and load
- Insert path to a PPF in the `Path to packfile` field under the `Packfile Viewer` header
- Load the desired model
- Insert a path to a folder below the `Export main model to ply` button
- Click the button and there will be a ply for each frame of the model and a png with the textures within the tpf
# To do
- Proper 3D view
- Fix ply export face order so blender can generate correct normals on import
- Fix memory leaks (models aren't cleaning up properly, it appears)
- Make texture packer increase bounds in power of twos
- Refactor some stuff
- Make a COLLADA export maybe
- Support the Dreamcast "pmd" files (they really are not PMDs)
- Lock the mouse
- Implement shader that fixes the colours (just multiply frag colour by 4x)
- Fix transparency
- Figure out if I can properly make models for XNA with quads and indices instead of a triangle list
- Add a bounding box around objects and prim gps
