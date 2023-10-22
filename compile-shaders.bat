@echo off

echo "Compiling shaders..."

echo "Assets/Shaders/Builtin.MaterialShader.vert.glsl -> Assets/Shaders/Builtin.MaterialShader.vert.spv"
%VULKAN_SDK%\bin\glslc.exe -fshader-stage=vert Assets/Shaders/Builtin.MaterialShader.vert.glsl -o Assets/Shaders/Builtin.MaterialShader.vert.spv
IF %ERRORLEVEL% NEQ 0 (echo Error: %ERRORLEVEL% && exit)

echo "Assets/Shaders/Builtin.MaterialShader.frag.glsl -> Assets/Shaders/Builtin.MaterialShader.frag.spv"
%VULKAN_SDK%\bin\glslc.exe -fshader-stage=frag Assets/Shaders/Builtin.MaterialShader.frag.glsl -o Assets/Shaders/Builtin.MaterialShader.frag.spv
IF %ERRORLEVEL% NEQ 0 (echo Error: %ERRORLEVEL% && exit)

echo "Done."