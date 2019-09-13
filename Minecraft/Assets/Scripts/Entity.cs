using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public System.Guid Id { get; set;}
    public virtual void Initialize() {
        this.Id = new System.Guid();
    }
}
