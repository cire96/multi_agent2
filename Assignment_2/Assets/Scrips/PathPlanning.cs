using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathPlanning : MonoBehaviour {

    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;
    public GameObject[] friends;

    // Use this for initialization
    void Start () {
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager> ();
        TerrainInfo terrainInfo = terrain_manager.myInfo;
        float tileXSize = (terrainInfo.x_high - terrainInfo.x_low) / terrainInfo.x_N;
        float tileZSize = (terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N;

        // check the resolution of the grid, and if the car's turret range is not enough to cover half it's length, make a finer grid
        float[, ] newTerrain;
        float stepx;
        float stepz;
        if (tileXSize <= 10 * 2 && tileZSize <= 10 * 2) {
            newTerrain = terrainInfo.traversability;

        } else {
            newTerrain = new float[(int) Mathf.Floor (tileXSize * tileXSize / 10 * 2), (int) Mathf.Floor (tileZSize * tileZSize / 10 * 2)];
            stepx = (terrainInfo.x_high - terrainInfo.x_low) / newTerrain.GetLength (0);
            stepz = (terrainInfo.z_high - terrainInfo.z_low) / newTerrain.GetLength (1);
            for (int i = 0; i < newTerrain.GetLength (0); i++) {
                float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
                for (int j = 0; j < newTerrain.GetLength (1); j++) {
                    float posz = terrainInfo.z_low + stepz / 2 + stepz * j;
                    newTerrain[i, j] = terrainInfo.traversability[terrainInfo.get_i_index (posx), terrainInfo.get_j_index (posz)];
                }
            }
        }

        // initialize the 2d array with all the nodes
        Node[, ] terrainNodes = new Node[newTerrain.GetLength (0), newTerrain.GetLength (1)];
        stepx = (terrainInfo.x_high - terrainInfo.x_low) / newTerrain.GetLength (0);
        stepz = (terrainInfo.z_high - terrainInfo.z_low) / newTerrain.GetLength (1);
        for (int i = 0; i < newTerrain.GetLength (0); i++) {
            float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
            for (int j = 0; j < newTerrain.GetLength (1); j++) {
                if (newTerrain[i, j] == 0) {
                    // float posz = terrainInfo.z_low + stepz / 2 + stepz * j;
                    // GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
                    // Collider c = cube.GetComponent<Collider> ();
                    // c.enabled = false;
                    // cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
                    // cube.transform.position = new Vector3 (posx, 0, posz);
                    terrainNodes[i, j] = new Node (posx, posz);
                } else {
                    terrainNodes[i, j] = null;
                }
            }
        }

        friends = GameObject.FindGameObjectsWithTag ("Player");

        Graph[] subtrees = MST.getSubgraphs (terrainNodes, terrainInfo, friends, newTerrain);

        for (int i = 0; i < subtrees.Length; i++) {
            Color[] c = { Color.red, Color.blue, Color.yellow };
            foreach (KeyValuePair<int, Node> pair in subtrees[i].getNodes ()) {
                foreach (int id in subtrees[i].getAdjList (pair.Value.getId ())) {
                    Debug.DrawLine (subtrees[i].getNode (id).getPosition (), pair.Value.getPosition (), c[i % 3], 100f);
                }
            }
        }
    }

    // Use this for initialization
    void Awake () {

    }

    // Update is called once per frame
    void Update () {

    }
}