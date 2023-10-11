# Project Dragon Fox Engine

Prototype game engine in C# and Vulkan. Based on Travis Roman's [Kohi Engine](https://github.com/travisvroman/kohi)

## Build requirements

The projects require the [Vulkan SDK](https://www.lunarg.com/vulkan-sdk/) to build/run. The SDK provides the Vulkan validation layers as well as the command line tools to compile the shaders. The projects include a targets file that compiles the shaders.

## Asset Licenses

This repository includes the same assets as the original tutorial assets. They are located in the `Assets/` folder.

The following [CC0 licensed image](https://pixabay.com/en/statue-sculpture-fig-historically-1275469/) resized to 512 x 512 pixels

* `Assets/texture.jpg`

## Compile shaders

Do this by hand for testing

```SHELL
C:\VulkanSDK\1.3.261.1\bin\glslc.exe -fshader-stage=frag "Builtin.UIShader.frag.glsl" -o "Builtin.UIShader.frag.spv"
C:\VulkanSDK\1.3.261.1\bin\glslc.exe -fshader-stage=vert "Builtin.UIShader.vert.glsl" -o "Builtin.UIShader.vert.spv"
```

### Engine Names

Bookwyrm Engine

Coffee Dragon Engine

Dragon Fox Engine

Kitsune Engine


## Publish

```SHELL

dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true

```