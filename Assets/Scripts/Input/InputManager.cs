﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using XInputDotNetPure;

/*
 * Author:      Zachary Schmalz
 * Version:     1.0.0
 * Date:        September 19, 2018
 */

/// <summary>
/// This class handles all XboxController and Keyboard input
/// </summary>
public class InputManager : MonoBehaviour
{
    /// <summary>
    /// Enum for the type of input being used
    /// </summary>
    public enum InputMethod
    {
        Keyboard,
        XboxController
    };

    /// <summary>
    /// A serialized class for creating and assigning Axis controls for Keyboard and Controller in the inspector
    /// </summary>
    [System.Serializable]
    public class Axis
    {
        public string axisName;
        public KeyCode positiveKeyboardAxis;
        public KeyCode negativeKeyboardAxis;
        public XboxController.XboxAxis xboxAxis;
    }

    /// <summary>
    /// A serialized class for creating and assigning Button controls for Keyboard and Controller in the inspector
    /// </summary>
    [System.Serializable]
    public class Button
    {
        public string buttonName;
        public KeyCode keyboardButton;
        public XboxController.XboxButton xboxButton;
    }

    public static InputManager singleton;

    // Public class variables
    public GameObject playerPrefab;
    [Range(1, 4)] public int maxPlayers;
    [HideInInspector] public List<GameObject> players;
    public List<Axis> axes;
    public List<Button> buttons;

    // Private class variables
    private KeyboardController keyboardController;
    private List<Dictionary<string, KeyCode>> keyboardAxisDictList;
    private List<Dictionary<string, KeyCode>> keyboardButtonDictList;
    private List<XboxController> xboxControllers;
    private List<Dictionary<string, XboxController.XboxAxis>> xboxAxisDictList;
    private List<Dictionary<string, XboxController.XboxButton>> xboxButtonDictList;

    // Public class properties
    /// <summary>
    /// Returns the amount of controllers connected
    /// </summary>
    public int ControllersConnected
    {
        get
        {
            int count = 0;
            foreach (XboxController xc in xboxControllers)
                if (xc.IsConnected)
                    count++;
            return count;
        }
    }

    void Awake()
    {
        if (singleton == null)
            singleton = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        players = new List<GameObject>();

        InitializeKeyboardInput();
        InitializeXboxInput();

        // By default, always add a keyboard player
        AddPlayer(InputMethod.Keyboard);

        Debug.InputLog("InputManager Awake");
    }

    void Start ()
    {
        keyboardController.Start();
        foreach (XboxController xc in xboxControllers)
            xc.Start();
	}
	
	void Update ()
    {
        keyboardController.Update();
        for (int i = 0; i < maxPlayers; i++)
            xboxControllers[i].Update();
    }

    /// <summary>
    /// Adds a new player with a specified InputMethod
    /// </summary>
    /// <param name="inputMethod"></param>
    public void AddPlayer(InputMethod inputMethod)
    {
        // Do not add more players than the maximum allowed
        if (players.Count >= maxPlayers)
            return;

        // If there is only 1 player and the players' input method is the keyboard, change the input method to a connected controller
        else if(players.Count == 1 && players[0].GetComponent<Player>().InputMethod == InputMethod.Keyboard)
        {
            if(inputMethod == InputMethod.XboxController)
            {
                players[0].GetComponent<Player>().PlayerIndex = 0;
                players[0].GetComponent<Player>().InputMethod = inputMethod;
                return;
            }
        }

        // Create a new player with index/input method, and create a new set of input dictioanries for the player
        else
        {
            GameObject newPlayer;
            players.Add(newPlayer = Instantiate(playerPrefab));

            newPlayer.GetComponent<Player>().InputMethod = inputMethod;
            newPlayer.GetComponent<Player>().PlayerIndex = players.IndexOf(newPlayer);

            DuplicateDictionaries();
            return;
        }
    }

