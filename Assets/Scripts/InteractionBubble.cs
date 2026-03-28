using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionBubble : MonoBehaviour
{
    [SerializeField] private float interactionRange;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("E key pressed");
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange);
            foreach (Collider2D collider in colliders)
            {
                Debug.Log(collider);
            }
            Debug.Log("Found: " + colliders.Length);
        }
        
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
