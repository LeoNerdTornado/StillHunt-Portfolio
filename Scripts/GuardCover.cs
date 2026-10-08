using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms.Impl;
using static GuardAI;

public class GuardCover : MonoBehaviour
{
    Dictionary<Transform, float> coverMemory = new Dictionary<Transform, float>();

    public Camera playerCamera;
    public LayerMask visionBlockers;
    public int failedPeeks = 0;
    public int maxFailedPeeks = 2;
    
    public float playerViewAngle = 180f; //35
    // Start is called before the first frame update


    public float dangerWeight = 10f;

    public float coverWaitTime = 1.5f;
    public float aiLeanDistance = 5f;
    public float coverWaitTimer = 0f;
    public float aiLeanSpeed = 3f;
    public float guardHeadHeight = 1f;
    public float fakePeekChance = 30f;
    public float trapShotChance = 25f;
    public float concealmentBonus = 25f;
    public float shadowBonus = 25f;
    public float bushBonus = 20f;
    public float TallGrassBonus = 18f;
    public float FogBonus = 17f;
    public float SmokeBonus = 15f;
    public float TreeBonus = 12f;
    public float RockBonus = 30;
    public float escapeBonus = 15f;
    public float flankBonus = 20f;
    public float maxCoverDistance = 20f;
    public float searchRadius = 12f;
    public float leftSuppression = 0f;
    public float rightSuppression = 0f;
    public float upSuppression = 0f;
    public float exposureTime = 0f;
    public float maxSafeExposure = 2f;
    public float minimumPredictionToShoot = 50f;
    public float relocateDangerThreshold = 150f;
    public float safetyWeight = 1f;
    public float opportunityWeight = 1f;
    public float leftFailedPeeks = 0f;
    public float rightFailedPeeks = 0f;
    public float upperFailedPeeks = 0f;
    public float sideFailedPeeks = 0f;

    private bool finalVisiblity = false;
    private bool nextLeanLeft = true;
    private bool bestNewCover = false;

    public Vector3 coverPosition;

    private Vector3 leftLeanPosition;
    private Vector3 rightLeanPosition;
    private Vector3 upLeanPosition;
    private Vector3 upPeekPosition;

    public Transform playerTarget;
    
    
    private Transform currentCover;
    private Transform previousCover;

    private float previousScore = 0f;
    private float newScore = 0f;
    private float bestScore = 0f;
    private float leftDanger = 0f;
    private float rightDanger = 0;
    private float upDanger = 0;
    private float silentTimer = 0;

