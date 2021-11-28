using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Number : MonoBehaviour, ISaveLoad
{
    public int number;

    Text text;

    public string GetDataTypeName() => nameof(NumberSLData);

    public string GetGameObjectName() => gameObject.GetFullName();

    public void Load(ISaveLoadData loadData)
    {
        var data = loadData as NumberSLData;
        number = data.number;
    }

    public ISaveLoadData Save()
    {
        return new NumberSLData()
        {
            number = this.number
        };
    }

    void Awake()
    {
        text = GetComponent<Text>();
    }

    void Update()
    {
        text.text = number.ToString();
    }
}

[Serializable]
public class NumberSLData : ISaveLoadData
{
    public int number;
}