using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretGraph : MonoBehaviour {


    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;
    TerrainInfo terrainInfo;
    public Graph turretGraph;
    public Graph mapGraph;
    public int[,] nodeIdMatrix;
    public GameObject[] enemies;
    List<GameObject> enemieList;
    public List<GameObject> enemiePrio;

    private Color[] colors = {Color.white,Color.blue,Color.red,Color.green,Color.cyan,Color.yellow,Color.black,Color.magenta,Color.grey,
    new Color(0.3f, 0.4f, 0.6f, 1.0f), new Color(1.00f,0.49f,0.00f, 1.0f), new Color(0.00f,1.00f,0.84f, 1.0f), new Color(1.00f,0.50f,0.84f, 1.0f),
    new Color(0.40f,0.27f,0.00f, 1.0f), new Color(0.66f, 0.0f, 0.0f, 1.0f),new Color(0.00f,0.49f,0.20f, 1.0f)};
    



    // Use this for early initialization
    void Start () {

            
    }
    public void makeMap(){
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();  
        terrainInfo = terrain_manager.myInfo;
        TerrainGraph TerrainGraphScript = GameObject.Find("AwakeObj").GetComponent<TerrainGraph>();
        TerrainGraphScript.makeMap();
        mapGraph = TerrainGraphScript.mapGraph;
        nodeIdMatrix = TerrainGraphScript.nodeIdMatrix;

        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        turretGraph = new Graph();
        int nodeId;
        
        enemieList =  new List<GameObject>(enemies);

        int colorid=0;
        GameObject fromNode = enemieList[0];
        enemiePrio.Add(fromNode);

        //foreach(Node fromNode in prioNodes)
        while(enemieList.Count>0){
            
            enemieList.Remove(fromNode);

            float minLenght=1000000;
            GameObject receivingNode=new GameObject();
            List<int> minPath= new List<int>();
            List<int> tempPath= new List<int>();
            foreach(GameObject toNode in enemieList){
                tempPath=aStar(getTilePos(fromNode.transform.position),getTilePos(toNode.transform.position));
                if(minLenght>tempPath.Count){
                    minLenght=tempPath.Count;
                    minPath=tempPath;
                    receivingNode=toNode;
                }
            }
            
            enemieList.Remove(receivingNode);
            enemiePrio.Add(receivingNode);
            //print(receivingNode.transform.position);
            
            /*
            int temp=minPath[0];
            foreach (int id in minPath){
                Debug.DrawLine(mapGraph.getNode(temp).getPosition(), mapGraph.getNode(id).getPosition(), colors[colorid], 200000f);
                temp = id;
                
            }
            colorid++;
            */


            fromNode=receivingNode;



        }
    }

    int getTilePos(Vector3 pos){
        return nodeIdMatrix[terrainInfo.get_i_index(pos.x),terrainInfo.get_j_index(pos.z)];
    }


    public List<int> aStar(int start, int Goal){
        //var numbers2 = new List<int>() { 2, 3, 5, 7 };
        List<int> openSet = new List<int>() {start};
        Dictionary<int,int> cameFrom = new Dictionary<int,int>();

        // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
        float[] gScore = new float[mapGraph.getSize()];
        float[] fScore = new float[mapGraph.getSize()];

        for ( int i = 0; i < mapGraph.getSize();i++ ) {
            gScore[i] = 1000000.0f;
            fScore[i] = 1000000.0f; 
        }
        gScore[start] = 0.0f;
        fScore[start] = cost(start,Goal);


        while (openSet.Count>0){//!openSet.Any()
            int current=helpCurrent(fScore,openSet);
            if (current == Goal){
                return reconstruct_path(cameFrom, current);}
            openSet.Remove(current);
            foreach (int neighbor in mapGraph.getAdjList(current)){
                // d(current,neighbor) is the weight of the edge from current to neighbor
                // tentative_gScore is the distance from start to the neighbor through current
                float tentative_gScore = gScore[current] + cost(current, neighbor);
                if (tentative_gScore < gScore[neighbor]){
                    // This path to neighbor is better than any previous one. Record it!
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative_gScore;
                    fScore[neighbor] = gScore[neighbor] + cost(neighbor,Goal);
                    if (openSet.Contains(neighbor)==false){
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return new List<int>();

    }

    public int helpCurrent(float[] fScore,List<int> openSet){
        float lowestCost=10000000000.0f;
        int current=0;
        foreach(int id in openSet){
            if(fScore[id]<lowestCost){
                lowestCost=fScore[id];
                current=id;
            }
        }
        return current;
    }

    public float cost(int id,int goal){
        return Vector3.Distance(mapGraph.getNode(id).getPosition(),mapGraph.getNode(goal).getPosition());
    } 

    public List<int> reconstruct_path(Dictionary<int,int> cameFrom,int current){
        foreach (KeyValuePair<int, int> kvp in cameFrom)
        {
            //textBox3.Text += ("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            //Debug.Log("Key = "+kvp.Key.ToString()+", Value = "+ kvp.Value.ToString());
        }
        List<int> total_path = new List<int>() {current};
        while(cameFrom.ContainsKey(current)){
            current = cameFrom[current];
            total_path.Insert(0,current);
        }
        return total_path;
    }

}