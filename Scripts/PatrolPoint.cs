using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class PatrolPoint : MonoBehaviour
{
    // Start is called before the first frame update

    

    public List<TypesOfGround> terrain = new List<TypesOfGround>();
    public Transform guardAI;
    public List<PatrolPoint> connectedPoints = new List<PatrolPoint>();

    

    public LayerMask obstacleMask;

    [Header("Escape Route")]
    public bool isHiddenPatrolPoint = false;

    [Header("Grass SetUp")]
    public Transform bushMesh;
    public bool autoFindBushMesh = true;
    public bool disableGrassShadows = true;
    public bool moveBushIntoGrassPrefab = true;

    public float connectionDistance = 15f;

    public void BuildConnection(PatrolPoint[] allPoints) {
        connectedPoints.Clear();

        foreach (PatrolPoint point in allPoints) {
            if (point == null) continue;
            if (point == this) continue;
            float distance = Vector3.Distance(
                               transform.position,
                               point.transform.position
                                );
            if (distance > connectionDistance) continue;

            Vector3 start = transform.position + Vector3.up * 0.5f;
            Vector3 target = point.transform.position + Vector3.up * 0.5f;
            Vector3 direction = target - start;

            if (Physics.Raycast(
                start,
                direction.normalized,
                direction.magnitude,
                obstacleMask
                ))
            {
                //Debug.Log("NO PATROL POINTS :" + point.name);
                continue;

            }
            connectedPoints.Add(point);

            //    Debug.DrawLine(
            //         transform.position,
            //         point.transform.position,
            //         Color.green,
            //         10f
            //        );
            //    Debug.Log(
            //        gameObject.name+
            //        " CONNECTED TO "+
            //        point.gameObject.name
            //        );
        }

    }
    void Start()
    {

        //yield return new WaitForSeconds(5f);
        SetUpBushMesh();
        SetUpGrassBush();
        //SetupGrassVision();
    }
    public enum TypesOfGround
    {
        CenterGround,
        CenterHill,
        WestCenterGround,
        WestCenterHill,
        EastCenterGround,
        EastCenterHill,
        SouthWestHill,
        SouthCenterHill,
        SouthEastHill,
        WestHill,
        EastHill
    }
    private void SetupGrassVision()
    {
        if(
            (terrain.Contains(TypesOfGround.CenterGround))
            || (terrain.Contains(TypesOfGround.CenterHill))
            || (terrain.Contains(TypesOfGround.WestCenterGround))
            || (terrain.Contains(TypesOfGround.WestCenterHill))
            || (terrain.Contains(TypesOfGround.EastCenterGround))
            || (terrain.Contains(TypesOfGround.EastCenterHill))
            || (terrain.Contains(TypesOfGround.SouthWestHill))
            || (terrain.Contains(TypesOfGround.SouthCenterHill))
            || (terrain.Contains(TypesOfGround.SouthEastHill))
            || (terrain.Contains(TypesOfGround.WestHill))
            || (terrain.Contains(TypesOfGround.EastHill))
            )
        {
            Transform grassVision = transform.Find("GrassVision");
            if (grassVision != null)
            {
                grassVision.gameObject.SetActive(false);
            //    Debug.Log(
            //    "PATROL POINT | GRASS VISION DISABLED | " +
            //    gameObject.name +
            //    " | Terrain: CenterGround/CenterHill"
            //);
            }
            else
            {
            //    Debug.Log(
            //   "PATROL POINT | GRASS VISION NOT FOUND | " +
            //   gameObject.name
            //);
            }
        }
    }
    private void SetUpGrassBush()
    {


        // Automatically find a Bush_Mesh if none was assigned manually.
        
        //if ((bushMesh==null) && (autoFindBushMesh))
        //{
        //    bushMesh = FindNearestBushMesh();
            
        //}
        Transform grassVision = transform.Find("GrassVision");
        
        if (grassVision == null)
        {
            //Debug.LogWarning(
            //    "PATROL POINT | GrassVision NOT FOUND | " +
            //    gameObject.name
            //);
            //Debug.Log("BUSH MESH | patrolPoint name 1 : " + bushMesh);
            return;
        }
        Transform grassVisual = grassVision.Find("GrassVisual");
        if (grassVisual == null)
        {
            //Debug.LogWarning(
            //    "PATROL POINT | GrassVisual NOT FOUND | " +
            //    gameObject.name
            //);
            //Debug.Log("BUSH MESH | patrolPoint name 2 : " + bushMesh);
            return;
        }
        Transform grassPrefab = grassVisual.Find("GrassPrehab");
        if (grassPrefab == null)
        {
            //Debug.LogWarning(
            //    "PATROL POINT | GrassPrefab NOT FOUND | " +
            //    gameObject.name
            //);
            //Debug.Log("BUSH MESH | patrolPoint name 3 : " + bushMesh);
            return;
        }
        
        if (bushMesh == null) 
        {
            //Debug.Log("BUSH MESH | patrolPoint name 4 : " + bushMesh);
            //{
            //    Debug.LogWarning(
            //    "PATROL POINT | Bush_Mesh NOT ASSIGNED | " +
            //    gameObject.name
            //);
            return;
        }
       
        // Move Bush_Mesh inside GrassPrefab
        if (moveBushIntoGrassPrefab)
        {
            //Debug.Log("BUSH MESH | patrolPoint name 5 : " + bushMesh);
            bushMesh.SetParent(grassPrefab, true);
        }
        // Turn Cast Shadows OFF
        if (disableGrassShadows)
        {
            //Debug.Log("BUSH MESH | patrolPoint name 6 : " + bushMesh);
            MeshRenderer[] grassRenderers =
                grassPrefab.GetComponentsInChildren<MeshRenderer>(true);
            foreach(MeshRenderer renderer in grassRenderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        Transform sourceRealGrass = grassPrefab.Find("real g");
        if (sourceRealGrass != null)
        {
            //bushMesh.position.y = sourceRealGrass.position.y;
            float grassY = Mathf.Min(
                sourceRealGrass.localPosition.y,
                4.30f
                );

            bushMesh.localPosition = new Vector3
                (
                 bushMesh.localPosition.x,
                 grassY,
                 bushMesh.localPosition.z
                );
            GameObject sourceRealGrassObject = sourceRealGrass.gameObject;
            sourceRealGrassObject.SetActive(false);
        }
        
    }
    private Transform FindNearestBushMesh()
    {
        Transform[] allTransform =
            FindObjectsByType<Transform>(FindObjectsSortMode.None);

        Transform nearestBush = null;
        float nearestDistance = Mathf.Infinity;

        foreach(Transform candidate in allTransform)
        {
            if(candidate == null) { continue; }

            // Accept Bush_Mesh, Bush_Mesh (1), Bush_Mesh (2), etc.
            if (!candidate.name.StartsWith("Bush_Mesh"))
            {
                continue;
            }
            // Don't use a Bush_Mesh that has already been placed
            // inside another GrassPrefab.
            if((candidate.parent!=null)
                &&(candidate.parent.name == "GrassPrefab"))
            {
                continue;
            }
            //float distance =
            //Vector3.Distance(transform.position, candidate.position);

            //if (distance < nearestDistance)
            //{
            //    nearestDistance = distance;
            //    nearestBush = candidate;
            //}
        }
        return nearestBush;
    
}
    private void SetUpBushMesh()
    {
        // Find the ONE Bush_Mesh already existing in the scene.
        GameObject sourceBush = GameObject.Find("Bush_Mesh");
        if (sourceBush == null)
        {
            //Debug.LogWarning(
            //    $"[{name}] Could not find Bush_Mesh in the scene."
            //);
            return;
        }
        // Find this patrol point's GrassPrefab.
        Transform grassPrefab = transform.Find("GrassVision/GrassVisual/GrassPrehab");

        if (grassPrefab == null)
        {
            //Debug.LogWarning(
            //    $"[{name}] Could not find GrassVision/GrassVisual/GrassPrefab."
            //);
            return;
        }
        // Don't create another Bush_Mesh if one is already here.
        // Don't create another Bush_Mesh if one is already here.
        Transform existingBush = grassPrefab.Find("Bush_Mesh");

        if (existingBush != null)
        {
            SetBushCastShadowsOff(existingBush.gameObject);
            return;
        }
        // Make a copy of the ONE source Bush_Mesh.
        GameObject bushCopy = Instantiate
            (
            sourceBush,
            grassPrefab
            );
        // Give the copy a predictable name.
        bushCopy.name = "Bush_Mesh";
        // Reset its local transform so it belongs cleanly to GrassPrefab.
        //bushCopy.transform.localPosition = Vector3.zero;
        //bushCopy.transform.localRotation = Quaternion.identity;
        //bushCopy.transform.localScale = Vector3.five;

        // THIS is the important line.
        bushMesh = bushCopy.transform;

        SetBushCastShadowsOff(bushCopy);

        //Debug.Log(
        //    $"[{name}] Bush_Mesh automatically added to GrassPrefab."
        //);
    }
    private void SetBushCastShadowsOff(GameObject bush)
    {
        MeshRenderer[] renderers =
            bush.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer renderer in renderers)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //SetUpGrassBush();
    }
    private void OnTriggerEnter(Collider other)
    {
        GuardAI guardAI = other.GetComponentInParent<GuardAI>();
        if (guardAI != null)
        {
            gameObject.layer = LayerMask.NameToLayer("Default");

            //Debug.Log("GUARD PATROL POINT | GUARD ENTERED |"+
            //            gameObject.name+" | Layer: Default"
            //    );

        }
    }
    private void OnTriggerExit(Collider other)
    {
        GuardAI guardAI = other.GetComponentInParent<GuardAI>();
        if (guardAI != null)
        {
            gameObject.layer = LayerMask.NameToLayer("Grass");

            //Debug.Log("GUARD PATROL POINT | GUARD LEFT |" +
            //            gameObject.name + " | Layer: Grass"
            //    );

        }
    }
}
