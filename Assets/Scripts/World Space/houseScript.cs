using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class houseScript : MonoBehaviour
{
    public GameObject zoneMarker;
    public Vector3 zoneOffset;
    public GameObject childPrefab;
    public Quaternion childRotation;
    List<GameObject> childrenList = new List<GameObject>();
    public List<Vector3> spawningSpots = new List<Vector3>();
    private GameObject marker;
    System.Random random = new System.Random();

    public void addChild() {
        if (childrenList.Count == 0)
        {
            marker = Instantiate(zoneMarker, transform.position + zoneOffset, Quaternion.identity, transform);
        }
        childrenList.Add(Instantiate(childPrefab, chooseSpawningSpot(), childRotation, transform));
    }

    Vector3 chooseSpawningSpot() {
        
        if (spawningSpots.Count != 0)
            return transform.position + spawningSpots[random.Next(0, spawningSpots.Count)];
        else {
            Debug.Log("Tried to spawn but no spawning point set");
            return new Vector3(0, 0, 0);
        }
    }

    public void removeChild () {
        for (int x = 0; x < childrenList.Count; x++)
            Destroy(childrenList[x]);
        
        Destroy(marker);
    }

}
