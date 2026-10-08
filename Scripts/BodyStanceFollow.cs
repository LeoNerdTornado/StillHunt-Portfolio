using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyStanceFollow : MonoBehaviour
{
    [SerializeField] private Transform bodyStanceAnchor;

    private Vector3 neutralLocalPosition;
    // Start is called before the first frame update
    void Start()
    {
        neutralLocalPosition = bodyStanceAnchor.localPosition;

    }
    public void SetLeanOffset(Vector3 offset)
    {
        bodyStanceAnchor.localPosition = neutralLocalPosition + offset;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
