using System.Numerics;
using GLFW;
using ImGuiNET;
using SierraEngine.Core.Rendering.Vulkan;
using SierraEngine.Engine.Classes;
using SierraEngine.Engine.Components;
using SierraEngine.Engine.Components.Lighting;
using Camera = SierraEngine.Engine.Components.Camera;
using Cursor = SierraEngine.Engine.Classes.Cursor;
using Window = SierraEngine.Core.Rendering.Window;

namespace SierraEngine.Core.Application;

public class Application
{
    private readonly Window window;
    private readonly UserInterface ui = new UserInterface(); 

    private readonly Camera camera = new GameObject("Camera").AddComponent<Camera>();
    
    private readonly PointLight pointLight;
    private readonly SpotLight spotLight;
    
    private const float CAMERA_MOVE_SPEED = 15.0f;
    private const float MOUSE_CAMERA_LOOK_SPEED = 0.1f;
    private const float GAMEPAD_CAMERA_LOOK_SPEED = 0.5f;
    private const float CAMERA_ZOOM_SPEED = 30.0f;
    private float yaw = -90.0f, pitch;

    private bool cursorShown = true;
    
    public Application()
    {
        // Create window
        window = new Window($"Sierra Engine { Version.ToString() }", true, true);
        
        // Create Vulkan renderer and assign it to the window
        VulkanRenderer vulkanRenderer = new VulkanRenderer(window);
        window.SetRenderer(ref vulkanRenderer);
        
        // Show the created window
        window.Show();
        
        // Create a new game object for the point light
        GameObject firstPointLightObject = new GameObject("First Point Light");
        GameObject spotLightObject = new GameObject("Spot Light");
        
        // Add a textured cube to the point light object so that we can see where in the world it is
        int lampTexture = vulkanRenderer.CreateTexture(Files.OUTPUT_DIRECTORY + "Textures/lamp.png", TextureType.Diffuse);

        // Add the mesh components to the point light objects
        Mesh firstPointLightMesh = firstPointLightObject.AddComponent<Mesh>(new Mesh(cubeVertices, cubeIndices));

        // Set the textures of the meshes
        firstPointLightMesh.SetTexture(TextureType.Diffuse, lampTexture);
        
        // Add the point light component to the object
        pointLight = firstPointLightObject.AddComponent<PointLight>();
        spotLight = spotLightObject.AddComponent<SpotLight>();

        // Set initial light properties
        spotLight.transform.position = camera.position;
        spotLight.radius = 10.15f;
        spotLight.spreadRadius = 42.0f;
        
        GameObject meshObject = new GameObject();

        // Define the vertices
        Vertex[] vertices = new Vertex[]
        {
            new Vertex() with { position = new Vector3(-0.1f, -0.4f, 0.0f) },
            new Vertex() with { position = new Vector3(-0.1f, 0.4f, 0.0f) },
            new Vertex() with { position = new Vector3(-0.9f, 0.4f, 0.0f) },
            new Vertex() with { position = new Vector3(-0.9f, -0.4f, 0.0f) }
        };

        // Define the indices
        UInt32[] indices = new UInt32[] { 0, 1, 2, 2, 3, 0 };

        // Create and add the mesh to the object
        Mesh mesh = new Mesh(vertices, indices);
        meshObject.AddComponent<Mesh>(mesh);
        
        // Load a tank model in the scene
        MeshObject.LoadFromModel(Files.OUTPUT_DIRECTORY + "Models/Chieftain/T95_FV4201_Chieftain.fbx");
    }

    public void Start()
    {   
        // Set the visibility of the cursor
        Cursor.SetCursorVisibility(cursorShown);

        // Set initial values for the lighting and camera
        camera.transform.position = new Vector3(0.0f, -3.0f, 10.0f);

        World.Init();
        
        // Loop until the window is closed
        while (!window.closed)
        {
            // Update utility classes
            World.UpdateClasses();
            
            // Update the game logic
            Update();
            
            // Update the world
            World.Update();
        }
        
        // Once closed destroy the window
        window.Destroy();
        
        // Terminate the windowing system
        GLFW.Glfw.Terminate();
    }
    
