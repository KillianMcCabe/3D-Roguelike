using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class Stat {
    
    private BarScript bar;
    [SerializeField]
    private float maxValue;
    [SerializeField]
    private float currentValue;

    public float CurrentValue
    {
        get
        {
            return currentValue;
        }

        set
        {
            currentValue = value;
            bar.SetFillAmount(currentValue, maxValue);
        }
    }

    public float MaxValue
    {
        get
        {
            return maxValue;
        }

        set
        {
            maxValue = value;
            bar.SetFillAmount(currentValue, maxValue);
        }
    }

    public void Init()
    {
        bar = GameObject.Find("HealthBar").GetComponent<BarScript>();
        bar.SetFillAmount(currentValue, maxValue);
    }
}
