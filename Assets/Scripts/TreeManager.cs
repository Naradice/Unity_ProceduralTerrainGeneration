using UnityEngine;
using System.Collections.Generic;

class TreeManager
{

    public void SpawnTrees(TerrainData terrainData, TreeData treeData, float[,] heights)
    {

        TreePrototype[] prototypes = new TreePrototype[1];
        prototypes[0] = new TreePrototype { prefab = treeData.treePrototype };
        terrainData.treePrototypes = prototypes;
        int side_length = terrainData.heightmapResolution;

        TreeInstance[] treeInstances = new TreeInstance[treeData.population];

        for (int i = 0; i < treeData.population; i++)
        {
            TreeInstance treeInstance = new TreeInstance();
            float x = Random.Range(0f, 1f);
            float z = Random.Range(0f, 1f);
            float y = heights[Mathf.FloorToInt(z * side_length), Mathf.FloorToInt(x * side_length)];

            treeInstance.position = new Vector3(x, y, z);
            treeInstance.prototypeIndex = 0; // Index of the tree prototype you want to use (0 if there's only one)

            // Adjust the size and color of the trees (optional)
            treeInstance.widthScale = 1f;
            treeInstance.heightScale = 3f;
            treeInstance.color = Color.white;

            treeInstances[i] = treeInstance;
        }

        terrainData.treeInstances = treeInstances;
        terrainData.RefreshPrototypes();
    }

    private bool isInHeights(float min_height, float max_height, float[,] heights){
        for(int i = 0; i < heights.GetLength(0); i++){
            for(int j = 0; j < heights.GetLength(1); j++){
                float current_height = heights[i, j];
                if(current_height >= min_height && current_height <= max_height){
                    return true;
                }
            }
        }
        return false;
    }

    public void SpawnTrees(TerrainData terrainData, TreeData[] treeData, float[,] heights){
        if (treeData.Length == 0){
            Debug.Log("No tree data");
            return;
        }

        List<TreePrototype> prototypes = new List<TreePrototype>();
        Dictionary<int, TreeSpawnData> treePrototypeIndex = new Dictionary<int, TreeSpawnData>();
        int numOfTrees = 0;

        for(int i = 0; i < treeData.Length; i++){
            if(treeData[i].treePrototype != null){
                if(isInHeights(treeData[i].AntitudeLowerBound, treeData[i].AntitudeUpperBound, heights)){
                    prototypes.Add(
                        new TreePrototype { prefab = treeData[i].treePrototype, bendFactor = 0.5f}
                    );
                    treePrototypeIndex.Add(i, new TreeSpawnData(treeData[i]));
                    numOfTrees += treeData[i].population;
                }else{
                    Debug.Log($"Tree prototype {i} is not in the height range");
                }
            }
        }
        
        terrainData.treePrototypes = prototypes.ToArray();
        // To use this for index, we reduce 1 from the length
        int side_length = terrainData.heightmapResolution - 1;

        TreeInstance[] treeInstances = new TreeInstance[numOfTrees];

        while(treePrototypeIndex.Count > 0)
        {
            TreeInstance treeInstance = new TreeInstance();
            float x = Random.Range(0f, 1f);
            float z = Random.Range(0f, 1f);
            float y = 0f;
            try{
                y = heights[Mathf.FloorToInt(z * side_length), Mathf.FloorToInt(x * side_length)];
            }catch{
                Debug.Log("x: " + x + " z: " + z + " side_length: " + side_length + " heights: " + heights.GetLength(0) + " " + heights.GetLength(1));
                throw;
            }
            

            foreach(KeyValuePair<int, TreeSpawnData> entry in treePrototypeIndex){
                if(entry.Value.IsInTreeSpawnArea(y)){
                    treeInstance.position = new Vector3(x, y, z);
                    treeInstance.prototypeIndex = entry.Key; // Index of the tree prototype you want to use (0 if there's only one)

                    // Adjust the size and color of the trees (optional)
                    treeInstance.widthScale = 1f;
                    treeInstance.heightScale = 3f;
                    treeInstance.color = Color.white;

                    treeInstances[entry.Value.added_population] = treeInstance;
                    entry.Value.added_population++;
                    if(entry.Value.IsTreeSpawnAreaEmpty()){
                        treePrototypeIndex.Remove(entry.Key);
                        Debug.Log($"Tree prototype {entry.Key} is fully created: {entry.Value.added_population}");
                    }
                    break;
                }else{
                    if(entry.Value.IsTreeSpawnAreaEmpty()){
                        treePrototypeIndex.Remove(entry.Key);
                        Debug.Log($"Tree prototype {entry.Key} is removed");
                    }
                    break;
                }
            }
        }

        terrainData.treeInstances = treeInstances;
        terrainData.RefreshPrototypes();
    }

