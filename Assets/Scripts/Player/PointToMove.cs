using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class PointToMove : MonoBehaviour
{

    // delete // when added player animation
    //Animator anim;
    Rigidbody2D rb;

    [SerializeField] private Transform head; //position for head accessories - assign in inspector
    //add other transforms for other decorations - body, feet, necklace, etc

    //public AudioClip walking;
    
    //AudioSource audioSource;


    void Start()
    {
       // audioSource = GetComponent<AudioSource>();

        //anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }


    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var mouse = Mouse.current.position.ReadValue();
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

        //float t = 0;
        var original = transform.position;
        var dir = pos - original;
        dir.Normalize();
        var dist = Vector3.Distance(transform.position, pos);

        while (dist > 0.01f) {
            //transform.position = Vector3.Lerp(original, pos, t);
            transform.position += dir * Time.deltaTime * 5.0f;
            //t += Time.deltaTime * 1.0f;
            yield return null;

            dist = Vector3.Distance(transform.position, pos);
        }

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
