using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Graph{
    Dictionary<int, Node> nodes;
    Dictionary<int, List<int>> adjList;
    List<int> endNodes;

    int size;

    public Graph() {
        nodes =  new Dictionary<int, Node>();
        adjList =  new Dictionary<int, List<int>>();
        endNodes = new List<int>();
        size = 0;
    }

    // The following constructor has parameters for two of the three 
    // properties. 

    public Dictionary<int, Node> getNodes()
    {
        return nodes;
    }
    public int getSize()
    {
        return size;
    }
    public int addNode(Node _newNode)
    {
        int id = size++;
        _newNode.setId(id);
        nodes.Add(id, _newNode);
        adjList.Add(id, new List<int>());
        return id;
    }
    public int addNode(Node _newNode, List<int> _adjList)
    {
        int id = size++;
        nodes.Add(id, _newNode);
        adjList.Add(id, _adjList);
        return id;
    }
    public Node getNode(int _id)
    {
        return nodes[_id];
    }
    public List<int> getAdjList(int _id)
    {
        return adjList[_id];
    }
    public void setAdjList(int _id, List<int> _adjList)
    {
        adjList[_id]= _adjList;
    }
    public void addEdge(int _idA, int _idB)
    {
        List<int> actualList;

        actualList = adjList[_idA];
        if (!actualList.Contains(_idB)) { 
            actualList.Add(_idB);
            setAdjList(_idA, actualList);
        }
        actualList = adjList[_idB];
        if (!actualList.Contains(_idA))
        {
            actualList.Add(_idA);
            setAdjList(_idB, actualList);
        }
    }
}