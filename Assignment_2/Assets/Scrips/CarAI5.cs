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

        private int SQUARE_SIZE = 5;
        private float[, ] newTerrain;
        private float[, , ] vision;
        private List<Node> leafs;
        private float stepx;
        private float stepz;
        private TerrainInfo terrainInfo;
        private Vector3 target;

        private void Start () {
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

            vision = new float[enemies.Length, newTerrain.GetLength (0), newTerrain.GetLength (1)];
            int enemy_nr = 0;
            foreach (GameObject enemy in enemies) {
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

            leafs = getSuccessors (null);
            target = getAvgPos ();

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

        private void FixedUpdate () {
            foreach (Node leaf in leafs) {
                if (leaf.getCost () == SQUARE_SIZE) {
                    target = leaf.getSquare ().getPosition ();
                }

                Debug.DrawLine (getAvgPos (), leaf.getSquare ().getPosition ());
            }

            print (leafs.Count);

            m_Car.Move (0f, 1f, 1f, 0f);
        }

        private Vector3 getAvgPos () {
            Vector3 result = new Vector3 ();

            foreach (GameObject g in friends) {
                result.x += g.transform.position.x;
                result.z += g.transform.position.z;
            }

            result.x /= friends.Length;
            result.z /= friends.Length;

            return result;
        }

        private int getIIndex (float posx) {
            return (int) ((posx - terrainInfo.x_low) / stepx);
        }

        private int getJIndex (float posz) {
            return (int) ((posz - terrainInfo.z_low) / stepz);
        }

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

        private Square getSquare (float x, float z) {
            int posx = getIIndex (x);
            int posz = getJIndex (z);

            if (posx < 0 || posx > newTerrain.GetLength (0) - 1 || posz < 0 || posz > newTerrain.GetLength (1) - 1) {
                return null;
            }

            Square square = new Square (new float[SQUARE_SIZE, SQUARE_SIZE], x, z);
            int half = (SQUARE_SIZE - 1) / 2;
            float[, ] heatMap = sumUpHeatMap (vision);

            for (int i = SQUARE_SIZE - 1; i >= 0; i--) {
                for (int j = SQUARE_SIZE - 1; j >= 0; j--) {
                    if (newTerrain[posx + i - half, posz + j - half] == 0) {
                        square.setSquare (i, j, heatMap[posx + i - half, posz + j - half]);
                    } else {
                        return null;
                    }
                }
            }

            square.setCost (cost (square));
            return square;
        }

        private float cost (Square square) {
            float sum = 0;
            for (int i = 0; i < square.getSquare ().GetLength (0); i++) {
                for (int j = 0; j < square.getSquare ().GetLength (1); j++) {
                    sum += square.getSquare () [i, j];
                }
            }

            return sum;
        }

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

            Square square = getSquare (posx - SQUARE_SIZE, posz - SQUARE_SIZE);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx, posz - SQUARE_SIZE);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx + SQUARE_SIZE, posz - SQUARE_SIZE);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx - SQUARE_SIZE, posz);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx + SQUARE_SIZE, posz);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx - SQUARE_SIZE, posz + SQUARE_SIZE);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx, posz + SQUARE_SIZE);
            if (square != null) {
                square.increaseCost (parent_cost);
                node = new Node (square, parent);
                result.Add (node);
            }

            square = getSquare (posx + SQUARE_SIZE, posz + SQUARE_SIZE);
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
                return new Vector3 (this.posx, this.posz);
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

            public Square getSquare () {
                return square;
            }
        }
    }
}