    public void SpawnTreesTest(TerrainData terrainData, GameObject treePrototype, int numberOfTrees, float[,] heights)
    {

        TreePrototype[] prototypes = new TreePrototype[1];
        prototypes[0] = new TreePrototype { prefab = treePrototype };
        terrainData.treePrototypes = prototypes;
        int side_length = terrainData.heightmapResolution;
        int short_side_length = Mathf.FloorToInt(side_length / 2);
        int num_trees = Mathf.FloorToInt(short_side_length * short_side_length / 9);

        TreeInstance[] treeInstances = new TreeInstance[num_trees];

        int i = 0;
        for (int x = 0; x < short_side_length; x++)
        {
            for (int z = 0; z < short_side_length; z++){
                if(x % 10 == 0){
                    TreeInstance treeInstance = new TreeInstance();
                    float y = heights[z, x];
                    float side_length_f = (float)side_length;

                    treeInstance.position = new Vector3((float)x/side_length_f, y, (float)z/side_length_f);
                    treeInstance.prototypeIndex = 0; // Index of the tree prototype you want to use (0 if there's only one)

                    // Adjust the size and color of the trees (optional)
                    treeInstance.widthScale = 1f;
                    treeInstance.heightScale = 3f;
                    //treeInstance.color = Color.white;

                    treeInstances[i] = treeInstance;
                    i++;
                }
            }
        }
        
        terrainData.treeInstances = treeInstances;
        terrainData.RefreshPrototypes();
    }

    private class TreeSpawnData { 

        private const int MAX_PATIENTS = 100;
        private int patients_count = 0;
        private float antitude_upper_bound;
        private float antitude_lower_bound;
        private int population;

        public int added_population = 0;
        public float height;

        public bool IsInTreeSpawnArea(float height){
            if(height >= antitude_lower_bound && height <= antitude_upper_bound){
                patients_count = 0;
                return true;
            }else{
                patients_count++;
                if(patients_count > MAX_PATIENTS){
                    Debug.Log("Too many patients");
                    added_population = population;
                    return false;
                }
                return false;
            }
        }

        public bool IsTreeSpawnAreaEmpty(){
            return added_population >= population;
        }

        public TreeSpawnData(TreeData treeData){
            antitude_upper_bound = treeData.AntitudeUpperBound;
            antitude_lower_bound = treeData.AntitudeLowerBound;
            population = treeData.population;
            height = treeData.heights;
        }
    }

}

[System.Serializable]
public class TreeData {

    // These parameters are set based on TerrainType
    private float antitude_upper_bound = 1f;
    public float AntitudeUpperBound{
        set{
            antitude_upper_bound = value;
        }
        get{
            return antitude_upper_bound;
        }
    }

    private float antitude_lower_bound = 0f;
    public float AntitudeLowerBound{
        set{
            antitude_lower_bound = value;
        }
        get{
            return antitude_lower_bound;
        }
    }

    // Serialized fields for the inspector
    public GameObject treePrototype;
    public int population;
    public float heights;
    public float heights_std;
    //public float slopes;
    //public float[,] fertility;

} 