using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Graph {
    Dictionary<int, Node> nodes;
    Dictionary<int, List<int>> adjList;

    int size;

    int id_counter = 0;

    public Graph () {
        nodes = new Dictionary<int, Node> ();
        adjList = new Dictionary<int, List<int>> ();
        size = 0;
    }

    // The following constructor has parameters for two of the three 
    // properties. 

    public List<int> getEndNodes () {
        List<int> endNodes = new List<int> ();

        foreach (KeyValuePair<int, Node> pair in nodes) {
            if (adjList[pair.Key].Count > 1) {
                continue;
            } else {
                endNodes.Add (pair.Key);
            }
        }

        return endNodes;
    }

    public Dictionary<int, Node> getNodes () {
        return nodes;
    }
    public int getSize () {
        return size;
    }
    public int addNode (Node _newNode) {
        size++;
        int id = id_counter++;
        _newNode.setId (id);
        nodes.Add (id, _newNode);
        adjList.Add (id, new List<int> ());
        return id;
    }
    public int addNode (Node _newNode, List<int> _adjList) {
        size++;
        int id = id_counter++;
        nodes.Add (id, _newNode);
        adjList.Add (id, _adjList);
        return id;
    }
    public Node getNode (int _id) {
        return nodes[_id];
    }
    public List<int> getAdjList (int _id) {
        return adjList[_id];
    }
    public void setAdjList (int _id, List<int> _adjList) {
        adjList[_id] = _adjList;
    }
    public void addEdge (int _idA, int _idB) {
        List<int> actualList;

        actualList = adjList[_idA];
        if (!actualList.Contains (_idB)) {
            actualList.Add (_idB);
            setAdjList (_idA, actualList);
        }
        actualList = adjList[_idB];
        if (!actualList.Contains (_idA)) {
            actualList.Add (_idA);
            setAdjList (_idB, actualList);
        }
    }

    public void removeEdge (int _idA, int _idB) {
        List<int> listA = adjList[_idA];
        List<int> listB = adjList[_idB];

        if (listA.Contains (_idB) && listB.Contains (_idA)) {
            bool x = listA.Remove (_idB);
            bool y = listB.Remove (_idA);

            if (!x || !y) {
                UnityEngine.Debug.Log ("------------------- Could not remove item from list ---------------");
            }

            setAdjList (_idA, listA);
            setAdjList (_idB, listB);
        } else {
            UnityEngine.Debug.Log ("------------------- No edge to remove found ---------------");
        }
    }

    public Node getRandomNode () {
        return nodes[new System.Random ().Next (nodes.Count)];
    }
    /*public void setAllCost(float c){
        foreach (Node n in nodes){
            n.setCost(c);
        }
    }*/
}