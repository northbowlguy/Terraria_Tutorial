using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    [Header("Lighting")]
    public Texture2D worldTilesMap;
    public Material lightShader;
    public float groundLightThreshold = 0.7f;
    public float airLightThreshold = 0.85f;
    public float lightRadius = 7f;
    List<Vector2Int> unlitBlocks = new List<Vector2Int>();


    public PlayerController player;
    public CamControl camera;
    public GameObject tileDrop;

    [Header("Tile Atlas")]
    public TileAtlas tileAtlas;
    public float seed;

    public BiomeClass [] biomes;
    //public BiomeClass ForestBiome;
    //public BiomeClass SnowBiome;
    //public BiomeClass DesertBiome;

    

    [Header("Biomes")]
    public float biomeFrequency;
    public Gradient biomeGradient;
    public Texture2D biomeMap;

    //[Header("Trees")]
    //public int treeChance = 10;
    //public int minTreeHeight = 4;
    //public int maxTreeHeight = 6;

    //[Header("Addons")]
    //public int tallGrassChance = 10;

    [Header("Generation Settings")]
    public int chunkSize = 16;
    public int worldSize = 100;
    public bool generateCaves = true;
    //public int dirtLayerHeight = 5;
    public float surfaceValue = 0.25f;
    //public float heightMultiplier = 4f;
    public int heightAddition = 25;

    [Header("Noise Settings")]
    //public float caveFreq = 0.05f;
    //public float terrainFreq = 0.05f;
    public float caveFreq = 0.05f;
    public float terrainFreq = 0.05f;
    public Texture2D caveNoiseTexture;

    [Header("Ore Settings")]
    public OreClass[] ores;

    public GameObject[] worldChunks;

    //public List<Vector2> worldTiles = new List<Vector2>();
    //private List<GameObject> worldTileObjects = new List<GameObject>();
    //private List<TileClass> worldTileClasses = new List<TileClass>();

    private GameObject[,] world_ForegroundObjects;
    private GameObject[,] world_BackgroundObjects;
    private TileClass[,] world_BackgroundTiles;
    private TileClass[,] world_ForegroundTiles;

    private BiomeClass curBiome;
    public Color[] biomeCols;

    //private void OnValidate()
    //{
    //    //biomeColors = new Color[biomes.Length];
    //    //for (int i = 0; i < biomeColors.Length; i++)
    //    //{
    //    //    biomeColors[i] = biomes[i].biomeColor;
    //    //}
    //    DrawTextures();
    //    DrawCavesAndOres();
    //}

    private void Start()
    {
        world_ForegroundTiles = new TileClass[worldSize, worldSize];
        world_BackgroundTiles = new TileClass[worldSize, worldSize];
        world_ForegroundObjects = new GameObject[worldSize, worldSize];
        world_BackgroundObjects = new GameObject[worldSize, worldSize];

        //initilise light
        worldTilesMap = new Texture2D(worldSize, worldSize);
        
        ////COMMENTING THIS OUT: changes it from pixel filter to smooth filter
        //worldTilesMap.filterMode = FilterMode.Point;
        
        lightShader.SetTexture("_ShadowTex", worldTilesMap);

        for(int x = 0; x < worldSize; x++)
        {
            for(int y = 0; y < worldSize; y++)
            {
                worldTilesMap.SetPixel(x, y, Color.white);
            }
        }
        worldTilesMap.Apply();

        //generate terrain

        seed = Random.Range(-10000, 100000);

        for (int i = 0; i < ores.Length; i++)
        {
            ores[i].spreadTexture = new Texture2D(worldSize, worldSize);
        }

        biomeCols = new Color[biomes.Length];
        for (int i = 0; i < ores.Length; i++)
        {
            biomeCols[i]= biomes[i].biomeCol;
        }

        //DrawTextures();
        DrawBiomeMap();
        DrawCavesAndOres();

        //GenerateNoiseTexture(ores[3].rarity, ores[3].size, ores[3].spreadTexture);

        CreateChunks();
        GenerateTerrain();

        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                if(worldTilesMap.GetPixel(x, y) == Color.white)
                {
                    LightBlock(x, y, 1f, 0);
                }
            }
        }
        worldTilesMap.Apply();

        camera.Spawn(new Vector3(player.spawnPos.x, player.spawnPos.y, camera.transform.position.z));
        camera.worldSize = worldSize;
        player.Spawn();

        RefreshChunks();
    }

    public void Update()
    {
        RefreshChunks();
    }

    void RefreshChunks()
    {
        for (int i = 0;i < worldChunks.Length; i++)
        {
            if (Vector2.Distance(new Vector2((i * chunkSize) + (chunkSize / 2), 0), new Vector2(player.transform.position.x, 0)) > Camera.main.orthographicSize * 5f)
            {
                worldChunks[i].SetActive(false);
            }
            else
            {
                worldChunks[i].SetActive(true);
            }
        }
    }

    public void DrawBiomeMap()
    {
        float b;
        Color col;
        biomeMap = new Texture2D(worldSize, worldSize);
        for (int x = 0; x < biomeMap.width; x++)
        {
            for (int y = 0; y < biomeMap.height; y++)
            {
                b = Mathf.PerlinNoise((x + seed) * biomeFrequency, (y + seed) * biomeFrequency);
                col = biomeGradient.Evaluate(b);
                biomeMap.SetPixel(x, y, col);

            }
        }
        biomeMap.Apply();
    }
    public void DrawCavesAndOres()
    {
        caveNoiseTexture = new Texture2D(worldSize, worldSize);
        float v;
        float o;

        for (int x = 0; x < worldSize; x++)
        {
            for (int y = 0; y < worldSize; y++)
            {
                curBiome = GetCurrentBiome(x, y);
                v = Mathf.PerlinNoise((x + seed) * caveFreq, (y + seed) * caveFreq);
                if (v > curBiome.surfaceValue)
                {
                    caveNoiseTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    caveNoiseTexture.SetPixel(x, y, Color.black);
                }
                
                for (int i = 0; i < curBiome.ores.Length; i++)
                {
                    ores[i].spreadTexture.SetPixel(x, y, Color.black);
                    if (curBiome.ores.Length > i)
                    {
                        o = Mathf.PerlinNoise((x + seed) * curBiome.ores[i].rarity, (y + seed) * curBiome.ores[i].rarity);
                        if (o > curBiome.ores[i].size)
                        {
                            ores[i].spreadTexture.SetPixel(x, y, Color.white);
                        }
                        ores[i].spreadTexture.Apply();
                    }
                }
            }
        }

        caveNoiseTexture.Apply();


        //for (int x = 0; x < worldSize; x++)
        //{
        //    for (int y = 0; y < worldSize; y++)
        //    {
        //        curBiome = GetCurrentBiome(x, y);
        //        for (int i = 0; i < curBiome.ores.Length; i++)
        //        {
        //            ores[i].spreadTexture.SetPixel(x, y, Color.black);
        //            if (curBiome.ores.Length > i)
        //            {
        //                float v = Mathf.PerlinNoise((x + seed) * curBiome.ores[i].rarity, (y + seed) * curBiome.ores[i].rarity);
        //                if (v > curBiome.ores[i].size)
        //                {
        //                    ores[i].spreadTexture.SetPixel(x, y, Color.white);
        //                }
        //                ores[i].spreadTexture.Apply();
        //            }
        //        }
        //    }
        //}
    }

    public void DrawTextures()
    {
        //biomeMap = new Texture2D(worldSize, worldSize);
        //DrawBiomeTexture();

        for (int i = 0; i < biomes.Length; i++)
        {
            biomes[i].caveNoiseTexture = new Texture2D(worldSize, worldSize);
            for (int o = 0; o < biomes[i].ores.Length; o++)
            {
                biomes[i].ores[o].spreadTexture = new Texture2D(worldSize, worldSize);
                //biomes[i].ores[1].spreadTexture = new Texture2D(worldSize, worldSize);
                //biomes[i].ores[2].spreadTexture = new Texture2D(worldSize, worldSize);
                //biomes[i].ores[3].spreadTexture = new Texture2D(worldSize, worldSize);
                GenerateNoiseTexture(biomes[i].ores[o].rarity, biomes[i].ores[o].size, biomes[i].ores[o].spreadTexture);
            }
        }
            
            //GenerateNoiseTexture(biomes[i].caveFreq, biomes[i].surfaceValue, biomes[i].caveNoiseTexture);
            
            //Ores
            //for (int o = 0; o < biomes[i].ores.Length; o++)
            //{
            //    //GenerateNoiseTexture(biomes[i].ores[o].rarity, biomes[i].ores[o].size, biomes[i].ores[o].spreadTexture);
            //}
            //GenerateNoiseTexture(ores[0].rarity, ores[0].size, ores[0].spreadTexture);
            //GenerateNoiseTexture(ores[1].rarity, ores[1].size, ores[1].spreadTexture);
            //GenerateNoiseTexture(ores[2].rarity, ores[2].size, ores[2].spreadTexture);
            //GenerateNoiseTexture(ores[3].rarity, ores[3].size, ores[3].spreadTexture);


    }

    //public void DrawBiomeTexture()
    //{
    //    for (int x = 0; x < biomeMap.width; x++)
    //    {
    //        for (int y = 0; y < biomeMap.height; y++)
    //        {
    //            float v = Mathf.PerlinNoise((x + seed) * biomeFrequency, (y + seed) * biomeFrequency);
    //            Color col = biomeGradient.Evaluate(v);
    //            biomeMap.SetPixel(x, y, col);
    //        }
    //    }
    //    biomeMap.Apply();
    //}


    public void GenerateNoiseTexture(float frequency, float limit, Texture2D noiseTexture)
    {
        //NOTED OUT FOR NOW -- CODE: Texture2D noise = new Texture2D(worldSize, worldSize);
        float v;
        float b;
        Color col;

        for (int x = 0; x < noiseTexture.width; x++)
        {
            for (int y = 0; y < noiseTexture.height; y++)
            {
                v = Mathf.PerlinNoise((x + seed) * frequency, (y + seed) * frequency);

                if (v > limit)
                {
                    noiseTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    noiseTexture.SetPixel(x, y, Color.black);
                }

            }
        }
        noiseTexture.Apply();
        
    }


    public void CreateChunks()
    {
        int numChunks = worldSize / chunkSize;
        worldChunks = new GameObject[numChunks];

        for (int i = 0; i < numChunks; i++)
        {
            GameObject newChunk = new GameObject();
            newChunk.name = i.ToString();
            newChunk.transform.parent = this.transform;
            worldChunks[i] = newChunk;
        }
    }

    public BiomeClass GetCurrentBiome(int x, int y)
    {
        //change curbiome value here;

        ////search through biomes
        //for (int i = 0; i < biomes.Length; i++)
        //{
        //    if (biomes[i].biomeCol == biomeMap.GetPixel(x, y))
        //    {
        //        return biomes[i];
        //    }
        //}

        if (System.Array.IndexOf(biomeCols, biomeMap.GetPixel(x, y)) >= 0)
        {
            return biomes[System.Array.IndexOf(biomeCols, biomeMap.GetPixel(x, y))];
        }

        return curBiome;
    }

    public void GenerateTerrain()
    {
        TileClass tileClass;
        for (int x = 0; x < worldSize - 1; x++)
        {
            float height;

            for (int y = 0; y < worldSize; y++)
            {
                curBiome = GetCurrentBiome(x, y);
                height = Mathf.PerlinNoise((x + seed) * terrainFreq, seed * terrainFreq) * curBiome.heightMultiplier + heightAddition;
                if (x == worldSize / 2)
                {
                    player.spawnPos = new Vector2(x, height + 2);
                }

                if (y >= height)
                { 
                    break; 
                }
                
                if (y < height - curBiome.dirtLayerHeight)
                {
                    tileClass = curBiome.tileAtlas.stone;

                    if (ores[0].spreadTexture.GetPixel(x,y).r > 0.5f && height - y > ores[0].maxSpawnHeight)
                    {
                        tileClass = tileAtlas.coal;
                    }
                    if (ores[1].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[1].maxSpawnHeight)
                    {
                        tileClass = tileAtlas.iron;
                    }
                    if (ores[2].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[2].maxSpawnHeight)
                    {
                        tileClass = tileAtlas.gold;
                    }
                    if (ores[3].spreadTexture.GetPixel(x, y).r > 0.5f && height - y > ores[3].maxSpawnHeight)
                    {
                        tileClass = tileAtlas.diamond;
                    }
                } 
                else if (y < height - 1)
                {
                    tileClass = curBiome.tileAtlas.dirt;
                }
                else
                {
                    //top layer of terrain
                    tileClass = tileAtlas.grass;
                    
                }
                if (generateCaves)
                {
                    if (caveNoiseTexture.GetPixel(x, y).r > 0.5f)
                    {
                        PlaceTile(tileClass, x, y, true);
                    }
                    else if (tileClass.wallVariant != null)
                    {
                        PlaceTile(tileClass.wallVariant, x, y, true);
                    }
                }
                else
                {
                    PlaceTile(tileClass, x, y, true);
                }
                if (y >= height - 1)
                {
                    int t = Random.Range(0, curBiome.treeChance);

                    if (t == 1)
                    {
                        //generate a tree
                        if (GetTileFromWorld(x,y))
                        {
                            if (curBiome.biomeName == "Desert")
                            {
                                GenerateCactus(curBiome.tileAtlas, Random.Range(curBiome.minTreeHeight, curBiome.maxTreeHeight), x, y + 1);
                            }
                            else
                            {
                                GenerateTree(Random.Range(curBiome.minTreeHeight, curBiome.maxTreeHeight), x, y + 1);
                            }
                        }
                    }
                    else
                    {
                        int i = Random.Range(0, curBiome.tallGrassChance);
                        if (i == 1)
                        {
                            //generate grass
                            if (GetTileFromWorld(x, y))
                            {
                                if (curBiome.tileAtlas.tallGrass != null)
                                {
                                    PlaceTile(curBiome.tileAtlas.tallGrass, x, y + 1, true);
                                }
                                
                            }
                        }

                    }
                }
            }
        }
        worldTilesMap.Apply();
    }





    void GenerateCactus(TileAtlas atlas, int treeHeight, int x, int y)
    {
        //define our cactus

        //generate cactus
        for (int i = 0; i < treeHeight; i++)
        {
            PlaceTile(atlas.log, x, y + i, true);
        }
    }



        void GenerateTree(int treeHeight, int x, int y)
    {
        //define our tree

        //generate log
        for (int i = 0; i < treeHeight; i++)
        {
            PlaceTile(tileAtlas.log, x, y + i, true);
        }

        //generate leaves
        PlaceTile(tileAtlas.leaf, x, y + treeHeight, true);
        PlaceTile(tileAtlas.leaf, x, y + treeHeight + 1, true);
        PlaceTile(tileAtlas.leaf, x, y + treeHeight + 2, true);

        PlaceTile(tileAtlas.leaf, x - 1, y + treeHeight, true);
        PlaceTile(tileAtlas.leaf, x - 1, y + treeHeight + 1, true);

        PlaceTile(tileAtlas.leaf, x + 1, y + treeHeight, true);
        PlaceTile(tileAtlas.leaf, x + 1, y + treeHeight + 1, true);
    }

    public void RemoveTile (int x, int y)
    {
        //TileClass tile = GetTileFromWorld(x, y);

        if (GetTileFromWorld(x, y) && x >= 0 && x <= worldSize && y >= 0 && y <= worldSize)
        {
            TileClass tile = GetTileFromWorld(x, y);
            RemoveTileFromWorld(x, y);

            if (tile.wallVariant != null)
            {
                if(tile.naturallyPlaced)
                {
                    PlaceTile(tile.wallVariant, x, y, true);
                }

            }

            //tileDrop tile
            if (tile.tileDrop)
            {
                GameObject newtileDrop = Instantiate(tileDrop, new Vector2(x, y + 0.5f), Quaternion.identity);
                newtileDrop.GetComponent<SpriteRenderer>().sprite = tile.tileDrop;
                ItemClass tileDropItem = new ItemClass(tile);
                newtileDrop.GetComponent<TileDropController>().item = tileDropItem;
            }

            //worldTileClasses.RemoveAt(worldTiles.IndexOf(new Vector2(x, y)));

            if (!GetTileFromWorld(x, y))
            {
                worldTilesMap.SetPixel(x, y, Color.white);
                LightBlock(x, y, 1f, 0);
                worldTilesMap.Apply();
                //if (GetTileFromWorld(x, y).inBackground && GetTileFromWorld(x, y).name.ToLower().Contains("wall"))
                //{
                    
                //}
            }

            //worldTileObjects.RemoveAt(worldTiles.IndexOf(new Vector2(x, y)));
            //worldTiles.RemoveAt(worldTiles.IndexOf(new Vector2(x, y)));
            Destroy(GetObjectFromWorld(x, y));
            RemoveObjectFromWorld(x, y);
        }
    }

    public void CheckTile(TileClass tile, int x, int y, bool isNaturallyPlaced)
    {
        if (x >= 0 && x <= worldSize && y >= 0 && y <= worldSize)
        {
            if (tile.inBackground)
            {
                if (!GetTileFromWorld(x, y).inBackground)
                {
                    RemoveLightSource(x, y);
                    PlaceTile(tile, x, y, isNaturallyPlaced);
                }
            }
            else
            {
                if (GetTileFromWorld(x + 1, y) ||
                    GetTileFromWorld(x - 1, y) ||
                    GetTileFromWorld(x, y + 1) ||
                    GetTileFromWorld(x, y - 1)) 
                {
                    if (!GetTileFromWorld(x, y))
                    {
                        RemoveLightSource(x, y);
                        PlaceTile(tile, x, y, isNaturallyPlaced);
                    }
                    else
                    {
                        if (GetTileFromWorld(x, y).inBackground)
                        { 
                            RemoveLightSource(x, y);
                            PlaceTile(tile, x, y, isNaturallyPlaced);
                        }
                    }
                }
            }
            //if (!worldTiles.Contains(new Vector2Int(x, y)))
            //{
            //    RemoveLightSource(x, y);
            //    //place tile regardless
            //    PlaceTile(tile, x, y, isNaturallyPlaced);
            //}
            //else
            //{
            //    if (world_ForegroundTiles[x, y].inBackground)
            //    {
            //        //overwrite existing tile
            //        RemoveLightSource(x, y);
            //        //RemoveTile(x, y);
            //        PlaceTile(tile, x, y, isNaturallyPlaced);
            //    }
            //}
        }
    }

    public void PlaceTile(TileClass tile, int x, int y, bool isNaturallyPlaced)
    {
        if (x >= 0 && x <= worldSize && y >= 0 && y <= worldSize)
        {
            GameObject newTile = new GameObject();

            int chunkCoord = Mathf.RoundToInt(Mathf.Round(x / chunkSize) * chunkSize);
            chunkCoord /= chunkSize;

            newTile.transform.parent = worldChunks[chunkCoord].transform;

            newTile.AddComponent<SpriteRenderer>();

            int spriteIndex = Random.Range(0, tile.tileSprites.Length);
            newTile.GetComponent<SpriteRenderer>().sprite = tile.tileSprites[spriteIndex];

            worldTilesMap.SetPixel(x, y, Color.black);
            if (tile.inBackground)
            {
                newTile.GetComponent<SpriteRenderer>().sortingOrder = -10;

                if (tile.name.ToLower().Contains("wall"))
                {
                    newTile.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.6f);
                }
                else
                {
                    worldTilesMap.SetPixel(x, y, Color.white);
                }

            }
            else
            {
                newTile.GetComponent<SpriteRenderer>().sortingOrder = -5;
                newTile.AddComponent<BoxCollider2D>();
                newTile.GetComponent<BoxCollider2D>().size = Vector2.one;
                newTile.tag = "Ground";
            }

            //else if (!tile.inBackground)
            //{
            //    worldTilesMap.SetPixel(x, y, Color.black);
            //}

            newTile.name = tile.tileSprites[0].name;
            newTile.transform.position = new Vector2(x + 0.5f, y + 0.5f);

            //tile.naturallyPlaced = isNaturallyPlaced;
            TileClass newTileClass = TileClass.CreateInstance(tile, isNaturallyPlaced);

            //worldTiles.Add(newTile.transform.position - (Vector3.one * 0.5f));
            //worldTileObjects.Add(newTile);
            //worldTileClasses.Add(newTileClass);
            AddObjectToWorld(x, y, newTile, newTileClass);
            AddTileToWorld(x, y, newTileClass);
        }
    }

    void AddTileToWorld(int x, int y, TileClass tile)
    {
        if (tile.inBackground)
        {
            world_BackgroundTiles[x, y] = tile;
        }
        else
        {
            world_ForegroundTiles[x, y] = tile;
        }
    }

    void AddObjectToWorld(int x, int y, GameObject tileObject, TileClass tile)
    {
        if (tile.inBackground)
        {
            world_BackgroundObjects[x, y] = tileObject;
        }
        else
        {
            world_ForegroundObjects[x, y] = tileObject;
        }
    }

    void RemoveTileFromWorld(int x, int y)
    {
        if (world_ForegroundTiles[x, y] != null)
        {
            world_ForegroundTiles[x, y] = null;
        }
        else if (world_BackgroundTiles[x, y] != null)
        {
            world_BackgroundTiles[x, y] = null;
        }
    }

    void RemoveObjectFromWorld( int x, int y)
    {
        if (world_ForegroundObjects[x, y] != null)
        {
            world_ForegroundObjects[x, y] = null;
        }
        else if (world_BackgroundObjects[x, y] != null)
        {
            world_BackgroundObjects[x, y] = null;
        }
    }

    GameObject GetObjectFromWorld (int x, int y)
    {
        if (world_ForegroundObjects[x, y] != null)
        {
            return world_ForegroundObjects[x, y];
        }
        else if (world_BackgroundObjects[x, y] != null)
        {
            return world_BackgroundObjects[x, y];
        }
        return null;
    }

    TileClass GetTileFromWorld(int x, int y)
    {
        if (world_ForegroundTiles[x, y] != null)
        {
            return world_ForegroundTiles[x, y];
        }
        else if (world_BackgroundTiles[x, y] != null)
        {
            return world_BackgroundTiles[x, y];
        }
        return null;
    }

    void LightBlock (int x, int y, float intensity, int iteration)
    {
        if (iteration < lightRadius)
        {
            worldTilesMap.SetPixel(x, y, Color.white * intensity);

            float thresh = groundLightThreshold;
            if (x >= 0 && x < worldSize && y >= 0 && y < worldSize)
            {
                if (world_ForegroundTiles[x,y])
                {
                    thresh = groundLightThreshold;
                }
                else
                {
                    thresh = airLightThreshold;
                }
            }

            for (int nx = x - 1; nx < x + 2; nx++)
            {
                for (int ny = y -1; ny < y + 2; ny++)
                {
                    if (nx != x || ny != y)
                    {
                        if (worldTilesMap.GetPixel(nx, ny) != null)
                        {
                            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
                            float targetIntensity = Mathf.Pow(thresh, dist) * intensity;

                            if (worldTilesMap.GetPixel(nx, ny).r < targetIntensity)
                            {
                                LightBlock(nx, ny, targetIntensity, iteration + 1);
                            }
                            
                        }
                        
                    }
                }
            }
            worldTilesMap.Apply();
        }
    }

    void RemoveLightSource(int x, int y)
    {
        unlitBlocks.Clear();
        UnLightBlock(x, y, x, y);

        List<Vector2Int> toRelight = new List<Vector2Int>();
        foreach (Vector2Int block in unlitBlocks)
        {
            for (int nx = block.x - 1; nx < block.x + 2; nx++)
            {
                for (int ny = block.y -1; ny < block.y + 2; ny++)
                {
                    if(worldTilesMap.GetPixel(nx, ny) != null)
                    {
                        if(worldTilesMap.GetPixel(nx, ny).r > worldTilesMap.GetPixel(block.x, block.y).r)
                        {
                            if (!toRelight.Contains(new Vector2Int(nx, ny)))
                            {
                                toRelight.Add(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
            }
            
        }
        foreach (Vector2Int source in toRelight)
        {
            LightBlock(source.x, source.y, worldTilesMap.GetPixel(source.x, source.y).r, 0);
        }

        worldTilesMap.Apply();
    }

    void UnLightBlock(int x, int y, int ix, int iy)
    {
        if(Mathf.Abs(x - ix) >= lightRadius || Mathf.Abs(y - iy) >= lightRadius || unlitBlocks.Contains(new Vector2Int(x, y)))
        {
            return;
        }

        for (int nx = -1; nx < x + 2; nx++)
        {
            for (int ny = -1; ny < y + 2; ny++)
            {
                if (nx != x || ny != y)
                {
                    if (worldTilesMap.GetPixel(nx, ny) != null)
                    {
                        if (worldTilesMap.GetPixel(nx, ny).r < worldTilesMap.GetPixel(x, y).r)
                        {
                            UnLightBlock(nx, ny, ix, iy);
                        }
                    }
                }
            }
        }

        worldTilesMap.SetPixel(x, y, Color.black);
        unlitBlocks.Add(new Vector2Int(x, y));

    }
}
