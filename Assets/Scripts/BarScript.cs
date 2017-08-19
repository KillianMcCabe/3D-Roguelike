using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BarScript : MonoBehaviour {

    [SerializeField]
    private Image healthContent;

    public void SetFillAmount(float value, float maxValue)
    {
        DepletionBar bar = gameObject.AddComponent<DepletionBar>();
        bar.Create(healthContent);

        healthContent.fillAmount = Mathf.Clamp(value / maxValue, 0, 1);
    }
    
}
