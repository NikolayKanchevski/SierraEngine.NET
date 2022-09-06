using System.Numerics;
using SierraEngine.Core;
using SierraEngine.Engine.Components;

namespace SierraEngine.Engine.Classes;

public class GameObject
{
    /// <summary>
    /// Name of the game object. For visualization purposes.
    /// </summary>
    public readonly string name = "Game Object";

    /// <summary>
    /// A fixed component that cannot be removed, nor replaced. The position, rotation, and scale in the 3D space depend on it. 
    /// </summary>
    public readonly Transform transform = new Transform();

    /// <summary>
    /// Parent object. Can be null.
    /// </summary>
    public GameObject parent { get; private set; } = null!;
    
    /// <summary>
    /// A bool indicating the presence of a parent object bound to this one.
    /// </summary>
    public bool hasParent { get; private set; }
    
    /// <summary>
    /// List of game objects which all share the same parent - this game object.
    /// </summary>
    public readonly List<GameObject> children = new List<GameObject>();

    /// <summary>
    /// A shortcut to the position of the transform component. See <see cref="transform"/> 
    /// </summary>
    public Vector3 position => transform.position;

    /// <summary>
    /// A shortcut to the rotation of the transform component. See <see cref="transform"/> 
    /// </summary>
    public Vector3 rotation => transform.rotation;

    /// <summary>
    /// A shortcut to the scale of the transform component. See <see cref="transform"/> 
    /// </summary>
    public Vector3 scale => transform.scale;
    
    /// <summary>
    /// True if the object is selected from the UI hierarchy.
    /// </summary>
    public bool selected;

    private readonly int ID;
    private readonly List<Component> components = new List<Component>();

    /// <summary>
    /// Creates a new and empty game object.
    /// </summary>
    public GameObject()
    {
        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
    }

    /// <summary>
    /// Creates a new game object with a given name.
    /// </summary>
    /// <param name="name">Name to assign to the object.</param>
    public GameObject(in string name)
    {
        this.name = name;

        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
    }

    /// <summary>
    /// Creates a new game object and assigns its parent object.
    /// </summary>
    /// <param name="parent">Parent object to bind to the newly created one.</param>
    public GameObject(GameObject parent)
    {
        SetParent(parent);

        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
    }

    /// <summary>
    /// Creates a new game object with a name and parent object.
    /// </summary>
    /// <param name="name">Name to assign to the object.</param>
    /// <param name="parent">Prent object to bind to the newly created one.</param>
    public GameObject(in string name, GameObject parent)
    {
        this.name = name;
        
        SetParent(parent);
        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
    }

    /// <summary>
    /// Sets the parent of the game object to a new one.
    /// </summary>
    /// <param name="newParent">Parent game object to assign.</param>
    public void SetParent(GameObject newParent)
    {
        if (hasParent)
        {
            parent.children.Remove(this);
        }
        
        this.parent = newParent;
        newParent.children.Add(this);

        this.hasParent = true;
    }

    /// <summary>
    /// Adds a child object to this game object.
    /// </summary>
    /// <param name="newChild">Child object to bind.</param>
    public void AddChild(GameObject newChild)
    {
        if (newChild.hasParent)
        {
            newChild.parent.children.Remove(newChild);
        }
        
        newChild.parent = this;
        children.Add(newChild);
    }

    /// <summary>
    /// Adds multiple children. See <see cref="AddChild"/>.
    /// </summary>
    /// <param name="givenChildren">Children objects to bind.</param>
    public void AddChildren(params GameObject[] givenChildren)
    {
        foreach (GameObject child in givenChildren)
        {
            AddChild(child);
        }
    }

    /// <summary>
    /// Checks whether the game object has a prent object assigned.
    /// </summary>
    /// <returns>A boolean indicating the presence of such parent object.</returns>
    public bool HasParent()
    {
        return parent == null;
    }
    
    /// <summary>
    /// Executed once every frame.
    /// </summary>
    public void Update()
    {
        foreach (Component component in components)
        {
            component.Update();
        }
    }

    /// <summary>
    /// Adds a new and empty component to the game object and returns it, so that it can be further modified.
    /// </summary>
    /// <typeparam name="T">Component class to bind.</typeparam>
    /// <returns>Reference to the component added to the object.</returns>
    public T AddComponent<T>() where T : Component
    {
        Component component = (Component) Activator.CreateInstance(typeof(T))!;
        component.gameObject = this;
        
        components.Add(component);
        return (T)(components.Last());
    }

    /// <summary>
    /// Adds an already defined component together with its data do the object and returns it, so that it can be further modified.
    /// </summary>
    /// <param name="component">Component data.</param>
    /// <typeparam name="T">Component class to bind.</typeparam>
    /// <returns></returns>
    public T AddComponent<T>(Component component) where T : Component
    {
        component.gameObject = this;
        
        components.Add(component);
        return (T)(components.Last());
    }
    
    /// <summary>
    /// Removes the given component from the game object.
    /// </summary>
    /// <param name="component">Component to remove.</param>
    public void RemoveComponent(Component component)
    {
        components.Remove(component);
    }

    /// <summary>
    /// Removes all components assigned to the game object at once.
    /// </summary>
    public void RemoveAllComponents()
    {
        components.Clear();
    }
}