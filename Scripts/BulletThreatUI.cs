using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BulletThreatUI : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;

    [SerializeField] private float indicatorDuration = 0.15f;
    [SerializeField] private float farThreatDistance = 3f;
    [SerializeField] private float veryCloseThreatDistance = 1f;

    [SerializeField] private Image whiteNorth;
    [SerializeField] private Image whiteSouth;
    [SerializeField] private Image whiteEast;
    [SerializeField] private Image whiteWest;

    [SerializeField] private Image redNorth;
    [SerializeField] private Image redSouth;
    [SerializeField] private Image redEast;
    [SerializeField] private Image redWest;

    private Coroutine whiteIndicatorRoutine;
    private Coroutine redIndicatorRoutine;


    // Start is called before the first frame update
    void Start()
    {
        HideAllWhiteIndicators();
        HideAllRedIndicators();
    }
    private IEnumerator HideWhiteAfterDelay()
    {

        yield return new WaitForSeconds(indicatorDuration);
        HideAllWhiteIndicators();
        whiteIndicatorRoutine = null;
    }
    private IEnumerator HideRedAfterDelay()
    {
        yield return new WaitForSeconds(indicatorDuration);
        HideAllRedIndicators();
        redIndicatorRoutine = null;

    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.T))
        //{
        //    ShowWhiteIndicator(playerCamera.position + playerCamera.forward * 5f,4f);
        //}
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    ShowWhiteIndicator(playerCamera.position + playerCamera.right * 5f, 2f);

        //}
        //if (Input.GetKeyDown(KeyCode.H))
        //{

        //    ShowWhiteIndicator(playerCamera.position - playerCamera.right * 5f, 0.5f);
        //}
        //if (Input.GetKeyDown(KeyCode.B))
        //{
        //    ShowWhiteIndicator(playerCamera.position - playerCamera.forward * 5f,1f);
        //}
    }
    public void HideAllWhiteIndicators()
    {
        SetAlpha(whiteNorth, 0f);
        SetAlpha(whiteSouth, 0f);
        SetAlpha(whiteEast, 0f);
        SetAlpha(whiteWest, 0f);

    }
    
    private void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;

    }
    public void ShowWhiteIndicator(Vector3 bulletClosestPoint, float bulletDistance)
    {
        if (playerCamera == null) return;

        if (whiteIndicatorRoutine != null)
        {
            StopCoroutine(whiteIndicatorRoutine);
            whiteIndicatorRoutine = null;
        }

        float alpha = CalculateThreatAlpha(bulletDistance);
        Vector3 directionToBullet = bulletClosestPoint - playerCamera.position;

        directionToBullet.y = 0f;
        if (directionToBullet.sqrMagnitude < 0.001f) return;
        float forwardAmount = Vector3.Dot(playerCamera.forward, directionToBullet);
        float rightAmount = Vector3.Dot(playerCamera.right, directionToBullet);

        HideAllWhiteIndicators();
        if(Mathf.Abs(forwardAmount) >= Mathf.Abs(rightAmount))
        {
            if(forwardAmount > 0f)
            {
                SetAlpha(whiteNorth, alpha);
                DevLog.Log("BULLET INDICATOR: NORTH");

            }
            else
            {
                SetAlpha(whiteSouth, alpha);
                DevLog.Log("BULLET INDICATOR: SOUTH");

            }

        }
        else
        {
            if(rightAmount > 0f)
            {

                SetAlpha(whiteEast, alpha);
                DevLog.Log("BULLET INDICATOR: EAST");
            }
            else
            {
                SetAlpha(whiteWest, alpha);
                DevLog.Log("BULLET INDICATOR: WEST");
            }

        }
        whiteIndicatorRoutine = StartCoroutine(HideWhiteAfterDelay());

    }
    private float CalculateThreatAlpha(float bulletDistance)
    {

        if(bulletDistance >= farThreatDistance)
        {
            return 0.10f;

        }
        if(bulletDistance <= veryCloseThreatDistance)
        {
            return 0.85f;
        }
        return Mathf.Lerp(
            0.85f,
            0.10f,
            Mathf.InverseLerp(
                veryCloseThreatDistance,
                farThreatDistance,
                bulletDistance
                )
            );
    }
    public void HideAllRedIndicators()
    {
        SetAlpha(redNorth, 0f);
        SetAlpha(redSouth, 0f);
        SetAlpha(redEast, 0f);
        SetAlpha(redWest, 0f);
    }
    public void ShowRedIndicator(Vector3 hitPosition)
    {
        if (playerCamera == null) return;

        if (redIndicatorRoutine != null)
        {
            StopCoroutine(redIndicatorRoutine);
            redIndicatorRoutine = null;
        }
        HideAllRedIndicators();
        Vector3 directionToHit = hitPosition - playerCamera.position;

        directionToHit.y = 0f;

        if (directionToHit.sqrMagnitude < 0.001f) return;

        float forwardAmount = Vector3.Dot(playerCamera.forward, directionToHit);
        float rightAmount = Vector3.Dot(playerCamera.right, directionToHit);

        

        if(Mathf.Abs(forwardAmount) >= Mathf.Abs(rightAmount))
        {
            if(forwardAmount > 0f)
            {
                SetAlpha(redNorth, 0.85f);
                DevLog.Log("BULLET HIT INDICATOR: NORTH");
            }
            else
            {
                SetAlpha(redSouth, 0.85f);
                DevLog.Log("BULLET HIT INDICATOR: SOUTH");

            }

        }
        else
        {
            if(rightAmount > 0f)
            {
                SetAlpha(redEast, 0.85f);
                DevLog.Log("BULLET HIT INDICATOR: EAST ");
            }
            else
            {
                SetAlpha(redWest, 0.85f);
                DevLog.Log("BULLET HIT INDICATOR: WEST ");
            }

        }
        redIndicatorRoutine = StartCoroutine(HideRedAfterDelay());

    }
}