    /// <summary>
    /// Remove a player at the specified index
    /// </summary>
    /// <param name="indexToRemove"></param>
    public void RemovePlayer(int indexToRemove)
    {
        // If removing the first player and the player is using a controller, do not remove the player and switch the input method to keyboard
        if(indexToRemove == 0 && players[indexToRemove].GetComponent<Player>().InputMethod == InputMethod.XboxController)
        {
            players[indexToRemove].GetComponent<Player>().InputMethod = InputMethod.Keyboard;
        }

        // Remove the player from lists and delete their associated dictionaries in the lists
        else if(players[indexToRemove].GetComponent<Player>().InputMethod == InputMethod.XboxController)
        {
            GameObject playerToRemove = players[indexToRemove];
            players.RemoveAt(indexToRemove);
            Destroy(playerToRemove);

            xboxAxisDictList[indexToRemove].Clear();
            xboxAxisDictList.RemoveAt(indexToRemove);
            xboxButtonDictList[indexToRemove].Clear();
            xboxButtonDictList.RemoveAt(indexToRemove);

            keyboardAxisDictList[indexToRemove].Clear();
            keyboardAxisDictList.RemoveAt(indexToRemove);
            keyboardButtonDictList[indexToRemove].Clear();
            keyboardButtonDictList.RemoveAt(indexToRemove);
        }
    }

    /// <summary>
    /// Returns the value of the input axis key from the player/controller
    /// </summary>
    /// <param name="axisKey">The name given to the axis in the inspector</param>
    /// <param name="player">The player index to check the input from</param>
    public float GetAxis(string axisKey, Player player)
    {
        if (player == null)
            return 0;
        else
        {
            if (player.InputMethod == InputMethod.Keyboard)
            {
                float keyboardPos = keyboardController.GetAxis(KeyboardAxisLookUp(keyboardAxisDictList[player.PlayerIndex + 1], "(Pos)" + axisKey));
                float  keyboardNeg = -keyboardController.GetAxis(KeyboardAxisLookUp(keyboardAxisDictList[player.PlayerIndex + 1], "(Neg)" + axisKey));
                return LargestAbsoluteValue(new float[] { keyboardPos, keyboardNeg });
            }

            else if (player.InputMethod == InputMethod.XboxController)
                return xboxControllers[player.PlayerIndex].GetAxis(XboxAxisLookUp(xboxAxisDictList[player.PlayerIndex + 1], axisKey));

            else return 0;
        }
    }

    /// <summary>
    /// Returns the axis value during the first frame the axis reaches its maximum aboslute value
    /// </summary>
    /// <param name="axisKey"></param>
    /// <param name="player"></param>
    public float GetAxisDown(string axisKey, Player player)
    {
        if (player == null)
            return 0;
        else
        {
            if (player.InputMethod == InputMethod.Keyboard)
            {
                float keyboardPos = keyboardController.GetAxisDown(KeyboardAxisLookUp(keyboardAxisDictList[player.PlayerIndex + 1], "(Pos)" + axisKey));
                float keyboardNeg = -keyboardController.GetAxisDown(KeyboardAxisLookUp(keyboardAxisDictList[player.PlayerIndex + 1], "(Neg)" + axisKey));
                return LargestAbsoluteValue(new float[] { keyboardPos, keyboardNeg });
            }

            else if (player.InputMethod == InputMethod.XboxController)
                return xboxControllers[(int)player.PlayerIndex].GetAxisDown(XboxAxisLookUp(xboxAxisDictList[(int)player.PlayerIndex + 1], axisKey));

            else return 0;
        }
    }

