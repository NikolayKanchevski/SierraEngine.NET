using System.Numerics;
using SierraEngine.Engine.Classes;

namespace SierraEngine.Engine.Components;

public class Component 
{
    public GameObject gameObject = null!;
    public Transform transform => gameObject != null ? this.gameObject.transform : Transform.Default;
    public Vector3 position => this.gameObject.transform.position;
    public Vector3 rotation => this.gameObject.transform.rotation;
    public Vector3 scale => this.gameObject.transform.scale;
    
    public virtual void Start() { }
    public virtual void Update() { }

    public virtual void Destroy()
    {
        gameObject?.RemoveComponent(this);
    }
}