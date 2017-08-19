using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class LevelManager : MonoBehaviour {

    public static List<Level> levels = new List<Level>();

	// Use this for initialization
	void Start () {
        GameObject.DontDestroyOnLoad(this);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public static void addLevel(int depth, MapGenerator.TileType[,] map, RenderTexture fogTexture)
    {
        Level newLevel = new Level(depth, MapGenerator.map, fogTexture);
        levels.Add(newLevel);
    }

    public static void updateLevel(int depth, RenderTexture _fogTexture)
    {
        Color[,] fogData = levels.Find(l => l.depth == depth).fogData;
        // copy fog texture data
        Texture2D texture;
        texture = new Texture2D(_fogTexture.width, _fogTexture.height);
        RenderTexture.active = _fogTexture;
        //don't forget that you need to specify rendertexture before you call readpixels
        //otherwise it will read screen pixels.
        texture.ReadPixels(new Rect(0, 0, _fogTexture.width, _fogTexture.height), 0, 0);
        for (int i = 0; i < _fogTexture.width; i++)
        {
            for (int j = 0; j < _fogTexture.height; j++)
            {
                fogData[i, j] = texture.GetPixel(i, j);
            }
        }
        texture.Apply();
        RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it.  
    }

    public static bool ExistsLevelForDepth(int depth)
    {
        return levels.Exists(l => l.depth == depth);
    }

    public static MapGenerator.TileType[,] GetMapForDepth(int depth)
    {
        return levels.Find(l => l.depth == depth).map;
    }

    public static Texture GetFogTextureForDepth(int depth)
    {
        print("retrieving fog texture for depth " + depth);
        RenderTexture fogTexture = levels.Find(l => l.depth == depth).fogTexture;
        Color[,] fogData = levels.Find(l => l.depth == depth).fogData;

        // copy fog texture data
        Texture2D texture;
        texture = new Texture2D(fogTexture.width, fogTexture.height);
        RenderTexture.active = fogTexture;
        //don't forget that you need to specify rendertexture before you call readpixels
        //otherwise it will read screen pixels.
        texture.ReadPixels(new Rect(0, 0, fogTexture.width, fogTexture.height), 0, 0);
        for (int i = 0; i < fogTexture.width; i++)
        {
            for (int j = 0; j < fogTexture.height; j++)
            {
                texture.SetPixel(i, j, fogData[i,j]);
            }
        }
        texture.Apply();
        RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it.

        return texture;
    }

    public class Level
    {
        public int depth;
        public MapGenerator.TileType[,] map;
        public RenderTexture fogTexture;
        public Color[,] fogData;

        public Level(int _no, MapGenerator.TileType[,] _map, RenderTexture _fogTexture)
        {
            depth = _no;
            map = _map;
            fogTexture = _fogTexture;
            fogData = new Color[fogTexture.width, fogTexture.height];

            // copy fog texture data
            Texture2D texture;
            texture = new Texture2D(_fogTexture.width, _fogTexture.height);
            RenderTexture.active = _fogTexture;
            //don't forget that you need to specify rendertexture before you call readpixels
            //otherwise it will read screen pixels.
            texture.ReadPixels(new Rect(0, 0, _fogTexture.width, _fogTexture.height), 0, 0);
            for (int i = 0; i < _fogTexture.width; i++)
            {
                for (int j = 0; j < _fogTexture.height; j++)
                {
                    fogData[i,j] = texture.GetPixel(i, j);
                }
            }
            texture.Apply();
            RenderTexture.active = null; //don't forget to set it back to null once you finished playing with it.  
        }
    }
    
}
