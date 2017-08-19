using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGenerator : MonoBehaviour
{
    public bool DEBUGMODE = false;

    public enum TileType
    {
        Floor,
        Wall,
        StairsDown,
        StairsUp,
        SecretTunnel,
        SecretDoor,
        SecretTile,
        Pit,
        Bridge,
        Tree
    }

    public int width;
    public int height;
    [Range(1, 10)]
    public int smoothIterations = 5;
    [Range(1, 5)]
    public int tunnelsize;
    public bool clearIslands;
    public int wallThresholdSize = 50;
    public bool clearSmallRooms;
    public int roomThresholdSize = 50;
    public int buildingCount;
    public int minRoomSize;
    public int maxRoomSize;
    [Range(0, 100)]
    public float percentageChanceOfCorridor;

    public string seed;
    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent;

    public static TileType[,] map;
    public static TileType[,] pitmap;

    public static List<Room> secretRooms = new List<Room>();
    public static List<SecretCorridor> secretCorridors = new List<SecretCorridor>();
    System.Random pseudoRandom;

    int borderSize = 1;
    Coord stairsUpPosition;

    void Start()
    {
        if (useRandomSeed)
        {
            //print("using random seed");
            //seed = Time.time.ToString();
            seed = System.DateTime.Now.Ticks.ToString();
        }

        print("Seed: " + seed);
        pseudoRandom = new System.Random(seed.GetHashCode());

        GenerateMap();
    }

    public void GenerateMap()
    {

        if (LevelManager.ExistsLevelForDepth(Player.depth))
        {
            secretRooms = new List<Room>();
            secretCorridors = new List<SecretCorridor>();
            map = LevelManager.GetMapForDepth(Player.depth);
            print("loaded level for depth " + Player.depth);
        }
        else
        {
            secretRooms = new List<Room>();
            secretCorridors = new List<SecretCorridor>();

            map = new TileType[width, height];
            pitmap = new TileType[width, height];
            RandomFillMap();

            for (int i = 0; i < smoothIterations; i++)
            {
                SmoothMap();
            }


            ProcessMap();

            ErodeFloorWithPits();

            GenerateBuildings();
            ConnectBuildingsToRestOfMap();

            ManageSecretRooms();
            placeSecretDoors();

            // add border to map
            TileType[,] borderedMap = new TileType[width + borderSize * 2, height + borderSize * 2];
            for (int x = 0; x < borderedMap.GetLength(0); x++)
            {
                for (int y = 0; y < borderedMap.GetLength(1); y++)
                {
                    if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                    {
                        borderedMap[x, y] = map[x - borderSize, y - borderSize];
                    }
                    else
                    {
                        borderedMap[x, y] = TileType.Wall;
                    }
                }
            }

            map = borderedMap;

            print("Secret room count: " + secretRooms.Count);

            // apply border size to secret rooms and corridors
            for (int i = 0; i < secretRooms.Count; i++)
            {
                for (int j = 0; j < secretRooms[i].tiles.Count; j++)
                {
                    Coord newCoord = new Coord(secretRooms[i].tiles[j].tileX + 1, secretRooms[i].tiles[j].tileY + 1);
                    secretRooms[i].tiles[j] = newCoord;
                    map[newCoord.tileX, newCoord.tileY] = TileType.SecretTile; // make tile secret so that it doesn't show on map
                }
            }
            for (int i = 0; i < secretCorridors.Count; i++)
            {
                for (int j = 0; j < secretCorridors[i].tiles.Count; j++)
                {
                    Coord newCoord = new Coord(secretCorridors[i].tiles[j].tileX + 1, secretCorridors[i].tiles[j].tileY + 1);
                    secretCorridors[i].tiles[j] = newCoord;
                    if (map[newCoord.tileX, newCoord.tileY] != TileType.SecretDoor)
                        map[newCoord.tileX, newCoord.tileY] = TileType.SecretTile; // make tile secret so that it doesn't show on map
                }
            }

            // re-apply stairs down
            map[stairsUpPosition.tileX + borderSize, stairsUpPosition.tileY + borderSize] = TileType.StairsUp;
            PlaceStaircaseDownToNextFloor();
        }

        MapViewer mapViewer = GetComponent<MapViewer>();
        mapViewer.GenerateMapView();

        TerrainGenerator terrainGen = GetComponent<TerrainGenerator>();
        terrainGen.GenerateTerrain(map);

        MapViewer.ApplyMapTexture();

        Grid grid = GetComponent<Grid>();
        grid.CreateGrid(new Vector2(width * 4, height * 4));
        
        EnemyGenerator enemyGen = GetComponent<EnemyGenerator>();
        enemyGen.GenerateEnemies();
    }

    void ProcessMap()
    {
        // clear any small wall islands
        if (clearIslands)
        {
            List<List<Coord>> wallRegions = GetRegions(TileType.Wall);

            foreach (List<Coord> wallRegion in wallRegions)
            {
                if (wallRegion.Count < wallThresholdSize)
                {
                    foreach (Coord tile in wallRegion)
                    {
                        map[tile.tileX, tile.tileY] = TileType.Floor;
                    }
                }
            }
        }

        // clear any small rooms
        List<List<Coord>> roomRegions = GetRegions(TileType.Floor);
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomThresholdSize && clearSmallRooms)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = TileType.Wall;
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }

        // connect surviving rooms with tunnels
        survivingRooms.Sort();
        survivingRooms[0].isMainRoom = true;
        survivingRooms[0].isAccessibleFromMainRoom = true;
        ConnectClosestRooms(survivingRooms);
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAssiblilityFromMainRoom = false, bool pathsAsCorridors = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAssiblilityFromMainRoom)
        {
            foreach(Room room in allRooms)
            {
                if (room.isAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if(!forceAssiblilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if(roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                {
                    continue;
                }

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound && !forceAssiblilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB, pathsAsCorridors);
            }
        }

        if (possibleConnectionFound && forceAssiblilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB, pathsAsCorridors);
            ConnectClosestRooms(allRooms, true, pathsAsCorridors);
        }

        if (!forceAssiblilityFromMainRoom)
        {
            ConnectClosestRooms(allRooms, true, pathsAsCorridors);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB, bool pathsAsCorridors = false)
    {
        Room.ConnectedRooms(roomA, roomB);
        if (DEBUGMODE)
        {
            Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);
        }

        if (pathsAsCorridors)
        {
            SecretCorridor corridor = getCorridor(tileA, tileB);
            createCorridor(corridor);

            corridor.associatedRooms.Add(roomA);
            corridor.associatedRooms.Add(roomB);
            secretCorridors.Add(corridor);

            if (!roomA.isMainRoom)
            {
                if(secretRooms.Contains(roomA))
                {
                    secretRooms.Remove(roomA);
                    //unsecretCorridorsConnectingWithRoom(roomA);
                }
                else
                {
                    secretRooms.Add(roomA);
                    corridor.tiles.Reverse();
                }
            }

            if (!roomB.isMainRoom)
            {
                if (secretRooms.Contains(roomB))
                {
                    secretRooms.Remove(roomB);
                    //unsecretCorridorsConnectingWithRoom(roomB);
                }
                else
                {
                    secretRooms.Add(roomB);
                }
            }
            //createSecretCorridor(corridor);
        }
        else
        {
            if (pseudoRandom.Next(0, 100) < percentageChanceOfCorridor)
            {
                createCorridor(tileA, tileB);
            } else {
                List<Coord> line = GetLine(tileA, tileB);
                foreach (Coord c in line)
                {
                    DrawCircle(c, tunnelsize);
                }
            }
        }

    }

    private void unsecretCorridorsConnectingWithRoom(Room room)
    {
        List<SecretCorridor> removeList = new List<SecretCorridor>();
        foreach (SecretCorridor corridor in secretCorridors)
        {
            if (corridor.associatedRooms.Contains(room))
            {
                removeList.Add(corridor);
            }
        }

        foreach (SecretCorridor corridor in removeList)
        {
            secretCorridors.Remove(corridor);
        }
    }

    void placeSecretDoors()
    {
        List<SecretCorridor> removeList = new List<SecretCorridor>();
        foreach (SecretCorridor corridor in secretCorridors)
        {
            bool keepSecret = false;
            foreach (Room r in corridor.associatedRooms)
            {
                if (secretRooms.Contains(r))
                {
                    keepSecret = true;
                }
            }
            if (!keepSecret)
            {
                removeList.Add(corridor);
            }
        }
        foreach (SecretCorridor corridor in removeList)
        {
            // remove linked secret room
            foreach (Room room in corridor.associatedRooms)
            {
                secretRooms.Remove(room);
            }
            
            secretCorridors.Remove(corridor);
        }


        removeList = new List<SecretCorridor>();
        removeList.Clear();

        foreach (SecretCorridor corridor in secretCorridors)
        {
            List<Coord> coordRemoveList = new List<Coord>();
            bool wallPlaced = false;

            for (int i = 0; i < corridor.tiles.Count && !wallPlaced; i++)
            {
                //int wallCount = 0;
                int tileX = corridor.tiles[i].tileX;
                int tileY = corridor.tiles[i].tileY;

                // place false wall where there are walls on both sides i.e. is corridor tile
                if (
                    (map[tileX + 1, tileY] == TileType.Wall && map[tileX - 1, tileY] == TileType.Wall) ||
                    (map[tileX, tileY + 1] == TileType.Wall && map[tileX, tileY - 1] == TileType.Wall)
                    )
                {
                    map[tileX, tileY] = TileType.SecretDoor;
                    wallPlaced = true;
                }
                else
                {
                    coordRemoveList.Add(corridor.tiles[i]);
                }
            }

            if (!wallPlaced)
            {
                removeList.Add(corridor);                
            }

            foreach (Coord c in coordRemoveList)
            {
                corridor.tiles.Remove(c);
            }
        }

        foreach (SecretCorridor corridor in removeList)
        {
            // remove linked secret room
            foreach (Room room in corridor.associatedRooms)
            {
                secretRooms.Remove(room);
            }
            secretCorridors.Remove(corridor);
        }
    }

    void ManageSecretRooms()
    {
        // remove rooms that are either too small or too big or have too many connections
        List<Room> removeList = new List<Room>();
        foreach (Room room in secretRooms)
        {
            if (room.roomSize < 10 || room.roomSize > 25 || room.connectedRooms.Count > 1)
            {
                removeList.Add(room);
            }
        }
        foreach (Room room in removeList)
        {
            secretRooms.Remove(room);
        }

        // reverse bad corridors
        foreach (SecretCorridor corridor in secretCorridors)
        {
            if (!isAdjacentToFloor(corridor.tiles[0]))
            {
                corridor.tiles.Reverse();
            }
        }

        // TODO: each floor should have a max number of secret rooms, cull the unnecessary ones


        // remove secret corridors that are no longer connected to secret rooms
        List<SecretCorridor> corridorRemoveList = new List<SecretCorridor>();
        foreach (SecretCorridor corridor in secretCorridors)
        {
            bool isConnectedRoSecretRoom = false;
            foreach (Room room in corridor.associatedRooms)
            {
                if (!secretRooms.Contains(room)) // TODO: make more efficient by adding isSecret variable to Room class
                {
                    isConnectedRoSecretRoom = true;
                }
            }
            if (!isConnectedRoSecretRoom)
            {
                corridorRemoveList.Add(corridor);
            }
        }
        foreach (SecretCorridor corridor in corridorRemoveList)
        {
            secretCorridors.Remove(corridor);
        }
    }

    void DrawCircle(Coord c, int r)
    {
        for(int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if(x*x + y*y <= r*r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if (IsInMapRange(drawX, drawY))
                    {
                        map[drawX, drawY] = TileType.Floor;
                    }
                }
            }
        }
    }

    //Bresenham's line algorithm
    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }
        return line;
    }

    // equivalent to Wu’s algorithm, but without the anti-aliasing
    List<Coord> GetLine2(Coord from, Coord to)
    {
        int lineStartX = from.tileX;
        int lineStartY = from.tileY;
        int lineEndX = to.tileX;
        int lineEndY = to.tileY;

        List<Coord> line = new List<Coord>();
        fillPixel(line, lineStartX, lineStartY, true);

        if (lineStartX == lineEndX && lineStartY == lineEndY)
            return line;

        fillPixel(line, lineEndX, lineEndY, true);

        if (Mathf.Abs(lineEndX - lineStartX) == Mathf.Abs(lineEndY - lineStartY)) // a diagonal slope tends to not cut far enough without this special case
            //straightTunnel(line, from, to);
        if (Mathf.Abs(lineEndX - lineStartX) >= Mathf.Abs(lineEndY - lineStartY))
            fillPixels(line, lineStartX, lineEndX, lineStartY, (double)(lineEndY - lineStartY) / (double)(lineEndX - lineStartX), true);
        else
            fillPixels(line, lineStartY, lineEndY, lineStartX, (double)(lineEndX - lineStartX) / (double)(lineEndY - lineStartY), false);

        return line;
    }

    void fillPixels(List<Coord> line, int start, int end, int startMinor, double slope, bool horizontal)
    {
        int advance = end > start ? 1 : -1;
        double curMinor = startMinor + 0.5 + (0.5 * advance * slope);
        for (int curMajor = start + advance; curMajor != end; curMajor += advance)
        {
            fillPixel(line, curMajor, (int)Math.Floor(curMinor), horizontal);

            double newMinor = curMinor + (advance * slope);
            if (Math.Floor(newMinor) != Math.Floor(curMinor))
                fillPixel(line, curMajor, (int)Math.Floor(newMinor), horizontal);
            curMinor = newMinor;
        }
    }

    void fillPixel(List<Coord> line, int major, int minor, bool horizontal)
    {
        if (horizontal) // X is major
            line.Add(new Coord(major, minor));
        else // Y is major
            line.Add(new Coord(minor, major));
    }

    // only use for drawing debug info on gizmo
    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width / 2 + .5f + tile.tileX, -2, -height / 2 + .5f + tile.tileY);
    }

    List<List<Coord>> GetRegions(List<TileType> tileTypes)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && tileTypes.Contains(map[x, y]))
                {
                    List<Coord> newRegion = GetRegionTiles(x, y, tileTypes);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    List<List<Coord>> GetRegions(TileType tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach(Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    List<List<Coord>> GetUpperRegions(List<TileType> tileTypes)
    {
        if (LevelManager.ExistsLevelForDepth(Player.depth - 1))
        {
            TileType[,] upperFloorMap = LevelManager.GetMapForDepth(Player.depth - 1);
            List<List<Coord>> regions = new List<List<Coord>>();

            int[,] mapFlags = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (mapFlags[x, y] == 0 && tileTypes.Contains(upperFloorMap[x, y]))
                    {
                        List<Coord> newRegion = GetRegionTiles(x, y, tileTypes);
                        regions.Add(newRegion);

                        foreach (Coord tile in newRegion)
                        {
                            mapFlags[tile.tileX, tile.tileY] = 1;
                        }
                    }
                }
            }

            return regions;
        }

        return null;
    }

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        TileType tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while(queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (x == tile.tileX || y == tile.tileY))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    List<Coord> GetRegionTiles(int startX, int startY, List<TileType> tileTypes)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (x == tile.tileX || y == tile.tileY))
                    {
                        if (mapFlags[x, y] == 0 && tileTypes.Contains(map[x, y]))
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1);
    }

    // this needs to occur after pits are made so that stairs down position is reliable floor also before some form of tunneling
    void MakeConnectionsWithUpperFloor()
    {
        if (LevelManager.ExistsLevelForDepth(Player.depth-1))
        {
            TileType[,] upperFloorMap = LevelManager.GetMapForDepth(Player.depth-1);
            for (int x = 0; x < upperFloorMap.GetLength(0); x++)
            {
                for (int y = 0; y < upperFloorMap.GetLength(1); y++)
                {
                    // create floor under pits and bridges
                    if (upperFloorMap[x, y] == TileType.Pit || upperFloorMap[x, y] == TileType.Bridge)
                    {
                        map[x - borderSize, y - borderSize] = TileType.Floor;
                    }
                    // erode a small room underneath the stairs down
                    else if (upperFloorMap[x, y] == TileType.StairsDown)
                    {
                        int xTile = x - borderSize;
                        int yTile = y - borderSize;
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (IsInMapRange(xTile + i, yTile + j))
                                    map[xTile + i, yTile + j] = TileType.Floor;
                            }
                        }
                        map[xTile, yTile] = TileType.StairsUp;
                        stairsUpPosition = new Coord(xTile, yTile);
                    }
                }
            }

            // find suitable area to plant tree
            List<List<Coord>> pitRegions = GetUpperRegions(new List<TileType> { TileType.Pit, TileType.Bridge });
            bool placedTree = false;
            foreach (List<Coord> roomRegion in pitRegions)
            {
                if (placedTree) break;

                foreach (Coord coord in roomRegion)
                {
                    if (placedTree) break;

                    if (hasRoomAround(coord.tileX, coord.tileY, 1, upperFloorMap))
                    {
                        print("map placed a tree");
                        map[coord.tileX, coord.tileY] = TileType.Tree;
                        placedTree = true;
                    }
                }
            }
        }
        else
        {
            Coord tile = GetRandomMapTile();
            map[tile.tileX, tile.tileY] = TileType.StairsUp;
            stairsUpPosition = new Coord(tile.tileX, tile.tileY);
        }
    }

    private bool hasRoomAround(int x, int y, int halfwidth, TileType[,] upperFloorMap)
    {
        for (int i = -halfwidth; i <= halfwidth; i++)
        {
            for (int j = -halfwidth; j <= halfwidth; j++)
            {
                if (upperFloorMap[x + i, y + j] != TileType.Pit && upperFloorMap[x + i, y + j] != TileType.Bridge)
                {
                    return false;
                }
            }
        }

        return true;
    }

    void ErodeFloorWithPits()
    {

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    pitmap[x, y] = TileType.Wall;
                }
                else
                {
                    if (pseudoRandom.Next(0, 100) < randomFillPercent-10)
                    {
                        pitmap[x, y] = TileType.Pit;
                    }
                    else
                    {
                        pitmap[x, y] = TileType.Wall;
                    }
                }
            }
        }

        // smooth
        int smoothIterations = 2;
        while (smoothIterations > 0)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighbourWallTiles = GetSurroundingPitWallCount(x, y);

                    if (neighbourWallTiles > 4)
                        pitmap[x, y] = TileType.Wall;
                    else if (neighbourWallTiles < 4)
                        pitmap[x, y] = TileType.Pit;
                }
            }

            smoothIterations--;
        }

        // merge with map
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (pitmap[x, y] == TileType.Pit)
                    map[x, y] = TileType.Pit;
            }
        }

        // remove pit sections that aren't adjacent to any floor tiles
        // clear any small rooms
        List<List<Coord>> pitRegions = GetRegions(TileType.Pit);
        List<Room> pitRooms = new List<Room>();

        foreach (List<Coord> roomRegion in pitRegions)
        {
            bool hasAccessibleTile = false;
            Room newRoom = new Room(roomRegion, map);
            for (int i = 0; i < newRoom.edgeTiles.Count && hasAccessibleTile != true; i++)
            {
                if (isAdjacentToFloor(newRoom.edgeTiles[i]))
                {
                    hasAccessibleTile = true;
                }
            }

            if (!hasAccessibleTile)
            {
                // turn all tiles to 0
                foreach (Coord tile in newRoom.tiles)
                {
                    map[tile.tileX, tile.tileY] = TileType.Wall;
                }
            }
            else
            {
                pitRooms.Add(newRoom);
            }
        }

        MakeConnectionsWithUpperFloor();

        // reconnect divided rooms with bridges
        List<List<Coord>> roomRegions = GetRegions(TileType.Floor);
        List<Room> rooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            rooms.Add(new Room(roomRegion, map));
        }
        
        rooms.Sort();
        rooms[0].isMainRoom = true;
        rooms[0].isAccessibleFromMainRoom = true;
        ConnectClosestRoomsWithBridges(rooms);
    }

    void ConnectClosestRoomsWithBridges(List<Room> allRooms, bool forceAssiblilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAssiblilityFromMainRoom)
        {
            foreach (Room room in allRooms)
            {
                if (room.isAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAssiblilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                {
                    continue;
                }

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound && !forceAssiblilityFromMainRoom)
            {
                CreateBridge(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if (possibleConnectionFound && forceAssiblilityFromMainRoom)
        {
            CreateBridge(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRoomsWithBridges(allRooms, true);
        }

        if (!forceAssiblilityFromMainRoom)
        {
            ConnectClosestRoomsWithBridges(allRooms, true);
        }
    }

    void CreateBridge(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectedRooms(roomA, roomB);
        if (DEBUGMODE)
        {
            Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.red, 100);
        }

        //createCorridor(tileA, tileB);
        CreateBridge(tileA, tileB);
    }

    private bool isAdjacentToFloor(Coord coord)
    {
        if (map[coord.tileX + 1, coord.tileY] == TileType.Floor)
        {
            return true;
        }
        else if (map[coord.tileX - 1, coord.tileY] == TileType.Floor)
        {
            return true;
        }
        else if (map[coord.tileX, coord.tileY + 1] == TileType.Floor)
        {
            return true;
        }
        else if (map[coord.tileX, coord.tileY - 1] == TileType.Floor)
        {
            return true;
        }

        return false;
    }

    void RandomFillMap()
    {

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = TileType.Wall;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? TileType.Wall : TileType.Floor;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 4)
                    map[x, y] = TileType.Wall;
                else if (neighbourWallTiles < 4)
                    map[x, y] = TileType.Floor;

            }
        }
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMapRange(neighbourX, neighbourY))
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        if (map[neighbourX, neighbourY] == TileType.Wall)
                        wallCount ++;
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    int GetSurroundingPitWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (IsInMapRange(neighbourX, neighbourY))
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        if (pitmap[neighbourX, neighbourY] == TileType.Wall)
                        {
                            wallCount += 1;
                        }
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    void PlaceStaircaseDownToNextFloor()
    {
        Coord tile = GetRandomMapTileAwayFrom(stairsUpPosition);
        map[tile.tileX, tile.tileY] = TileType.StairsDown;
    }

    Coord GetRandomMapTile()
    {
        int x, y;
        bool suitablePositionFound = false;

        do
        {
            x = pseudoRandom.Next(0, map.GetLength(0));
            y = pseudoRandom.Next(0, map.GetLength(1));

            if (map[x, y] == TileType.Floor)
                suitablePositionFound = true;
        } while (!suitablePositionFound);

        return new Coord(x, y);
    }

    Coord GetRandomMapTileAwayFrom(Coord avoidPosition)
    {
        int x, y;
        bool suitablePositionFound = false;

        do
        {
            x = pseudoRandom.Next(0, map.GetLength(0));
            y = pseudoRandom.Next(0, map.GetLength(1));

            if (map[x, y] == TileType.Floor && Vector2.Distance(new Vector2(x, y), new Vector2(avoidPosition.tileX, avoidPosition.tileY)) > 10)
                suitablePositionFound = true;
        } while (!suitablePositionFound);

        return new Coord(x, y);
    }

    void GenerateBuildings()
    {
        int b = 0;
        while (b < buildingCount) {
            int width = pseudoRandom.Next(minRoomSize, maxRoomSize);
            int height = pseudoRandom.Next(minRoomSize, maxRoomSize);

            Building currentBuilding = new Building(
                    new Coord(
                        pseudoRandom.Next(1, map.GetLength(0) - width - 1),
                        pseudoRandom.Next(1, map.GetLength(1) - height - 1)
                    ),
                    width,
                    height
                );

            ExcavateBuilding(currentBuilding);

            b++;
        }
    }

    private void ExcavateBuilding(Building building)
    {
        for (int x = building.position.tileX; x < building.position.tileX + building.width; x++)
        {
            for (int y = building.position.tileY; y < building.position.tileY + building.height; y++)
            {
                if (map[x, y] != TileType.Pit && map[x, y] != TileType.Bridge && map[x, y] != TileType.StairsDown && map[x, y] != TileType.StairsUp) // dont pave over important sections
                    map[x, y] = TileType.Floor;
            }
        }
    }

    SecretCorridor getCorridor(Coord start, Coord end)
    {
        int x = start.tileX;
        int y = start.tileY;

        SecretCorridor corridor = new SecretCorridor();

        if (x < end.tileX)
        {
            for (; x < end.tileX; x++)
            {
                corridor.tiles.Add(new Coord(x, y));
            }
        }
        else
        {
            for (; x > end.tileX; x--)
            {
                corridor.tiles.Add(new Coord(x, y));
            }
        }
        if (y < end.tileY)
        {
            for (; y < end.tileY; y++)
            {
                corridor.tiles.Add(new Coord(x, y));
            }
        }
        else
        {
            for (; y > end.tileY; y--)
            {
                corridor.tiles.Add(new Coord(x, y));
            }
        }

        return corridor;
    }

    void CreateBridge(Coord start, Coord end)
    {
        // count number of pit tiles along both paths
        int count1 = 0;
        int x = start.tileX;
        int y = start.tileY;
        if (x < end.tileX)
        {
            for (; x < end.tileX; x++)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count1++;
                }
            }
        }
        else
        {
            for (; x > end.tileX; x--)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count1++;
                }
            }
        }
        if (y < end.tileY)
        {
            for (; y < end.tileY; y++)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count1++;
                }
            }
        }
        else
        {
            for (; y > end.tileY; y--)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count1++;
                }
            }
        }
        int count2 = 0;
        x = start.tileX;
        y = start.tileY;
        if (y < end.tileY)
        {
            for (; y < end.tileY; y++)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count2++;
                }
            }
        }
        else
        {
            for (; y > end.tileY; y--)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count2++;
                }
            }
        }
        if (x < end.tileX)
        {
            for (; x < end.tileX; x++)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count2++;
                }
            }
        }
        else
        {
            for (; x > end.tileX; x--)
            {
                if (map[x, y] == TileType.Pit)
                {
                    count2++;
                }
            }
        }


        // create whichever path which goes over the most pit tiles
        x = start.tileX;
        y = start.tileY;
        if (count1 >= count2)
        {
            if (x < end.tileX)
            {
                for (; x < end.tileX; x++)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
            else
            {
                for (; x > end.tileX; x--)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
            if (y < end.tileY)
            {
                for (; y < end.tileY; y++)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
            else
            {
                for (; y > end.tileY; y--)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
        }
        else
        {
            if (y < end.tileY)
            {
                for (; y < end.tileY; y++)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
            else
            {
                for (; y > end.tileY; y--)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
            if (x < end.tileX)
            {
                for (; x < end.tileX; x++)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
            else
            {
                for (; x > end.tileX; x--)
                {
                    if (map[x, y] == TileType.Pit)
                    {
                        map[x, y] = TileType.Bridge;
                    }
                    else
                    {
                        map[x, y] = TileType.Floor;
                    }
                }
            }
        }
        
    }

    void createCorridor(Coord start, Coord end)
    {
        int x = start.tileX;
        int y = start.tileY;

        if (x < end.tileX)
        {
            for (; x < end.tileX; x++)
            {
                map[x, y] = TileType.Floor;
            }
        }
        else
        {
            for (; x > end.tileX; x--)
            {
                map[x, y] = TileType.Floor;
            }
        }
        if (y < end.tileY)
        {
            for (; y < end.tileY; y++)
            {
                map[x, y] = TileType.Floor;
            }
        }
        else
        {
            for (; y > end.tileY; y--)
            {
                map[x, y] = TileType.Floor;
            }
        }
    }

    void createCorridor(SecretCorridor corridor)
    {
        foreach (Coord tile in corridor.tiles)
        {
            if (map[tile.tileX, tile.tileY] == TileType.Pit)
            {
                map[tile.tileX, tile.tileY] = TileType.Bridge;
            }
            else
            {
                map[tile.tileX, tile.tileY] = TileType.Floor;
            }
            
        }
    }

    void createSecretCorridor(SecretCorridor corridor)
    {
        map[corridor.tiles[0].tileX, corridor.tiles[0].tileY] = TileType.SecretDoor;
        for (int i = 1; i < corridor.tiles.Count; i++)
        {
            map[corridor.tiles[i].tileX, corridor.tiles[i].tileY] = TileType.SecretTunnel;
        }
    }

    void ConnectBuildingsToRestOfMap()
    {

        //List<List<Coord>> roomRegions = GetRegions(0);
        List<List<Coord>> roomRegions = GetRegions(new List<TileType> { TileType.Floor, TileType.Bridge });
        List<Room> rooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
        {
            rooms.Add(new Room(roomRegion, map));
        }

        // connect surviving rooms with tunnels
        rooms.Sort();
        rooms[0].isMainRoom = true;
        rooms[0].isAccessibleFromMainRoom = true;
        ConnectClosestRooms(rooms, false, true);
    }

    public struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    struct Building
    {
        public Coord position;
        public int width;
        public int height;

        public Building(Coord _position, int _width, int _height)
        {
            position = _position;
            width = _width;
            height = _height;
        }
    }

    public class SecretCorridor
    {
        public List<Coord> tiles;
        public bool placedFalseWall;
        public List<Room> associatedRooms;

        public SecretCorridor()
        {
            tiles = new List<Coord>();
            associatedRooms = new List<Room>();
            placedFalseWall = false;
        }
    }

    public class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;

        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room()
        {
        }

        public Room(List<Coord> roomTiles, TileType[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                for (int x = tile.tileX-1; x <= tile.tileX+1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (getMapTileValue(x, y) == TileType.Wall)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        TileType getMapTileValue(int x, int y)
        {
            if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1))
                return map[x, y];
            else
                return TileType.Wall;
        }

        public void SetAccessibleFromMainRoom()
        {
            if (!isAccessibleFromMainRoom)
            {
                isAccessibleFromMainRoom = true;
                foreach(Room connectedRoom in connectedRooms)
                {
                    connectedRoom.SetAccessibleFromMainRoom();
                }
            }
        }

        public static void ConnectedRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessibleFromMainRoom)
            {
                roomB.SetAccessibleFromMainRoom();
            }
            if (roomB.isAccessibleFromMainRoom)
            {
                roomA.SetAccessibleFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }

    void OnDrawGizmos()
    {
        if (!DEBUGMODE)
        {
            return;
        }

        if (map != null)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    Gizmos.color = (map[x, y] == TileType.Wall) ? Color.black : Color.white;
                    if (map[x, y] == TileType.SecretTunnel)
                        Gizmos.color = Color.green;
                    if (map[x, y] == TileType.SecretDoor)
                        Gizmos.color = Color.red;
                    if (map[x, y] == TileType.SecretTile)
                        Gizmos.color = Color.cyan;
                    if (map[x, y] == TileType.Pit)
                        Gizmos.color = Color.yellow;
                    if (map[x, y] == TileType.Bridge)
                        Gizmos.color = Color.magenta;
                    Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
        }

        // draw pitmap
        //if (pitmap != null)
        //{
        //    for (int x = 0; x < pitmap.GetLength(0); x++)
        //    {
        //        for (int y = 0; y < pitmap.GetLength(1); y++)
        //        {
        //            Gizmos.color = (pitmap[x, y] == 1) ? Color.clear : Color.white;
        //            if (pitmap[x, y] == 8)
        //                Gizmos.color = Color.red;
        //            Vector3 pos = new Vector3(-width / 2 + x + .5f, 2, -height / 2 + y + .5f);
        //            Gizmos.DrawCube(pos, Vector3.one);
        //        }
        //    }
        //}
    }
}