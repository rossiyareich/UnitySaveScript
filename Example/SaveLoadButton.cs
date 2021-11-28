using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadButton : MonoBehaviour
{
    public void OnSaveButtonClicked()
    {
        SaveLoadManager.I.SaveAll(0, out _);
    }

    public void OnLoadButtonClicked()
    {
        SaveLoadManager.I.LoadAll(0, out _);
    }
}