    GuardMovement guardMovement;
    GuardAI guardAI;
    CoverPoint coverPoints;
    GuardVision guardVision;
    float GetCoverScore(Transform cover) {
        float score = 0f;
        score += Vector3.Distance(transform.position, cover.position);
        return score;
    }
    public Transform ChoosePatrolCheckPoint(
                    Vector3 guardPosition,
                    float checkRadius,
                    List<Transform> recentlyChecked,
                    Transform[] coverArray) {

        Transform best = null;
        float bestDistance = float.MaxValue;
        foreach (Transform cover in coverArray)
        {
            if (cover == null) continue;
            //if (cover == lastChecked) continue;

            if (recentlyChecked.Contains(cover)) continue;

            float distance = Vector3.Distance(
                                guardPosition,
                                cover.position
                                );
            if (distance > checkRadius) continue;
            if (distance < bestDistance) {
                bestDistance = distance;
                best = cover;
            }

        }
        return best;
    }
    public void RememberBadCover(Transform currentCover, float amount) {
        if (currentCover == null) {
            return;
        }

        if (!coverMemory.ContainsKey(currentCover)) {
            coverMemory.Add(currentCover, 0f);
        }
        coverMemory[currentCover] += amount;

        //Debug.Log(
        //   "COVER MEMORY:"+
        //   currentCover.name +
        //   " danger memory = "+
        //   coverMemory[currentCover]
        //    );
    }
    private float GetCoverMemoryPenalty(Transform cover) {
        if (cover == null) {
            return 0f;
        }

        if (coverMemory.ContainsKey(cover)) {
            return coverMemory[cover];
        }

        return 0f;
    }
    public enum PeekDirection { 
    Left,
    Right,
    Up
    }
    PeekDirection bestPeekDirection;
    public PeekDirection lastPeekSide;
    public PeekDirection currentPeekSide;
    //public bool ShouldRelocate(float prediction)
    //{
    //    //return AreBothSidesDangerous() && ShouldRelocateAfterFailures() && prediction < 50f ;
    //    //ShouldRelocateAfterFailures() &&
    //    Debug.Log("iSuppression ShouldRelocate:" + (AreBothSidesDangerous() && prediction < 50f));
    //    return AreBothSidesDangerous() && prediction < 50f;
    //    //return false;
    //}
    public float GetRelocationPressure(float suppressionLevel) {
        //RefreshPeekDangers();
        float pressure = ((leftDanger * 0.4f) + (rightDanger  * 0.4f) + (suppressionLevel * 0.10f));
        //Debug.Log("LeftDanger: "+ leftDanger + 
        //        " RightDanger:"+ rightDanger +
        //        " SuppressionLevel:" + (suppressionLevel * 0.10f) +
        //        " PressureV :" + pressure);
        return Mathf.Clamp(pressure, 0f, 100f);
        
    }
    public bool AreBothSidesDangerous(float suppressionLevel) {
        //float pressure = GetRelocationPressure(suppressionLevel);
        //RefreshPeekDangers();
        //float averageDanger = (leftDanger + rightDanger) * 0.5f;
        //float relocateScore = averageDanger + (suppressionLevel * 0.5f);

        DevLog.Log("iSuppression leftDanger:" + leftDanger + " and rightDanger:" + rightDanger + " > relocateDangerThreshold:" + relocateDangerThreshold);
        return leftDanger >= relocateDangerThreshold && rightDanger >= relocateDangerThreshold && upDanger >= relocateDangerThreshold;
        //return pressure >= relocateDangerThreshold;
    }
    public bool SilentWait(float duration)
    {
        silentTimer += Time.deltaTime;
        if (silentTimer >= duration) {
            silentTimer = 0f;
            return true;
        }

        return false;

    }
    public bool ShouldFakePeek() {
        float difference = Mathf.Abs(leftSuppression - rightSuppression);

        return difference >= 25f;
    }
    public PeekDirection GetMostSuppressedSide() {
        if (leftSuppression >= rightSuppression) {
            return PeekDirection.Left;
        }else if(upSuppression >= rightSuppression)
        {
            return PeekDirection.Up;
        }
            return PeekDirection.Right;
    }
    public enum ConcealmentType
    {
        Shadow,
        Bush,
        TallGrass,
        Tree,
        Smoke,
        Fog,
        Rock
    }
    public LayerMask movementObstacleMask;
    public void StartExposure() {
        exposureTime = 0f;
    }
    public void TickExposure() {
        exposureTime += Time.deltaTime;

    }
    public void ResetFailedPeeks() {
        failedPeeks = 0;
    }
    public void ResetExposure() {
        exposureTime = 0f;
    }
    public bool HasEnoughPrediction(float prediction) {
        //Debug.Log("iSuppression hasEnoughPrediction:"+(prediction >= minimumPredictionToShoot));
        return prediction >= minimumPredictionToShoot;
    }
    public bool IsOverExposed() {
        return exposureTime >= maxSafeExposure;
    }
    public bool IsCurrentSideTooHot(float threshold = 150f) {

        DevLog.Log("Observe2 Lean failedPeek MostSuppressedSide: "+ GetMostSuppressedSide() + " leftSuppression: " + leftSuppression + " rightSuppresion:" + rightSuppression);
        if ((GetMostSuppressedSide() == PeekDirection.Left)&&(guardAI.sidePeek == PeekDirection.Left)) {
           
            return leftSuppression >= threshold;
            
        }
        if ((GetMostSuppressedSide() == PeekDirection.Right) && (guardAI.sidePeek == PeekDirection.Right))
        {
            return rightSuppression >= threshold;
        }
        if ((GetMostSuppressedSide() == PeekDirection.Up) && (guardAI.sidePeek == PeekDirection.Up))
        {
            return upSuppression >= threshold;
        }
        return false;
    }
   

    public bool IsReachable(Transform Cover) {
        CoverPoint point = Cover.GetComponent<CoverPoint>();
        float distance = Vector3.Distance(transform.position, Cover.position);
        //Debug.Log(Cover.name + " Too Far it enters distance:"+distance+" > maxCoverDistance:"+ maxCoverDistance);
        Vector3.Distance(
            transform.position,
            Cover.position
        );

        if (distance > maxCoverDistance)
        {
            //Debug.Log("hit2 " + Cover.name + " Too Far");
            return false;
        }
        Vector3 start = transform.position + Vector3.up * 0.3f;
        Vector3 target = Cover.position;
        //Vector3 target = Cover.position + Vector3.up * 0.5f;
        Vector3 direction = target - start;

        distance = direction.magnitude;
        //Debug.Log(" movementObstacleMask:" + movementObstacleMask.value);
        RaycastHit hit;
        //Debug.DrawLine(
        //    start,
        //    target,
        //    Color.black,
        //    10f
        //    );

     
        LayerMask mask = 1 << 6;

        if (Physics.Raycast(
            start,
            direction.normalized,
            out hit,
            distance,
            mask))
        {
            //Debug.Log("hit2 "+Cover.name+" blocked by "+hit.collider.name);
            return false;
        }

        if (point != null && point.occupied) {
            return false;
        }
       
        return true;
    }

