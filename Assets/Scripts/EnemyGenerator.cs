using UnityEngine;
using System.Collections;

public class EnemyGenerator : MonoBehaviour {

    public GameObject[] enemies;
    public int[] enemyCounts;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void GenerateEnemies()
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            while (enemyCounts[i] > 0)
            {
                Instantiate(enemies[i], Grid.GetRandomGridPositionAwayFrom(TerrainGenerator.stairsDownPosition), Quaternion.Euler(new Vector3(0, Random.Range(0, 3) * 90, 0)));
                enemyCounts[i]--;
            }
        }
        
    }
}
