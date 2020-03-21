using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI3 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        
        public List<GameObject>  myEnemies;
        public int nr;
        Graph mapGraph;
        int[,] nodeIdMatrix;
        GameObject targetTurret = null;
        List<int> currentPath;
        int tragetNodeId = 0;
        bool backing=false;
        bool start = true;

        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            TerrainInfo terrainInfo = terrain_manager.myInfo;
            float[, ] traversability = terrainInfo.traversability;
            int xLen = traversability.GetLength(0);int zLen = traversability.GetLength(1);

            TerrainGraph TerrainGraphScript = GameObject.Find("AwakeObj").GetComponent<TerrainGraph>();
            TerrainGraphScript.makeMap();
            
            mapGraph = TerrainGraphScript.mapGraph;
            nodeIdMatrix = TerrainGraphScript.nodeIdMatrix;

            TurretGraph turretGraphScript = GameObject.Find("TurretGraphObj").GetComponent<TurretGraph>();
            print(turretGraphScript);
            turretGraphScript.makeMap();
            List<GameObject> enemies = turretGraphScript.enemiePrio;



            

            


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work


            int nrCars = 3; 
            int len = (int)Math.Floor(GameObject.FindGameObjectsWithTag("Enemy").GetLength(0)/3.0f);
            print(len);
            if(nr==nrCars-1){


                for(int i = nr*len;i<enemies.Count;i++){
                    //print(enemies[i].transform.position);
                    myEnemies.Add( enemies[i]);
                }
            }
            else{

                for(int i = nr*len;i<(nr+1)*len;i++){
                    //print(enemies[i].transform.position);
                    myEnemies.Add( enemies[i]);

                }
            }
            print(myEnemies.Count);
            


            // Plan your path here
            // ...
        }


        private void FixedUpdate()
        {
            if(start){
                start=false;
                if(Vector3.Distance(transform.position,myEnemies[0].transform.position)>Vector3.Distance(transform.position,myEnemies[myEnemies.Count-1].transform.position)){
                    myEnemies.Reverse();
                }
            }

            if(targetTurret==null){
                foreach(GameObject enemy in myEnemies){
                    if(enemy!=null){
                        targetTurret=enemy;
                        int myX = terrain_manager.myInfo.get_i_index(transform.position.x);
                        int myZ = terrain_manager.myInfo.get_j_index(transform.position.z);
                        int enemyX = terrain_manager.myInfo.get_i_index(targetTurret.transform.position.x);
                        int enemyZ = terrain_manager.myInfo.get_j_index(targetTurret.transform.position.z);
                        currentPath=aStar(nodeIdMatrix[myX,myZ],nodeIdMatrix[enemyX,enemyZ]);
                        int temp=currentPath[0];
                        foreach (int nodeId in currentPath){
                            Debug.DrawLine(mapGraph.getNode(temp).getPosition(), mapGraph.getNode(nodeId).getPosition(), Color.red, 200000f);
                            temp = nodeId;
                            
                        }
                        tragetNodeId=0;
                        break;
                    }
                }
            }

            //foreach(int i=tragetNodeId;i<currentPath.Count;i++){}
            if(8.0f>Vector3.Distance(transform.position,mapGraph.getNode(currentPath[tragetNodeId]).getPosition()) && tragetNodeId!=currentPath.Count-1){
                tragetNodeId++;
            }

            if(targetTurret!=null){
               
            
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
                bool hitBack  = Physics.SphereCast(transform.position,2.0f,steeringPoint,out rayHit,4.0f, mask);
                bool hitForward  = Physics.SphereCast(transform.position,2.0f, -steeringPoint,out rayHit,2.5f,  mask);
                Debug.DrawRay(transform.position, steeringPoint*5.0f,Color.cyan,0.1f);
                Debug.DrawRay(transform.position, -steeringPoint*5.0f,Color.red,0.1f);
                bool hitContinue = Physics.SphereCast(transform.position,2.0f,steeringPoint,out rayHit,12.0f, mask);
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
            }else{
                m_Car.Move (0f, 0f, 0f, 0f);
                print("stop");
            }

            foreach (GameObject obj in myEnemies)
            {
                if(obj!=null){Debug.DrawLine(transform.position, obj.transform.position, Color.black);}
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
}
