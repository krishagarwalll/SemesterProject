using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCollisionHandler : MonoBehaviour
{
    public event Action<Collider2D> TriggerEntered;
    public event Action<Collider2D> TriggerExited;
    public event Action<Collision2D> CollisionEntered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TriggerEntered?.Invoke(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        TriggerExited?.Invoke(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CollisionEntered?.Invoke(collision);
    }
}
