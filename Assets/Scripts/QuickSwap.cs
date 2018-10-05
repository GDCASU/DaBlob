﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickSwap : MonoBehaviour {
    /*
    Allows the player to swap between a stored color and their currect color
     */
	ColorSwap colorSwap;
    private Player player;
    private GameObject PalletCurrent;
    private GameObject PalletBackup;
    private SpriteRenderer PalletCurrentRender;
    private SpriteRenderer PalletBackupRender;
    public int storedColor;
    void Awake () {
        colorSwap = GetComponent<ColorSwap>(); 
        player = GetComponent<Player>();
    }
	void Start () { 
        // Set the stored color to whatever the player has at the time
        storedColor = colorSwap.currentColor; 

        // Set up the UI
        PalletCurrent = Instantiate(Resources.Load("Prefabs/UI Prefabs/PalletCurrent", typeof(GameObject))) as GameObject;
        PalletBackup = Instantiate(Resources.Load("Prefabs/UI Prefabs/PalletBackup", typeof(GameObject))) as GameObject;

        PalletCurrent.transform.parent = colorSwap.playerCamera.transform;
        PalletCurrent.transform.localPosition = new Vector3 (0.29F, -0.18F, 0.4F); // These are hardcoded for now (no UI canvas)

        PalletBackup.transform.parent = colorSwap.playerCamera.transform;
        PalletBackup.transform.localPosition = new Vector3 (0.35F, -0.18F, 0.4F);

        // Get references to sprite renderers for setting colors 
        PalletCurrentRender = PalletCurrent.GetComponent<SpriteRenderer>();
        PalletBackupRender = PalletBackup.GetComponent<SpriteRenderer>();

        // Set colors on the UI
        updatePalletUI();
    }
	
	void Update () {
		if(InputManager.GetButtonDown("X", player)) {
            int temp = colorSwap.currentColor;
            colorSwap.SetColor(storedColor);
            storedColor = temp;
        }
        // We don't really want to call this every update
        updatePalletUI();
	}

    public void updatePalletUI () {
        Color c;
        if(colorSwap.spriteColors.TryGetValue( (ColorSwap.PlayerColor) colorSwap.currentColor, out c)) PalletCurrentRender.color = c;
        if(colorSwap.spriteColors.TryGetValue( (ColorSwap.PlayerColor) storedColor, out c)) PalletBackupRender.color = c;
    }
}
