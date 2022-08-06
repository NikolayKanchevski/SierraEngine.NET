using System.Numerics;
using SierraEngine.Core;
using SierraEngine.Engine.Components;

namespace SierraEngine.Engine.Classes;

public class GameObject
{
    public string name = "Game Object";

    public readonly Transform transform = new Transform();

    public GameObject parent { get; private set; } = null!;
    public bool hasParent;
    public readonly List<GameObject> children = new List<GameObject>();

    public Vector3 position => transform.position;
    public Vector3 rotation => transform.rotation;
    public Vector3 scale => transform.scale;

    private readonly List<Component> components = new List<Component>();

    public GameObject()
    {
        World.hierarchy.Add(this);
    }

    public GameObject(in string name)
    {
        this.name = name;
        World.hierarchy.Add(this);
    }

    public GameObject(GameObject parent)
    {
        SetParent(parent);
        World.hierarchy.Add(this);
    }

    public GameObject(in string name, GameObject parent)
    {
        this.name = name;
        SetParent(parent);
        World.hierarchy.Add(this);
    }

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

    public void AddChild(GameObject newChild)
    {
        if (newChild.hasParent)
        {
            newChild.parent.children.Remove(newChild);
        }
        
        newChild.parent = this;
        children.Add(newChild);
    }

    public void AddChildren(params GameObject[] children)
    {
        foreach (GameObject child in children)
        {
            AddChild(child);
        }
    }

    public bool HasParent()
    {
        return parent == null;
    }

    public Component AddComponent(Component component)
    {
        component.gameObject = this;
        
        components.Add(component);
        return components.Last();
    }
}