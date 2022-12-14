using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class CityGenerator : MonoBehaviour
{

    public RawImage debugImg;
    public GameObject busObject; //should be set in the editor

    //stats (cannot be tweaked in the editor must be tweaked in script)
    public float tileSize {get;} = 6; //the size of tiles in the gameWorld
    public int defaultYPos {get;} = 9;
    public int mapWidth; //width of the map lol
    public int mapHeight; //height of the map lol
    public Color streetColor; //color of the texture that the algorithm will read as a street 
    public int textureResolution {get;} = 2;
    public Texture2D pattern; //input pattern to the WFC algorithm

    public int seed;
    public float streetXOffsetX;
    public float streetXOffsetZ;
    public float streetZOffsetX;
    public float streetZOffsetZ;
    public float grassOffsetX;
    public float grassOffsetZ;
    public float houseUpOffsetX;
    public float houseUpOffsetZ;
    public float houseDownOffsetX;
    public float houseDownOffsetZ;
    public float houseLeftOffsetX;
    public float houseLeftOffsetZ;
    public float houseRightOffsetX;
    public float houseRightOffsetZ;

    public int totalNumChildren = 0;
    public int houseCount = 0;
    public bool isSchool = false;

    static System.Random random = new System.Random(); //instantiate a random class (idk why c# does this either)
    public int pickUpZoneProbability {get;} = 10;
    public int carSpawnProbability {get;} = 20;

    Dictionary <Vector2, GameObject> tileDictionary;
    Texture cityTexture; //proc gen texture to build the city off of 
    public int [ , ] cityMap; // thing that store the location of every street and everything so that we can make a map of it later
    Dictionary <Vector2, bool> isPickUpZone;

    public GameObject[] cityElements = new GameObject[15]; //array of all of the cityElements (models) that can be set in the editor

    //the only reason this exists is to act as a reference to which index the city elements are in
    public enum cityElementsNames { 

        buildingBase = 1,
        buildingWindow = 2,
        houseUp = 3,
        houseDown = 4,
        houseLeft = 5,
        houseRight = 6,
        school = 7,
        streetX = 8,
        streetZ = 9,
        streetIntersection = 10,
        grass = 11,
        pickUpZone = 12,
        dropOffZone = 13,
        car = 14

    }

    void WFCToStreet(CityGenerator thisClass) {

        int width = mapWidth;
        int height = mapHeight;

        cityMap = new int[height, width];

        isPickUpZone = new Dictionary<Vector2, bool>();


        bool foundGoodSeed = false;
        Texture2D WFCTexture = WFC.Generate(pattern, 3, width, height, false, true, false, 8, (int)1e9, seed);

        while (!foundGoodSeed) {
            
            WFCTexture = WFC.Generate(pattern, 3, width, height, false, true, false, 8, (int)1e9, seed);

            for (int xTextureIndex = 0; xTextureIndex < width; xTextureIndex += textureResolution) {
                if (WFCTexture.GetPixel(xTextureIndex, 0) == streetColor) {
                    foundGoodSeed = true; 

                    Debug.Log("(" + xTextureIndex + ", " + (height-1) + ")");
                }                  
            }

            seed++;

            Debug.Log("Found good seed: " + seed);
        }

        //debugImg.texture = WFCTexture;
        //loop through the WFC texture and generate a street if the pixel is the right color
        for (int xTextureIndex = 0; xTextureIndex < width; xTextureIndex += textureResolution) {
            for (int yTextureIndex = 0; yTextureIndex < height; yTextureIndex += textureResolution) { 

                //Debug.Log(xTextureIndex + " " + yTextureIndex);
                
                Color pixelColor = WFCTexture.GetPixel(xTextureIndex, yTextureIndex);

                if (pixelColor == streetColor) 
                    generateStreet(xTextureIndex, yTextureIndex,    WFCTexture);

            }   
        }

        generateGrass (thisClass);
        generateHouses (thisClass);
        generateSchool (thisClass, busObject);
        //generateCars (thisClass);
    }

    //generate a street at the specified point in space
    void generateStreet (int xIndex, int zIndex,   Texture2D WFCTexture) {

        if (IsVerticalStreet(xIndex, zIndex,   WFCTexture) && IsHorizontalStreet(xIndex, zIndex,   WFCTexture)) {  // (if its a part of an intersection)
            Instantiate (cityElements[(int)cityElementsNames.streetIntersection],   new Vector3 ((xIndex * tileSize) / textureResolution, defaultYPos, (zIndex * tileSize) / textureResolution),   Quaternion.identity, transform);
            cityMap [xIndex / textureResolution, zIndex / textureResolution] = (int)cityElementsNames.streetIntersection;
        }

        else if (IsVerticalStreet(xIndex, zIndex,   WFCTexture)) { // (if its a street thats going in the z dir)
            Instantiate (cityElements[(int)cityElementsNames.streetZ] ,   new Vector3 (streetZOffsetX + (xIndex * tileSize) / textureResolution, defaultYPos, streetZOffsetZ + (zIndex * tileSize) / textureResolution),   Quaternion.identity, transform);
            cityMap [xIndex / textureResolution, zIndex / textureResolution] = (int)cityElementsNames.streetZ;
        }

        else if (IsHorizontalStreet(xIndex, zIndex,   WFCTexture)) { // (if its a street thats going in the x dir)
            Instantiate (cityElements[(int)cityElementsNames.streetX],   new Vector3 (streetXOffsetX + (xIndex * tileSize) / textureResolution, defaultYPos, streetXOffsetZ + (zIndex * tileSize) / textureResolution),   Quaternion.identity, transform);
            cityMap [xIndex / textureResolution, zIndex / textureResolution] = (int)cityElementsNames.streetX; 
        }
    } 


    //Check if there are streets above or below the current street
    bool IsVerticalStreet(int xIndex, int zIndex,   Texture2D WFCTexture) {

        if (WFCTexture.GetPixel(xIndex, zIndex + textureResolution) == streetColor || WFCTexture.GetPixel(xIndex, zIndex - textureResolution) == streetColor)
            return true;
        return false;
    }

    //Check if there are streets to the sides of the current street
    bool IsHorizontalStreet(int xIndex, int zIndex,   Texture2D WFCTexture) {

        if (WFCTexture.GetPixel(xIndex + textureResolution, zIndex) == streetColor || WFCTexture.GetPixel(xIndex - textureResolution, zIndex) == streetColor)
            return true;
        return false;
    }
    

    void generateGrass (CityGenerator thisClass) {

        for (int xIndex = 0; xIndex < mapWidth; xIndex++) {
            for (int zIndex = 0; zIndex < mapHeight; zIndex++) {
                
                if (cityMap [xIndex, zIndex] == 0) {
                    Instantiate (cityElements[(int)cityElementsNames.grass], new Vector3 (grassOffsetX + (xIndex * tileSize), defaultYPos, grassOffsetZ + (zIndex * tileSize)), Quaternion.identity, transform);
                    cityMap [xIndex, zIndex] = (int)cityElementsNames.grass;
                }
                    
            }
        }
    }


    void generateHouses (CityGenerator thisClass) {

        for (int xIndex = 0; xIndex < mapWidth; xIndex++) {
            for (int zIndex = 0; zIndex < mapHeight; zIndex++) {

                if (cityMap [xIndex, zIndex] == (int)cityElementsNames.grass) {
                    generateHouseIfNearStreet (xIndex, zIndex,   cityMap);
                }
            }
        }
    }

    void generateHouseIfNearStreet (int xIndex, int zIndex,   int[ , ] cityMap) {

        if (indexExists (xIndex + 1, zIndex, cityMap) &&
            (cityMap [xIndex + 1, zIndex] == (int)cityElementsNames.streetIntersection || 
             cityMap [xIndex + 1, zIndex] == (int)cityElementsNames.streetX || 
             cityMap [xIndex + 1, zIndex] == (int)cityElementsNames.streetZ)) { //street to the right
            
            var house = Instantiate (cityElements[(int)cityElementsNames.houseRight], new Vector3 (houseRightOffsetX + (xIndex * tileSize), defaultYPos, houseRightOffsetZ + (zIndex * tileSize)), Quaternion.Euler (0, 180, 0), transform);
            cityMap [xIndex, zIndex] = (int)cityElementsNames.houseRight;
            generatePickUpZone (++xIndex, zIndex,   cityMap,   house);
            houseCount++;
            
        }

        else if (indexExists (xIndex - 1, zIndex, cityMap) &&
            (cityMap [xIndex - 1, zIndex] == (int)cityElementsNames.streetIntersection || 
            cityMap [xIndex - 1, zIndex] == (int)cityElementsNames.streetX || 
            cityMap [xIndex - 1, zIndex] == (int)cityElementsNames.streetZ)) { //street to the left

            var house = Instantiate (cityElements[(int)cityElementsNames.houseLeft], new Vector3 (houseLeftOffsetX + (xIndex * tileSize), defaultYPos, houseLeftOffsetZ + (zIndex * tileSize)), Quaternion.Euler (0, 0, 0), transform);
            cityMap [xIndex, zIndex] = (int)cityElementsNames.houseLeft;
            generatePickUpZone (xIndex, zIndex,   cityMap,   house);
            houseCount++;
        }

        else if (indexExists (xIndex, zIndex + 1, cityMap) &&
            (cityMap [xIndex, zIndex + 1] == (int)cityElementsNames.streetIntersection || 
            cityMap [xIndex, zIndex + 1] == (int)cityElementsNames.streetX || 
            cityMap [xIndex, zIndex + 1] == (int)cityElementsNames.streetZ)) { //street above

            var house = Instantiate (cityElements[(int)cityElementsNames.houseUp], new Vector3 (houseUpOffsetX + (xIndex * tileSize), defaultYPos, houseUpOffsetZ + (zIndex * tileSize)), Quaternion.Euler (0, 90, 0), transform);
            cityMap [xIndex, zIndex] = (int)cityElementsNames.houseUp;
            generatePickUpZone (xIndex, ++zIndex,   cityMap,   house);
            houseCount++;
        }

        else if (indexExists (xIndex, zIndex - 1, cityMap) &&
            (cityMap [xIndex, zIndex - 1] == (int)cityElementsNames.streetIntersection || 
            cityMap [xIndex, zIndex - 1] == (int)cityElementsNames.streetX || 
            cityMap [xIndex, zIndex - 1] == (int)cityElementsNames.streetZ)) {//street below

            var house = Instantiate (cityElements[(int)cityElementsNames.houseDown], new Vector3 (houseDownOffsetX + (xIndex * tileSize), defaultYPos, houseDownOffsetZ + (zIndex * tileSize)), Quaternion.Euler (0, 270, 0), transform);
            cityMap [xIndex, zIndex] = (int)cityElementsNames.houseDown;
            generatePickUpZone (xIndex, --zIndex,   cityMap,   house);
            houseCount++;
        }
    }

    bool indexExists (int xIndex, int zIndex, int [ , ] matrix) {

        if (xIndex >= 0 && zIndex >= 0 && xIndex < matrix.GetLength(1) && zIndex < matrix.GetLength(0))
            return true;
        return false;
    }

    void generatePickUpZone (int xIndex, int zIndex,   int [ , ] cityMap,   GameObject house) {


        //this doesn't work on up? and left facing houses
        if (random.Next(0, pickUpZoneProbability) == 0) {
            
            var pickUpZoneTemp = Instantiate (cityElements[(int)cityElementsNames.pickUpZone], new Vector3 ((xIndex * tileSize), defaultYPos, (tileSize / 2) + (zIndex * tileSize)), Quaternion.identity, transform);
            pickUpZoneTemp.transform.localScale = new Vector3 (tileSize * 1.5f, tileSize * 1.5f, tileSize * 1.5f);
            pickUpZoneTemp.GetComponent<Pick_Up_Zone_Collisions>().setHouse(house);
            house.GetComponent<houseScript>().addChild();
            isPickUpZone [new Vector2 (xIndex, zIndex)] = true;

            totalNumChildren++;
        }
        else
            isPickUpZone [new Vector2 (xIndex, zIndex)] = false;

    }

    void generateSchool (CityGenerator thisClass,  GameObject bus) { 

        for (int x = 0; x < cityMap.GetLength(0); x++){    
            if (cityMap[x, 0] == 8 || cityMap[x, 0] == 9) {
                Instantiate (cityElements [(int) cityElementsNames.school], new Vector3 (x * tileSize, defaultYPos, -1 * tileSize), Quaternion.Euler (0, 90, 0), transform);
                busObject.transform.position = new Vector3(x * tileSize, defaultYPos + 0.3f, tileSize/4);
                break;
            }
        }
    }
    
    void generateCars (CityGenerator thisClass) {

        for (int xIndex = 0; xIndex < mapWidth; xIndex++) {
            for (int zIndex = 0; zIndex < mapHeight; zIndex++) {

                if (random.Next(0, carSpawnProbability) == 0) {
                    generateCarAtStreet (xIndex, zIndex);
                }
            }
        }
    }

    void generateCarAtStreet (int xIndex, int zIndex) {

        if (cityMap [xIndex, zIndex] == (int)cityElementsNames.streetIntersection) {
            Instantiate (cityElements[(int)cityElementsNames.car], new Vector3 (xIndex * tileSize, defaultYPos, zIndex * tileSize), Quaternion.Euler (0, 0, 0), transform);
        }

        else if (cityMap [xIndex, zIndex] == (int)cityElementsNames.streetX) {
            Instantiate (cityElements[(int)cityElementsNames.car], new Vector3 (xIndex * tileSize, defaultYPos, zIndex * tileSize), Quaternion.Euler (0, 90, 0), transform);
        }

        else if (cityMap [xIndex, zIndex] == (int)cityElementsNames.streetZ) {
            Instantiate (cityElements[(int)cityElementsNames.car], new Vector3 (xIndex * tileSize, defaultYPos, zIndex * tileSize), Quaternion.Euler (0, 0, 0), transform);
        }
    }

    void Start()
    {
        seed = seed >= 0 ? seed : Random.Range(0, (int)1e6);
        WFCToStreet (this);
        FindObjectOfType<borderScript>().setSize((uint)mapWidth);
        Debug.Log(totalNumChildren);
    }
}
