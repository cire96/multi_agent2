using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Vehicles.Car {
    [RequireComponent (typeof (CarController))]
    public class CarAI1 : MonoBehaviour {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        public int ID;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;

        public GameObject pathPlanner;
        private PathPlanning planning;

        Graph tree;
        int tragetNodeId = 0;
        int lastNodeId = -1;
        Orientation bestOreint = Orientation.NONE;

        private void Start () {
            // get the car controller
            m_Car = GetComponent<CarController> ();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager> ();
            planning = pathPlanner.GetComponent<PathPlanning> ();
            TerrainInfo terrainInfo = terrain_manager.myInfo;
            float tileXSize = (terrainInfo.x_high - terrainInfo.x_low) / terrainInfo.x_N;
            float tileZSize = (terrainInfo.z_high - terrainInfo.z_low) / terrainInfo.z_N;

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

            Node[, ] terrainNodes = new Node[newTerrain.GetLength (0), newTerrain.GetLength (1)];
            stepx = (terrainInfo.x_high - terrainInfo.x_low) / newTerrain.GetLength (0);
            stepz = (terrainInfo.z_high - terrainInfo.z_low) / newTerrain.GetLength (1);
            for (int i = 0; i < newTerrain.GetLength (0); i++) {
                float posx = terrainInfo.x_low + stepx / 2 + stepx * i;
                for (int j = 0; j < newTerrain.GetLength (1); j++) {
                    float posz = terrainInfo.z_low + stepz / 2 + stepz * j;
                    GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
                    Collider c = cube.GetComponent<Collider> ();
                    c.enabled = false;
                    cube.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
                    cube.transform.position = new Vector3 (posx, 0, posz);
                    terrainNodes[i, j] = new Node (posx, posz);
                }
            }

            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work

            friends = GameObject.FindGameObjectsWithTag ("Player");
            tree = planning.subtrees[ID];

            // Note that you are not allowed to check the positions of the turrets in this problem

            // Plan your path here
            // ...

        }

        private void getNextTargetPoint (Node currentNode, Node nextNode) {
            Orientation currentOrientation = MST.getOrientation (
                currentNode.getPositionX (), currentNode.getPositionZ (),
                nextNode.getPositionX (), nextNode.getPositionZ ());

        }

        private void FixedUpdate () {

            enemies = GameObject.FindGameObjectsWithTag ("Enemy");
            float xNext;
            float zNext;
            float zCurrent = tree.getNode (tragetNodeId).getPositionZ ();
            float xCurrent = tree.getNode (tragetNodeId).getPositionX ();
            int bestNodeId = 0;
            bestOreint = Orientation.NONE;

            float x_offset = 0;
            float z_offset = 0;

            //Always go to the left, figure out best next node
            if (Vector3.Distance (tree.getNode (tragetNodeId).getPosition (), transform.position) < 3.0f) {
                if (lastNodeId == -1) {
                    foreach (int id in tree.getAdjList (tragetNodeId)) {
                        xNext = tree.getNode (id).getPositionX ();
                        zNext = tree.getNode (id).getPositionZ ();

                        bestOreint = Orientation.NORTH;
                        Orientation nextOrientation = MST.getOrientation (xCurrent, zCurrent, xNext, zNext);
                        switch (nextOrientation) {
                            case Orientation.SOUTH:
                                if (bestOreint == Orientation.NONE) {
                                    bestNodeId = id;
                                    bestOreint = nextOrientation;

                                    x_offset = 10;
                                    z_offset = 0;
                                }
                                break;
                            case Orientation.EAST:
                                if (bestOreint == Orientation.NONE || bestOreint == Orientation.SOUTH) {
                                    bestNodeId = id;
                                    bestOreint = nextOrientation;

                                    x_offset = 0;
                                    z_offset = 10;
                                }
                                break;
                            case Orientation.NORTH:
                                if (bestOreint == Orientation.NONE || bestOreint == Orientation.EAST || bestOreint == Orientation.SOUTH) {
                                    bestNodeId = id;
                                    bestOreint = nextOrientation;

                                    x_offset = -10;
                                    z_offset = 0;
                                }
                                break;
                            case Orientation.WEST:
                                bestNodeId = id;
                                bestOreint = nextOrientation;

                                x_offset = 0;
                                z_offset = -10;
                                break;
                        }
                        tragetNodeId = bestNodeId;
                    }
                } else {
                    float xLast = tree.getNode (lastNodeId).getPositionX ();
                    float zLast = tree.getNode (lastNodeId).getPositionZ ();

                    Node nextNode = getNextNode (tree.getNode (tragetNodeId), MST.getOrientation (xLast, zLast, xCurrent, zCurrent));
                    Node nextNextNode = getNextNode (nextNode, MST.getOrientation (xCurrent, zCurrent, nextNode.getPositionX (), nextNode.getPositionZ ()));

                    bestNodeId = nextNode.getId ();
                }

                lastNodeId = tragetNodeId;
                tragetNodeId = bestNodeId;
            }

            Vector3 target = tree.getNode (tragetNodeId).getPosition ();
            target[0] += x_offset;
            target[2] += z_offset;
            Vector3 carToTarget = m_Car.transform.InverseTransformPoint (target);
            float newSteer = (carToTarget.x / carToTarget.magnitude);
            float newSpeed = (carToTarget.z / carToTarget.magnitude);
            float handBreak = 0f;

            // Execute your path here
            // ...

            Vector3 avg_pos = Vector3.zero;

            foreach (GameObject friend in friends) {
                avg_pos += friend.transform.position;
            }
            avg_pos = avg_pos / friends.Length;
            Vector3 direction = (avg_pos - transform.position).normalized;

            bool is_to_the_right = Vector3.Dot (direction, transform.right) > 0f;
            bool is_to_the_front = Vector3.Dot (direction, transform.forward) > 0f;

            float steering = 0f;
            float acceleration = 0;

            if (is_to_the_right && is_to_the_front) {
                steering = 1f;
                acceleration = 1f;
            } else if (is_to_the_right && !is_to_the_front) {
                steering = -1f;
                acceleration = -1f;
            } else if (!is_to_the_right && is_to_the_front) {
                steering = -1f;
                acceleration = 1f;
            } else if (!is_to_the_right && !is_to_the_front) {
                steering = 1f;
                acceleration = -1f;
            }

            // this is how you access information about the terrain
            int i = terrain_manager.myInfo.get_i_index (transform.position.x);
            int j = terrain_manager.myInfo.get_j_index (transform.position.z);
            float grid_center_x = terrain_manager.myInfo.get_x_pos (i);
            float grid_center_z = terrain_manager.myInfo.get_z_pos (j);

            Debug.DrawLine (transform.position, target);

            // this is how you control the car
            //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
            m_Car.Move (newSteer, newSpeed, newSpeed, 0f);
            //m_Car.Move(0f, -1f, 1f, 0f);

        }

        private Node getNextNode (Node currentNode, Orientation currentOrientation) {
            int bestNodeId = -1;
            Orientation bestOrient = Orientation.NONE;
            List<int> adjList = tree.getAdjList (currentNode.getId ());
            foreach (int id in adjList) {
                Node n = tree.getNode (id);
                Orientation nextOrientation = MST.getOrientation (currentNode.getPositionX (), currentNode.getPositionZ (), n.getPositionX (), n.getPositionZ ());
                if (currentOrientation == Orientation.NORTH) {
                    switch (nextOrientation) {
                        case Orientation.SOUTH:
                            if (bestOrient == Orientation.NONE) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.EAST:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.SOUTH) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.NORTH:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.EAST || bestOrient == Orientation.SOUTH) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.WEST:
                            bestNodeId = id;
                            bestOrient = nextOrientation;
                            break;
                    }
                } else if (currentOrientation == Orientation.EAST) {
                    switch (nextOrientation) {
                        case Orientation.WEST:
                            if (bestOrient == Orientation.NONE) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.SOUTH:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.WEST) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.EAST:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.SOUTH || bestOrient == Orientation.WEST) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.NORTH:
                            bestNodeId = id;
                            bestOrient = nextOrientation;
                            break;
                    }
                } else if (currentOrientation == Orientation.SOUTH) {
                    switch (nextOrientation) {
                        case Orientation.NORTH:
                            if (bestOrient == Orientation.NONE) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.WEST:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.NORTH) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.SOUTH:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.WEST || bestOrient == Orientation.NORTH) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.EAST:
                            bestNodeId = id;
                            bestOrient = nextOrientation;
                            break;
                    }
                } else {
                    switch (nextOrientation) {
                        case Orientation.EAST:
                            if (bestOrient == Orientation.NONE) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.NORTH:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.EAST) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.WEST:
                            if (bestOrient == Orientation.NONE || bestOrient == Orientation.NORTH | bestOrient == Orientation.EAST) {
                                bestNodeId = id;
                                bestOrient = nextOrientation;
                            }
                            break;
                        case Orientation.SOUTH:
                            bestNodeId = id;
                            bestOrient = nextOrientation;
                            break;
                    }
                }
            }

            return tree.getNode (bestNodeId);
        }
    }
}