    public Transform ChooseSearchPoint(
        Vector3 origin,
        Vector3 likelyDirection,
        List<Transform> alreadySearched,
        Transform[] coverArray)
    {
        List<Transform> candidates = new List<Transform>();

        if (coverArray != null && coverArray.Length > 0)
        {
            candidates.AddRange(coverArray);
        }
        else
        {
            CoverPoint[] points = FindObjectsOfType<CoverPoint>();
            foreach (CoverPoint cp in points)
            {
                candidates.Add(cp.transform);
            }
        }

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (Transform cover in candidates)
        {
            if (cover == null) continue;
            if (alreadySearched.Contains(cover)) continue;
            if (!IsReachable(cover)) continue;
            
            float distFromOrigin = Vector3.Distance(origin, cover.position);
            if (distFromOrigin > searchRadius) continue;
            
            Vector3 toPoint = (cover.position - origin).normalized;
            float directionBonus = Vector3.Dot(toPoint, likelyDirection) * -5f;
            float score = distFromOrigin + directionBonus;

            //Debug.Log("SEARCH POINT cover: " + cover.name);
            if (score < bestScore)
            {
                bestScore = score;
                best = cover;
            }
        }

        if (best != null)
        {
            DevLog.Log("For fire SEARCH POINT chosen: " + best.name + " score=" + bestScore);
        }
        else
        {
            DevLog.Log("SEARCH POINT : none found near " + origin);
        }

        return best;
    }

    public Transform ChooseConnectedSearchPoint(
        CoverPoint fromPoint,
        Vector3 origin,
        Vector3 likelyDirection,
        List<Transform> alreadySearched,
        Transform[] coverArray)
    {
        if (fromPoint != null)
        {
            foreach (CoverPoint neighbor in fromPoint.connectedCovers)
            {
                if (neighbor == null) continue;
                Transform neighborTransform = neighbor.transform;
                if (alreadySearched.Contains(neighborTransform)) continue;
                if (!IsReachable(neighborTransform)) continue;

                DevLog.Log("For fire SEARCH POINT : neightbor: " + neighborTransform);
                return neighborTransform;
            }
        }

        return ChooseSearchPoint(origin, likelyDirection, alreadySearched, coverArray);
    }

