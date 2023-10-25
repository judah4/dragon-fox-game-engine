@echo off

echo "Compiling shaders..."

echo "Assets/Shaders/Builtin.MaterialShader.vert.glsl -> Assets/Shaders/Builtin.MaterialShader.vert.spv"
%VULKAN_SDK%\bin\glslc.exe -fshader-stage=vert Assets/Shaders/Builtin.MaterialShader.vert.glsl -o Assets/Shaders/Builtin.MaterialShader.vert.spv
IF %ERRORLEVEL% NEQ 0 (echo Error: %ERRORLEVEL% && exit)

echo "Assets/Shaders/Builtin.MaterialShader.frag.glsl -> Assets/Shaders/Builtin.MaterialShader.frag.spv"
%VULKAN_SDK%\bin\glslc.exe -fshader-stage=frag Assets/Shaders/Builtin.MaterialShader.frag.glsl -o Assets/Shaders/Builtin.MaterialShader.frag.spv
IF %ERRORLEVEL% NEQ 0 (echo Error: %ERRORLEVEL% && exit)

echo "assets/shaders/Builtin.UIShader.vert.glsl -> assets/shaders/Builtin.UIShader.vert.spv"
%VULKAN_SDK%\bin\glslc.exe -fshader-stage=vert assets/shaders/Builtin.UIShader.vert.glsl -o assets/shaders/Builtin.UIShader.vert.spv
IF %ERRORLEVEL% NEQ 0 (echo Error: %ERRORLEVEL% && exit)

echo "assets/shaders/Builtin.UIShader.frag.glsl -> assets/shaders/Builtin.UIShader.frag.spv"
%VULKAN_SDK%\bin\glslc.exe -fshader-stage=frag assets/shaders/Builtin.UIShader.frag.glsl -o assets/shaders/Builtin.UIShader.frag.spv
IF %ERRORLEVEL% NEQ 0 (echo Error: %ERRORLEVEL% && exit)



echo "Done."