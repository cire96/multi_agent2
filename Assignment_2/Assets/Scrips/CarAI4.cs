using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI4 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;
        public int Nr;
        bool backing =false;
        List<Vector3> friendsPosition = new List<Vector3>();
        List<Quaternion> friendsOrientation = new List<Quaternion>();
        List<Vector3> waypointList = new List<Vector3>();
        Vector3 offset;
        int currentNode = 0;
        Vector3 target;
        float timer = 0;
        float waitTime = 0.3f;
        bool firstNodeBool = false;



        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            

           

            

            // Plan your path here
            // ...
        }


        private void FixedUpdate()
        {

            if(!firstNodeBool){
                Vector3 off=new Vector3(0,0,0);
                if(Nr==0){
                    off = friends[0].transform.rotation*(new Vector3(-10,0,-20));
                }else if(Nr==1){
                    off = friends[0].transform.rotation*(new Vector3(10,0,-20));
                }else if(Nr==2){
                    off = friends[0].transform.rotation*(new Vector3(-25,0,-30));
                }else if(Nr==3){
                    off = friends[0].transform.rotation*(new Vector3(25,0,-30));
                }
                Vector3 pos=friends[0].transform.position+off;
                waypointList.Add(pos);
            }
            // Execute your path here
            // ...
            timer += Time.deltaTime;

            // Check if we have reached beyond 2 seconds.
            // Subtracting two is more accurate over time than resetting to zero.
            if (timer > waitTime){
                Vector3 off=new Vector3(0,0,0);
                if(Nr==0){
                    off = friends[0].transform.rotation*(new Vector3(-5,0,-20));
                }else if(Nr==1){
                    off = friends[0].transform.rotation*(new Vector3(5,0,-20));
                }else if(Nr==2){
                    off = friends[0].transform.rotation*(new Vector3(-10,0,-30));
                }else if(Nr==3){
                    off = friends[0].transform.rotation*(new Vector3(10,0,-30));
                }
                friendsPosition.Add(friends[0].transform.position);
                friendsOrientation.Add(friends[0].transform.rotation);
                
                GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
                Collider c = cube.GetComponent<Collider> ();
                c.enabled = false;
                cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
                Vector3 pos=friends[0].transform.position+off;
                waypointList.Add(pos);
                cube.transform.position=new Vector3(pos.x,0.0f,pos.z);

                // Remove the recorded 2 seconds.
                timer = timer - waitTime;
            }

            
            if( currentNode<waypointList.Count  ){//&& Vector3.Distance(transform.position, waypointList[currentNode])<5.0f
                //currentNode++;
                print("HOW do i get in here");
                float tempLength=100000;
                int tempNode=currentNode;
                for(int i=currentNode+1;i<waypointList.Count;i++){
                    if(Vector3.Distance(transform.position, waypointList[i])<tempLength){
                        tempLength=Vector3.Distance(transform.position, waypointList[i]);
                        tempNode=i;
                    }
                }
                currentNode=tempNode;
            }
            target = waypointList[currentNode];
            Vector3 carToTarget = m_Car.transform.InverseTransformPoint(target);
            float newSteer = (carToTarget.x / carToTarget.magnitude);
            float newSpeed = 1f;//(carToTarget.z / carToTarget.magnitude);

            
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
            LayerMask mask = LayerMask.GetMask("CubeWalls");
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

            Debug.DrawLine (transform.position, target);

            // this is how you control the car
            //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
            m_Car.Move (newSteer, newSpeed, newSpeed, 0f);
            //m_Car.Move(0f, -1f, 1f, 0f);

        }
    }
}
