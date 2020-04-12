using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestManager : MonoBehaviour {
    public Map.PathAgent randomAgent;
    public Map.PathAgent imageAgent;
    public Map.PathAgent activeAgent;

    // Use this for initialization
    void Start () {
        imageAgent.GoToPosition(Random.Range(0, imageAgent.mapSize), Random.Range(0, imageAgent.mapSize), true);
        randomAgent.gameObject.SetActive(false);
        activeAgent = imageAgent;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnTypeChange(int type)
    {
        imageAgent.pathType = (Map.PathType)type;
        randomAgent.pathType = (Map.PathType)type;
        activeAgent.GoToPosition(Random.Range(0, activeAgent.mapSize), Random.Range(0, activeAgent.mapSize), true);
    }

    public void OnToggleFullSpeed(bool value)
    {
        imageAgent.threadWait = !value;
        randomAgent.threadWait = !value;
        activeAgent.GoToPosition(Random.Range(0, activeAgent.mapSize), Random.Range(0, activeAgent.mapSize), true);
    }

    public void SetActiveAgent(int agent)
    {
        if(agent == 0)
        {
            activeAgent = imageAgent;
            imageAgent.gameObject.SetActive(true);
            randomAgent.gameObject.SetActive(false);
            randomAgent.stopped = true;
            imageAgent.stopped = false;
            imageAgent.GoToPosition(Random.Range(0, imageAgent.mapSize), Random.Range(0, imageAgent.mapSize), true);
        }else
        {
            activeAgent = randomAgent;
            imageAgent.gameObject.SetActive(false);
            randomAgent.gameObject.SetActive(true);
            randomAgent.stopped = false;
            imageAgent.stopped = true;
            randomAgent.GoToPosition(Random.Range(0, imageAgent.mapSize), Random.Range(0, imageAgent.mapSize), true);
        }
    }

}
