using UnityEngine;
using System.Collections;

public class RenderEffects : MonoBehaviour {

    public Shader ReplacementShader;
    public Color OverDrawColor;
    private bool active = false;

    void Start()
    {
        Shader.SetGlobalColor("_OverDrawColor", OverDrawColor);
    }

    public void ToggleActive()
    {
        if (active)
        {
            Disable();
        }
        else
        {
            Enable();
        }
    }

    public void Enable()
    {
        if (ReplacementShader != null)
        {
            GetComponent<Camera>().SetReplacementShader(ReplacementShader, "");
            active = true;
        }
    }

    public void Disable()
    {
        GetComponent<Camera>().ResetReplacementShader();
        active = false;
    }
    
}
