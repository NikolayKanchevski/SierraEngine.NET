using System.Numerics;
using SierraEngine.Core;
using SierraEngine.Engine.Components;

namespace SierraEngine.Engine.Classes;

public class GameObject
{
    public readonly string name = "Game Object";

    public readonly Transform transform = new Transform();

    public GameObject parent { get; private set; } = null!;
    public bool hasParent;
    public readonly List<GameObject> children = new List<GameObject>();

    public Vector3 position => transform.position;
    public Vector3 rotation => transform.rotation;
    public Vector3 scale => transform.scale;
    public bool selected;

    public readonly int ID;
    private readonly List<Component> components = new List<Component>();

    public GameObject()
    {
        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
    }

    public GameObject(in string name)
    {
        this.name = name;

        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
    }

    public GameObject(GameObject parent)
    {
        SetParent(parent);

        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
    }

    public GameObject(in string name, GameObject parent)
    {
        this.name = name;
        
        SetParent(parent);
        World.hierarchy.Add(this);
        this.ID = World.hierarchy.Count - 1;
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

    public void AddChildren(params GameObject[] givenChildren)
    {
        foreach (GameObject child in givenChildren)
        {
            AddChild(child);
        }
    }

    public bool HasParent()
    {
        return parent == null;
    }

    public void Update()
    {
        foreach (Component component in components.ToList())
        {
            component.Update();
        }
    }

    public Component AddComponent(Component component)
    {
        component.gameObject = this;
        
        components.Add(component);
        return components.Last();
    }

    public void RemoveComponent(Component component)
    {
        components.Remove(component);
    }

    public void RemoveAllComponents()
    {
        components.Clear();
    }
}