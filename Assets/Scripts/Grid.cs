using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid : MonoBehaviour {


    public bool displayGridGizmos = false;
    public LayerMask walkableMask;
    public LayerMask unwalkableMask;
    public float nodeRadius;

    static Vector2 gridWorldSize;
    static Node[,] grid;

    float nodeDiameter;
    static int gridSizeX, gridSizeY;

    static bool gridEstablished = false;

    static List<Node> walkablePositions;

    void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        walkablePositions = new List<Node>();
    }

    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }

    public static bool isEstablished
    {
        get
        {
            return gridEstablished;
        }
    }

    public void CreateGrid(Vector2 _gridWorldSize)
    {
        gridWorldSize = _gridWorldSize;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        grid = new Node[gridSizeX, gridSizeY];
        
        Vector3 worldBottomLeft = transform.position;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = (Physics.CheckSphere(worldPoint, nodeRadius, walkableMask)) && !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                Node newNode = new Node(walkable, worldPoint, x, y);
                if (walkable)
                    walkablePositions.Add(newNode);
                grid[x, y] = newNode;
            }
        }

        gridEstablished = true;
    }

    public static Node NodeFromWorldPosition(Vector3 worldPosition)
    {
        float percentX = worldPosition.x / gridWorldSize.x;
        float percentY = worldPosition.z / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
            return grid[x, y];
        else
            return null;
    }

    public List<Node> GetNeightbours(Node node)
    {
        List<Node> neightbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neightbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neightbours;
    }

    public bool GridPositionIsWalkable(int x, int y)
    {
        return grid[x, y].walkable;
    }

    public static Vector3 GetRandomGridPosition()
    {
        return walkablePositions[Random.Range(0, walkablePositions.Count)].worldPosition;
    }

    public static bool isWalkable(Vector3 position)
    {
        Node node = NodeFromWorldPosition(position);
        if (node != null)
            return node.walkable;

        return false;
    }

    public static Vector3 GetRandomGridPositionAwayFrom(Vector3 avoidPosition)
    {
        bool suitablePositionFound = false;
        Node node;
        do
        {
            node = walkablePositions[Random.Range(0, walkablePositions.Count)];
            if (Vector3.Distance(node.worldPosition, avoidPosition) > 20)
                suitablePositionFound = true;

        } while (!suitablePositionFound);

        return node.worldPosition;
    }

    

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + Vector3.right * gridWorldSize.x / 2 + Vector3.forward * gridWorldSize.y / 2, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        if (grid != null && displayGridGizmos)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
            }
        }
        
    }
}
