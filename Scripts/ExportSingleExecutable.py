#!/usr/bin/env python3

import os
from platform import platform
import sys
import platform
import shutil

ROOT_DIRECTORY = "../"
SHADERS_FOLDER = ROOT_DIRECTORY + "Core/Rendering/Shading/Shaders/Compiled"
REQUIRED_DIRECTORIES = ["Core/Rendering/Textures", "Core/Rendering/Models", "Core/Rendering/Fonts"]
SUPPORTED_PLATFORMS = [
    "win-x64", "win-x86", "win-arm", "win-arm64", "win7-x64", "win7-x86", "win81-x64", "win81-x86", "win81-arm", "win10-x64", "win10-x86", "win10-arm", "win10-arm64",
    "osx-x64", "osx.10.10-x64", "osx.10.11-x64", "osx.10.12-x64", "osx.10.13-x64", "osx.10.14-x64", "osx.10.15-x64", "osx.11.0-x64", "osx.11.0-arm64", "osx.12-x64", "osx.12-arm64",
    "linux-x64", "linux-musl-x64", "linux-arm", "linux-arm64", "rhel-x64", "rhel.6-x64", "tizen", "tizen.4.0.0", "tizen.5.0.0"
]

def Main():
    if (len(sys.argv) <= 2):
        print("Error: You must run the program with two arguments: --[PLATFORM] and --[OUTPUT_DIRECTORY]!")
        return

    platform = sys.argv[1].replace("--", "")

    if (platform not in SUPPORTED_PLATFORMS):
        print(f"Error: The platform [{ platform }] is either a non-existant, or unsupported!")
        return

    try:
        shutil.rmtree(ROOT_DIRECTORY + f"bin/Release/net6.0/{platform}")
    except:
        pass

    outputDirectory = ROOT_DIRECTORY + f"bin/Release/net6.0/{platform}/publish/"
    copyDirectory = sys.argv[2].replace("--", "")

    try:
        shutil.rmtree(copyDirectory + f"/Sierra Engine Game ({ platform })")
    except:
        pass

    try:
        shutil.rmtree(copyDirectory + "/publish")
    except:
        pass

    command = f"dotnet publish ../SierraEngine.csproj -r { platform } --configuration Release --no-self-contained /property:PublishSingleFile=True /property:IncludeNativeLibrariesForSelfExtract=True /property:SelfContained=False /property:ReadyToRun=True"

    try:
        os.system(command)

        for dir in REQUIRED_DIRECTORIES:
            CopyFolder(ROOT_DIRECTORY + dir, outputDirectory)

        os.makedirs(outputDirectory + "Shaders/")
        CompileShaders(outputDirectory)

    except Exception as exception:  
        print(exception)
        return

    try:
        os.remove(outputDirectory + "glfw-sharp.dll.config")
        os.remove(outputDirectory + "SierraEngine.pdb")
    except:
        pass

    try:
        MoveFolder(outputDirectory, copyDirectory)
    except:
        print(f"Could not move the export to { copyDirectory }! Make sure the directory is valid and exists!")
        return

    os.rename(copyDirectory + "/publish", copyDirectory + f"/Sierra Engine Game ({ platform })")
    shutil.rmtree(ROOT_DIRECTORY + f"bin/Release/net6.0/{platform}")

    print("\nSuccess!")

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

def MoveFolder(directoryToMove, target):
    shutil.move(directoryToMove, target)


Main()
