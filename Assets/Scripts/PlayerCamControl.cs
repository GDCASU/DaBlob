﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamControl : MonoBehaviour
{
    public int controlState = 0;

    //An array of cameras to switch between
    [Header("Cameras")]
    public GameObject[] cams = new GameObject[3]; //Third,overhead,side
    public int activecam = 0;
    public float sensitivity = 80.0f;

    [Header("Inputs")]
    public string camHorizAxis = "Mouse X";
    public string camVertAxis = "Mouse Y";


    private KeyCode orbitCamInput = KeyCode.Mouse2;
    private float cameraHorizAngle = 0;
    private float cameraVertAngle = 0;

    // Use this for initialization
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        cameraHorizAngle = cams[0].transform.rotation.eulerAngles.y;
        cameraVertAngle = 0;
    }

    void LateUpdate()
    {
        // Camera Angle Input
        float xAxis = Input.GetAxis(camHorizAxis) * Time.deltaTime * sensitivity;
        float yAxis = -Input.GetAxis(camVertAxis) * Time.deltaTime * sensitivity;
        cameraHorizAngle += xAxis;
        cameraVertAngle += yAxis;
        switch (activecam)
        {
            case 0:
                cams[0].transform.rotation = Quaternion.Euler(0, cameraHorizAngle, 0);
                cams[0].transform.Rotate(Vector3.right, cameraVertAngle);
                cams[0].transform.position = transform.position + Vector3.up * 0.4f + cams[0].transform.forward * -2.5f;
                break;
            case 1:
                //edit to look down
                break;
            case 3:
                break;
        }
    }

    public void ChangeCamera(int camNumber)
    {
        if (camNumber == -1)
        {
            if (cams[activecam] != null)
                cams[activecam].SetActive(false);
            activecam++;
            activecam = activecam % cams.Length;
            if (cams[activecam] != null)
                cams[activecam].SetActive(true);
            else
                ChangeCamera(camNumber);
        }
        else
        {
            if (cams[camNumber] != null)
            {
                cams[activecam].SetActive(false);
                activecam = camNumber;
                cams[activecam].SetActive(true);
            }
        }
    }
}