    bool IsCoverVisibleToPlayer(Transform cover)
    {
            //Debug.DrawRay(
            //playerCamera.transform.position,
            //playerCamera.transform.forward * 20f,
            //Color.black
            //        );
       
        Vector3 playerEye =playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
        Vector3 target = cover.position + Vector3.up * guardHeadHeight;

        //Debug.DrawRay(
        //         playerEye,
        //         playerCamera.transform.forward * 5f,
        //         Color.black
        //            );

        //Debug.DrawLine(
        //    playerEye,
        //    target,
        //    Color.white
        //);
        Vector3 direction =target - playerEye;

        float angle =
            Vector3.Angle(
                playerCamera.transform.forward,
                direction.normalized
            );

                    if (angle > playerViewAngle)
                    {
                        //Debug.Log( cover.name +" Angle = " +angle);
                        return false;
                    }

        RaycastHit hit;

        if (Physics.Raycast(
            playerEye,
            direction.normalized,
            out hit,
            direction.magnitude,
            visionBlockers))
        {
            //Debug.Log(
            //    cover.name +
            //    " BLOCKED by " +
            //    hit.collider.name
            //);

            return false;
        }

        return true;
    }
    public void SetCoverWeights(float newSafetyWeight, float newOpportunityWeight) {
        safetyWeight = newSafetyWeight;
        opportunityWeight = newOpportunityWeight;

        //Debug.Log(
        //    " COVER WEIGHT | Safety:"+safetyWeight +
        //    " | Opportunity: "+opportunityWeight
        //    );
    
    }
    float GetSafetyScores(Transform cover) {
        coverPoints = cover.GetComponent<CoverPoint>();
        if (coverPoints == null) {
            return 50f;
        }
        float score = 100f - coverPoints.safetyScore;
        

        

        return score;
    }
    float GetOpportunityScores(Transform cover) {
        coverPoints = cover.GetComponent<CoverPoint>();
        if (coverPoints == null) { 
        return 0f;
        }

        return -coverPoints.opportunityScore;
    }
    private readonly Vector3[] bodyOffsets = {
        new Vector3 (0f,1.7f, 0f), //head
        new Vector3 (0f, 1.1f, 0f), //Chest
        new Vector3(0f, 0.5f, 0f)  // Legs
    }; 
    public float GetCoverOcclusionScore(Transform coverPoint)
    {
        int blockedCount = 0;
        int protectionLayers = 0;
        float protectionScore = 0f;
        float bodyScore = 0f;
        Vector3 start = playerTarget.position + Vector3.up * 1.6f;
        Vector3 target = Vector3.zero;
        Vector3 direction = Vector3.zero;
        bool bodyPartBlocked = false;
        float distance = 0f;
        string colliderName = "";
        HashSet<Transform> blockers = new HashSet<Transform>();
        foreach (Vector3 offset in bodyOffsets)
        {
            bodyPartBlocked = false;
            target = coverPoint.position + offset;
            direction = target - start;
            distance = direction.magnitude;
            direction.Normalize();
            //Debug.DrawRay(
            //start,
            //direction * distance,
            //Color.cyan
            //);
            RaycastHit[] hits = Physics.RaycastAll(
                start,
                direction,
                distance,
                visionBlockers
                );
            System.Array.Sort(
                hits,
                (a,b) =>
                a.distance.CompareTo(b.distance)
                
                );
            if(hits.Length > 0) {
                bodyPartBlocked = true;
            }
            if (bodyPartBlocked) {
                blockedCount++;
            }
            foreach (RaycastHit hit in hits) {
                //        Debug.Log(
                //    hit.collider.name +
                //    "Distance: " +
                //    hit.distance
                //);
            blockers.Add(hit.transform);
                protectionLayers = blockers.Count;
            }
            //Debug.Log("Occluded protectionLayers: " + protectionLayers);
            protectionScore = Mathf.Clamp(
                protectionLayers * 25f,0f, 100f
                        );
                //Debug.Log( "Occluded Protection Layers: " +protectionLayers +" Protection Score: " +protectionScore+" block Count: "+ blockedCount);
            //RaycastHit hit;

            //if (Physics.Raycast(
            //    start,
            //    direction,
            //    out hit,
            //    distance,
            //    visionBlockers
            //    ))
            //{
            //    blockedCount++;
            //    colliderName = hit.collider.name;
            //    Debug.Log("Occluded by: " + colliderName + " blockCount: "+blockedCount);

            //}
            //else
            //{
            //    Debug.Log("Occluded No Occlusion ");
            //}

        } //For Loop
        bodyScore = (blockedCount / (float)bodyOffsets.Length) * 100f;
        
        //float score = (blockedCount / 3f) * 100f;

        //Debug.Log("Occluded by: " + colliderName + " Occlussion Score: "+score+ "Blocked: " +blockedCount);
        //Vector3 target = coverPoint.position + Vector3.up * 1.0f;
        //Vector3 direction = target - start;
        //float distance = direction.magnitude;
        //direction.Normalize();
        float finalOcclussionScore = (bodyScore * 0.7f) + (protectionScore * 0.3f);
        //Debug.Log("Blocked Score Body Score: " + bodyScore + 
        //         " protection Score: " + protectionScore + 
        //         " protectionLayers: " + protectionLayers +
        //         " finalOcclusionScore: "+ finalOcclussionScore);
        return finalOcclussionScore;
    }
    public float GetCoverScores(Transform cover) {
        coverPoints = cover.GetComponent<CoverPoint>();
        //float coverFacingScore = 0f;
        float dangerScore = GetDangerScore(cover);
        float score = dangerScore;
        //Vector3 directionToPlayer = (playerTarget.position - cover.position);
        //float facing = Vector3.Dot(cover.forward, directionToPlayer);
        //Debug.Log(cover.name +": " +facing);
        float occlusionScore = GetCoverOcclusionScore(cover);
        score -= occlusionScore;
        if (coverPoints == null) {
            return score;
        }
        
        score -= coverPoints.escapeScore * 5f;
        bool visible = IsCoverVisibleToPlayer(cover);

        if (cover == previousCover)
        {
            score += 20f;
        }
        float memoryPenalty = GetCoverMemoryPenalty(cover);
        score += memoryPenalty;
        float SafetyScores = GetSafetyScores(cover);
        score += SafetyScores * safetyWeight;
        float OpportunityScores = GetOpportunityScores(cover);
        score += OpportunityScores * opportunityWeight;

        

        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Shadow))
        {
            
            score -= shadowBonus;
            //Debug.Log("Guard Entered decreasing danger points for shadow.");
        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Bush))
        {
            //Debug.Log("Guard Entered decreasing danger points for bush.");
            score -= bushBonus;

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Tree))
        {
            //Debug.Log("Guard Entered decreasing danger points for tree.");
            score -= TreeBonus;

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Smoke))
        {
            //Debug.Log("Guard Entered decreasing danger points for Smoke.");
            score -= SmokeBonus;

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.TallGrass))
        {
            //Debug.Log("Guard Entered decreasing danger points for TallGrass.");
            score -= TallGrassBonus;

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Rock))
        {
            //Debug.Log("Guard Entered decreasing danger points for Rock.");
            score -= RockBonus;

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Fog))
        {
            //Debug.Log("Guard Entered decreasing danger points for Fog.");
            score -= FogBonus;

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.EscapeSpace))
        {
            //Debug.Log("Guard Entered decreasing danger points for EscapeBonus.");
            score -= escapeBonus;

        }
        float flankScore = GetFlankScore(cover);
        score += flankScore;
        if (!visible)
        {

            score -= concealmentBonus;

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Rock))
        {
            //Debug.Log(
            //    " COVER SCORE | " + CoverPoint.ConcealmentType.Rock +
            //    " COVER SCORE : " + cover.name +

            //    " | Memory: " + memoryPenalty +
            //    " | SafetyRaw: " + SafetyScores +
            //    " | SafetyWeight: " + safetyWeight +
            //    " | OpportunityRaw: " + OpportunityScores +
            //    " | OpportunityWeight: " + opportunityWeight +

            //    " | Shadow: " + shadowBonus +
            //    " | bushBonus: " + bushBonus +
            //    " | TreeBonus: " + TreeBonus +
            //    " | SmokeBonus: " + SmokeBonus +
            //     " | TallGrassBonus: " + TallGrassBonus +
            //     " | RockBonus: " + RockBonus +
            //     " | FogBonus: " + FogBonus +
            //     " | escapeBonus: " + escapeBonus +
            //      " | flankScore: " + flankScore +
            //      " | concealmentBonus: " + concealmentBonus +
            //      " | dangerScore: " + dangerScore +
            //    " | FINAL: " + score
            //    );

        }
        if (coverPoints.concealmentTypes.Contains(CoverPoint.ConcealmentType.Bush))
        {
            //Debug.Log(
            //    " COVER SCORE | " + CoverPoint.ConcealmentType.Bush +
            //    " COVER SCORE : " + cover.name +

            //    " | Memory: " + memoryPenalty +
            //    " | SafetyRaw: " + SafetyScores +
            //    " | SafetyWeight: " + safetyWeight +
            //    " | OpportunityRaw: " + OpportunityScores +
            //    " | OpportunityWeight: " + opportunityWeight +

            //    " | Shadow: " + shadowBonus +
            //    " | bushBonus: " + bushBonus +
            //    " | TreeBonus: " + TreeBonus +
            //    " | SmokeBonus: " + SmokeBonus +
            //     " | TallGrassBonus: " + TallGrassBonus +
            //     " | RockBonus: " + RockBonus +
            //     " | FogBonus: " + FogBonus +
            //     " | escapeBonus: " + escapeBonus +
            //      " | flankScore: " + flankScore +
            //      " | concealmentBonus: " + concealmentBonus +
            //      " | dangerScore: " + dangerScore +
            //    " | FINAL: " + score
            //    );

        }


        return score;
    }
    public void TestRiskReward(Transform[] covers) {
        //Debug.Log("COVER SCORES ======= SAFE TEST =======");
        SetCoverWeights(1.5f, 0.5f);
        //Transform safeWinner = ChooseBestCover(covers);
        //Debug.Log("COVER SCORES SAFE WINNER: "+(safeWinner != null ? safeWinner.name : "NONE"));
        //Debug.Log("COVER SCORES ======= AGGRESSIVE TEST =======");
        SetCoverWeights(0.7f, 1.5f);
        Transform aggressiveWinner = ChooseBestCover(covers);
        //Debug.Log("COVER SCORES AGGRESSIVE WINNER: " + (aggressiveWinner != null ? aggressiveWinner.name : "NONE"));
    }
    public Transform ChooseBestCover(Transform[] covers)

    {
        bool visible = false;

        if (covers == null || covers.Length == 0)
        {
            return null;
        }
        
        Transform bestCover = covers[0];
        bestScore = GetCoverScores(bestCover);
        foreach (Transform cover in covers)
        {
            
            float score = GetCoverScores(cover);
            if (bestNewCover) {
                bestCover = cover;
                bestScore = score;
                bestNewCover = false;
            }

            if (!IsReachable(cover)) {
                if (bestCover == cover) {

                    bestNewCover = true;
                }
                continue;
            }
            //Debug.Log("hit2 what cover had passed:"+cover+" score:"+score+" < bestScore:"+bestScore);
            if (score < bestScore)
            {
                bestCover = cover;
                bestScore = score;
                finalVisiblity = visible;
                
            }

            //Debug.Log("All best cover:" + cover.name + " visibility:"+ visible + " score:" + score+" previous:"+ previousScore+" OF "+previousCover);
            //previousScore = score;
            //previousCover = cover;
        }
        previousCover = bestCover;
        DevLog.Log("The winning best cover :" + bestCover.name + " visibility = "+ finalVisiblity + " score:" + bestScore);
        return bestCover;
    }
    //public Transform ChooseBestCover(Transform[] covers)
    //{
    //    Debug.Log("visible cover name GAME!");
    //    bool isSetNewBestScore = false;
    //    if (covers == null || covers.Length == 0)
    //    { return null; }
    //        Transform best = covers[0];
    //        float bestScore = GetDangerScore(best);
    //        foreach (Transform cover in covers)
    //        {
    //        float score = 0;
    //        Debug.Log("visible cover name in the list:" + cover.name);
    //        if (isSetNewBestScore) { isSetNewBestScore = false; bestScore = GetDangerScore(cover); }
    //        if (IsCoverVisibleToPlayer(cover)) {
    //            //score = 100f;
    //            Debug.Log("visible cover name:"+cover.name);
    //        }

    //        //     score += GetDangerScore(cover);
    //        //Debug.Log("Previous Cover:" + previousCover + ". Now the cover name:"+cover+" the score:"+score+" < the best cover now is " + bestScore);
    //        //if (score < bestScore)
    //        //    {
    //        //        best = cover;
    //        //        bestScore = score;

    //        //    }
    //        }
    //    previousCover = best;
    //    //Debug.Log("Previous Cover:" + previousCover + " and the best cover now is "+ best);
    //    //Debug.Log("Be*st Cover for now is "+ best);
    //    return best;


    //}
    float GetDangerScore(Transform cover) {
        
        float score = Vector3.Distance(transform.position, cover.position);
        Vector3 direction = playerTarget.position - cover.position;

        RaycastHit hit;

        if (Physics.Raycast(
            cover.position,
            direction.normalized,
            out hit,
            direction.magnitude
            )) {
            if (hit.transform == playerTarget.transform) {
                //Debug.Log("Got Danger Weight");
                score += dangerWeight;
            }
        
        }
        //Debug.Log("Cover name:" + cover + " and its score:" + score);
        return score;
    }
    float GetFlankScore(Transform cover)
    {

        Vector3 currentDirection = coverPosition - playerTarget.position;
        Vector3 candidateDirection = cover.position - playerTarget.position;
        float angle = Vector3.Angle(
            currentDirection,
            candidateDirection
            );
        float moveDistance = Vector3.Distance(
               coverPosition,
               cover.position
            );
        if (moveDistance < 3f)
        {
            return 0f;
        }
        if (angle > 45f) {
            return -(angle/180f) * flankBonus;
        }
        
        //Debug.Log(cover.name+" angle = "+angle);
        return 0f;
    }    

    void Start()
    {
        guardMovement = GetComponent<GuardMovement>();
        guardAI = gameObject.GetComponent<GuardAI>();
        guardVision = gameObject.GetComponent<GuardVision>();

    }
   
    public void AddSideSuppression(Vector3 bulletPosition, float amount)
    { //From WeaponSystem.cs
        float leftDistance = Vector3.Distance(bulletPosition, leftLeanPosition);
        float rightDistance = Vector3.Distance(bulletPosition, rightLeanPosition);
        float upDistance = Vector3.Distance(bulletPosition, upPeekPosition);

        if ((leftDistance <= rightDistance) && (leftDistance <= upDistance))
        {
            leftSuppression += amount;
            leftSuppression = Mathf.Clamp(
            leftSuppression,
            0,
            200
            );
            DevLog.Log("Observe2 Lean SideSuppress left iSuppression:" + leftSuppression);
            //Debug.DrawLine(
            //    bulletPosition,
            //    leftLeanPosition,
            //    Color.black,
            //    10f
            //    );

        }
        else if ((leftDistance >= rightDistance) && (upDistance >= rightDistance))
        {
            rightSuppression += amount;
            rightSuppression = Mathf.Clamp(
                rightSuppression,
                0,
                200
                );
            //Debug.DrawLine(
            //    bulletPosition,
            //    rightLeanPosition,
            //    Color.black,
            //    10f
            //    );
            DevLog.Log("Observe2 Lean SideSuppress right iSuppression:" + rightSuppression);

        }
        else
        {
            upSuppression += amount;
            upSuppression = Mathf.Clamp(
                upSuppression,
                0f,
                200f
                );
            DevLog.Log("Observe2 Lean SideSuppress Upper iSuppression : " + upSuppression);

        }
        if ((leftSuppression >= 150f) || (rightSuppression >= 150f) || (upSuppression >= 150f))
        {
            RememberBadCover(currentCover, 10f);
        }
        guardVision.PlayerRepeatedShot(playerTarget.transform.position);

    }
    void EvaluatePeekDirection() {
        bestPeekDirection = PeekDirection.Left;
        float lowest = leftDanger;
        if (rightDanger < lowest) {
            lowest = rightDanger;
            bestPeekDirection = PeekDirection.Right;
         
        }
        if (upDanger < lowest)
        {
            lowest = upDanger;
            bestPeekDirection = PeekDirection.Up;
        }
        DevLog.Log("Observe2 Lean BEST PEEK: "+bestPeekDirection
                    +" | Left: "+ leftDanger+
                    " | Right: "+rightDanger+
                    " | Up: "+upDanger);
        
    }
    public PeekDirection GetBestPeekDirection()
    {
        EvaluatePeekDirection();
        return bestPeekDirection;
    }
    public void RefreshPeekDangers()
    {
        
        leftDanger = GetPeekDanger(leftLeanPosition, leftSuppression);
        rightDanger = GetPeekDanger(rightLeanPosition, rightSuppression);
        upDanger = GetPeekDanger(upPeekPosition, upSuppression);
        leftDanger = leftDanger + leftFailedPeeks;
        rightDanger = rightDanger + rightFailedPeeks;
        upDanger = upDanger + upperFailedPeeks;
        DevLog.Log("PressureV leftDanger: " + leftDanger +
                " leftFailedPeeks:" + leftFailedPeeks +

            " rightDanger: " + rightDanger +
            " rightFailedPeeks:" + rightFailedPeeks+
            " upDanger:"+ upDanger+
            " upDangerFailedPeeks: "+ upperFailedPeeks);
        EvaluatePeekDirection();
    }
    float GetPeekDanger(Vector3 peekPosition, float sideSuppression)
    {
        float danger = 0f;
        float distance = 0f;
        Vector3 playerEye = playerCamera.transform.position;
        Vector3 direction = peekPosition - playerEye;

        RaycastHit hit;
        if (Physics.Raycast(
            playerEye,
            direction.normalized,
            out hit,
            direction.magnitude,
            visionBlockers
            ))
        {
            danger += 0;
        //    Debug.DrawLine(
        //    playerEye,
        //    peekPosition,
        //    Color.black,
        //    10f
        //);

        }
        else
        {
            danger += 60f;
        //    Debug.DrawLine(
        //    playerEye,
        //    peekPosition,
        //    Color.white,
        //    10f
        //);
        }
        distance = Vector3.Distance(playerTarget.position, peekPosition);
        danger += 20f / distance;
        danger += (guardAI.GetSuppressionLevel() * 0.10f) + sideSuppression;
        //Debug.Log(peekPosition + " danger = " + danger);
        return danger;
    }
    public bool leanUp()
    {
        //upLeanPosition = coverPosition + Vector3.up * 0.4f;
        transform.position = Vector3.Lerp(
            transform.position,
            upPeekPosition,
            Time.deltaTime *aiLeanSpeed
            );
        if (Vector3.Distance(
            transform.position,
            upPeekPosition
            ) < 0.05f) {
            lastPeekSide = PeekDirection.Up;
            currentPeekSide = PeekDirection.Up;
            //RefreshPeekDangers();
            //Debug.Log("Observe2 Lean Up Successfully!");
            return true;
        }
        return false;
    }
    public bool leanLeft()
    {
        //Vector3 leftLeanPosition = coverPosition - transform.right * aiLeanDistance;
        transform.position = Vector3.Lerp(
            transform.position,
            leftLeanPosition,
            Time.deltaTime * aiLeanSpeed
            );
        //guardMovement.MoveTo(leftLeanPosition);
        //transform.position += -transform.right * 2f * Time.deltaTime;
        //Debug.Log("Lean Left: TRUE that the distance is " + Vector3.Distance(transform.position, leftLeanPosition) + " < 0.05f");
        //Debug.Log("Transform.Position:" + transform.position);
        //Debug.Log("leftLeanPosition:" + leftLeanPosition);
        //Debug.Log("TRANSFORM.RIGHT:" + transform.right);
        //transform.position = leftLeanPosition;
        if (Vector3.Distance(transform.position, leftLeanPosition) < 0.05f)
        {
            //isLeaningLeft = false;
            //isFiringFromCover = true;
            // leftDanger = GetPeekDanger(leftLeanPosition, leftSuppression);
            //Debug.Log("Left Lean Danger2 "+ leftDanger);
            //Debug.Log("Observe2 Lean Left complete!");


            lastPeekSide = PeekDirection.Left;
            currentPeekSide = PeekDirection.Left;
            //RefreshPeekDangers();
            return true;
        }
        return false;
    }
    public bool leanRight()
    {
        //Vector3 target = coverPosition + transform.right * aiLeanDistance;

        //Vector3 rightLeanPosition = coverPosition + transform.right * aiLeanDistance;
        transform.position = Vector3.Lerp(
            transform.position,
            rightLeanPosition,
            Time.deltaTime * aiLeanSpeed
            );
        //Debug.Log("Observe2 Lean Right: TRUE");
        if (Vector3.Distance(transform.position, rightLeanPosition) < 0.05f)
        {
            //isLeaningRight = false;
            //isFiringFromCover = true;A
            //rightDanger = GetPeekDanger(rightLeanPosition, rightSuppression);
            //Debug.Log("Right Lean Danger2 " + rightDanger);
            //Debug.Log("Observe2 Right lean complete!");

            lastPeekSide = PeekDirection.Right;
            currentPeekSide = PeekDirection.Right;
            //RefreshPeekDangers();
            return true;
        }
        else return false;
    }
    
    public void SetCover(Transform cover) {
        currentCover = cover;
        coverPosition = cover.position;
        DevLog.Log("Set To Cover");
    }
    public bool MoveToCover(Transform cover) {
        currentCover = cover;
        //coverPosition = transform.position;
        //Debug.Log("MOVE TO Current Cover Position:"+ currentCover.position);
        //guardMovement.MoveTo(currentCover.position);
        if (guardMovement.MoveTo(currentCover.position))
        {
            //isMovingToCover = false;

            //isInCover = true;
            //isWaitingInCover = true;

            //coverWaitTimer = 0f;
            //coverPosition = transform.position; 
            coverPosition = transform.position;
            leftLeanPosition = coverPosition - transform.right * aiLeanDistance;
            rightLeanPosition = coverPosition + transform.right * aiLeanDistance;
            upPeekPosition = coverPosition + Vector3.up * 1.6f;//0.4
            leftDanger = 0f;
            rightDanger = 0f;
            upDanger = 0f;
            leftSuppression = 0;
            rightSuppression = 0;
            upSuppression = 0f;
            ResetFailedPeeks();
            return true;
        }
        //Debug.Log("Move To Cover Result:"+Vector3.Distance(transform.position, currentCover.position));
        //Debug.Log("Observe2 Lean UP PEEK POSITION : "+upPeekPosition+
        //            " | UP SUPPRESSION: "+upSuppression);

        return false;
    }
    public bool trapShotFire() {
        float roll = Random.Range(0f, 100f);
        if (roll <= trapShotChance)
        {
            //Debug.Log("BRO TrapShotFire:TRUE");
            return true;
        }
        //Debug.Log("BRO TrapShotFire:FALSE");
        return false;
    }
    public bool fakePeek(float suppressionLevel) {
        float roll = Random.Range(0f, 100f);
        float chance = fakePeekChance;
        chance += suppressionLevel * 0.2f;
        chance = Mathf.Clamp(
            chance,
            0f, 
            (fakePeekChance+ chance)
            );
        if (roll <= chance) {
            //Debug.Log("iSuppression BRO Fake Peek: TRUE");
            return true;
        }
        //Debug.Log("BRO Fake Peek: FALSE");
        return false;
    }
    public bool WaitInCover(float randomWaitTime) {
        
        coverWaitTimer += Time.deltaTime;
        
        

        if (coverWaitTimer >= randomWaitTime)
        {
            coverWaitTimer = 0f;
           
                
                //Debug.Log("Peek Wait Right: " + randomWaitTime);
                return true;
            
        }
        //Debug.Log("Peek Wait Less: " + randomWaitTime);
        return false;
    }
    public bool lean() {
        if (nextLeanLeft) {
            

            return true; 
        }

        return false;
    }
    
    

    public bool ReturnToCover() {
        transform.position = Vector3.Lerp(
                                        transform.position,
                                        coverPosition,
                                        Time.deltaTime * aiLeanSpeed
                                        );
        //Debug.Log("Is Returning for Cover Now!");
        sideFailedPeeks = 0f;
        if (Vector3.Distance(transform.position, coverPosition) < 0.05f)
        {
            
            //isReturningToCover = false;
            //Debug.Log("Return behind that cover!");
            guardMovement.isPeekingSide = false;
            RefreshPeekDangers();

            return true;
            
        }
        return false;
    }
    public bool ShouldRelocateAfterFailures()
    {
        return failedPeeks >= maxFailedPeeks;
    }
    public void ToggleLeanSide()
    {
        nextLeanLeft = !nextLeanLeft;
    }
   
    public void RegisterFailedPeek()
    {
        failedPeeks++;
        sideFailedPeeks++;
        if (guardAI.sidePeek == PeekDirection.Left)
        {
            leftFailedPeeks += sideFailedPeeks * 10f;
        }
        else if (guardAI.sidePeek == PeekDirection.Right) {
            rightFailedPeeks += sideFailedPeeks * 10f;
        }else if(guardAI.sidePeek == PeekDirection.Up)
        {
            upperFailedPeeks += sideFailedPeeks * 10f;
        }

            RememberBadCover(currentCover, 15f);
        DevLog.Log("Vertical iSuppression failedPeeks: sidePeek:"+ guardAI.sidePeek +" failedPeeks: "+ sideFailedPeeks);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
