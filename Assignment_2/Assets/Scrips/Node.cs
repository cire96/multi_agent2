using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Node{
    private int id;
    private Vector3 position;
    private float x, z;
    private int parent;

    
    public int getParent()
    {
        return parent;
    }
    public void setParent(int p)
    {
        parent = p;
    }
    public Vector3 getPosition()
    {
        return position;
    }

    public float getPositionX()
    {
        return position.x;
    }
    public float getPositionZ()
    {
        return position.z;
    }
    public int getId()
    {
        return id;
    }
    public Node(float _x, float _z)
    {
        x = _x;
        z = _z;
        position = new Vector3(_x, 0, _z);
        id = -1;
    }
    public Node(Vector3 _position)
    {
        position = _position;
        id = -1;
    }
    public Node()
    {
        x = 0;
        z = 0;
        id = -1;
    }
    public void setId(int _id)
    {
        id = _id;
    }
    public void setPositionX(float _x)
    {
        x = _x;
        position.x = _x;
    }
    public void setPositionZ(float _z)
    {
        z = _z;
        position.z = _z;
    }
}