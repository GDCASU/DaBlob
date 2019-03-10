﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonToggle : MonoBehaviour
{
    public Material off;
    public Material on;
    public bool state = false;
    public GameColor colorValue;

    // Used for animated button
    public bool holdState = false;
    public bool onOnly = false;


    public float countdownTime = 0; // anything larger than 0 will do a countdown then toggle the state off - 0 ignores the timer and will not toggle off
    private float timeCounter = 0;

    public AudioClip buttonTimer;


    public AudioClip buttonTrigger;
    AudioSource audioSource;
    private bool stateChangeMemory;
    
    private float offset = 0.0f;
    private Vector3 startPosition;

    private int onButton = 0;

    

    void Start()
    {
        stateChangeMemory = state;
        audioSource = GetComponent<AudioSource>();
        GetComponent<MeshRenderer>().material = state ? on : off;
        startPosition = transform.position;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (holdState == false)
        {
            if (!state || onOnly == false)
            {
                if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Box"))
                {
                    ColorState color = other.GetComponent<ColorState>();
                    if (color.currentColor == colorValue)
                    {
                        state = !state;
                        GetComponent<MeshRenderer>().material = state ? on : off;
                        if (countdownTime > 0)
                        {
                            audioSource.Stop();
                            timeCounter = countdownTime;
                        }
                    }
                }
            }
        }
    }

    // Used for the hold down button trigger collision
    public void OnTriggerStay(Collider other)
    {
        if (holdState == true)
        {
            if (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Box"))
            {
                ColorState color = other.GetComponent<ColorState>();
                if (color.currentColor == colorValue)
                {
                    // color.transform.Translate(Vector3.down * 0.07f);
                    offset = offset + 0.04f;
                    if (offset > 0.64f)
                    {
                        offset = 0.64f;
                    }
                    onButton = 4;
                }
            }
        }
    }


    private void Update()
    {
        if (stateChangeMemory != state)
        {
            audioSource.PlayOneShot(buttonTrigger, 1.0F);
            stateChangeMemory = state;
        }
        // Animate and detect state of the hold down buttons
        if (holdState == true)
        {
            state = offset > 0.60f;

            if (offset > 0.0f)
            {
                if (onButton == 0)
                {
                    offset = offset - 0.01f;
                }
            }

            if (onButton > 0)
            {
                onButton = onButton - 1;
            }

            transform.position = new Vector3(startPosition.x, startPosition.y, startPosition.z);
            transform.Translate(Vector3.back * offset);
        } else
        {
            if (timeCounter > 0) {
                timeCounter = timeCounter - Time.deltaTime;

                //audioSource.clip = buttonTimer;
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = buttonTimer;
                    audioSource.Play();
                }

                //audioSource.PlayOneShot(buttonTimer, 1.0F);

                if (timeCounter < 0)
                {
                    audioSource.Stop();
                    GetComponent<MeshRenderer>().material = off;
                    timeCounter = 0;
                    state = false;
                }
            }
        }
    }
}
