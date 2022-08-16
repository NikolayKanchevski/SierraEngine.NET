#!/usr/bin/env python3
import os
import shutil
import platform

ROOT_DIRECTORY = "../SierraEngine.NET/"
RELEASE_DIRECTORY = ROOT_DIRECTORY + "bin/Release/net6.0/"

DLL_DIRECTORY = "Core/Dynamic Link Libraries/Windows/"
TEXTURE_DIRECTORY = "Core/Rendering/Textures/"
MODEL_DIRECTORY = "Core/Rendering/Models/"


def Main():
    print(ROOT_DIRECTORY + "bin/Release/")
    return
    try:
        shutil.rmtree(ROOT_DIRECTORY + "bin/Release/")
    except:
        pass

    try:
        CreateDirectories()
        CopyFiles()
        CompileShaders()
    except:
        print("Error occured!")
        return

    print("Success!")


def CreateDirectories():
    os.makedirs(RELEASE_DIRECTORY)
    os.makedirs(RELEASE_DIRECTORY + "Shaders")


def CopyFiles():
    for root, dirs, files in os.walk(DLL_DIRECTORY):
        for file in files:
            filePath = str(os.path.join(root, file))
            CopyFile(filePath, RELEASE_DIRECTORY)

    CopyFolder(TEXTURE_DIRECTORY, RELEASE_DIRECTORY + "Textures/")
    CopyFolder(MODEL_DIRECTORY, RELEASE_DIRECTORY + "Models/")



def CompileShaders():
    command = ""
    operatingSystem = platform.system()
    if operatingSystem == "Windows":
        CompileWindowsShaders()
    else:
        CompileUnixShaders()


def CompileWindowsShaders():
    command = f"Core/Rendering/Shading/Compilers/glslc.exe Core/Rendering/Shading/Shaders/shader.vert -o { RELEASE_DIRECTORY }Shaders/shader.vert.spv\n"
    command+= f"Core/Rendering/Shading/Compilers/glslc.exe Core/Rendering/Shading/Shaders/shader.frag -o { RELEASE_DIRECTORY }Shaders/shader.frag.spv"

    os.system(command)


def CompileUnixShaders():
    command = f"Core/Rendering/Shading/Compilers/glslc Core/Rendering/Shading/Shaders/shader.vert -o { RELEASE_DIRECTORY }Shaders/shader.vert.spv\n"
    command+= f"Core/Rendering/Shading/Compilers/glslc Core/Rendering/Shading/Shaders/shader.frag -o { RELEASE_DIRECTORY }Shaders/shader.frag.spv"

    os.system(command)


def CopyFile(file, destination):
    shutil.copy(file, destination)


def CopyFolder(folder, destination):
    shutil.copytree(folder, destination)


Main()