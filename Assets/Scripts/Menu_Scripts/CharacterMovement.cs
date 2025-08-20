using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    [Header("Path Settings")]
    public PathMaps pathMap;
    public float moveDuration = 2f;
    public bool startAtUnlockedLevel = true;
    public static int currentIndex = 0;
    [Header("Level Progress")]
    public int unlockedLevel =0;

    private void Start()
    {
       /* if (startAtUnlockedLevel)
        {
            // Move to current unlocked level on start
            transform.position = pathMap.Waypoints[unlockedLevel].position;
        }*/
    }

    
    public void CompleteLevel(int num)
    {
        
        MoveTo(num);
    }

    public void MoveTo(int targetIndex)
    {
         currentIndex = GetClosestWaypointIndex();

        if (targetIndex >= pathMap.Waypoints.Count || targetIndex == currentIndex)
            return;

        StopAllCoroutines();
        StartCoroutine(MoveAlongPath(currentIndex, targetIndex));
    }

    private IEnumerator MoveAlongPath(int startIndex, int endIndex)
    {
        float time = 0f;

        if (pathMap.IsCurved)
        {
            int[] indexes = pathMap.GetSplinePointIndexes(startIndex, true);
            Vector3 a = pathMap.Waypoints[indexes[0]].position;
            Vector3 b = pathMap.Waypoints[indexes[1]].position;
            Vector3 c = pathMap.Waypoints[indexes[2]].position;
            Vector3 d = pathMap.Waypoints[indexes[3]].position;

            while (time < moveDuration)
            {
                float t = time / moveDuration;
                transform.position = SplineCurve.GetPoint(a, b, c, d, t);
                time += Time.deltaTime;
                yield return null;
            }

            transform.position = pathMap.Waypoints[endIndex].position;
        }
        else
        {
            Vector3 startPos = pathMap.Waypoints[startIndex].position;
            Vector3 endPos = pathMap.Waypoints[endIndex].position;

            while (time < moveDuration)
            {
                float t = time / moveDuration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                time += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
        }
    }

    private int GetClosestWaypointIndex()
    {
        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < pathMap.Waypoints.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, pathMap.Waypoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}
