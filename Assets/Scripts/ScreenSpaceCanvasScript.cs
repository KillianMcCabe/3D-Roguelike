using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ScreenSpaceCanvasScript : MonoBehaviour {

    List<DepthUIScript> panels = new List<DepthUIScript>();

    void Awake()
    {
        panels.Clear();
    }

    void Update()
    {
        Sort();
    }

    public void AddToCanvas(GameObject objectToAdd)
    {
        panels.Add(objectToAdd.GetComponent<DepthUIScript>());
    }

    void Sort()
    {
        panels.Sort((x, y) => x.depth.CompareTo(y.depth));
        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].transform.SetSiblingIndex(i);
        }
    }

    public void RemoveFromCanvas(GameObject objectToRemove)
    {
        panels.Remove(objectToRemove.GetComponent<DepthUIScript>());
    }
}