    private void Update()
    {
        HandleCameraMovement();

        UpdateObjects();

        UpdateUI();
    }

    private void UpdateObjects()
    {
        // Calculate some example values to be used for animations
        float upTimeSin = (float) Math.Sin(Time.upTime);
        
        // Point light settings
        pointLight.intensity = 1f;
        pointLight.transform.position = new Vector3(0f, -7.5f, upTimeSin * 10f);
        pointLight.linear = 0.09f;
        pointLight.quadratic = 0.032f;
        
        // Spot light settings
        spotLight.direction = Vector3.Normalize(new Vector3(0, -3, 10) - new Vector3(0, 1.5f, 0));
        spotLight.linear = 0.09f;
        spotLight.quadratic = 0.032f;
        
        // Apply rotations to the turret and gun objects 
        World.meshes[^2].transform.rotation = new Vector3(0.0f, upTimeSin * 45f, 0.0f);
        World.meshes[^1].transform.rotation = new Vector3(0.0f, upTimeSin * 45f, 0.0f);
    }

    private void UpdateUI()
    {
        // Draw the static UI
        ui.Update(window);

        // Draw the application-specific UI
        if (ImGui.Begin("Lighting Sliders", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize))
        {
            ImGui.SliderFloat("Spot Light Intensity", ref spotLight.intensity, 0f, 10f);
            ImGui.SliderFloat("Spot Light Radius", ref spotLight.radius, 0f, 180);
            ImGui.SliderFloat("Spot Light Spread Radius", ref spotLight.spreadRadius, 0f, 360f);
            ImGui.SliderFloat3("Spot Light Color", ref spotLight.color, 0f, 1f);
            ImGui.End();
        }
    }
    
    private void HandleCameraMovement()
    {
        // If escape is pressed toggle mouse visibility
        if (Input.GetKeyPressed(Keys.Escape))
        {
            cursorShown = !cursorShown;
            Cursor.SetCursorVisibility(cursorShown);
        }
        
        // If the cursor is shown do not do any camera movement
        if (cursorShown) return;
        
        // Increase the FOV based on mouse scroll input
        camera.fov -= Input.GetVerticalMouseScroll() * CAMERA_ZOOM_SPEED;

        // If tab is pressed reset the FOV to 45.0f
        if (Input.GetKeyHeld(Keys.Tab)) 
        {
            camera.fov = 45.0f;
        }
        
        // Clamp the FOV so it doesn't get less than 5.0f and bigger than 90.0f
        camera.fov = Mathematics.Clamp(camera.fov, 5.0f, 90.0f);

        // Move the camera based on held keys
        if (Input.GetKeyHeld(Keys.W))
        {
            camera.transform.position += CAMERA_MOVE_SPEED * Time.deltaTime * camera.frontDirection;
        }
        if (Input.GetKeyHeld(Keys.S))
        {            
            camera.transform.position -= CAMERA_MOVE_SPEED * Time.deltaTime * camera.frontDirection;
        }
        
        if (Input.GetKeyHeld(Keys.A))
        {
            camera.transform.position += CAMERA_MOVE_SPEED * Time.deltaTime * camera.leftDirection;
        }
        if (Input.GetKeyHeld(Keys.D))
        {
            camera.transform.position -= CAMERA_MOVE_SPEED * Time.deltaTime * camera.rightDirection;
        }
        
        if (Input.GetKeyHeld(Keys.Q) || Input.GetKeyHeld(Keys.LeftControl))
        {
            camera.transform.position += CAMERA_MOVE_SPEED * Time.deltaTime * camera.upDirection;
        }
        if (Input.GetKeyHeld(Keys.E) || Input.GetKeyHeld(Keys.Space))
        {
            camera.transform.position -= CAMERA_MOVE_SPEED * Time.deltaTime * camera.upDirection;
        }

        // Rotate the camera based on mouse movement
        yaw += Cursor.GetHorizontalCursorOffset() * MOUSE_CAMERA_LOOK_SPEED;
        pitch += Cursor.GetVerticalCursorOffset() * MOUSE_CAMERA_LOOK_SPEED;

        // Check if a single controller is connected
        if (Input.GamepadConnected())
        {
            // Get the left stick's axis and calculate movement based on it
            camera.transform.position -= Input.GetVerticalGamepadLeftStickAxis() * CAMERA_MOVE_SPEED * Time.deltaTime * camera.frontDirection;
            camera.transform.position += Input.GetHorizontalGamepadLeftStickAxis() * CAMERA_MOVE_SPEED * Time.deltaTime * camera.leftDirection;

            // Depending on what buttons are held move the camera
            if (Input.GetGamepadButtonHeld(GamePadButton.A)) camera.transform.position -= CAMERA_MOVE_SPEED * Time.deltaTime * camera.upDirection;
            if (Input.GetGamepadButtonHeld(GamePadButton.X)) camera.transform.position += CAMERA_MOVE_SPEED * Time.deltaTime * camera.upDirection;
            
            // Rotate the camera based on the right stick's axis
            yaw += Input.GetHorizontalGamepadRightStickAxis() * GAMEPAD_CAMERA_LOOK_SPEED;
            pitch += Input.GetVerticalGamepadRightStickAxis() * GAMEPAD_CAMERA_LOOK_SPEED;
        }

        // Clamp the pitch
        pitch = Mathematics.Clamp(pitch, -89.0f, 89.0f);

        // Calculate new camera front direction
        Vector3 newCameraFrontDirection;
        newCameraFrontDirection.X = (float) (Math.Cos(Mathematics.ToRadians(yaw)) * Math.Cos(Mathematics.ToRadians(pitch)));
        newCameraFrontDirection.Y = (float) (Math.Sin(Mathematics.ToRadians(pitch)));
        newCameraFrontDirection.Z = (float) (Math.Sin(Mathematics.ToRadians(yaw)) * Math.Cos(Mathematics.ToRadians(pitch)));
        camera.frontDirection = Vector3.Normalize(newCameraFrontDirection);
    }

