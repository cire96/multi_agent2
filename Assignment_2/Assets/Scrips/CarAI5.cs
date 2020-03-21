using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI5 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;
        public int Nr;
        public float speedParam = 1.0f;
        public float lengthToGoal= 1.0f;
        bool backing =false;
        List<Vector3> friendsPosition = new List<Vector3>();
        List<Quaternion> friendsOrientation = new List<Quaternion>();
        List<Vector3> waypointList = new List<Vector3>();
        Vector3 offset;
        int currentNode = 0;
        Vector3 target;
        float timer = 0;
        float waitTime = 0.5f;
        bool firstNodeBool = false;
        float lastDistance=10.0f;
        Vector3 lastPoint = new Vector3(0,0,0);

        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject obj in enemies)
            {
                Debug.DrawLine(transform.position, obj.transform.position, Color.black, 10f);
            }


            // Plan your path here
            // ...
        }


        private void FixedUpdate()
        {

        Vector3 off=new Vector3(0,0,0);
        LayerMask mask = LayerMask.GetMask("CubeWalls");
        float distanceToPoint;
        if(0==Nr%2){
            RaycastHit evenInfo;
            bool hiteven = Physics.SphereCast(friends[0].transform.position+(new Vector3(0,2,0)),5.0f,friends[0].transform.rotation*(new Vector3(-1,0,0)),out evenInfo,40.0f,mask);
            //bool hiteven = Physics.Raycast(friends[0].transform.position+(new Vector3(0,2,0)),friends[0].transform.rotation*(new Vector3(-1,0,0)),out evenInfo,40.0f,mask);
            Debug.DrawRay(friends[0].transform.position+(new Vector3(0,2,0)), friends[0].transform.rotation*(new Vector3(-1,0,0)) * 40.0f, Color.cyan,0.1f);
            if(hiteven){
                distanceToPoint=evenInfo.distance-5.0f;
            }else{
                distanceToPoint=lastDistance;
            }
            if(distanceToPoint>lastDistance && 3.0f>(distanceToPoint-lastDistance)){
                distanceToPoint=lastDistance+3.0f;
            }
        }else{
            RaycastHit oddInfo;
            bool hitodd = Physics.SphereCast(friends[0].transform.position+(new Vector3(0,2,0)),5.0f,friends[0].transform.rotation*(new Vector3(1,0,0)),out oddInfo,40.0f,mask);
            //bool hitodd=Physics.Raycast(friends[0].transform.position+(new Vector3(0,2,0)),friends[0].transform.rotation*(new Vector3(1,0,0)),out oddInfo,40.0f,mask);
            Debug.DrawRay(friends[0].transform.position+(new Vector3(0,2,0)), friends[0].transform.rotation*(new Vector3(1,0,0)) * 40.0f, Color.cyan,0.1f);
            if(hitodd){
                distanceToPoint=oddInfo.distance-5.0f;
            }else{
                distanceToPoint=lastDistance;
            }
            
            if(distanceToPoint>lastDistance && 3.0f>(distanceToPoint-lastDistance)){
                distanceToPoint=lastDistance+3.0f;
            }
        }
        lastDistance=distanceToPoint;


        if(Nr==0){
            off = friends[0].transform.rotation*(new Vector3(-(distanceToPoint/2),0,-00));
        }else if(Nr==2){
            off = friends[0].transform.rotation*(new Vector3(-distanceToPoint,0,-00));
        }else if(Nr==1){
            off = friends[0].transform.rotation*(new Vector3((distanceToPoint/2),0,-00));
        }else if(Nr==3){
            off = friends[0].transform.rotation*(new Vector3(distanceToPoint,0,-00));
        }
        

        if(!firstNodeBool){
        
            Vector3 pos=friends[0].transform.position+off;
            waypointList.Add(pos);
            lastPoint=pos;

            firstNodeBool=true;
        }
        // Execute your path here
        // ...

        timer += Time.deltaTime;
        // Check if we have reached beyond 0.2 seconds.
        // Subtracting two is more accurate over time than resetting to zero.
        if (timer > waitTime){
            
            friendsPosition.Add(friends[0].transform.position);
            friendsOrientation.Add(friends[0].transform.rotation);
            
            Vector3 pos=friends[0].transform.position+off;
            waypointList.Add(pos);


            /*GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
            Collider c = cube.GetComponent<Collider> ();
            c.enabled = false;
            cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
            cube.transform.position=new Vector3(pos.x,0.0f,pos.z);*/

            Debug.DrawLine(pos,lastPoint,Color.red,25);
            lastPoint=pos;

            // Remove the recorded 2 seconds.
            timer = timer - waitTime;
        }

        
        if( currentNode<waypointList.Count  ){//&& Vector3.Distance(transform.position, waypointList[currentNode])<15.0f
            //currentNode++;
            float tempLength=10.0f;
            if(currentNode+1<waypointList.Count){
                tempLength=Vector3.Distance(waypointList[currentNode], waypointList[currentNode+1])+10.0f;
            }

            int tempNode=currentNode;
            for(int i=currentNode+1;i<waypointList.Count;i++){
                if(Vector3.Distance(transform.position, waypointList[i])<tempLength){
                    tempLength=Vector3.Distance(transform.position, waypointList[i]);
                    tempNode=i;
                }
            }
            currentNode=tempNode;
            if(2<waypointList.Count){
                float tempDistance = 0.0f;  
                for(int i=currentNode;i<waypointList.Count-2;i++){
                    print(waypointList[currentNode+1]);
                    tempDistance =+ Vector3.Distance(waypointList[currentNode], waypointList[currentNode+1]);
                }
                lengthToGoal = tempDistance;
            }
        }

        //what of cars aprox length to optimal point is longest.
        float longestLeangthToGoal = 0;
        for(int i=1;i<friends.Length;i++){
            if(longestLeangthToGoal<friends[i].GetComponent<CarAI4>().lengthToGoal){
                longestLeangthToGoal=friends[i].GetComponent<CarAI4>().lengthToGoal;
            }
        }
        //Speed param on witch has the longest way to go and how much 
        //closer others are to there optimal position
        speedParam=lengthToGoal/longestLeangthToGoal;


        target = waypointList[currentNode];
        Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
        float newSteer = (carToTarget.x / carToTarget.magnitude);
        float newSpeed = (float) Math.Exp(-speedParam);

        
        float infrontOrbehind = (carToTarget.z / carToTarget.magnitude);
        if(infrontOrbehind<0){
            newSpeed =-1;
            if(newSteer<0){
                newSteer =1;
            }else{
                newSteer =-1;
            }
        }else{newSpeed = 1f;}

        if(m_Car.CurrentSpeed>40){
            newSpeed = 0;
        }
        

        //if(infrontOrbehind<0 && Mathf.Abs(newSteer)<0.1){newSteer =1;}
        float handBreak = 0f;

        Vector3 steeringPoint = (transform.rotation * new Vector3(0,0,1));
        RaycastHit rayHit;
        //bool hitBack = body.SweepTest(steeringPoint,out rayHit, 2.0f);
        //bool hitContinue = body.SweepTest(steeringPoint,out rayHit, 8.0f);
        bool hitBack  = Physics.SphereCast(transform.position,3.0f,steeringPoint,out rayHit,3.0f, mask);
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
        
        if(20.0f>Vector3.Distance(transform.position,waypointList[waypointList.Count-1])){
            newSpeed=-1/(20.0f-Vector3.Distance(transform.position,waypointList[waypointList.Count-1]));
        }

        Debug.DrawLine (transform.position, target);

        // this is how you control the car
        //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
        m_Car.Move (newSteer, newSpeed, newSpeed, 0f);

        }
    }
}
