using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cover : MonoBehaviour
{
    public Transform[] coverPoints;

    [Header("Attachment")]
    public float attachOffset = 0.25f;

    [Header("Point Height")]
    public float pointHeight = 0.9f;

    public float coverRadius = 1.0f;
    //public Transform leftAnchor;
    //public Transform rightAnchor;
    //public Transform frontAnchor;
    //public Transform backwardAnchor;
    //public float attachOffset = 0.25f;
    //public float coverRadius = 1.0f;

    //public bool point01Occuppied;
    //public bool point02Occuppied;
    //public bool point03Occuppied;
    //public bool point04Occuppied;

    private void Awake()
    {
        CoverPoint[] points = GetComponentsInChildren<CoverPoint>();
        coverPoints = new Transform[points.Length];


        for (int i=0; i < points.Length; i++ ) {
            coverPoints[i] = points[i].transform;
        
        }
        //Debug.Log("COVER1 | "+
        //        gameObject.name+
        //        " | Found Points: "+
        //        coverPoints.Length
        //    );
        //SetUpCoverPoints();
    }
    private void SetUpCoverPoints()
    {
        if(coverPoints.Length < 4)
        {
            //Debug.Log("COVER1 | "+gameObject.name+ "needs 4 CoverPoints");
            return;
        }
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if(renderers.Length == 0)
        {
            //Debug.LogWarning("COVER1 | "+gameObject.name+" has no renderer.");
            return;
        }
        Bounds bounds = renderers[0].bounds;
        for(int i =1;i <renderers.Length; i++)
        {

            bounds.Encapsulate(
                renderers[i].bounds
                );
        }
        Vector3 center = bounds.center;

        float leftX = bounds.min.x - attachOffset;

        float rightX = bounds.max.x + attachOffset;

        float frontZ = bounds.max.z + attachOffset;

        float backZ = bounds.min.y + attachOffset;

        float y = bounds.min.y + pointHeight;

        //Point 01 = Left

        coverPoints[0].position = new Vector3(
            leftX,
            y,
            center.z
            );
        //Point 02 = Right

        coverPoints[1].position = new Vector3(
            rightX,
            y,
            center.z
            );
        // Point 03 = Front
        coverPoints[2].position =
            new Vector3(
                center.x,
                y,
                frontZ
            );

        // Point 04 = Back
        coverPoints[3].position =
            new Vector3(
                center.x,
                y,
                backZ
            );

        //Debug.Log(
        //    "COVER1 | " +
        //    gameObject.name +
        //    " | POINTS AUTOMATICALLY POSITIONED.");
            
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
