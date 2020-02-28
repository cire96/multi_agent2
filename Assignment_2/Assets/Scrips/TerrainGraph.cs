using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGraph : MonoBehaviour {


    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;
    public Graph mapGraph;
    public int[,] nodeIdMatrix;

    // Use this for early initialization
    void Start () {
        makeMap();
            
    }
    public void makeMap(){
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        print(terrain_manager);
        TerrainInfo terrainInfo = terrain_manager.myInfo;
        print(terrainInfo);
        float[, ] traversability = terrainInfo.traversability;
        print(traversability);
        
        int xLen = traversability.GetLength(0);
        int zLen = traversability.GetLength(1);
        nodeIdMatrix = new int[xLen, zLen];
        mapGraph = new Graph();
        int nodeId;
        for (int i = 0; i < xLen; i++) {
            float posx = terrainInfo.get_x_pos(i);
            for (int j = 0; j < zLen; j++) {
                float posz = terrainInfo.get_z_pos(j);
                if (traversability[i,j]==0.0f){
                    GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
                    Collider c = cube.GetComponent<Collider> ();
                    c.enabled = false;
                    cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
                    cube.transform.position = new Vector3 (posx, 0, posz);
                    nodeIdMatrix[i,j] = mapGraph.addNode(new Node (posx, posz));
                }else{
                    nodeIdMatrix[i,j] = -1;
                }
            }
        }
        for (int i = 0; i < xLen; i++){
            for (int j = 0; j < zLen; j++){
                if(nodeIdMatrix[i,j] != -1){
                    nodeId = nodeIdMatrix[i,j];
                    if(nodeIdMatrix[i,j+1] != -1){mapGraph.addEdge(nodeId,nodeIdMatrix[i,j+1]);}
                    if(nodeIdMatrix[i,j-1] != -1){mapGraph.addEdge(nodeId,nodeIdMatrix[i,j-1]);}
                    if(nodeIdMatrix[i+1,j] != -1){mapGraph.addEdge(nodeId,nodeIdMatrix[i+1,j]);}
                    if(nodeIdMatrix[i-1,j] != -1){mapGraph.addEdge(nodeId,nodeIdMatrix[i-1,j]);}
                }
            }
        }
    }
}