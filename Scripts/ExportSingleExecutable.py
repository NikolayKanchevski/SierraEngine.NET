#!/usr/bin/env python3

import os
from platform import platform
import sys
import platform
import shutil

selectedPlatform = ""

MACOS_64 = "--osx-x64"
WIN_64 = "--win-x64"
LINUX_64 = "--unix-64"
SUPPORTED_PLATFORMS = [ WIN_64, MACOS_64, LINUX_64 ]

ROOT_DIRECTORY = "../"
SHADERS_FOLDER = ROOT_DIRECTORY + "Core/Rendering/Shading/Shaders/Compiled"
REQUIRED_DIRECTORIES = ["Core/Rendering/Textures", "Core/Rendering/Models", "Core/Rendering/Fonts"]

def Main():
    if (len(sys.argv) <= 1 or sys.argv[1] not in SUPPORTED_PLATFORMS):
        print(f"Error: You must execute the program with one of the following arguments: { WIN_64 }, { MACOS_64 }, { LINUX_64 }!")
        return

    platform = sys.argv[1].replace("--", "")
    outputDirectory = ROOT_DIRECTORY + f"bin/Release/net6.0/{platform}/publish/"

    command = f"dotnet publish ../SierraEngine.csproj -r { platform } --configuration Release /property:PublishSingleFile=True /property:IncludeNativeLibrariesForSelfExtract=True /property:SelfContained=False /property:ReadyToRun=True"

    try:
        os.system(command)

        for dir in REQUIRED_DIRECTORIES:
            CopyFolder(ROOT_DIRECTORY + dir, outputDirectory)

        os.makedirs(outputDirectory + "Shaders/")
        CompileShaders(outputDirectory)

        os.remove(outputDirectory + "glfw-sharp.dll.config")
        os.remove(outputDirectory + "SierraEngine.pdb")

    except Exception as exception:
        print(exception)
        return



    print("\n\nSuccess!")

def CompileShaders(OUTPUT_DIRECTORY):
    operatingSystem = platform.system()
    if operatingSystem == "Windows":
        CompileWindowsShaders(OUTPUT_DIRECTORY)
    else:
        CompileUnixShaders(OUTPUT_DIRECTORY)


def CompileWindowsShaders(OUTPUT_DIRECTORY):
    command = f"..\Core\Rendering\Shading\Compilers\glslc.exe { ROOT_DIRECTORY }Core\Rendering\Shading\Shaders\shader.vert -o { OUTPUT_DIRECTORY }Shaders\shader.vert.spv"

    os.system(command)

    command = f"..\Core\Rendering\Shading\Compilers\glslc.exe { ROOT_DIRECTORY }Core\Rendering\Shading\Shaders\shader.frag -o { OUTPUT_DIRECTORY }Shaders\shader.frag.spv"

    os.system(command)


def CompileUnixShaders(OUTPUT_DIRECTORY):
    command = f"{ ROOT_DIRECTORY }Core/Rendering/Shading/Compilers/glslc { ROOT_DIRECTORY }Core/Rendering/Shading/Shaders/shader.vert -o { OUTPUT_DIRECTORY }Shaders/shader.vert.spv\n"
    command+= f"{ ROOT_DIRECTORY }Core/Rendering/Shading/Compilers/glslc { ROOT_DIRECTORY }Core/Rendering/Shading/Shaders/shader.frag -o { OUTPUT_DIRECTORY }Shaders/shader.frag.spv"

    os.system(command)


def CopyFolder(folder, destination):
    index = folder.rindex("/")
    shutil.copytree(folder, destination + folder[index:len(folder)], dirs_exist_ok=True)


Main()