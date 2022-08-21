#!/usr/bin/env python3

import os
import shutil
import platform
import sys

ROOT_DIRECTORY = "../"
OUTPUT_DIRECTORY = ""

DLL_DIRECTORY = ROOT_DIRECTORY + "Core/Dynamic Link Libraries/Windows/"
TEXTURE_DIRECTORY = ROOT_DIRECTORY + "Core/Rendering/Textures/"
MODEL_DIRECTORY = ROOT_DIRECTORY + "Core/Rendering/Models/"
FONT_DIRECTORY = ROOT_DIRECTORY + "Core/Rendering/Fonts/"
# FIRST_TIME = False


def Main():
    if len(sys.argv) <= 1:
        print("Error: You must run the scripts with either a --Debug or --Release argument!")
        return
    else:
        if sys.argv[1] != "--Debug" and sys.argv[1] != "--Release":
            print(f"Error: Unrecognized argument: { sys.argv[1] }!")
            return
        elif sys.argv[1] == "--Debug":
            OUTPUT_DIRECTORY = ROOT_DIRECTORY + "bin/Debug/net6.0/"
        else:
            OUTPUT_DIRECTORY = ROOT_DIRECTORY + "bin/Release/net6.0/"

        if (len(sys.argv) >= 3):
            print("Error: Too many arguments specified!")
            return

    FIRST_TIME = CheckFirstTime(OUTPUT_DIRECTORY) and CheckFirstTime(OUTPUT_DIRECTORY + "Shaders/") and CheckFirstTime(OUTPUT_DIRECTORY + "Textures/")

    if FIRST_TIME:
        try:
            shutil.rmtree(ROOT_DIRECTORY + "bin/" + ("Debug/" if sys.argv[1] == "--Debug" else "Release/"))
        except:
            pass

    try:
        if FIRST_TIME:
            CreateDirectories(OUTPUT_DIRECTORY)

        CopyFiles(OUTPUT_DIRECTORY)
        CompileShaders(OUTPUT_DIRECTORY)
    except:
        print("Error occured!")
        return

    if FIRST_TIME:
        CreateDirectories(OUTPUT_DIRECTORY)

    CopyFiles(OUTPUT_DIRECTORY)
    CompileShaders(OUTPUT_DIRECTORY)

    print("Success!")


def CreateDirectories(OUTPUT_DIRECTORY):
    os.makedirs(OUTPUT_DIRECTORY + "Shaders", exist_ok=True)
    os.makedirs(OUTPUT_DIRECTORY + "Fonts", exist_ok=True)


def CopyFiles(OUTPUT_DIRECTORY):
    for root, dirs, files in os.walk(DLL_DIRECTORY):
        for file in files:
            filePath = str(os.path.join(root, file))
            CopyFile(filePath, OUTPUT_DIRECTORY)

    CopyFolder(TEXTURE_DIRECTORY, OUTPUT_DIRECTORY + "Textures/")
    CopyFolder(MODEL_DIRECTORY, OUTPUT_DIRECTORY + "Models/")
    CopyFolder(FONT_DIRECTORY, OUTPUT_DIRECTORY + "Fonts/")



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


def CheckFirstTime(DIRECTORY_TO_CHECK):
    return os.path.isdir(DIRECTORY_TO_CHECK) == False


def CopyFile(file, destination):
    shutil.copy(file, destination)


def CopyFolder(folder, destination):
    shutil.copytree(folder, destination, dirs_exist_ok=True)


Main()
