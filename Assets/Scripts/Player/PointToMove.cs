using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class PointToMove : MonoBehaviour
{

    // delete // when added player animation
    //Animator anim;
    Rigidbody2D rb;
    private DragDropHandler dragDropHandler;

    [SerializeField] private Transform head; //position for head accessories - assign in inspector
    //add other transforms for other decorations - body, feet, necklace, etc

    //public AudioClip walking;
    
    //AudioSource audioSource;


    void Start()
    {
       // audioSource = GetComponent<AudioSource>();

        //anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        dragDropHandler = FindFirstObjectByType<DragDropHandler>();
    }


    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var mouse = Mouse.current.position.ReadValue();
            if (dragDropHandler != null && dragDropHandler.BlocksMovementClick(mouse))
            {
                return;
            }

            var pos = Camera.main.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0));
            pos.z = 0;

            StopAllCoroutines();
            StartCoroutine(DoTheThing(pos));
        }
        
            //anim.SetBool("isWalking", true);
            //audioSource.PlayOneShot(walking);
        else
        {
            //anim.SetBool("isWalking", false);
        }

        
        
    }

    IEnumerator DoTheThing(Vector3 pos) 
    {
        while (Vector3.Distance(transform.position, pos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                pos,
                5.0f * Time.deltaTime
            );

            yield return null;
        }
            transform.position = pos;
    }



    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("enemy"))
        {
            //when player collides with other tagged objects
            //also go into dialog when collide
            Debug.Log("died");
        }
        else if(collision.CompareTag("decoration"))
        {
            collision.transform.SetParent(head);
            // Snap to head position
            collision.transform.localPosition = Vector3.zero;
        }
    }
}
