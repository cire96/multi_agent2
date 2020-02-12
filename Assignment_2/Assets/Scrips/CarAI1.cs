using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI1 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;

        private void Start()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            TerrainInfo terrainInfo=terrain_manager.myInfo;
            float tileXSize = (terrainInfo.x_high - terrainInfo.x_low)/terrainInfo.x_N;
            float tileZSize = (terrainInfo.z_high - terrainInfo.z_low)/terrainInfo.z_N;

            float[,] newTerrain; 
            float stepx;
            float stepz;
            if (tileXSize<=10*2 && tileZSize<=10*2){
                newTerrain = terrainInfo.traversability;
                
            }else{
                newTerrain = new float[(int)Mathf.Floor(tileXSize*tileXSize/10*2),(int)Mathf.Floor(tileZSize*tileZSize/10*2)];
                stepx = (terrainInfo.x_high - terrainInfo.x_low) / newTerrain.GetLength(0);
                stepz = (terrainInfo.z_high - terrainInfo.z_low) / newTerrain.GetLength(1);
                for(int i=0; i<newTerrain.GetLength(0);i++){
                    float posx= terrainInfo.x_low + stepx / 2 + stepx * i;
                    for(int j=0; j<newTerrain.GetLength(1);j++){
                        float posz= terrainInfo.z_low + stepz / 2 + stepz * j;
                        newTerrain[i,j]=terrainInfo.traversability[terrainInfo.get_i_index(posx),terrainInfo.get_j_index(posz)];
                    }
                }
            }

            float[,] terrainNodes=new float[newTerrain.GetLength(0),newTerrain.GetLength(1)];
            stepx = (terrainInfo.x_high - terrainInfo.x_low) / newTerrain.GetLength(0);
            stepz = (terrainInfo.z_high - terrainInfo.z_low) / newTerrain.GetLength(1);
            for(int i=0; i<newTerrain.GetLength(0);i++){
                float posx= terrainInfo.x_low + stepx / 2 + stepx * i;
                for(int j=0; j<newTerrain.GetLength(1);j++){
                    float posz= terrainInfo.z_low + stepz / 2 + stepz * j;
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Collider c = cube.GetComponent<Collider>();
                    c.enabled = false;
                    cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    cube.transform.position = new Vector3(posx,0,posz);
                    terrainNodes[i,j] = new Node(posx,posz);
                }
            }
            


            


            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            // Note that you are not allowed to check the positions of the turrets in this problem

           


            // Plan your path here
            // ...


        }


        private void FixedUpdate()
        {

            enemies = GameObject.FindGameObjectsWithTag("Enemy");

            // Execute your path here
            // ...

            Vector3 avg_pos = Vector3.zero;

            foreach (GameObject friend in friends)
            {
                avg_pos += friend.transform.position;
            }
            avg_pos = avg_pos / friends.Length;
            Vector3 direction = (avg_pos - transform.position).normalized;

            bool is_to_the_right = Vector3.Dot(direction, transform.right) > 0f;
            bool is_to_the_front = Vector3.Dot(direction, transform.forward) > 0f;

            float steering = 0f;
            float acceleration = 0;

            if (is_to_the_right && is_to_the_front)
            {
                steering = 1f;
                acceleration = 1f;
            }
            else if (is_to_the_right && !is_to_the_front)
            {
                steering = -1f;
                acceleration = -1f;
            }
            else if (!is_to_the_right && is_to_the_front)
            {
                steering = -1f;
                acceleration = 1f;
            }
            else if (!is_to_the_right && !is_to_the_front)
            {
                steering = 1f;
                acceleration = -1f;
            }

            // this is how you access information about the terrain
            int i = terrain_manager.myInfo.get_i_index(transform.position.x);
            int j = terrain_manager.myInfo.get_j_index(transform.position.z);
            float grid_center_x = terrain_manager.myInfo.get_x_pos(i);
            float grid_center_z = terrain_manager.myInfo.get_z_pos(j);

            Debug.DrawLine(transform.position, new Vector3(grid_center_x, 0f, grid_center_z));


            // this is how you control the car
            //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
            m_Car.Move(steering, acceleration, acceleration, 0f);
            //m_Car.Move(0f, -1f, 1f, 0f);


        }
    }
}
