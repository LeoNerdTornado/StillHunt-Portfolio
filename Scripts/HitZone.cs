using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitZone : MonoBehaviour
{
    // Start is called before the first frame update
    public enum ZoneType
    {
        Head,
        Chest,
        Abdomen,
        Leg

    }
    public ZoneType zoneType;
    public float GetDamage()
    {

        switch (zoneType)
        {
            case ZoneType.Head:
                return 80f;
            break;
            case ZoneType.Chest:
                return 50f;
            break;
            case ZoneType.Abdomen:
                return 30f;
            break;
            case ZoneType.Leg:
                return 20f;
            break;

        }
        return 0f;
    }
    private void OnTriggerEnter(Collider other)
    {
        DevLog.Log("HIT ZONE TEST : " +
            gameObject.name +
            " | Zone: " +
            zoneType +
            " | Damage: "+
            GetDamage());


    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
