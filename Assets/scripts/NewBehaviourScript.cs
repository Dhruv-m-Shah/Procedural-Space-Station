using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject fourWayJunction;
    public GameObject pipe;
    List<GameObject> healthPackClones = new List<GameObject>();
    List<GameObject> visitedPipes = new List<GameObject>();

    public bool checkVisited(Vector3 location, int rotation) 
    {
        for(int i = 0; i < visitedPipes.Count(); i++)
        {
            if(visitedPipes[i].transform.position.x == location.x && visitedPipes[i].transform.position.z == location.z)
            {
                Destroy(visitedPipes[i]);
                visitedPipes.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public void AddPipeFront(Vector3 junction)
    {
        if(checkVisited(new Vector3(junction.x + 3.5f, junction.y, junction.z), 90))
        {
            Instantiate(fourWayJunction, new Vector3(junction.x + 3.5f, junction.y, junction.z), Quaternion.identity);
        }
        else
        {
            visitedPipes.Add(Instantiate(pipe, new Vector3(junction.x + 3.5f, junction.y, junction.z), Quaternion.Euler(270, 0, 90)));
        }
       
    }
    public void AddPipeBack(Vector3 junction)
    {
        if (checkVisited(new Vector3(junction.x - 3.5f, junction.y, junction.z), 90))
        {
            Instantiate(fourWayJunction, new Vector3(junction.x - 3.5f, junction.y, junction.z), Quaternion.identity);
        }
        else
        {
            visitedPipes.Add(Instantiate(pipe, new Vector3(junction.x - 3.5f, junction.y, junction.z), Quaternion.Euler(270, 0, 90)));
        }       
    }
    public void AddPipeRight(Vector3 junction)
    {
        if (checkVisited(new Vector3(junction.x, junction.y, junction.z - 3.5f), 0)) 
        {
            Instantiate(fourWayJunction, new Vector3(junction.x, junction.y, junction.z - 3.5f), Quaternion.identity);
        }
        else
        {
            visitedPipes.Add(Instantiate(pipe, new Vector3(junction.x, junction.y, junction.z - 3.5f), Quaternion.Euler(270, 0, 0)));
        }

    }
    public void AddPipeLeft(Vector3 junction)
    {
        if (checkVisited(new Vector3(junction.x, junction.y, junction.z + 3.5f), 0))
        {
            Instantiate(fourWayJunction, new Vector3(junction.x, junction.y, junction.z + 3.5f), Quaternion.identity);
        }
        else
        {
            visitedPipes.Add(Instantiate(pipe, new Vector3(junction.x, junction.y, junction.z + 3.5f), Quaternion.Euler(270, 0, 0)));
        }
    }

    public void ConnectTheDots(Vector3 node, Vector3 node1)   
    {
        Vector3 x;
        Vector3 z;
        if(node.x < node1.x)
        {
            x = node;
            while (x.x < node1.x - 3.5)
            {
                AddPipeFront(x);
                x.x += 3.5f;
            }
        }

        if (node.x > node1.x)
        {
            x = node;
            while (x.x > node1.x + 3.5)
            {
                AddPipeBack(x);
                x.x -= 3.5f;
            }
        }

        if (node1.z < node.z)
        {
            z = node1;
            while (z.z < node.z - 3.5)
            {
                AddPipeLeft(z);
                z.z += 3.5f;
            }
        }

        if (node1.z > node.z)
        {
            z = node1;
            while (z.z > node.z + 3.5)
            {
                AddPipeRight(z);
                z.z -= 3.5f;
            }
        }

        Instantiate(fourWayJunction, new Vector3(node1.x, 0, node.z), Quaternion.identity); // Add a four way junction to connect pipes.


    }
/*
    public bool renderCheck(Vector3 tunnelPiece) 
    {
        private float renderDist = 10.5;

        private float playerX = GameObject.Find("Main Camera").transform.position.x;
        private float playerY = GameObject.Find("Main Camera").transform.position.y;
        private float pieceX = GameObject.Find("tunnelPiece").transform.position.x;
        private float pieceY = GameObject.Find("tunnelPiece").transform.position.y;
        private float pieceDist = sqrt(abs(((pieceX - playerX) ** 2) + ((pieceY - playerY) ** 2)));

        private bool render = true;

        if (pieceDist > renderDist) 
        {
            render = false;
        }

        return render;
    }
*/
    void Start()
    {


        healthPackClones.Add(Instantiate(fourWayJunction, new Vector3(0, 0, 0), Quaternion.identity));
        for (int i = 0; i < 30; i++) {
            float x;
            float y;
            while (true)
            {
                 x = (float)UnityEngine.Random.Range(-100, 101);
                 y = (float)UnityEngine.Random.Range(-100, 101);
                int marker = 0;
                for(int j = 0; j < healthPackClones.Count(); j++)
                {
                    if(healthPackClones[j].transform.position.x == x * 3.5f || healthPackClones[j].transform.position.y == y * 3.5f)
                    {
                        marker = 1;
                    }
                }
                if(marker != 1)
                {
                    break;
                }
            }
            healthPackClones.Add(Instantiate(fourWayJunction, new Vector3(x * 3.5f, 0, y * 3.5f), Quaternion.identity)); 
        }
        for(int i = 0; i < healthPackClones.Count() - 1; i++)
        {
            ConnectTheDots(healthPackClones[i].transform.position, healthPackClones[i + 1].transform.position);
        }


    }

    // Update is called once per frame
    void Update()
    { 
        float cameraX;
        float cameraY;
        for (int j = 0; j < visitedPipes.Count(); j++)
        {
            cameraX = GameObject.Find("Main Camera").transform.position.x;
            cameraY = GameObject.Find("Main Camera").transform.position.y;
            if (Math.Pow(Math.Pow(cameraX - visitedPipes[j].transform.position.x, 2) + Math.Pow(cameraY - visitedPipes[j].transform.position.y, 2), 0.5) > 10.5f)
            {
                visitedPipes[j].GetComponent<Renderer>().enabled = false;
            }
            else
            {
                visitedPipes[j].GetComponent<MeshRenderer>().enabled = true;
            }
        }
    }}