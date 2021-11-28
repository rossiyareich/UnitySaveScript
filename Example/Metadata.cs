using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Metadata : MonoBehaviour
{
    Text text;

	void Awake()
	{
		text = GetComponent<Text>();
	}
	
    void Start()
    {
        SaveLoadManager.I.OnSaveLoad += OnSaveLoad;
    }

    private void OnSaveLoad(object sender, bool e)
    {
        text.text = e ? "Saved!\n" : "Loaded!\n";
        text.text += $"Save created on: {DateTime.Parse(SaveLoadManager.I.GetSaveMetadata(0).CreatedAt).ToLocalTime()}\n";
        text.text += $"Last saved on: {DateTime.Parse(SaveLoadManager.I.GetSaveMetadata(0).LastSaved).ToLocalTime()}";
    }
}
