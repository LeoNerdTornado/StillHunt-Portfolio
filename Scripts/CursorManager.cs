using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting;

public class CursorManager : MonoBehaviour
{
    // Start is called before the first frame update
    public Texture2D footCursor;
    public Texture2D coverCursor;
    public Texture2D invalidCursor;

    [Header("Hover Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float rayDistance = 20f;
    [SerializeField] private float coverPointHoverRadius = 0.8f;

    public CoverPoint hoveredCoverPoint;


    void Start()
    {
      if(playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }
    public void ShowFootCursor() {
        Cursor.SetCursor(
              footCursor,            
              Vector2.zero,
              CursorMode.Auto
            );
    }
    public void ShowCoverCursor() {
        Cursor.SetCursor(
               coverCursor,
               Vector2.zero,
               CursorMode.Auto
            ); 
    }

    public void ShowInvalidCursor() {
        Cursor.SetCursor(
                invalidCursor,
                Vector2.zero,
                CursorMode.Auto
            );
    }

    // Update is called once per frame
    void Update()
    {
        DetectHover();
    }
    public void DetectHover(CoverPoint selectedPoint = null)
    {
        //Debug.Log("CURSOR DEBUG | "+
        //        "playerCamera = "+playerCamera+
        //        " | Camera.main = "+Camera.main);

        if(playerCamera == null)
        {
            Debug.LogError("CURSOR DEBUG | playerCamera is NULL !");
            return;
        }
        // ---------------------------------------------------------
        // NEW:
        // If CoverSystem already selected a CoverPoint,
        // trust that point.
        // ---------------------------------------------------------
        if (selectedPoint != null)
        {
            if(selectedPoint.CanPlayerUse() &&
                !selectedPoint.occupied)
            {
                hoveredCoverPoint = selectedPoint;
                ShowCoverCursor();
                DevLog.Log("CURSOR | SELECTED COVER POINT | " + selectedPoint.name);
                return;
            }
            // Selected point exists but cannot be used.
            hoveredCoverPoint = null;
            ShowFootCursor();
            DevLog.Log("CURSOR | SELECTED COVER POINT INVALID | "+selectedPoint.name );
            return;
        }
        // ---------------------------------------------------------
        // OLD MOUSE HOVER SYSTEM
        // Used when DetectHover() is called normally from Update().
        // ---------------------------------------------------------
        hoveredCoverPoint = null;
        Vector3 mousePosition = Input.mousePosition;

        if ((float.IsNaN(mousePosition.x))||
            (float.IsNaN(mousePosition.y)) ||
            (float.IsInfinity(mousePosition.x)) ||
            (float.IsInfinity(mousePosition.y))
            ) 
        {
            //Debug.Log("CURSOR | WRONG RAY!");
            ShowFootCursor();
            return;
        }
        Ray ray = playerCamera.ScreenPointToRay(
            Input.mousePosition

            );
        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                rayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
                );
        System.Array.Sort(
            hits,
            (a,b) =>
            a.distance.CompareTo(b.distance)

            );
        foreach(RaycastHit currentHit in hits)
        {
            Cover cover = currentHit.collider.GetComponentInParent<Cover>();

            if (cover == null)
            {
                //Debug.Log("CURSOR | SKIP NON-COVER | " + currentHit.collider.name);
                continue;
            }
            Transform bestPoint =
                GetCoverPointFromMouseRay
                (
                    cover,
                    ray
                    );
            if (bestPoint != null)
            {
                CoverPoint point =
                    bestPoint.GetComponent<CoverPoint>();
                if(
                    (point!=null)&&
                    (point.CanPlayerUse()) &&
                    !point.occupied
                    )
                {
                    hoveredCoverPoint = point;

                    ShowCoverCursor();

                    DevLog.Log("CURSOR | HOVER CURSOR | " 
                        + cover.name +
                        " | POINT: "+point.name);
                    return;
                }
            }
        }
        //Debug.Log("CURSOR | NO USABLE COVER FOUND");
        ShowFootCursor();

        //if(!Physics.Raycast(
        //        ray,
        //        out RaycastHit hit,

        //        rayDistance
        //    )) 
        //{
        //    Debug.Log("CURSOR | WRONG RAY2!");
        //    ShowFootCursor();
        //    return;
        //}
        //Cover cover = hit.collider.GetComponentInParent<Cover>();
        
        //if(cover == null)
        //{
        //    Debug.Log("CURSOR | WRONG RAY3!");
        //    ShowFootCursor();
        //    return;
        //}
        //Transform bestPoint =
        //    GetCoverPointFromMouseRay
        //    (
        //        cover,
        //        ray
        //        );
        //if (bestPoint != null)
        //{
        //    CoverPoint point = bestPoint.GetComponent<CoverPoint>();

        //    if ((point!=null)&&
        //        (point.CanPlayerUse())&&
        //        (!point.occupied)
        //        ) 
        //    {
        //        hoveredCoverPoint = point;
        //        ShowCoverCursor();
        //        Debug.Log(
        //            "CURSOR | HOVER CURSOR | "+cover.name
        //            +" | POINT: "+point.name);
        //        return;
        //    }

        //}
        //Debug.Log("CURSOR | WRONG RAY4!");
        //ShowFootCursor();
    }
    private Transform GetCoverPointFromMouseRay(Cover cover, Ray ray) {
        Transform bestPoint = null;
        float bestDistance = Mathf.Infinity;
        foreach (Transform pointTransform in cover.coverPoints) {
            if (pointTransform == null) continue;
            CoverPoint point = pointTransform.GetComponent<CoverPoint>();
            if (point == null) continue;
            if (!point.CanPlayerUse()) continue;
            if (point.occupied) continue;

            Vector3 pointPosition = pointTransform.position;

            Vector3 closestPoint =
                ray.origin +
                ray.direction * Vector3.Dot(pointPosition-ray.origin, ray.direction);

            float distanceFromRay = Vector3.Distance(pointPosition, closestPoint);
            if((distanceFromRay < coverPointHoverRadius)&&(distanceFromRay < bestDistance))
            {
                bestDistance = distanceFromRay;
                bestPoint = pointTransform;
            }
        }


        return bestPoint;
    }
}
