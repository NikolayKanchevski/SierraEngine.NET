using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

public class Component
{
    public GameObject gameObject;
    public Transform transform => this.gameObject.transform;

    public virtual void Start() { }
    public virtual void Update() { }
    public virtual void Destroy() { }
}   