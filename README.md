<h1 align="center">Sierra Engine</h1>
<h6 align="center">By: <a href="https://nikichatv.com/Website/N-Studios.html">Nikolay Kanchevski</a></h6>
<br>

<p>
A little <strong>open-source</strong> game engine, written in C# (.NET 6.0), which features some of the most common game programming techniques built-in. 
It is based on the <b><a href="https://www.vulkan.org/">Vulkan</a></b> rendering API, which provides support for 
<b>Windows 7-11</b>, <b>macOS</b>, <b>Linux</b>, <b>Android</b>, <b>iOS</b>, <b>tvOS</b> and other operating systems,
though the engine has only been tested on <b>Windows 11</b> and <b>macOS</b>. All of its features are listed below.
</p>

<br>

## 🧭 Object Transformations

Every object has a field called <b>"transform"</b> and, you guessed it, it is capable of changing the <b>position</b>, <b>rotation</b>, and <b>scale</b> of each object in the 3D space. Here is an example on how to do just that:

```c#
someObject.transform.position = new Vector3(10.0f, 0.0f, -5.0f); // Changes the position in the world
someObject.transform.rotation = new Vector3(0.0f, 0.0f, 90.0f);  // Changes the rotation of the object
someObject.transform.scale = new Vector3(10.0f, 0.0f, -5.0f);    // Changes the scale of the object
```
<br>

You could, for example, put a tank in your world and make its turret rotate like so:

```c#
turretObject.transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
gunObject.transform.rotation = new Vector3(0.0f, upTimeCos * 0.65f, 0.0f);
```
<br>

This is the result:
![Screenshot](Screenshots/TransformTank.gif)

<br>

Hold on a second! Is that a custom 3D model?

<br>


## 🗿 Model Loader

That's right! How can a game engine be an engine and... not allow importing custom 3D models? In Sierra Engine it is as simple as:

```c#
    MeshObject model = new MeshObject.LoadFromModel(FILE_NAME_HERE);
    MeshObject model = new MeshObject.LoadFromModel("Models/Train.obj"); // <-- Example
```

<br>

## 🖼️ Texturing System

Loading models is cool, but what is the point of it if they are not going to be colored, right? Well, when importing your model, the program automatically picks all textures applied to it and loads them into the renderer. Here is what the following code results in:

```c#
    MeshObject model = MeshObject.LoadFromModel("Models/Chieftain/T95_FV4201_Chieftain.fbx");
```

![ScreenShot](Screenshots/TextureSystem.png)

<br>

## ☀️ Directional Lighting 

What is that? The tank's lower plate is very dark... strange, right? Nope, not at all.  There is <a href="https://en.wikipedia.org/wiki/Shading#Directional_lighting">Directional Lighting</a> implemented, which is calculated based on the normals of each mesh's vertices. Here is a comparison between <b>no MSAA</b> and <b>MSAAx8</b>:

<br>

## 👾 MSAA Anti-Aliasing

Because I doubt anyone likes pixelated images, there is a <a href="https://en.wikipedia.org/wiki/Multisample_anti-aliasing#:~:text=Multisample%20anti%2Daliasing%20(MSAA),computer%20graphics%20to%20remove%20jaggies.">MSAA</a> (Multisample Anti-Aliasing) system in place to get rid of all those pesky pixelelated fragments.

![ScreenShot](Screenshots/NoMsaa.jpg)
![ScreenShot](Screenshots/8xMsaa.jpg)

<br>

## 🗺️ Mip Mapping

There is also <a href="https://en.wikipedia.org/wiki/Mipmap">Mip Mapping</a>, which, not only does it get rid of <a href="https://en.wikipedia.org/wiki/Moir%C3%A9_pattern">Moiré patterns</a>, but it also greatly increases the frame rate. What it does is lower the quality of textures when the camera is far. It is barely noticable to the user, but saves a lot of resources on textures. Here is an example:

![ScreenShot](Screenshots/MipMappingClose.jpg)
![ScreenShot](Screenshots/MipMappingFar.jpg)

<p style="opacity: 0.5">Note: that the second picture is zoomed in a lot to show the effect. In reality you cannot even see the quality being lowered due to how far the object is from you.</p>

<br>

## 👍 Ease of Use

The program is packed with numerous useful classes. Let's say you wanted to maximize the window. This is how you would do it:

```c#
window.Maximize();
```
<br>

If you want to get the current position of the cursor over the window:
```c#
Vector2 cursorPosition = Cursor.cursorPosition;
```
<br>

And if you want to check whether a key on the keyboard is pressed:
```c#
bool spacePressed = Input.GetKeyPressed(Key.Space);
```
<br>

Wait, you want to detect the GPU model of the machine? Easy:
```c#
string gpuModel = SystemInformation.gpuModel;
```
<br>

There are many other similar utility classes that make the gathering of data within your game/program/app incredibly easy. All of this - in a single namespace:

```c#
using SierraEngine.Engine;
```

<br>

## 🆕 What next?

There are many other features planned. Some of them are:

<p>
    <ul>
        <li><a href="https://en.wikipedia.org/wiki/User_interface">UI</a>
        <li><a href="https://en.wikipedia.org/wiki/Video_post-processing">Post-Processing</a></li>
        <li><a href="https://en.wikipedia.org/wiki/Computer_graphics_lighting#Point">Point Lights</a></li>
        <li><a href="https://en.wikipedia.org/wiki/Multithreading_(computer_architecture)">Multi-Threading 😥</a></li>
        <li>Performance Monitoring</li>
        <li>Camera System</li>
    </ul>
</p>

<br>

##  🤓️  About

<h4>Information on the project:</h4>
<br>
<p>
    Frameworks used: 
    <ul>
        <li><a href="https://www.vulkan.org/">Vulkan</a> - For both cross-platform and pefromant-friendly rendering.</li>
        <li><a href="https://github.com/glfw/glfw">GLFW</a> - For creating window interface and connecting it to the <b>Vulkan</b> renderer.</li>
        <li><a href="https://github.com/assimp/assimp">Assimp</a> - For the loading of all kinds of 3D model formats (.obj, .fbx, .dae, etc.).</li>
        <li><a href="https://github.com/nothings/stb">Stb</a> - For loading image data from all kinds of image formats (.jpb, .png, etc.).</li>
        <li><a href="https://ih1.redbubble.net/image.528192883.5730/st,small,845x845-pad,1000x1000,f8f8f8.u9.jpg">My Brain</a> - There is not much left of it, actually...</li>
    </ul>
    <br>
    Softwares used: 
    <ul>
        <li><a href="https://www.jetbrains.com/rider/">JetBrains Rider</a> - A <b>cross-platform</b> IDE used to develop the .NET project on both my <b>macOS</b> and <b>Windows</b> systems.</li>
        <li><a href="https://www.blender.org/">Blender</a> - For the testing of 3D model functionality, and their textures.</li>
        <li><a href="https://trello.com/en">Trello</a> - For pretending to have an organized list of things to implement next.</li>
    </ul>
</p>