using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terrain_Manager : Singleton<Terrain_Manager>
{
    public GameObject tilePrefab;
    public TileSettings tileSettings;
    [Space]
    public Vector3 worldStart;
    [Space]
    [SerializeField]
    private Vector2 randomOffset;
    private List<TerrainTile> tiles = new List<TerrainTile>();
    public Vector2 terrainSize;
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
        StartCoroutine(TerrainCoroutine());
    }

    void GenerateTile(Vector2Int _position) 
    {
        Vector2 posXSize = _position * tileSettings.size;
        Vector3 worldPos = new Vector3(posXSize.x * tileSettings.vSize.x, 0, posXSize.y * tileSettings.vSize.x) + worldStart;
        GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);

        TerrainTile tile = tileObj.GetComponent<TerrainTile>();
        tile.settings = tileSettings;
        tile.tilePosition = _position;
        tile.perlinOffset = randomOffset;
        tile.GenerateMesh();

        tiles.Add(tile);
    }

    private IEnumerator TerrainCoroutine()
    {
        randomOffset = Random.insideUnitCircle * Random.Range(0.0f, 99999.0f);
        int totalTiles = 0;
        for (int x = 0; x < terrainSize.x; x++)
        {
            for (int y = 0; y < terrainSize.y; y++)
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

    
}
