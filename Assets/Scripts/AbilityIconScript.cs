using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AbilityIconScript : MonoBehaviour {

    private float fillAmount;

    [SerializeField]
    private Image content;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        content.fillAmount = fillAmount;
    }

    public void SetFillAmount(float timeSinceSpellUsed, float spellCooldown)
    {
        fillAmount = Mathf.Clamp(1-(timeSinceSpellUsed / spellCooldown), 0, 1);
    }
}
