using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DepletionBar : MonoBehaviour
{
    static float displayTime = 0.5f;

    private float timeSinceCreated;

    public Image image;

    public void Create(Image content)
    {
        image = Instantiate(content);
        image.transform.SetParent(content.transform.parent, false);
        image.transform.SetSiblingIndex(0);
        image.fillAmount = content.fillAmount;

        timeSinceCreated = 0;
    }

    void Update()
    {
        
        if (timeSinceCreated >= displayTime && image != null)
        {
            //gameObject.SetActive(false);

            Destroy(image.gameObject);
            Destroy(gameObject.GetComponent<DepletionBar>());
        }
        else
        {
            timeSinceCreated += Time.deltaTime;
            image.color = Color.Lerp(Color.red, Color.grey, timeSinceCreated * (1/displayTime));
        }
    }



}
