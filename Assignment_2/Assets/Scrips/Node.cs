using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Node{
    private Color[] colors = {Color.white,Color.blue,Color.red,Color.green,Color.cyan,Color.yellow,Color.black,Color.magenta,Color.grey,
    new Color(0.3f, 0.4f, 0.6f, 1.0f), new Color(1.00f,0.49f,0.00f, 1.0f), new Color(0.00f,1.00f,0.84f, 1.0f), new Color(1.00f,0.50f,0.84f, 1.0f),
    new Color(0.40f,0.27f,0.00f, 1.0f), new Color(0.66f, 0.0f, 0.0f, 1.0f),new Color(0.00f,0.49f,0.20f, 1.0f)};
    private int id;
    private Vector3 position;
    private float x, z;
    private Node parent;
    private int color;
    private GameObject cube;
    //private float Cost;

    
    public Node getParent()
    {
        return parent;
    }
    public void setParent(Node p)
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

    public int getColor(){
        return color;
    }
    public void setColor(int _color){
        color = _color;
        cube.GetComponent<Renderer>().material.color = colors[_color];

    }
    public Node(float _x, float _z)
    {
        x = _x;
        z = _z;
        position = new Vector3(_x, 0, _z);
        id = -1;
        color = 0;
    }
    public Node(Vector3 _position)
    {
        position = _position;
        id = -1;
        color = 0;
    }
    public Node()
    {
        x = 0;
        z = 0;
        id = -1;
        color = 0;
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
    public void setCube(GameObject _c){
        cube = _c;
        Collider c = cube.GetComponent<Collider> ();
        c.enabled = false;
        cube.transform.localScale = new Vector3 (1.5f, 1.5f, 1.5f);
        cube.transform.position = new Vector3 (x, 0, z);
                    
    }
    public GameObject getCube(){
        return cube;
    }
    
    
    /*public void setCost(float c){
        cost=c;
    }
    public float getCost(){
        return cost;
    }*/
}