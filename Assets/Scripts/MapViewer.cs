using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MapViewer : MonoBehaviour {

    public static MapViewer instance = null;

    public Canvas mapCanvas;
    public Material mapMaterial;
    public static int tileSize = 28;

    public Sprite walkableTile;
    public Sprite pitTile;
    public Sprite unwalkableTile;
    
    public GameObject miniMapPlayerPos;
    public GameObject fogPlane;

    public Camera fogCamera;
    public RenderTexture fogTexture;

    public bool mapEnabledOnStartup = true;

    public Texture2D mapTexture;

    Texture2D texture;
    void Awake()
    {
        if (instance == null)
            instance = this;
        
        else if (instance != this)
            Destroy(gameObject);
        
        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        mapCanvas.enabled = mapEnabledOnStartup;

        //// resize camera and texture to map size
        //int width = MapGenerator.map.GetLength(0);
        //int height = MapGenerator.map.GetLength(1);

        ////fogCamera.rect = new Rect(0, 0, width / Screen.width, height / Screen.height);
        //if (fogCamera.targetTexture != null)
        //{
        //    fogCamera.targetTexture.Release();
        //}
        //fogTexture = new RenderTexture(width/2, height/2, 24);
        //fogCamera.targetTexture = fogTexture;

        if (LevelManager.ExistsLevelForDepth(Player.depth))
        {
            Texture tex = LevelManager.GetFogTextureForDepth(Player.depth);
            //renderer.material.mainTexture = tex;
            Graphics.Blit(tex, fogTexture);
            //renderer.material.mainTexture = fogTexture;
        }
    }
    
    void Update()
    {
        if(Input.GetButtonDown("Map"))
            mapCanvas.enabled = !mapCanvas.enabled;

        // position player arrow and aperature mask
        miniMapPlayerPos.transform.position = new Vector3(
            fogPlane.transform.position.x - (fogPlane.transform.lossyScale.x * 10 / 2) + Player.GetPosition().x / TerrainGenerator.blocksize * fogPlane.transform.lossyScale.x * 10 / MapGenerator.map.GetLength(0) + 0.1f,
            fogPlane.transform.position.y - (fogPlane.transform.lossyScale.y * 10 / 2) + Player.GetPosition().z / TerrainGenerator.blocksize * fogPlane.transform.lossyScale.y * 10 / MapGenerator.map.GetLength(1) + 0.1f,
            miniMapPlayerPos.transform.position.z
            );
        miniMapPlayerPos.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -Player.instance.transform.rotation.eulerAngles.y));
    }

    public void GenerateMapView()
    {
        mapTexture = new Texture2D(MapGenerator.map.GetLength(0) * tileSize, MapGenerator.map.GetLength(1) * tileSize);
        mapMaterial.mainTexture = mapTexture;

        for (int x = 0; x < MapGenerator.map.GetLength(0); x++)
        {
            for (int y = 0; y < MapGenerator.map.GetLength(1); y++)
            {
                if (MapGenerator.map[x, y] == MapGenerator.TileType.Floor || MapGenerator.map[x, y] == MapGenerator.TileType.StairsDown || MapGenerator.map[x, y] == MapGenerator.TileType.StairsUp) // floor
                {
                    Draw(x, y, walkableTile);
                }
                else if (MapGenerator.map[x, y] == MapGenerator.TileType.Pit || MapGenerator.map[x, y] == MapGenerator.TileType.Bridge) // pit
                {
                    Draw(x, y, pitTile);
                }
                else
                {
                    Draw(x, y, unwalkableTile);
                }
            }
        }
    }

    public static void Draw(int xCoord, int yCoord, Sprite sprite, bool rotate = false)
    {
        for (int x = 0; x < (int)sprite.rect.width && (xCoord * tileSize + x < instance.mapTexture.width); x++)
        {
            for (int y = 0; y < (int)sprite.rect.width && (yCoord * tileSize + y < instance.mapTexture.height); y++)
            {
                if (sprite.texture.GetPixel(x, y).a != 0)
                {
                    if (rotate)
                        instance.mapTexture.SetPixel(xCoord * tileSize + y, yCoord * tileSize + x, sprite.texture.GetPixel(x, y));
                    else
                        instance.mapTexture.SetPixel(xCoord * tileSize + x, yCoord * tileSize + y, sprite.texture.GetPixel(x, y));
                }
            }
        }
    }

    public static void DrawFromWorldPosition(int xCoord, int yCoord, Sprite sprite)
    {
        for (int x = 0; x < (int)sprite.rect.width; x++)
        {
            for (int y = 0; y < (int)sprite.rect.width; y++)
            {
                if (x >= 0 && x < instance.mapTexture.width && y >= 0 && y < instance.mapTexture.height)
                    instance.mapTexture.SetPixel(xCoord * tileSize + x, yCoord * tileSize + y, sprite.texture.GetPixel(x, y));
            }
        }
    }

    public static void ApplyMapTexture()
    {
        instance.mapTexture.Apply();
    }
}
