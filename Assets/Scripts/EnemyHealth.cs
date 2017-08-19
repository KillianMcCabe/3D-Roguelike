using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour {

    [SerializeField]
    private GameObject healthBarPrefab;

    private BarScript healthBar;

    public float maxHP = 100;
    public float HP = 100;

    private DepthUIScript depthUIScript;

    private EnemyScript enemyScript;

    //Canvas canvas;
    GameObject canvas;

    public float healthPanelOffset = 0.35f;
    private GameObject healthPanel;
    private Renderer selfRenderer;

    // Use this for initialization
    void Start () {
        
        canvas = GameObject.Find("ScreenCanvas");
        selfRenderer = GetComponentInChildren<Renderer>();
        enemyScript = GetComponent<EnemyScript>();

        healthPanel = Instantiate(healthBarPrefab) as GameObject;
        healthPanel.transform.SetParent(canvas.transform, false);
        healthBar = healthPanel.GetComponent<BarScript>();
        depthUIScript = healthPanel.GetComponent<DepthUIScript>();
        canvas.GetComponent<ScreenSpaceCanvasScript>().AddToCanvas(healthPanel);

        HP = maxHP;
        healthBar.SetFillAmount(HP, maxHP);
    }
	
	// Update is called once per frame
	void Update () {

        Vector3 worldPos = new Vector3(transform.position.x, transform.position.y + healthPanelOffset, transform.position.z);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        healthPanel.transform.position = new Vector3(screenPos.x, screenPos.y, screenPos.z);

        float distance = (worldPos - Camera.main.transform.position).magnitude;
        depthUIScript.depth = -distance;

        distance = Mathf.Max(distance, 1.5f);
        
        healthPanel.GetComponent<Image>().rectTransform.localScale = new Vector2(2 / distance, 2 / distance);
        if (selfRenderer.isVisible && enemyScript.agro && Vector3.Dot(Camera.main.transform.forward, enemyScript.transform.position - Player.instance.transform.position) > 0)
        {
            healthPanel.SetActive(true);
        }
        else
        {
            healthPanel.SetActive(false);
        }
    }

    public void TakeDamage(float damage)
    {
        HP -= damage;
        enemyScript.ActivateAgro();
        healthBar.SetFillAmount(HP, maxHP);
    }

    public void Destroy()
    {
        canvas.GetComponent<ScreenSpaceCanvasScript>().RemoveFromCanvas(healthPanel);
        Destroy(healthPanel);
    }

}