    private readonly Vertex[] cubeVertices = new Vertex[]
    {
        new Vertex()
        {
            position = new Vector3(-1.0f, -1.0f, -1.0f),
            normal = new Vector3(0, 0, 1),
            textureCoordinates = new Vector2(0.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, -1.0f, -1.0f),
            normal = new Vector3(1, 0, 0),
            textureCoordinates = new Vector2(1.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, 1.0f, -1.0f),
            normal = new Vector3(0, 0, -1),
            textureCoordinates = new Vector2(1.0f, 1.0f)
        },
        new Vertex()
        {
            position = new Vector3(-1.0f, 1.0f, -1.0f),
            normal = new Vector3(-1, 0, 0),
            textureCoordinates = new Vector2(0.0f, 1.0f)
        },
        new Vertex()
        {
            position = new Vector3(-1.0f, -1.0f, 1.0f),
            normal = new Vector3(0, 1, 0),
            textureCoordinates = new Vector2(0.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, -1.0f, 1.0f),
            normal = new Vector3(0, -1, 0),
            textureCoordinates = new Vector2(1.0f, 0.0f)
        },
        new Vertex()
        {
            position = new Vector3(1.0f, 1.0f, 1.0f),
            normal = new Vector3(0, 0, 1),
            textureCoordinates = new Vector2(1.0f, 1.0f)
        },
        new Vertex()
        {
            position = new Vector3(-1.0f, 1.0f, 1.0f),
            normal = new Vector3(1, 0, 0),
            textureCoordinates = new Vector2(0.0f, 1.0f)
        }
    };

    private readonly UInt32[] cubeIndices = new UInt32[]
    {
        0, 1, 3, 3, 1, 2,
        1, 5, 2, 2, 5, 6,
        5, 4, 6, 6, 4, 7,
        4, 0, 7, 7, 0, 3,
        3, 2, 7, 7, 2, 6,
        4, 5, 0, 0, 5, 1
    };
}