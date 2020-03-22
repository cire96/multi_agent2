using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car {
    [RequireComponent (typeof (CarController))]
    public class CarAI5 : MonoBehaviour {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;

        private int SQUARE_SIZE = 8;
        private float[, ] newTerrain;
        private float[, , ] vision;
        private List<Node> leafs;
        private float stepx;
        private float stepz;
        private TerrainInfo terrainInfo;
        private bool backing = false;
        private int counter;
        private Node bestTarget;
        private Vector3 lastPoint;
        private List<Vector3> visited;
        private int enemySize;
        private bool gotTarget;
        private Node target;

        public int nr;

        private void Start () {
            if (SQUARE_SIZE % 2 != 1) {
                print ("SQUARE_SIZE must be an odd number!");
            }
            // get the car controller
            m_Car = GetComponent<CarController> ();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager> ();

            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag ("Player");
            enemies = GameObject.FindGameObjectsWithTag ("Enemy");

            terrainInfo = terrain_manager.myInfo;
            float tileXSize = (terrainInfo.x_high - terrainInfo.x_low) / terrainInfo.x_N;
            float tileZSize = (terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N;

            int factor = 2;
            newTerrain = new float[(int) Mathf.Floor (tileXSize * tileXSize / factor), (int) Mathf.Floor (tileZSize * tileZSize / factor)];
            stepx = (terrainInfo.x_high - terrainInfo.x_low) / newTerrain.GetLength (0);
            stepz = (terrainInfo.z_high - terrainInfo.z_low) / newTerrain.GetLength (1);
            for (int i = 0; i < newTerrain.GetLength (0); i++) {
                float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
                for (int j = 0; j < newTerrain.GetLength (1); j++) {
                    float posz = terrainInfo.z_low + stepz / 2 + stepz * j;
                    newTerrain[i, j] = terrainInfo.traversability[terrainInfo.get_i_index (posx), terrainInfo.get_j_index (posz)];
                }
            }

            enemySize = enemies.Length;

            initVision ();

            leafs = getSuccessors (null);
            target = null;

            gotTarget = false;
            lastPoint = getAvgPos ();

            visited = new List<Vector3> ();
            visited.Add (getSquare (lastPoint.x, lastPoint.z).getPosition ());

            // use this to visualize the heat map
            //------------------------------------

            // float[, ] maps = sumUpHeatMap (vision);

            // for (int i = 0; i < newTerrain.GetLength (0); i++) {
            //     float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
            //     for (int j = 0; j < newTerrain.GetLength (1); j++) {
            //         float posz = terrainInfo.z_low + stepz / 2 + stepz * j;

            //         if (maps[i, j] != 0) {
            //             Color c;
            //             switch (maps[i, j]) {
            //                 case 1:
            //                     c = Color.magenta;
            //                     break;
            //                 case 2:
            //                     c = Color.blue;
            //                     break;
            //                 case 3:
            //                     c = Color.yellow;
            //                     break;
            //                 case 4:
            //                     c = Color.cyan;
            //                     break;
            //                 case 5:
            //                     c = Color.grey;
            //                     break;
            //                 case 6:
            //                     c = Color.white;
            //                     break;
            //                 case 7:
            //                     c = Color.red;
            //                     break;
            //                 case 8:
            //                     c = Color.black;
            //                     break;
            //                 default:
            //                     c = Color.green;
            //                     break;
            //             }

            //             GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
            //             Collider col = cube.GetComponent<Collider> ();
            //             col.enabled = false;
            //             cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
            //             cube.transform.position = new Vector3 (posx, 0, posz);
            //             cube.GetComponent<Renderer> ().material.color = c;
            //         }
            //     }
            // }
        }

        // creates a 2d array with integers which tell how many turrets have vision to which cells on the map
        private void initVision () {
            vision = new float[enemySize, newTerrain.GetLength (0), newTerrain.GetLength (1)];
            int enemy_nr = 0;
            foreach (GameObject enemy in enemies) {
                if (enemy != null) {
                    for (int i = 0; i < newTerrain.GetLength (0); i++) {
                        float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
                        for (int j = 0; j < newTerrain.GetLength (1); j++) {
                            float posz = terrainInfo.z_low + stepz / 2 + stepz * j;

                            Vector3 destination = new Vector3 (posx, 0, posz);

                            float distance = (enemy.transform.position - destination).magnitude;
                            Vector3 direction = (enemy.transform.position - destination).normalized;
                            int layerMask = 1 << 9;

                            if (Physics.Linecast (enemy.transform.position, destination, layerMask)) {
                                vision[enemy_nr, i, j] = 0;
                            } else {
                                vision[enemy_nr, i, j] = 1;
                            }

                        }
                    }
                    enemy_nr++;
                }
            }
        }

        // recursively gives a target point which points to a square where at least one turret can be seen
        private Node getTarget () {

            float bestCost = int.MaxValue;
            bestTarget = null;

            // if a square with vision from at least half of the cells is found, take it
            foreach (Node leaf in leafs) {
                if (visited.Contains (leaf.getSquare ().getPosition ())) {
                    continue;
                }

                float cost = leaf.getCost ();
                if (cost > (SQUARE_SIZE * SQUARE_SIZE) / 2 && cost < bestCost) {
                    bestCost = cost;
                    bestTarget = leaf;
                }
            }

            if (bestTarget == null) {
                float bestDistance = 0;

                // get the successor with max distance to where you've just been and call getTarget() again with updated leafs
                foreach (Node leaf in leafs) {
                    if (visited.Contains (leaf.getSquare ().getPosition ())) {
                        continue;
                    }

                    float distance = Vector3.Distance (leaf.getSquare ().getPosition (), lastPoint);
                    if (distance > bestDistance) {
                        bestTarget = leaf;
                        bestDistance = distance;
                    }
                }

                visited.Add (bestTarget.getSquare ().getPosition ());
                leafs = null;
                leafs = getSuccessors (bestTarget);

                return getTarget ();
            } else {
                visited.Add (bestTarget.getSquare ().getPosition ());
                leafs = null;
                leafs = getSuccessors (bestTarget);
                return bestTarget;
            }
        }

        // check if an enemy has been destroyed -> implies need for new vision map
        private bool checkChange () {
            int counter = 0;
            foreach (GameObject g in enemies) {
                if (g != null) {
                    counter++;
                }
            }

            if (counter != enemySize) {
                enemySize = counter;
                return true;
            } else {
                return false;
            }
        }

        private void FixedUpdate () {
            // when a car is destroyed
            if (m_Car.transform == null) {
                return;
            }

            // if a turret is destroyed, initialize the vision map again and reset the visited list -> "start from new"
            if (checkChange ()) {
                initVision ();
                lastPoint = getAvgPos ();
                gotTarget = false;
                visited = null;
                visited = new List<Vector3> ();
                visited.Add (getSquare (lastPoint.x, lastPoint.z).getPosition ());
            }

            if (!gotTarget) {
                leafs = null;
                leafs = getSuccessors (null);

                target = getTarget ();
                if (target != null) {
                    gotTarget = true;
                } else {
                    return;
                }
            } else {
                if (Vector3.Distance (getAvgPos (), target.getSquare ().getPosition ()) < 7) {
                    gotTarget = false;
                    return;
                }
            }

            Debug.DrawLine (getAvgPos (), target.getSquare ().getPosition ());


            Vector3 off=new Vector3(0,0,0);
            if(nr==0){
                off = (new Vector3(4f,0,4));
            }else if(nr==2){
                off = (new Vector3(-4f,0,-4));
            }else if(nr==1){
                off = (new Vector3(0,0,0));
            }
            Vector3 pos = target.getSquare().getPosition()+off;



            Vector3 carToTarget = m_Car.transform.InverseTransformPoint (pos);
            float newSteer = (carToTarget.x / carToTarget.magnitude);
            float newSpeed = 1f; //(carToTarget.z / carToTarget.magnitude);

            float infrontOrbehind = (carToTarget.z / carToTarget.magnitude);
            if (infrontOrbehind < 0) {
                newSpeed = -1;
                if (newSteer < 0) {
                    newSteer = 1;
                } else {
                    newSteer = -1;
                }
            } else { newSpeed = 1f; }
            //if(infrontOrbehind<0 && Mathf.Abs(newSteer)<0.1){newSteer =1;}
            float handBreak = 0f;

            Vector3 steeringPoint = (transform.rotation * new Vector3 (0, 0, 1));
            RaycastHit rayHit;
            LayerMask mask = LayerMask.GetMask ("CubeWalls");
            //bool hitBack = body.SweepTest(steeringPoint,out rayHit, 2.0f);
            //bool hitContinue = body.SweepTest(steeringPoint,out rayHit, 8.0f);
            bool hitBack = Physics.SphereCast (transform.position, 2.0f, steeringPoint, out rayHit, 3.0f, mask);
            bool hitForward = Physics.SphereCast (transform.position, 2.0f, -steeringPoint, out rayHit, 2.5f, mask);
            bool hitContinue = Physics.SphereCast (transform.position, 2.0f, steeringPoint, out rayHit, 12.0f, mask);
            if (hitBack) {
                backing = true;
                newSpeed = -1f;
                if (m_Car.BrakeInput > 0 && m_Car.AccelInput <= 0) {
                    newSteer = -newSteer;
                }
                print ("back");

            }

            if (hitContinue && backing == true) {
                newSpeed = -1f;
                newSteer = -newSteer;
                print ("continue");
            } else {
                backing = false;
            }
            if (hitForward) {
                newSpeed = 1;
            }

            float breakDis = 2.5f*m_Car.CurrentSpeed;
            if(breakDis>Vector3.Distance(transform.position,pos)){
                newSpeed=-1/(breakDis-Vector3.Distance(transform.position,pos)*m_Car.CurrentSpeed);
            }


            Debug.DrawLine (transform.position, pos);

            m_Car.Move (newSteer, newSpeed, newSpeed, 0f);
        }

        private Vector3 getAvgPos () {
            Vector3 result = new Vector3 ();
            int counter = 0;
            foreach (GameObject g in friends) {
                if (g != null && g.transform != null) {
                    result.x += g.transform.position.x;
                    result.z += g.transform.position.z;
                    counter++;
                }
            }

            result.x /= counter;
            result.z /= counter;

            return result;
        }

        private int getIIndex (float posx) {
            return (int) ((posx - terrainInfo.x_low) / stepx);
        }

        private int getJIndex (float posz) {
            return (int) ((posz - terrainInfo.z_low) / stepz);
        }

        // get a 2d array with the summed up values of all enemies in the heat map
        private float[, ] sumUpHeatMap (float[, , ] maps) {
            float[, ] result = new float[maps.GetLength (1), maps.GetLength (2)];

            for (int i = 0; i < maps.GetLength (1); i++) {
                for (int j = 0; j < maps.GetLength (2); j++) {
                    for (int k = 0; k < maps.GetLength (0); k++) {
                        result[i, j] += maps[k, i, j];
                    }
                }
            }

            return result;
        }

        // create a square at the given position
        // return null if the square lies outside the map or if the position given lies within an object
        private Square getSquare (float x, float z) {
            int posx = getIIndex (x);
            int posz = getJIndex (z);
            int half = (SQUARE_SIZE - 1) / 2;

            if (posx < 0 || posx > newTerrain.GetLength (0) - 1 - half || posz < 0 || posz > newTerrain.GetLength (1) - 1 - half || newTerrain[posx, posz] != 0) {
                return null;
            }

            Square square = new Square (new float[SQUARE_SIZE, SQUARE_SIZE], terrainInfo.x_low + (posx * stepx), terrainInfo.z_low + (posz * stepz));
            float[, ] heatMap = sumUpHeatMap (vision);

            for (int i = SQUARE_SIZE - 1; i >= 0; i--) {
                for (int j = SQUARE_SIZE - 1; j >= 0; j--) {
                    square.setSquare (i, j, heatMap[posx + i - half, posz + j - half]);
                }
            }

            square.setCost (cost (square));
            return square;
        }

        private void drawSquare (Square square) {

            GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
            Collider col = cube.GetComponent<Collider> ();
            col.enabled = false;
            cube.transform.localScale = new Vector3 (1f, 0.5f, 1f);
            cube.transform.position = square.getPosition ();
        }

        // sums up the values of all cells in the given square
        private float cost (Square square) {
            float sum = 0;
            for (int i = 0; i < square.getSquare ().GetLength (0); i++) {
                for (int j = 0; j < square.getSquare ().GetLength (1); j++) {
                    sum += square.getSquare () [i, j];
                }
            }

            return sum;
        }

        // returns all possible successors from the given parent node
        private List<Node> getSuccessors (Node parent) {
            List<Node> result = new List<Node> ();
            Node node;
            float posx;
            float posz;
            float parent_cost;

            if (parent == null) {
                Vector3 pos = getAvgPos ();
                posx = pos.x;
                posz = pos.z;
                parent_cost = 0;
            } else {
                Vector3 pos = parent.getSquare ().getPosition ();
                posx = pos.x;
                posz = pos.z;
                parent_cost = parent.getSquare ().getCost ();
            }

            Square square = getSquare (posx - (SQUARE_SIZE * stepx), posz - (SQUARE_SIZE * stepz));
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx, posz - (SQUARE_SIZE * stepz));
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx + (SQUARE_SIZE * stepx), posz - (SQUARE_SIZE * stepz));
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx - (SQUARE_SIZE * stepx), posz);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx + (SQUARE_SIZE * stepx), posz);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx - (SQUARE_SIZE * stepx), posz + (SQUARE_SIZE * stepz));
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx, posz + (SQUARE_SIZE * stepz));
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx + (SQUARE_SIZE * stepx), posz + (SQUARE_SIZE * stepz));
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            return result;
        }

        public class Square {
            float posx;
            float posz;
            float[, ] square;
            float cost;

            public Square (float[, ] square, float posx, float posz) {
                this.square = square;
                this.posx = posx;
                this.posz = posz;
            }

            public void setSquare (int i, int j, float value) {
                this.square[i, j] = value;
            }

            public void setCost (float cost) {
                this.cost = cost;
            }

            public void increaseCost (float cost) {
                this.cost += cost;
            }

            public float getCost () {
                return this.cost;
            }

            public float[, ] getSquare () {
                return this.square;
            }

            public Vector3 getPosition () {
                return new Vector3 (this.posx, 0, this.posz);
            }
        }

        public class Node {
            Square square;
            Node parent;

            public Node (Square square, Node parent) {
                this.square = square;
                this.parent = parent;
            }

            public float getCost () {
                return this.square.getCost ();
            }

            public void setCost (float cost) {
                this.square.setCost (cost);
            }

            public Square getSquare () {
                return square;
            }
        }
    }
}