    /// <summary>
    /// Returns true if the player is holding down the button 
    /// </summary>
    /// <param name="buttonKey"></param>
    /// <param name="player"></param>
    public bool GetButton(string buttonKey, Player player)
    {
        if (player == null)
            return false;
        else
        {
            if (player.InputMethod == InputMethod.Keyboard)
                return keyboardController.GetButton(KeyboardButtonLookUp(keyboardButtonDictList[player.PlayerIndex + 1], buttonKey));

            else if (player.InputMethod == InputMethod.XboxController)
                return xboxControllers[player.PlayerIndex].GetButton(XboxButtonLookUp(xboxButtonDictList[player.PlayerIndex + 1], buttonKey));

            else return false;
        }
    }

    /// <summary>
    /// Returns true during the first frame the player presses the button
    /// </summary>
    /// <param name="buttonKey"></param>
    /// <param name="player"></param>
    public bool GetButtonDown(string buttonKey, Player player)
    {
        if (player == null)
            return false;
        else
        {
            if (player.InputMethod == InputMethod.Keyboard)
                return keyboardController.GetButtonDown(KeyboardButtonLookUp(keyboardButtonDictList[player.PlayerIndex + 1], buttonKey));

            else if (player.InputMethod == InputMethod.XboxController)
                return xboxControllers[player.PlayerIndex].GetButtonDown(XboxButtonLookUp(xboxButtonDictList[player.PlayerIndex + 1], buttonKey));

            else return false;
        }
    }

    /// <summary>
    /// Returns true during the first frame the player releases the button
    /// </summary>
    /// <param name="buttonKey"></param>
    /// <param name="player"></param>
    public bool GetButtonUp(string buttonKey, Player player)
    {
        if (player == null)
            return false;
        else
        {
            if (player.InputMethod == InputMethod.Keyboard)
                return keyboardController.GetButtonUp(KeyboardButtonLookUp(keyboardButtonDictList[player.PlayerIndex + 1], buttonKey));

            else if (player.InputMethod == InputMethod.XboxController)
                return xboxControllers[player.PlayerIndex].GetButtonUp(XboxButtonLookUp(xboxButtonDictList[player.PlayerIndex + 1], buttonKey));

            else return false;
        }
    }

