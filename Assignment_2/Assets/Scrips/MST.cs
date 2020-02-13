using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MST {
    static Node[, ] terrainNodes;
    static float[, ] terrain;
    static bool[, ] terrainOccupied;
    static int x_N, z_N;
    static TerrainInfo terrainInfo;
    static int[, ] car_cell_positions;
    Orientation car_orientation = Orientation.NORTH;
    Graph g;
    int id;
    public MST (Node root, int id) {
        g = new Graph ();
        g.addNode (root);
        this.id = id;
    }

    public static Graph[] getSubgraphs (Node[, ] terrainNodes, TerrainInfo terrainInfo, GameObject[] cars, float[, ] terrain) {
        MST.terrainInfo = terrainInfo;
        MST.terrainNodes = terrainNodes;
        x_N = terrainNodes.GetLength (0);
        z_N = terrainNodes.GetLength (1);

        MST.terrain = terrain;

        terrainOccupied = new bool[x_N, z_N];
        for (int i = 0; i < x_N; i++) {
            for (int j = 0; j < z_N; j++) {
                if (terrain[i, j] == 1) {
                    terrainOccupied[i, j] = true;
                }
            }
        }

        Vector3[] positions = new Vector3[cars.Length];
        Node[] roots = new Node[cars.Length];
        MST[] trees = new MST[cars.Length];
        car_cell_positions = new int[cars.Length, 2];

        for (int i = 0; i < cars.Length; i++) {
            positions[i] = cars[i].transform.position;

            int x_i = get_i_index (positions[i].x);
            int z_j = get_j_index (positions[i].z);

            car_cell_positions[i, 0] = x_i;
            car_cell_positions[i, 1] = z_j;

            roots[i] = terrainNodes[x_i, z_j];
            roots[i].setParent (null);
            terrainOccupied[get_i_index (roots[i].getPositionX ()), get_j_index (roots[i].getPositionZ ())] = true;

            trees[i] = new MST (roots[i], i);
        }

        bool finished = false;
        bool[] allFail = new bool[cars.Length];

        while (!finished) {
            for (int i = 0; i < cars.Length; i++) {
                Node bestSuccessor = trees[i].getBestSuccessor ();
                if (bestSuccessor != null) {
                    Node parent = terrainNodes[car_cell_positions[i, 0], car_cell_positions[i, 1]];
                    bestSuccessor.setParent (parent);
                    int succ_id = trees[i].g.addNode (bestSuccessor);
                    trees[i].g.addEdge (parent.getId (), succ_id);

                    int old_x = car_cell_positions[i, 0];
                    int old_z = car_cell_positions[i, 1];
                    int new_x = get_i_index (bestSuccessor.getPositionX ());
                    int new_z = get_j_index (bestSuccessor.getPositionZ ());

                    terrainOccupied[new_x, new_z] = true;

                    car_cell_positions[i, 0] = new_x;
                    car_cell_positions[i, 1] = new_z;

                    trees[i].car_orientation = MST.getOrientation (old_x, old_z, new_x, new_z);
                } else {

                    bool update1 = true;
                    bool update2 = true;

                    while (update1 || update2) {
                        update1 = trees[i].hilling_1 ();
                        update2 = trees[i].hilling_2 ();
                    }

                    Node current = trees[i].g.getRandomNode ();
                    Node parent;
                    bool loop = true;
                    int iter = terrain.GetLength (0) * terrain.GetLength (1);

                    while (loop && iter > 0) {
                        iter--;

                        car_cell_positions[i, 0] = get_i_index (current.getPositionX ());
                        car_cell_positions[i, 1] = get_j_index (current.getPositionZ ());
                        List<Node> successors = trees[i].getSuccessors (car_cell_positions[i, 0], car_cell_positions[i, 1], false);
                        if (successors.Count > 0) {
                            loop = false;

                            Node n = successors[new System.Random ().Next (successors.Count)];
                            car_cell_positions[i, 0] = get_i_index (n.getPositionX ());
                            car_cell_positions[i, 1] = get_j_index (n.getPositionZ ());
                            trees[i].car_orientation = getOrientation (
                                current.getPositionX (), current.getPositionZ (),
                                n.getPositionX (), n.getPositionZ ());

                            n.setParent (current);
                            int succ_id = trees[i].g.addNode (n);
                            trees[i].g.addEdge (current.getId (), succ_id);

                            terrainOccupied[car_cell_positions[i, 0], car_cell_positions[i, 1]] = true;
                            break;
                        } else {
                            current = trees[i].g.getRandomNode ();
                        }

                    }

                    bool allOccupied = true;
                    for (int k = 0; k < terrainOccupied.GetLength (0); k++) {
                        for (int l = 0; l < terrainOccupied.GetLength (1); l++) {
                            if (!terrainOccupied[k, l]) {
                                allOccupied = false;
                            }
                        }
                    }

                    finished = allOccupied;
                }
            }
        }

        Graph[] graphs = new Graph[trees.Length];
        for (int i = 0; i < trees.Length; i++) {
            graphs[i] = trees[i].g;
        }

        return graphs;
    }

    public Node getBestSuccessor () {
        int x = car_cell_positions[this.id, 0];
        int z = car_cell_positions[this.id, 1];

        List<Node> successors = getSuccessors (x, z, true);

        if (successors.Count == 0) {
            return null;
        }

        Node bestNode = null;
        float minDistance = float.MaxValue;
        float maxMinDistance = float.MinValue;
        foreach (Node n in successors) {
            for (int i = 0; i < car_cell_positions.GetLength (0); i++) {
                if (this.id == i) {
                    continue;
                } else {
                    float tmp = getManhattanDistance (n.getPosition (),
                        new Vector3 (get_x_pos (car_cell_positions[i, 0]), 0, get_z_pos (car_cell_positions[i, 1])));
                    if (tmp < minDistance) {
                        minDistance = tmp;
                    }
                }
            }

            if (minDistance > maxMinDistance) {
                maxMinDistance = minDistance;
                bestNode = n;
            }
        }

        return bestNode;
    }

    public List<Node> getSuccessors (int i, int j, bool with_orientation) {
        List<Node> successors = new List<Node> ();
        if (with_orientation) {
            switch (car_orientation) {
                case Orientation.NORTH:
                    if (i < terrainNodes.GetLength (0) - 1 && !terrainOccupied[i + 1, j]) {
                        successors.Add (terrainNodes[i + 1, j]);
                    }

                    if (i > 0 && !terrainOccupied[i - 1, j]) {
                        successors.Add (terrainNodes[i - 1, j]);
                    }

                    if (j < terrainNodes.GetLength (1) - 1 && !terrainOccupied[i, j + 1]) {
                        successors.Add (terrainNodes[i, j + 1]);
                    }

                    break;
                case Orientation.SOUTH:
                    if (i < terrainNodes.GetLength (0) - 1 && !terrainOccupied[i + 1, j]) {
                        successors.Add (terrainNodes[i + 1, j]);
                    }

                    if (i > 0 && !terrainOccupied[i - 1, j]) {
                        successors.Add (terrainNodes[i - 1, j]);
                    }

                    if (j > 0 && !terrainOccupied[i, j - 1]) {
                        successors.Add (terrainNodes[i, j - 1]);
                    }
                    break;
                case Orientation.EAST:
                    if (i < terrainNodes.GetLength (0) - 1 && !terrainOccupied[i + 1, j]) {
                        successors.Add (terrainNodes[i + 1, j]);
                    }

                    if (j < terrainNodes.GetLength (1) - 1 && !terrainOccupied[i, j + 1]) {
                        successors.Add (terrainNodes[i, j + 1]);
                    }

                    if (j > 0 && !terrainOccupied[i, j - 1]) {
                        successors.Add (terrainNodes[i, j - 1]);
                    }
                    break;
                case Orientation.WEST:
                    if (i > 0 && !terrainOccupied[i - 1, j]) {
                        successors.Add (terrainNodes[i - 1, j]);
                    }

                    if (j < terrainNodes.GetLength (1) - 1 && !terrainOccupied[i, j + 1]) {
                        successors.Add (terrainNodes[i, j + 1]);
                    }

                    if (j > 0 && !terrainOccupied[i, j - 1]) {
                        successors.Add (terrainNodes[i, j - 1]);
                    }
                    break;
            }
        } else {
            if (i < terrainNodes.GetLength (0) - 1 && !terrainOccupied[i + 1, j]) {
                successors.Add (terrainNodes[i + 1, j]);
            } else if (j < terrainNodes.GetLength (1) - 1 && !terrainOccupied[i, j + 1]) {
                successors.Add (terrainNodes[i, j + 1]);
            } else if (j > 0 && !terrainOccupied[i, j - 1]) {
                successors.Add (terrainNodes[i, j - 1]);
            } else if (i > 0 && !terrainOccupied[i - 1, j]) {
                successors.Add (terrainNodes[i - 1, j]);
            }
        }

        return successors;
    }

    public bool hilling_1 () {
        Node n1;
        Node n2;

        Node adj1 = null;
        Node adj2 = null;
        int x_i1, x_i2, z_j1, z_j2;

        bool update = false;

        List<int> leaves = this.g.getEndNodes ();

        n2 = this.g.getNode (leaves[new System.Random ().Next (leaves.Count)]);

        for (int i = 0; i < this.g.getSize () - 1; i++) {
            n1 = n2;
            n2 = n1.getParent ();

            if (n1 == null || n2 == null) {
                return update;
            }

            x_i1 = MST.get_i_index (n1.getPositionX ());
            x_i2 = MST.get_i_index (n2.getPositionX ());
            z_j1 = MST.get_j_index (n1.getPositionZ ());
            z_j2 = MST.get_j_index (n2.getPositionZ ());

            switch (MST.getOrientation (n1.getPositionX (), n1.getPositionZ (), n2.getPositionX (), n2.getPositionZ ())) {
                case Orientation.SOUTH:
                    if (x_i1 > 0 && x_i2 > 0) {
                        adj1 = terrainNodes[x_i1 - 1, z_j1];
                        adj2 = terrainNodes[x_i2 - 1, z_j2];
                    }
                    break;
                case Orientation.NORTH:
                    if (x_i1 < terrainNodes.GetLength (0) - 1 && x_i2 < terrainNodes.GetLength (0) - 1) {
                        adj1 = terrainNodes[x_i1 + 1, z_j1];
                        adj2 = terrainNodes[x_i2 + 1, z_j2];
                    }

                    break;
                case Orientation.WEST:
                    if (z_j1 < terrainNodes.GetLength (1) - 1 && z_j2 < terrainNodes.GetLength (1) - 1) {
                        adj1 = terrainNodes[x_i1, z_j1 + 1];
                        adj2 = terrainNodes[x_i2, z_j2 + 1];
                    }

                    break;
                case Orientation.EAST:
                    if (z_j1 > 0 && z_j2 > 0) {
                        adj1 = terrainNodes[x_i1, z_j1 - 1];
                        adj2 = terrainNodes[x_i2, z_j2 - 1];
                    }
                    break;
            }

            if (adj1 == null || adj2 == null ||
                terrainOccupied[get_i_index (adj1.getPositionX ()), get_j_index (adj1.getPositionZ ())] ||
                terrainOccupied[get_i_index (adj2.getPositionX ()), get_j_index (adj2.getPositionZ ())]) {
                continue;
            } else {
                adj1.setParent (n1);
                this.g.addNode (adj1);
                adj2.setParent (adj1);
                this.g.addNode (adj2);

                n2.setParent (adj2);

                terrainOccupied[get_i_index (adj1.getPositionX ()), get_j_index (adj1.getPositionZ ())] = true;
                terrainOccupied[get_i_index (adj2.getPositionX ()), get_j_index (adj2.getPositionZ ())] = true;

                this.g.addEdge (n1.getId (), adj1.getId ());
                this.g.addEdge (adj1.getId (), adj2.getId ());
                this.g.addEdge (adj2.getId (), n2.getId ());

                this.g.removeEdge (n1.getId (), n2.getId ());

                update = true;
            }

        }

        return update;
    }
    public bool hilling_2 () {
        Node n1, n2;
        Node adj1 = null;
        Node adj2 = null;
        int x_i1, x_i2, z_j1, z_j2;

        List<int> leaves = this.g.getEndNodes ();

        n2 = this.g.getNode (leaves[new System.Random ().Next (leaves.Count)]);

        bool update = false;

        for (int i = this.g.getSize () - 1; i > 0; i--) {
            n1 = n2;
            n2 = n1.getParent ();

            if (n1 == null || n2 == null) {
                return update;
            }

            x_i1 = MST.get_i_index (n1.getPositionX ());
            x_i2 = MST.get_i_index (n2.getPositionX ());
            z_j1 = MST.get_j_index (n1.getPositionZ ());
            z_j2 = MST.get_j_index (n2.getPositionZ ());

            switch (MST.getOrientation (n1.getPositionX (), n1.getPositionZ (), n2.getPositionX (), n2.getPositionZ ())) {
                case Orientation.NORTH:
                    if (x_i1 > 0 && x_i2 > 0) {
                        adj1 = terrainNodes[x_i1 - 1, z_j1];
                        adj2 = terrainNodes[x_i2 - 1, z_j2];
                    }
                    break;
                case Orientation.SOUTH:
                    if (x_i1 < terrainNodes.GetLength (0) - 1 && x_i2 < terrainNodes.GetLength (0) - 1) {
                        adj1 = terrainNodes[x_i1 + 1, z_j1];
                        adj2 = terrainNodes[x_i2 + 1, z_j2];
                    }
                    break;
                case Orientation.EAST:
                    if (z_j1 < terrainNodes.GetLength (1) - 1 && z_j2 < terrainNodes.GetLength (1) - 1) {
                        adj1 = terrainNodes[x_i1, z_j1 + 1];
                        adj2 = terrainNodes[x_i2, z_j2 + 1];
                    }
                    break;
                case Orientation.WEST:
                    if (z_j1 > 0 && z_j2 > 0) {
                        adj1 = terrainNodes[x_i1, z_j1 - 1];
                        adj2 = terrainNodes[x_i2, z_j2 - 1];
                    }
                    break;
            }

            if (adj1 == null || adj2 == null ||
                terrainOccupied[get_i_index (adj1.getPositionX ()), get_j_index (adj1.getPositionZ ())] ||
                terrainOccupied[get_i_index (adj2.getPositionX ()), get_j_index (adj2.getPositionZ ())]) {
                continue;
            } else {
                adj1.setParent (n1);
                this.g.addNode (adj1);
                adj2.setParent (adj1);
                this.g.addNode (adj2);

                n2.setParent (adj2);

                terrainOccupied[get_i_index (adj1.getPositionX ()), get_j_index (adj1.getPositionZ ())] = true;
                terrainOccupied[get_i_index (adj2.getPositionX ()), get_j_index (adj2.getPositionZ ())] = true;

                this.g.addEdge (n1.getId (), adj1.getId ());
                this.g.addEdge (adj1.getId (), adj2.getId ());
                this.g.addEdge (adj2.getId (), n2.getId ());

                this.g.removeEdge (n1.getId (), n2.getId ());

                update = true;
            }
        }
        return update;
    }

    public static Orientation getOrientation (float old_x, float old_z, float new_x, float new_z) {
        if (old_x - new_x < 0) {
            return Orientation.EAST;
        } else if (old_x - new_x > 0) {
            return Orientation.WEST;
        } else if (old_z - new_z > 0) {
            return Orientation.SOUTH;
        } else {
            return Orientation.NORTH;
        }
    }

    public static int get_i_index (float x) {
        int index = (int) Mathf.Floor (x_N * (x - terrainInfo.x_low) / (terrainInfo.x_high - terrainInfo.x_low));
        if (index < 0) {
            index = 0;
        } else if (index > x_N - 1) {
            index = x_N - 1;
        }
        return index;
    }
    public static int get_j_index (float z) {
        int index = (int) Mathf.Floor (z_N * (z - terrainInfo.z_low) / (terrainInfo.z_high - terrainInfo.z_low));
        if (index < 0) {
            index = 0;
        } else if (index > z_N - 1) {
            index = z_N - 1;
        }

        return index;
    }

    public static float get_x_pos (int i) {
        float step = (terrainInfo.x_high - terrainInfo.x_low) / x_N;
        return terrainInfo.x_low + step / 2 + step * i;
    }

    public static float get_z_pos (int j) {
        float step = (terrainInfo.z_high - terrainInfo.z_low) / z_N;
        return terrainInfo.z_low + step / 2 + step * j;
    }

    public float getManhattanDistance (Vector3 v1, Vector3 v2) {
        return Mathf.Abs (v1.x - v2.x) + Mathf.Abs (v1.z - v2.z);
    }
}

public enum Orientation {
    NORTH,
    SOUTH,
    EAST,
    WEST
}