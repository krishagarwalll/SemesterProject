using UnityEngine;
using TMPro;

public class MetalDetectorController : MonoBehaviour
{
    public AudioSource metaldetectAudioSource;
    public AudioClip[] audios;
    public TMP_Text textDistance;

    public float[] parameterDetections; 
    
    public GameObject pointA;
    public GameObject pointB;
    private float distance;

    private bool audioToggle;
    private int audioNumber;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        metaldetectAudioSource.Stop();
        audioToggle = false;
    }

    // Update is called once per frame
    void Update()
    {
        distance = Vector3.Distance(pointA.transform.position, pointB.transform.position);
        textDistance.text = distance.ToString();
        audioChanger();
        audioUpdate();

    }


    private void audioUpdate()
    {
        if (distance <= parameterDetections[0] && !audioToggle)
        {
            metaldetectAudioSource.Play();
            audioToggle = true;

        }
        else if (distance > parameterDetections[0] && audioToggle)
        {
            metaldetectAudioSource.Stop();
            audioToggle = false;
        }
     
    }

    private void audioChanger()
    {
        if (distance < parameterDetections[0] && distance >= parameterDetections[1] && audioNumber != 1)
        {
            metaldetectAudioSource.clip = audios[0];
            audioNumber = 1;
            audioToggle = false;
        }
        else if (distance < parameterDetections[1] && distance >= parameterDetections[2] && audioNumber != 2)
        {
            metaldetectAudioSource.clip = audios[1];
            audioNumber = 2;
            audioToggle = false;
        }
        else if (distance < parameterDetections[2] && audioNumber != 3)
        {
            metaldetectAudioSource.clip = audios[2];
            audioNumber = 3;
            audioToggle = false;
        }
}
    
}
