using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class TerrainGenerator : MonoBehaviour {

    public GameObject playerPrefab;

    [Header("Terrain Prefabs")]
    public GameObject floorPrefab; // 0
    public GameObject ceilingPrefab;
    public GameObject wallPrefab; // 1
    public GameObject wallTorchPrefab;
    public GameObject pitWallPrefab;
    public GameObject skyWallPrefab;
    public GameObject columnPrefab;
    public GameObject pitColumnPrefab;
    public GameObject stairsDownPrefab; // 2
    public GameObject stairsUpPrefab; // 3
    public GameObject doorPrefab;
    public GameObject falseWallPrefab;
    public GameObject pitPrefab; // 8
    public GameObject bridgePrefab; // 9
    public GameObject treePrefab;

    [Header("Mapview Sprites")]
    public Sprite doorSprite;
    public Sprite stairsUpSprite;
    public Sprite stairsDownSprite;
    public Sprite secretSprite;
    public Sprite bridgeSprite;

    [Header("Variables")]
    [SerializeField, Range(0f, 100f), Tooltip("The percentage of walls that will hold a torch.")]
    private float chanceOfTorch = 10;

    // sharing is caring and functional
    public static int blocksize = 4;
    public static Vector3 stairsDownPosition;
    public static Vector3 stairsUpPosition;
    public static bool isReady = false;

    List<Coord> placedColumns;

    MapGenerator.TileType[,] map; //TODO: this doesn't need a local copy, access map generators instead

    struct Coord
    {
        public float xPos;
        public float yPos;

        public Coord(float x, float y)
        {
            xPos = x;
            yPos = y;
        }
    }

    void Awake()
    {
        isReady = false;
    }

    public void GenerateTerrain(MapGenerator.TileType[,] _map)
    {
        map = _map;

        placedColumns = new List<Coord>();

        GameObject ceilings = new GameObject();
        ceilings.name = "Ceilings";
        
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                GameObject parentGO = new GameObject();
                parentGO.isStatic = true;
                GameObject go;

                if (map[x, y] == MapGenerator.TileType.Floor || map[x, y] == MapGenerator.TileType.SecretTile || map[x, y] == MapGenerator.TileType.SecretTunnel) // floor or secret tunnel/room
                {
                    go = Instantiate(floorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = parentGO.transform;

                    generateWallsAroundTile(x, y, parentGO);
                }
                else if (map[x, y] == MapGenerator.TileType.StairsUp) // stairs up to previous level
                {
                    stairsUpPosition = new Vector3(x * blocksize, 0, y * blocksize);

                    go = Instantiate(floorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = parentGO.transform;

                    go = Instantiate(stairsUpPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = parentGO.transform;

                    MapViewer.Draw(x, y, stairsUpSprite);
                    generateWallsAroundTile(x, y, parentGO);
                }
                else if (map[x, y] == MapGenerator.TileType.StairsDown) // stairs down to next level
                {
                    stairsDownPosition = new Vector3(x * blocksize, 0, y * blocksize);
                    go = Instantiate(stairsDownPrefab, stairsDownPosition, Quaternion.identity) as GameObject;
                    go.transform.parent = parentGO.transform;

                    MapViewer.Draw(x, y, stairsDownSprite);
                    generateWallsAroundTile(x, y, parentGO);
                }
                else if (map[x, y] == MapGenerator.TileType.SecretDoor) // secret door
                {
                    go = Instantiate(floorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = parentGO.transform;

                    go = Instantiate(falseWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = parentGO.transform;

                    MapViewer.Draw(x, y, secretSprite);
                    generateWallsAroundTile(x, y, parentGO);
                }
                else if (map[x, y] == MapGenerator.TileType.Pit) // pit
                {
                    go = Instantiate(pitPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = parentGO.transform;

                    generateWallsAroundTile(x, y, parentGO);
                    generatePitWallsAroundTile(x, y, parentGO);
                }
                else if (map[x, y] == MapGenerator.TileType.Bridge) // bridge
                {
                    if (map[x - 1, y] == MapGenerator.TileType.Bridge || map[x + 1, y] == MapGenerator.TileType.Bridge)
                    {
                        go = Instantiate(bridgePrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 90, 0))) as GameObject;
                        go.transform.parent = parentGO.transform;

                        MapViewer.Draw(x, y, bridgeSprite, true);
                    }
                    else if (map[x, y - 1] == MapGenerator.TileType.Bridge || map[x, y + 1] == MapGenerator.TileType.Bridge)
                    {
                        go = Instantiate(bridgePrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                        go.transform.parent = parentGO.transform;

                        MapViewer.Draw(x, y, bridgeSprite);
                    }
                    else if (map[x - 1, y] == MapGenerator.TileType.Floor || map[x + 1, y] == MapGenerator.TileType.Floor)
                    {
                        go = Instantiate(bridgePrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 90, 0))) as GameObject;
                        go.transform.parent = parentGO.transform;

                        MapViewer.Draw(x, y, bridgeSprite, true);
                    }
                    else
                    {
                        go = Instantiate(bridgePrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                        go.transform.parent = parentGO.transform;

                        MapViewer.Draw(x, y, bridgeSprite);
                    }

                    generateWallsAroundTile(x, y, parentGO);
                    generatePitWallsAroundTile(x, y, parentGO);
                }
                else if (map[x, y] == MapGenerator.TileType.Tree) {
                    Instantiate(treePrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity);
                    Instantiate(floorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity);
                    generateWallsAroundTile(x, y, parentGO);
                }

                if (map[x, y] == MapGenerator.TileType.Wall || map[x, y] == MapGenerator.TileType.StairsUp)
                {
                    // do nothing
                }
                else if (!LevelManager.ExistsLevelForDepth(Player.depth - 1))
                {
                    go = Instantiate(ceilingPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = ceilings.transform;
                }
                else if (LevelManager.GetMapForDepth(Player.depth - 1)[x, y] == MapGenerator.TileType.Pit || LevelManager.GetMapForDepth(Player.depth - 1)[x, y] == MapGenerator.TileType.Bridge)
                {
                    generateSkyWallsAroundTile(x, y);
                }
                else
                {
                    go = Instantiate(ceilingPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                    go.transform.parent = ceilings.transform;
                }

                if (parentGO.transform.childCount <= 0)
                {
                    Destroy(parentGO);
                }
                
            }
        }

        //if (ceilings.transform.childCount <= 0)
        //{
        //    Destroy(ceilings);
        //    ceilings = new GameObject();
        //}
        //else
        //{
        //    ceilings.AddComponent<MeshFilter>();
        //    ceilings.AddComponent<MeshRenderer>();
        //    ceilings.AddComponent<CombineMeshes>();
        //    ceilings.GetComponent<Renderer>().material = ceilingMaterial;
        //}


        isReady = true;
    }

    void addColumn(List<Coord> placedColumns, float x, float y, GameObject parentGO)
    {
        if (placedColumns.Exists(c => (c.xPos == x && c.yPos == y)))
            return;

        Coord newCoord = new Coord(x, y);
        GameObject go;
        if (
            map[(int)Mathf.Floor(x), (int)Mathf.Floor(y)] == MapGenerator.TileType.Pit
            || map[(int)Mathf.Floor(x), (int)Mathf.Ceil(y)] == MapGenerator.TileType.Pit
            || map[(int)Mathf.Ceil(x), (int)Mathf.Floor(y)] == MapGenerator.TileType.Pit
            || map[(int)Mathf.Ceil(x), (int)Mathf.Ceil(y)] == MapGenerator.TileType.Pit
            )
        {
            go = Instantiate(pitColumnPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
        }
        else
        {
            go = Instantiate(columnPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
        }

        
        go.transform.parent = parentGO.transform;
        placedColumns.Add(newCoord);
    }

    void addColumn(List<Coord> placedColumns, float x, float y)
    {
        if (placedColumns.Exists(c => (c.xPos == x && c.yPos == y)))
            return;

        Coord newCoord = new Coord(x, y);
        
        if (
            map[(int)Mathf.Floor(x), (int)Mathf.Floor(y)] == MapGenerator.TileType.Pit
            || map[(int)Mathf.Floor(x), (int)Mathf.Ceil(y)] == MapGenerator.TileType.Pit
            || map[(int)Mathf.Ceil(x), (int)Mathf.Floor(y)] == MapGenerator.TileType.Pit
            || map[(int)Mathf.Ceil(x), (int)Mathf.Ceil(y)] == MapGenerator.TileType.Pit
            )
        {
            Instantiate(pitColumnPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity);
        }
        else
        {
            Instantiate(columnPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity);
        }

        placedColumns.Add(newCoord);
    }

    MapGenerator.TileType mapValue(int x, int y)
    {
        if (x < 0 || x >= map.GetLength(0) || y < 0 || y >= map.GetLength(1))
            return MapGenerator.TileType.Wall;

        return map[x, y];
    }

    void placeSecretDoor(int x, int y)
    {
        bool doorPlaced = false;
        int locX = x;
        int locY = y;
        int wallCount;

        List<Coord> triedPositions = new List<Coord>();

        while (!doorPlaced)
        {
            triedPositions.Add(new Coord(locX, locY));
            wallCount = 0;

            if (mapValue(locX, locY - 1) == MapGenerator.TileType.Wall)
            {
                wallCount++;
            }
            if (mapValue(locX, locY + 1) == MapGenerator.TileType.Wall)
            {
                wallCount++;
            }
            if (mapValue(locX - 1, locY) == MapGenerator.TileType.Wall)
            {
                wallCount++;
            }
            if (mapValue(locX + 1, locY) == MapGenerator.TileType.Wall)
            {
                wallCount++;
            }

            if (wallCount >= 2)
            {
                MapViewer.Draw(locX, locY, secretSprite);
                doorPlaced = true;
                Instantiate(falseWallPrefab, new Vector3(locX * blocksize, 0, locY * blocksize), Quaternion.Euler(new Vector3(0, 0, 0)));
                print("found");
            }
            else
            {
                print("proceeding");

                if (map[locX - 1, locY] == MapGenerator.TileType.SecretDoor && !triedPositions.Contains(new Coord(locX - 1, locY)))
                {
                    locX = locX - 1;
                }
                else if (map[locX + 1, locY] == MapGenerator.TileType.SecretDoor && !triedPositions.Contains(new Coord(locX + 1, locY)))
                {
                    locX = locX + 1;
                }
                else if (map[locX, locY - 1] == MapGenerator.TileType.SecretDoor && !triedPositions.Contains(new Coord(locX, locY - 1)))
                {
                    locY = locY - 1;
                }
                else if (map[locX, locY + 1] == MapGenerator.TileType.SecretDoor && !triedPositions.Contains(new Coord(locX, locY + 1)))
                {
                    locY = locY + 1;
                }
                else
                {
                    print("FAIL: Gave up placing false wall");
                    doorPlaced = true;
                }
            }
        }
    }

    void generateSkyWallsAroundTile(int x, int y)
    {
        if (LevelManager.ExistsLevelForDepth(Player.depth - 1))
        {
            MapGenerator.TileType[,] skymap = LevelManager.GetMapForDepth(Player.depth - 1);
            if (skymap[x - 1, y] != MapGenerator.TileType.Pit && skymap[x - 1, y] != MapGenerator.TileType.Bridge)
            {
                Instantiate(skyWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity);
                addColumn(placedColumns, (x - 0.5f), (y - 0.5f));
                addColumn(placedColumns, (x - 0.5f), (y + 0.5f));
            }
            if (skymap[x, y + 1] != MapGenerator.TileType.Pit && skymap[x, y + 1] != MapGenerator.TileType.Bridge)
            {
                Instantiate(skyWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 90, 0)));
                addColumn(placedColumns, (x - 0.5f), (y + 0.5f));
                addColumn(placedColumns, (x + 0.5f), (y + 0.5f));
            }
            if (skymap[x + 1, y] != MapGenerator.TileType.Pit && skymap[x + 1, y] != MapGenerator.TileType.Bridge)
            {
                Instantiate(skyWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 180, 0)));
                addColumn(placedColumns, (x + 0.5f), (y - 0.5f));
                addColumn(placedColumns, (x + 0.5f), (y + 0.5f));
            }
            if (skymap[x, y - 1] != MapGenerator.TileType.Pit && skymap[x, y - 1] != MapGenerator.TileType.Bridge)
            {
                Instantiate(skyWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 270, 0)));
                addColumn(placedColumns, (x - 0.5f), (y - 0.5f));
                addColumn(placedColumns, (x + 0.5f), (y - 0.5f));
            }
        }
    }

    void generateWallsAroundTile(int x, int y, GameObject parentGO)
    {
        int walls = 0;
        bool horizontal = false;
        bool vertical = false;
        bool torchPlaced = false;

        // dont place a torch over a pit
        if (map[x, y] == MapGenerator.TileType.Pit)
        {
            torchPlaced = true;
        }

        GameObject go;

        // create each possible wall and column
        if (mapValue(x - 1, y) == MapGenerator.TileType.Wall)
        {
            if (UnityEngine.Random.Range(0, 100) < chanceOfTorch && !torchPlaced)
            {
                go = Instantiate(wallTorchPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
                go.transform.parent = parentGO.transform;
                torchPlaced = true;
            }
            
            go = Instantiate(wallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
            go.transform.parent = parentGO.transform;
            
            addColumn(placedColumns, (x - 0.5f), (y - 0.5f), parentGO);
            addColumn(placedColumns, (x - 0.5f), (y + 0.5f), parentGO);
            walls++;
            vertical = true;
        }
        if (map[x, y + 1] == MapGenerator.TileType.Wall)
        {
            if (UnityEngine.Random.Range(0, 100) < chanceOfTorch && !torchPlaced)
            {
                go = Instantiate(wallTorchPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 90, 0))) as GameObject;
                go.transform.parent = parentGO.transform;
                torchPlaced = true;
            }
            
            go = Instantiate(wallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 90, 0))) as GameObject;
            go.transform.parent = parentGO.transform;
            
            
            addColumn(placedColumns, (x - 0.5f), (y + 0.5f), parentGO);
            addColumn(placedColumns, (x + 0.5f), (y + 0.5f), parentGO);
            walls++;
            horizontal = true;
        }
        if (map[x + 1, y] == MapGenerator.TileType.Wall)
        {
            if (UnityEngine.Random.Range(0, 100) < chanceOfTorch && !torchPlaced)
            {
                go = Instantiate(wallTorchPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 180, 0))) as GameObject;
                go.transform.parent = parentGO.transform;
                torchPlaced = true;
            }
            
            go = Instantiate(wallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 180, 0))) as GameObject;
            go.transform.parent = parentGO.transform;
            
            addColumn(placedColumns, (x + 0.5f), (y - 0.5f), parentGO);
            addColumn(placedColumns, (x + 0.5f), (y + 0.5f), parentGO);
            walls++;
            vertical = true;
        }
        if (map[x, y - 1] == MapGenerator.TileType.Wall)
        {
            if (UnityEngine.Random.Range(0, 100) < chanceOfTorch && !torchPlaced)
            {
                go = Instantiate(wallTorchPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 270, 0))) as GameObject;
                go.transform.parent = parentGO.transform;
                torchPlaced = true;
            }
            go = Instantiate(wallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 270, 0))) as GameObject;
            go.transform.parent = parentGO.transform;
            
            
            addColumn(placedColumns, (x - 0.5f), (y - 0.5f), parentGO);
            addColumn(placedColumns, (x + 0.5f), (y - 0.5f), parentGO);
            walls++;
            horizontal = true;
        }

        // place door
        if (walls == 2 && map[x, y] != MapGenerator.TileType.SecretDoor && map[x, y] != MapGenerator.TileType.SecretTunnel)
        {
            if (horizontal && !vertical)
            {
                int d1 = openSpaceCount(x + 1, y);
                int d2 = openSpaceCount(x - 1, y);
                if (d1 >= 6 && d1 > d2)
                {
                    Instantiate(doorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 90, 0)));
                    MapViewer.Draw(x, y, doorSprite, true);                    
                }
                else if (d2 >= 6)
                { 
                    Instantiate(doorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 270, 0)));
                    MapViewer.Draw(x, y, doorSprite, true);
                }
            }
            if (vertical && !horizontal)
            {
                int d1 = openSpaceCount(x, y + 1);
                int d2 = openSpaceCount(x, y - 1);
                if (d1 >= 6 && d1 > d2)
                {
                    Instantiate(doorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 0, 0)));
                    MapViewer.Draw(x, y, doorSprite);
                }
                else if (d2 >= 6)
                {
                    Instantiate(doorPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 180, 0)));
                    MapViewer.Draw(x, y, doorSprite);
                }
            }
        }

    }

    void generatePitWallsAroundTile(int x, int y, GameObject parentGO)
    {
        if (mapValue(x - 1, y) != MapGenerator.TileType.Pit && mapValue(x - 1, y) != MapGenerator.TileType.Bridge)
        {
            GameObject go = Instantiate(pitWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.identity) as GameObject;
            go.transform.parent = parentGO.transform;
        }
        if (map[x + 1, y] != MapGenerator.TileType.Pit && map[x + 1, y] != MapGenerator.TileType.Bridge)
        {
            GameObject go = Instantiate(pitWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 180, 0))) as GameObject;
            go.transform.parent = parentGO.transform;
        }
        if (map[x, y - 1] != MapGenerator.TileType.Pit && map[x, y - 1] != MapGenerator.TileType.Bridge)
        {
            GameObject go = Instantiate(pitWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 270, 0))) as GameObject;
            go.transform.parent = parentGO.transform;
        }
        if (map[x, y + 1] != MapGenerator.TileType.Pit && map[x, y + 1] != MapGenerator.TileType.Bridge)
        {
            GameObject go = Instantiate(pitWallPrefab, new Vector3(x * blocksize, 0, y * blocksize), Quaternion.Euler(new Vector3(0, 90, 0))) as GameObject;
            go.transform.parent = parentGO.transform;
        }
    }

    private int openSpaceCount(int x, int y)
    {
        int count = 0;
        
        for (int r = -1; r <= 1; r++)
        {
            for (int c = -1; c <= 1; c++)
            {
                if (map[x+r, y+c] == 0)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
