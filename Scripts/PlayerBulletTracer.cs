using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBulletTracer : MonoBehaviour
{
    // Start is called before the first frame update

    private Vector3 startPosition;
    private Vector3 endPosition;

    private float travelTime;
    private float timer;

    public void Initialize(
        Vector3 start,
        Vector3 end,
        float speed
        )
    {
        startPosition = start;
        endPosition = end;

        float distance = Vector3.Distance(start, end);

        travelTime = distance / speed;

        if(travelTime <= 0f) { travelTime = 0.01f; }

        timer = 0f;

        transform.position = start;

        Vector3 direction = (end - start).normalized;

        if(direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);

        }

    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / travelTime;

        transform.position = Vector3.Lerp(
            startPosition,
            endPosition,
            t
            );
        if(t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
