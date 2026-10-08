using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitZone : MonoBehaviour
{
    // Start is called before the first frame update

    [ContextMenu("Test Apply Damage")]
    private void TestApplyDamage()
    {
        ApplyDamage(GetDamage());
    }
    public enum ZoneType
    {
        Head,
        Chest,
        Abdomen,
        LeftLeg,
        RightLeg
    }
    public ZoneType zoneType;
    
    private PlayerHealth playerHealth;


    public int GetDamage()
    {

        switch (zoneType)
        {

            case ZoneType.Head:
            return 80;
            break;
            case ZoneType.Chest:
            return 50;
            break;
            case ZoneType.Abdomen:
            return 30;
            break;
            case ZoneType.LeftLeg:
            return 20;
            break;
            case ZoneType.RightLeg:
            return 20;
            break;

        }
        return 0;
    }
    public void ApplyDamage(int damage)
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {

            //Debug.LogWarning("PLAYER HITBOX ERROR | PlayerHealth not found on parent!");
            return;
        }
        playerHealth.TakeDamage(ConvertZoneType(),damage );
    }
    private PlayerHealth.ZoneType ConvertZoneType()
    {
        switch (zoneType)
        {
            case ZoneType.Head:
            return PlayerHealth.ZoneType.Head;

            case ZoneType.Chest:
            return PlayerHealth.ZoneType.Chest;

            case ZoneType.Abdomen:
            return PlayerHealth.ZoneType.Abdomen;

            case ZoneType.LeftLeg:
            return PlayerHealth.ZoneType.LeftLeg;

            case ZoneType.RightLeg:
            return PlayerHealth.ZoneType.RightLeg;
        }

        return PlayerHealth.ZoneType.Chest;
    }
    void Start()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();

        DevLog.Log("PLAYER HITBOX READY | "+
                    gameObject.name+
                    " | Zone: "+
                    zoneType+
                    " | Damage: "+
                    GetDamage());

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
