using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibilityGraph : MonoBehaviour {


    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;
    TerrainInfo terrainInfo;
    public Graph VisGraph;
    public int[,] nodeIdMatrix;        
    public Graph mapGraph;
    public List<Node> prioNodes = new List<Node>();
    List<Node> receivingPrioNodes = new List<Node>();
    public int numberOfPrios = 0;
 
    private Color[] colors = {Color.white,Color.blue,Color.red,Color.green,Color.cyan,Color.yellow,Color.black,Color.magenta,Color.grey,
    new Color(0.3f, 0.4f, 0.6f, 1.0f), new Color(1.00f,0.49f,0.00f, 1.0f), new Color(0.00f,1.00f,0.84f, 1.0f), new Color(1.00f,0.50f,0.84f, 1.0f),
    new Color(0.40f,0.27f,0.00f, 1.0f), new Color(0.66f, 0.0f, 0.0f, 1.0f),new Color(0.00f,0.49f,0.20f, 1.0f)};
    


    public int nummberOfColors;
    int nummberOfVertex;
    // Use this for early initialization
    void Start () {
        
            
    }
    public void makeMap(){
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();  
        terrainInfo = terrain_manager.myInfo;
        float[, ] traversability = terrainInfo.traversability;
        float[] offsets = {2.1f,-2.1f};
        int xLen = traversability.GetLength(0);
        int zLen = traversability.GetLength(1);
        VisGraph = new Graph();





        int nodeId;
        Node node;
        for (int i = 0; i < xLen-1; i++) {
            for (int j = 0; j < zLen-1; j++) {
                float posx = terrainInfo.get_x_pos(i) + (0.5f*(terrainInfo.x_high - terrainInfo.x_low) / terrainInfo.x_N);
                float posz = terrainInfo.get_z_pos(j) + (0.5f*(terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N);
                

                float gridSum=traversability[i,j]+traversability[i,j+1]+traversability[i+1,j]+traversability[i+1,j+1];
                if (1.0f==gridSum){
                    foreach(float xoffset in offsets){
                        foreach(float zoffset in offsets){
                            if(1.0f==traversability[terrainInfo.get_i_index(posx+xoffset),terrainInfo.get_j_index(posz+zoffset)]){
                                posx=posx-xoffset;
                                posz=posz-zoffset;
                                goto FoundOne;
                            }
                        }
                    }
                    FoundOne:
                    nodeId = VisGraph.addNode(new Node (posx, posz));
                    node=VisGraph.getNode(nodeId);
                    node.setCube(GameObject.CreatePrimitive (PrimitiveType.Cube));
                }else if(3.0f==gridSum){
                    foreach(float xoffset in offsets){
                        foreach(float zoffset in offsets){
                            if(0.0f==traversability[terrainInfo.get_i_index(posx+xoffset),terrainInfo.get_j_index(posz+zoffset)]){
                                //print(i.ToString()+"--"+j.ToString());
                                posx=posx+xoffset;
                                posz=posz+zoffset;
                                goto FoundZero;
                            }
                        }
                    }
                    FoundZero:
                    nodeId = VisGraph.addNode(new Node (posx, posz));
                    node=VisGraph.getNode(nodeId);
                    node.setCube(GameObject.CreatePrimitive (PrimitiveType.Cube)); 
                }
                
                if(i!=0 && j!=0 && traversability[i,j]==1){
                    if(traversability[i,j+1]==0){
                        nodeId = VisGraph.addNode(new Node (terrainInfo.get_x_pos(i), terrainInfo.get_z_pos(j) + (5.0f+0.5f*(terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N)));
                        node=VisGraph.getNode(nodeId);
                        node.setCube(GameObject.CreatePrimitive (PrimitiveType.Cube)); 
                    }
                    if(traversability[i+1,j]==0){
                        nodeId = VisGraph.addNode(new Node (terrainInfo.get_x_pos(i)+(5.0f+0.5f*(terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N), terrainInfo.get_z_pos(j)));
                        node=VisGraph.getNode(nodeId);
                        node.setCube(GameObject.CreatePrimitive (PrimitiveType.Cube)); 
                    }
                    if(traversability[i,j-1]==0){
                        nodeId = VisGraph.addNode(new Node (terrainInfo.get_x_pos(i), terrainInfo.get_z_pos(j) - (5.0f+0.5f*(terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N)));
                        node=VisGraph.getNode(nodeId);
                        node.setCube(GameObject.CreatePrimitive (PrimitiveType.Cube)); 
                    }
                    if(traversability[i-1,j]==0){
                        nodeId = VisGraph.addNode(new Node (terrainInfo.get_x_pos(i)-(5.0f+0.5f*(terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N), terrainInfo.get_z_pos(j)));
                        node=VisGraph.getNode(nodeId);
                        node.setCube(GameObject.CreatePrimitive (PrimitiveType.Cube)); 
                    }

                }
                

            }
        }




        RaycastHit rayHit;
        LayerMask mask = LayerMask.GetMask("CubeWalls");
        for (int i = 0; i < VisGraph.nodes.Count; i++) {
            for (int j = 0; j < VisGraph.nodes.Count; j++) {
                
                Vector3 iPos = VisGraph.getNode(i).getPosition();
                iPos = new Vector3(iPos.x,0.5f,iPos.z);
                Vector3 jPos = VisGraph.getNode(j).getPosition();
                jPos = new Vector3(jPos.x,0.5f,jPos.z);
                float dis = Vector3.Distance(iPos, jPos);
                //print("---");

                if(false==Physics.Raycast(iPos,jPos-iPos,out rayHit,dis-1.0f, mask) && i!=j){
                    //print(iPos.ToString()+"--"+jPos.ToString());
                    VisGraph.addEdge(i,j);
                    Debug.DrawLine(new Vector3(iPos.x,0.5f,iPos.z),new Vector3(jPos.x,0.5f,jPos.z),Color.cyan,4);
                }
            }
        }

        Dictionary<int, Node> copyOfNodes = new Dictionary<int, Node>(VisGraph.nodes);
        graphPrio(copyOfNodes);

        TerrainGraph TerrainGraphScript = GameObject.Find("MapGraphObj").GetComponent<TerrainGraph>();
        TerrainGraphScript.makeMap();
        mapGraph = TerrainGraphScript.mapGraph;
        nodeIdMatrix = TerrainGraphScript.nodeIdMatrix;

        foreach(KeyValuePair<int, Node> nodeItem in VisGraph.nodes){
            if(nodeItem.Value.getColor()==1){
                
                receivingPrioNodes.Add(nodeItem.Value);
            }
        }
        numberOfPrios=receivingPrioNodes.Count;
        int colorid=0;
        Node fromNode = receivingPrioNodes[0];
        prioNodes.Add(fromNode);

        //foreach(Node fromNode in prioNodes)
        while(receivingPrioNodes.Count>0){
            
            receivingPrioNodes.Remove(fromNode);

            float minLenght=1000000;
            Node receivingNode=new Node();
            List<int> minPath= new List<int>();
            List<int> tempPath= new List<int>();
            foreach(Node toNode in receivingPrioNodes){
                tempPath=aStar(getTilePos(fromNode.getPosition()),getTilePos(toNode.getPosition()));
                if(minLenght>tempPath.Count){
                    minLenght=tempPath.Count;
                    minPath=tempPath;
                    receivingNode=toNode;
                }
            }
            
            receivingPrioNodes.Remove(receivingNode);
            prioNodes.Add(receivingNode);

            
            /*int temp=minPath[0];
            VisGraph.getNode(fromNode.getId()).setColor(colorid);
            foreach (int id in minPath){
                Debug.DrawLine(mapGraph.getNode(temp).getPosition(), mapGraph.getNode(id).getPosition(), colors[colorid], 200000f);
                temp = id;
                
            }
            colorid++;*/
            


            fromNode=receivingNode;



        }

        
        


            
        


        
 
        
        
        
    }

    int getTilePos(Vector3 pos){
        return nodeIdMatrix[terrainInfo.get_i_index(pos.x),terrainInfo.get_j_index(pos.z)];
    }

    void graphPrio(Dictionary<int, Node> nodes){
        while(nodes.Count>0){
            int tempMaxNieghbours=0;
            int maxNode=-1;
            foreach(KeyValuePair<int, Node> nodeItem in nodes){
                if(VisGraph.adjList[nodeItem.Key].Count > tempMaxNieghbours){
                    tempMaxNieghbours=VisGraph.adjList[nodeItem.Key].Count;
                    maxNode=nodeItem.Key;
                }
            }

            VisGraph.getNode(maxNode).setColor(1);//set this node to prio
            nodes.Remove(maxNode);
            foreach(int id in VisGraph.adjList[maxNode]){
                VisGraph.getNode(id).setColor(2);
                nodes.Remove(id);
            }

        }
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