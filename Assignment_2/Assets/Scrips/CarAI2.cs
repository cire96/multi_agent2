using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI2 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;

        Graph VisibilityGraph;
        List<Node> myPath = new List<Node>();
        int[,] nodeIdMatrix;        
        Graph mapGraph;
        TerrainInfo terrainInfo;

        
        bool start = true;
        bool planNext = true;
        int listDir = 1;
        int prioNodeIndex = -1;
        int tragetNodeId = 0;
        bool backing = false;
        List<int> currentPath;

        public int nr;

        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            terrainInfo = terrain_manager.myInfo;
            float[, ] traversability = terrainInfo.traversability;
            int xLen = traversability.GetLength(0);int zLen = traversability.GetLength(1);

            VisibilityGraph visibilityGraphScript = GameObject.Find("VisibilityGraphObj").GetComponent<VisibilityGraph>();
            //visibilityGraphScript.makeMap();
            VisibilityGraph = visibilityGraphScript.VisGraph;
            nodeIdMatrix=visibilityGraphScript.nodeIdMatrix;        
            mapGraph=visibilityGraphScript.mapGraph;

            

            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            // Note that you are not allowed to check the positions of the turrets in this problem



            // Plan your path here
            // ...
            List<Node> fullPathList=visibilityGraphScript.prioNodes;
            print(fullPathList.Count);

            int nrCars = 3; 
            int len = (int)Math.Floor(fullPathList.Count/3.0f);
            print(len);
            if(nr==nrCars-1){
                for(int i = nr*len;i<fullPathList.Count;i++){ 
                    print(fullPathList[i].getPosition());              
                    myPath.Add(fullPathList[i]);
                }
            }
            else{

                for(int i = nr*len;i<(nr+1)*len;i++){
                    myPath.Add(fullPathList[i]);
                }
            }
            foreach (Node node in myPath)
            {
                Debug.DrawLine(transform.position, mapGraph.getNode(getTilePos(node.getPosition())).getPosition(), Color.black, 10f);
            }

        }


        private void FixedUpdate(){
            if(start){
                start=false;
                if(Vector3.Distance(transform.position,myPath[0].getPosition())<Vector3.Distance(transform.position,myPath[myPath.Count-1].getPosition())){
                    listDir = 1;
                    prioNodeIndex = 0;
                }else{
                    listDir = -1;
                    prioNodeIndex = myPath.Count-1;
                }
            }
            if( 20.0f>Vector3.Distance(transform.position,myPath[prioNodeIndex].getPosition()) ){
                planNext = true;
                prioNodeIndex=prioNodeIndex+listDir;
            }


            if(planNext){
                planNext = false;
                currentPath=aStar(getTilePos(transform.position),getTilePos(myPath[prioNodeIndex].getPosition()));
                int temp=currentPath[0];
                foreach (int nodeId in currentPath){
                    Debug.DrawLine(mapGraph.getNode(temp).getPosition(), mapGraph.getNode(nodeId).getPosition(), Color.red, 200000f);
                    temp = nodeId;
                }
                tragetNodeId=0;
            }


            if(8.0f>Vector3.Distance(transform.position,mapGraph.getNode(currentPath[tragetNodeId]).getPosition()) && tragetNodeId!=currentPath.Count-1){
                tragetNodeId++;
            }

            
               
            
            Vector3 target = mapGraph.getNode(currentPath[tragetNodeId]).getPosition();
        
            Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
            float newSteer = (carToTarget.x / carToTarget.magnitude);
            float newSpeed = 1f;//(carToTarget.z / carToTarget.magnitude);

            float infrontOrbehind = (carToTarget.z / carToTarget.magnitude);
            if(infrontOrbehind<-0.5){
                newSpeed =-1;
                if(newSteer<0){
                    newSteer =1;
                }else{
                    newSteer =-1;
                }
            }else{newSpeed = 1f;}
            //if(infrontOrbehind<0 && Mathf.Abs(newSteer)<0.1){newSteer =1;}
            float handBreak = 0f;

            Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
            RaycastHit rayHit;
            LayerMask mask = LayerMask.GetMask("CubeWalls");
            //bool hitBack = body.SweepTest(steeringPoint,out rayHit, 2.0f);
            //bool hitContinue = body.SweepTest(steeringPoint,out rayHit, 8.0f);
            bool hitBack  = Physics.SphereCast(transform.position,3.0f,steeringPoint,out rayHit,4.0f, mask);
            bool hitForward  = Physics.SphereCast(transform.position,3.0f, -steeringPoint,out rayHit,2.5f,  mask);
            Debug.DrawRay(transform.position, steeringPoint*5.0f,Color.cyan,0.1f);
            Debug.DrawRay(transform.position, -steeringPoint*5.0f,Color.red,0.1f);
            bool hitContinue = Physics.SphereCast(transform.position,3.0f,steeringPoint,out rayHit,12.0f, mask);
            if(hitBack){
                backing=true;
                newSpeed=-1f;
                if(m_Car.BrakeInput>0 && m_Car.AccelInput<=0){
                    newSteer=-newSteer;
                }
                print("back");

            }
            //if(hitContinue && m_Car.AccelInput>=0 && backing==false){newSteer= newSteer*2;} 
            if(hitContinue && backing==true ){
                newSpeed=-1f;
                newSteer=-newSteer;
                print("continue");
            }else{
                backing=false;
            }
            if(hitForward){
                newSpeed=1;
            }

            Debug.DrawLine (transform.position, target);

            // this is how you control the car
            //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
            m_Car.Move (newSteer, newSpeed, newSpeed, 0f);
            

            

            

            


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
}
