using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class CoverPoint : MonoBehaviour
{
    // Start is called before the first frame update
    public enum ConcealmentType
    {
        Shadow,
        Bush,
        TallGrass,
        Tree,
        Smoke,
        Fog,
        Rock,
        EscapeSpace
    }
    public Transform playerTarget;
    public Transform guardAI;
    public enum CoverUser
    {
        Player,
        AI,
        BOTH
    }
    public CoverUser coverUser = CoverUser.BOTH;

    public bool occupied;
    public bool playerInside;

    public float escapeScore = 0f;
    [Range(0f, 100f)] public float safetyScore = 70f;
    [Range(0f, 100f)] public float opportunityScore = 30f;

    [SerializeField] private int bushConnectionCount = 5;
    [SerializeField] private int bushConnectionRadius = 15;

    public bool CanPlayerUse()
    {

        return (coverUser == CoverUser.Player) || (coverUser == CoverUser.BOTH);
    }
    public bool CanPlayerUseOnly()
    {

        return (coverUser == CoverUser.Player);
    }
    public bool CanAiUse()
    {

        return (coverUser == CoverUser.AI) || (coverUser == CoverUser.BOTH);
    }
    public bool CanAiUseOnly()
    {

        return (coverUser == CoverUser.AI); 
    }
    public List<CoverPoint> connectedCovers = new List<CoverPoint>();
    public List<ConcealmentType> concealmentTypes = new List<ConcealmentType>();
    private  IEnumerator Start()
    {

        AddRockAndTreesConcealMentTypes();
        AddBushConcealMentTypes();
        yield return new WaitForSeconds(5f);
        AddNearbyBushCovers();

    }
    //private  Start()
    //{
        
    //    // Startup code here
    //}
    private void awake()
    {
        
    }
    private void AddRockAndTreesConcealMentTypes()
    {
        if (
            ((!concealmentTypes.Contains(ConcealmentType.Rock)) && (!CanAiUseOnly()) &&(concealmentTypes.Count == 1))
             ||((!concealmentTypes.Contains(ConcealmentType.Tree))&& (!CanAiUseOnly()) && (concealmentTypes.Count == 1))
            )
        {
            return;
        }
        if (!concealmentTypes.Contains(ConcealmentType.Shadow))
        {
            concealmentTypes.Add(ConcealmentType.Shadow);
        }

        if (!concealmentTypes.Contains(ConcealmentType.TallGrass))
        {
            concealmentTypes.Add(ConcealmentType.TallGrass);
        }

        if (!concealmentTypes.Contains(ConcealmentType.EscapeSpace))
        {
            concealmentTypes.Add(ConcealmentType.EscapeSpace);
        }
        if (!concealmentTypes.Contains(ConcealmentType.Tree))
        {
            concealmentTypes.Add(ConcealmentType.Tree);
        }
        if (!concealmentTypes.Contains(ConcealmentType.Fog))
        {
            concealmentTypes.Add(ConcealmentType.Fog);
        }
        if (!concealmentTypes.Contains(ConcealmentType.Smoke))
        {
            concealmentTypes.Add(ConcealmentType.Smoke);
        }
        //Debug.Log(
        //    "COVER POINT | ROCK PACKAGE | " +
        //    gameObject.name
        //);
    }
    private void AddBushConcealMentTypes()
    {

        
        if (!concealmentTypes.Contains(ConcealmentType.Bush))
        {
            return;
        }
        concealmentTypes.Clear();
        if (!concealmentTypes.Contains(ConcealmentType.Shadow))
        {
            concealmentTypes.Add(ConcealmentType.Shadow);
        }

        if (!concealmentTypes.Contains(ConcealmentType.TallGrass))
        {
            concealmentTypes.Add(ConcealmentType.TallGrass);
        }
        //if (!concealmentTypes.Contains(ConcealmentType.Bush))
        //{
        //    concealmentTypes.Add(ConcealmentType.Bush);
        //}
        if (!concealmentTypes.Contains(ConcealmentType.EscapeSpace))
        {
            concealmentTypes.Add(ConcealmentType.EscapeSpace);
        }
        //if (!concealmentTypes.Contains(ConcealmentType.Tree))
        //{
        //    concealmentTypes.Add(ConcealmentType.Tree);
        //}
        if (!concealmentTypes.Contains(ConcealmentType.Bush))
        {
            concealmentTypes.Add(ConcealmentType.Bush);
        }
        //if (!concealmentTypes.Contains(ConcealmentType.Fog))
        //{
        //    concealmentTypes.Add(ConcealmentType.Fog);
        //}
        //if (!concealmentTypes.Contains(ConcealmentType.Smoke))
        //{
        //    concealmentTypes.Add(ConcealmentType.Smoke);
        //}
        //Debug.Log(
        //    "COVER POINT | ROCK PACKAGE | " +
        //    gameObject.name
        //);
    }
    private void AddNearbyBushCovers()
    {

        if (!concealmentTypes.Contains(ConcealmentType.Bush))
        {
            return;
        }
        connectedCovers.Clear();
        CoverPoint[] allCoverPoint =
            FindObjectsOfType<CoverPoint>();

        List<CoverPoint> bushCovers =
            new List<CoverPoint>();

        foreach (CoverPoint point in allCoverPoint)
        {
            if(point == null) { continue; }

            if(point == this)
            {
                continue;
            }
            if (point.concealmentTypes.Contains(ConcealmentType.Bush))
            {
                float distance = Vector3.Distance(
                    transform.position,
                    point.transform.position
                    );

                if (distance <= bushConnectionRadius)
                {

                    bushCovers.Add(point);
                    //Debug.Log(
                    //    "BUSH DISTANCE | " +
                    //    gameObject.name +
                    //    " -> " +
                    //    point.gameObject.name +
                    //    " | Distance: " +
                    //    distance
                    //);
                }
            }
        }
        bushCovers.Sort(
            (a,b)=>
            Vector3.Distance(
                transform.position,
                a.transform.position
                ).CompareTo(
                    Vector3.Distance(
                            transform.position,
                            b.transform.position
                        )
                )
            );
        int count =
            Mathf.Min
            (
                bushConnectionCount,
                bushCovers.Count
                );
        for (int i=0; i<count;i++)
        {
            if (!connectedCovers.Contains(bushCovers[i]))
            {
                connectedCovers.Add(bushCovers[i]);
            }
        }
   //     Debug.Log(
   //    "COVER POINT | BUSH CONNECTIONS | " +
   //    gameObject.name +
   //    " | Added: " +
   //    count
   //);
    }
    private void OnTriggerExit(Collider other)
    {
        PlayerMovement playerMovement =
       other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            return;
        }

        playerInside = false;

        //Debug.Log("CoverPoint direction EXIT  :" + gameObject.name);
    }
    private void OnTriggerEnter(Collider other)
    {

        PlayerMovement playerMovement =
            other.GetComponentInParent<PlayerMovement>();

        if(playerMovement == null) { return; }
        playerInside = true;
        //Debug.Log("CoverPoint direction ENTER  :" + gameObject.name);
    }

        // Update is called once per frame
        void Update()
    {
        escapeScore = connectedCovers.Count;
        
        //Debug.Log("connected Covers.Count:" + escapeScore);
    }
}

