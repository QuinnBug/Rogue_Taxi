using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;

public class Terrain_Manager : Singleton<Terrain_Manager>
{
    public GameObject tilePrefab;
    public TileSettings tileSettings;
    [Space]
    public Vector3 worldStart;
    [Space]
    [SerializeField]
    private Vector2 perlinOffset;
    private List<TerrainTile> tiles = new List<TerrainTile>();
    public Vector2Int m_tileCounts;
    [Space]
    public int d_tilesPerLoop = 50;
    public float d_timePerLoop = 0.0001f;

    public void Start()
    {
        Event_Manager.Instance.AddListener(E_Event.Nodes, E_Action.Finished, CreateTerrain);
    }

    public void CreateTerrain() 
    {
        Event_Manager.Instance.InvokeEvent(E_Event.Terrain, E_Action.Start);

        CalculateSizes();

        StartCoroutine(TerrainCoroutine());
    }

    private void CalculateSizes()
    {
        var nodeCoords = LNode_Manager.Instance.m_nodeMap.Keys;
        int lowerX = int.MaxValue;
        int lowerY = int.MaxValue;
        int upperX = int.MinValue;
        int upperY = int.MinValue;

        foreach (var node in nodeCoords) 
        {
            lowerX = Mathf.Min(lowerX, node.x);
            lowerY = Mathf.Min(lowerX, node.y);

            upperX = Mathf.Max(upperX, node.x);
            upperY = Mathf.Max(upperY, node.y);
        }

        lowerX -= 2;
        lowerY -= 2;
        upperX += 1;
        upperY += 1;

        Vector3 bottomLeft = LNode_Manager.Instance.MapKeyToWorldPos(new Vector2Int(lowerX, lowerY));
        Vector3 topRight = LNode_Manager.Instance.MapKeyToWorldPos(new Vector2Int(upperX, upperY));
        Vector3 difference = topRight - bottomLeft;

        Debug.Log(lowerX + " : " + lowerY + " && " + upperX + "  : " + upperY);
        Debug.Log(bottomLeft + " >> " + topRight + " >> " + difference);

        worldStart = bottomLeft;
        m_tileCounts = new Vector2Int (
            (int)(difference.x / LNode_Manager.Instance.m_nodeLimitRange.max),
            (int)(difference.z / LNode_Manager.Instance.m_nodeLimitRange.max)
        );
    }

    void GenerateTile(Vector2Int _position) 
    {
        Vector2 posXSize = _position * tileSettings.size;
        Vector3 worldPos = new Vector3(posXSize.x * tileSettings.vSize.x, 0, posXSize.y * tileSettings.vSize.x) + worldStart;
        GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);

        TerrainTile tile = tileObj.GetComponent<TerrainTile>();
        tile.settings = tileSettings;
        tile.tileCoords = _position;
        tile.perlinOffset = perlinOffset;
        tile.GenerateMesh();

        tiles.Add(tile);
    }

    private IEnumerator TerrainCoroutine()
    {
        perlinOffset = Random.insideUnitCircle * Random.Range(0.0f, 99999.0f);
        int totalTiles = 0;
        for (int x = 0; x < m_tileCounts.x; x++)
        {
            for (int y = 0; y < m_tileCounts.y; y++)
            {
                GenerateTile(new Vector2Int(x,y));

                if (++totalTiles % d_tilesPerLoop == 0) 
                {
                    yield return new WaitForSeconds(d_timePerLoop);
                }
            }
        }

        Event_Manager.Instance.InvokeEvent(E_Event.Terrain, E_Action.Finished);
    }

    public float GetHeightAtPoint(Vector3 point) 
    {
       float y = (Mathf.PerlinNoise(
                    (perlinOffset.x + point.x) * tileSettings.perlinZoom,
                    (perlinOffset.y + point.z) * tileSettings.perlinZoom) - 0.5f)
                    * tileSettings.perlinHeight;

        if (y > tileSettings.perlinHeightLimit) y = tileSettings.perlinHeightLimit;
        else if (y < -tileSettings.perlinHeightLimit) y = -tileSettings.perlinHeightLimit;

        return y;
    }
}