    /// <summary>
    /// Returns the first Keyboard button that is pressed
    /// </summary>
    public KeyCode GetNextKeyboardButton()
    {
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            // 329 is the maximum enum value for all keys on the keyboard
            if ((int)key > 329)
                break;

            if (keyboardController.GetButton(key))
                return key;
        }
        return KeyCode.None;
    }

    /// <summary>
    /// If the player is not null, returns the first xbox button pressed by the player.
    /// If the player is null, returns the first xbox button pressed by any player.
    /// If no player exists, returns None
    /// </summary>
    /// <param name="player"></param>
    public XboxController.XboxButton GetNextXboxButton(Player player = null)
    {
        if (player == null)
        {
            foreach(XboxController xc in xboxControllers)
            {
                XboxController.XboxButton nextButton;
                if ((nextButton = xc.NextXboxButton) != XboxController.XboxButton.None)
                    return nextButton;
            }
            return XboxController.XboxButton.None;
        }
        else
            return xboxControllers[(int)player.PlayerIndex].NextXboxButton;
    }

    /// <summary>
    /// Resets the state of the button pressed by the player
    /// </summary>
    /// <param name="buttonKey"></param>
    /// <param name="player"></param>
    public void ResetButton(string buttonKey, Player player)
    {
        if (player == null)
            return;
        else
            xboxControllers[player.PlayerIndex].ResetButton(XboxButtonLookUp(xboxButtonDictList[player.PlayerIndex + 1], buttonKey));
        Input.ResetInputAxes();
    }

    /// <summary>
    /// Resets the state of the button pressed by the player
    /// </summary>
    /// <param name="buttonKey"></param>
    /// <param name="player"></param>
    public void ResetButton(XboxController.XboxButton button, Player player)
    {
        if (player == null)
            return;
        else
            xboxControllers[player.PlayerIndex].ResetButton(button);
        Input.ResetInputAxes();
    }

    /// <summary>
    /// Remaps/Updates the Keyboard Axis/Button dictionaries with a new key
    /// </summary>
    /// <param name="buttonKey"></param>
    /// <param name="key"></param>
    /// <param name="player"></param>
    public void RemapKeyboardButton(string buttonKey, KeyCode key, Player player)
    {
        if (player == null)
            return;
        else
        {
            if (keyboardAxisDictList[player.PlayerIndex + 1].ContainsKey(buttonKey))
                keyboardAxisDictList[player.PlayerIndex + 1][buttonKey] = key;
            else if (keyboardButtonDictList[player.PlayerIndex + 1].ContainsKey(buttonKey))
                keyboardButtonDictList[player.PlayerIndex + 1][buttonKey] = key;
            else
                Debug.InputLog("No keyboard dictionary contains the key: " + buttonKey, Debug.LogType.Warning);
        }
    }

    /// <summary>
    /// Remaps/Updates the Xbox button dictionary with a new button
    /// </summary>
    /// <param name="buttonKey"></param>
    /// <param name="button"></param>
    /// <param name="player"></param>
    public void RemapXboxButton(string buttonKey, XboxController.XboxButton button, Player player)
    {
        // !!!NOTE!!! - Currently remapping xbox axis is not supported
        if (player == null)
            return;
        else
            xboxButtonDictList[(int)player.PlayerIndex + 1][buttonKey] = button;
    }

    /// <summary>
    /// Returns a list containing a KeyboardAxisDictionary and KeyboardButtonDictionary at the specified index
    /// </summary>
    /// <param name="index"></param>
    public List<Dictionary<string, KeyCode>> GetKeyboardDictionary(int index)
    {
        if(index >= keyboardAxisDictList.Count) { Debug.InputLog("Index is out of bounds of Keyboard Axis/Button Dictionary list", Debug.LogType.Error); return null; }
        return new List<Dictionary<string, KeyCode>>() { keyboardAxisDictList[index], keyboardButtonDictList[index] };
    }

    /// <summary>
    /// Returns an XboxAxisDictionary at the specified index
    /// </summary>
    /// <param name="index"></param>
    public Dictionary<string, XboxController.XboxAxis> GetXboxAxisDictionary(int index)
    {
        if (index >= xboxAxisDictList.Count) { Debug.InputLog("Index is out of bounds of Xbox Axis Dictionary list", Debug.LogType.Error); return null; }
        return xboxAxisDictList[index];
    }

    /// <summary>
    /// Returns an XboxButtonDictionary at the specified index
    /// </summary>
    /// <param name="index"></param>
    public Dictionary<string, XboxController.XboxButton> GetXboxButtonDictionary(int index)
    {
        if (index >= xboxButtonDictList.Count) { Debug.InputLog("Index is out of bounds of Xbox Button Dictionary list", Debug.LogType.Error); return null; }
        return xboxButtonDictList[index];
    }

    private KeyCode KeyboardAxisLookUp(Dictionary<string, KeyCode> dict, string key)
    {
        if (dict.ContainsKey(key) == false)
        {
            Debug.InputLog("The Keyboard Axis Dictionary does not contain the key: " + key, Debug.LogType.Error);
            return KeyCode.None;
        }
        else return dict[key];
    }

    private KeyCode KeyboardButtonLookUp(Dictionary<string, KeyCode> dict, string key)
    {
        if (dict.ContainsKey(key) == false)
        {
            Debug.InputLog("The Keyboard Button Dictionary does not contain the key: " + key, Debug.LogType.Error);
            return KeyCode.None;
        }
        else return dict[key];
    }

    private XboxController.XboxAxis XboxAxisLookUp(Dictionary<string, XboxController.XboxAxis> dict, string key)
    {
        if (dict.ContainsKey(key) == false)
        {
            Debug.InputLog("The Xbox Axis Dictionary does not contain the key: " + key, Debug.LogType.Error);
            return XboxController.XboxAxis.None;
        }
        else return dict[key];
    }

    private XboxController.XboxButton XboxButtonLookUp(Dictionary<string, XboxController.XboxButton> dict, string key)
    {
        if (dict.ContainsKey(key) == false)
        {
            Debug.InputLog("The Xbox Button Dictionary does not contain the key: " + key, Debug.LogType.Error);
            return XboxController.XboxButton.None;
        }
        else return dict[key];
    }

    /// <summary>
    /// Returns the value that has the highest aboslute value in a list of floats
    /// </summary>
    /// <param name="values"></param>
    private float LargestAbsoluteValue(float[] values)
    {
        float largest = 0f;
        foreach(float f in values)
            if (Mathf.Abs(f) > Mathf.Abs(largest))
                largest = f;
        return largest;
    }

    /// <summary>
    /// Creates a copy of the Keyboard Axis/Button and Xbox Axis/Button dictionaries for each player added
    /// </summary>
    private void DuplicateDictionaries()
    {
        Dictionary<string, KeyCode> newKeyboardAxisDict = keyboardAxisDictList[0].ToDictionary(entry => entry.Key, entry => entry.Value);
        keyboardAxisDictList.Add(newKeyboardAxisDict);

        Dictionary<string, KeyCode> newKeyboardButtonDict = keyboardButtonDictList[0].ToDictionary(entry => entry.Key, entry => entry.Value);
        keyboardButtonDictList.Add(newKeyboardButtonDict);

        Dictionary<string, XboxController.XboxAxis> newXboxAxisDict = xboxAxisDictList[0].ToDictionary(entry => entry.Key, entry => entry.Value);
        xboxAxisDictList.Add(newXboxAxisDict);

        Dictionary<string, XboxController.XboxButton> newXboxButtonDict = xboxButtonDictList[0].ToDictionary(entry => entry.Key, entry => entry.Value);
        xboxButtonDictList.Add(newXboxButtonDict);
    }

    /// <summary>
    /// Initializes the Keyboard input and value dictionaries
    /// </summary>
    private void InitializeKeyboardInput()
    {
        keyboardController = new KeyboardController();
        keyboardAxisDictList = new List<Dictionary<string, KeyCode>>() { new Dictionary<string, KeyCode>() };

        // Add all axes to the keyboard axis dictionary
        foreach (Axis x in axes)
        {
            // Add the positive/negative keyboard axis with the (Pos)/(Neg) addition to the key
            keyboardAxisDictList[0].Add("(Pos)" + x.axisName, x.positiveKeyboardAxis);
            keyboardAxisDictList[0].Add("(Neg)" + x.axisName, x.negativeKeyboardAxis);
        }

        // Add all buttons to the keyboard button dictionary
        keyboardButtonDictList = new List<Dictionary<string, KeyCode>>() { new Dictionary<string, KeyCode>() };
        foreach (Button b in buttons)
            keyboardButtonDictList[0].Add(b.buttonName, b.keyboardButton);
    }

    /// <summary>
    /// Initializes the Xbox controllers and value dictionaries
    /// </summary>
    private void InitializeXboxInput()
    {
        // Create controller list and objects for each controller
        xboxControllers = new List<XboxController>();
        for (int i = 0; i < maxPlayers; i++)
            xboxControllers.Add(new XboxController((PlayerIndex)i));

        // Initialize XboxAxis dictionary
        xboxAxisDictList = new List<Dictionary<string, XboxController.XboxAxis>>() { new Dictionary<string, XboxController.XboxAxis>() };
        foreach (Axis x in axes)
            xboxAxisDictList[0].Add(x.axisName, x.xboxAxis);

        // Initialize XboxButton dictionary
        xboxButtonDictList = new List<Dictionary<string, XboxController.XboxButton>>() { new Dictionary<string, XboxController.XboxButton>() };
        foreach (Button b in buttons)
            xboxButtonDictList[0].Add(b.buttonName, b.xboxButton);
    }
}