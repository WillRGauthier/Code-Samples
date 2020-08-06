using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class AsteroidInstance : MonoBehaviour
{
    private Collider coll;
    private Rigidbody rb;

    private AsteroidController controller;
    private float speed;
    private int levelIndex;
    private float pointVal;
    private BoxCollider playBounds;

    private Vector3 moveDir;
    private bool isMoving = false;

    public void Initialize(AsteroidController inController, float inPointVal, int inLevelIndex, BoxCollider inPlayBounds)
    {
        controller = inController;
        pointVal = inPointVal;
        levelIndex = inLevelIndex;
        playBounds = inPlayBounds;
    }

    public int GetLevelIndex()
    {
        return levelIndex;
    }

    public float GetPointVal()
    {
        return pointVal;
    }

    public void SetSpeed(float inSpeed)
    {
        speed = inSpeed;
    }

    public void NotifyHit(bool givePoints, bool screenwrappedHit, bool bulletHit)
    {
        controller.SplitAsteroid(this, givePoints, screenwrappedHit, bulletHit);
    }

    public void StartMoving()
    {
        isMoving = true;
        moveDir = GetRandomDirectionFromAsteroidCenterToPlayBounds().normalized;
        GetComponent<Screenwrap>().enabled = true;
        rb.isKinematic = false;
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    public void Release()
    {
        isMoving = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        GetComponent<Screenwrap>().enabled = false;
        ObjectPool.Instance.ReleaseObject(gameObject);
    }

    void Awake()
    {
        coll = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if(isMoving)
        {
            rb.velocity = moveDir * speed;
            rb.angularVelocity = transform.forward * speed;
        }
    }

    Vector3 GetRandomDirectionFromAsteroidCenterToPlayBounds()
    {
        // If asteroid center outside of play bounds
        // Sectors extend from center to edges of play bounds and angle is acute
        if(!playBounds.bounds.Contains(coll.bounds.center))
        {
            Vector3 sectorSide1, sectorSide2;
            sectorSide1 = sectorSide2 = Vector3.zero;
            // Find play bounds side asteroid is outside of
            // Left side
            if (coll.bounds.center.x < playBounds.bounds.min.x)
            {
                sectorSide2.y = playBounds.bounds.min.y;
                sectorSide1.y = playBounds.bounds.max.y;
                // If asteroid bounds extend to a perpendicular side, include that direction
                // Else only include the outside of side
                if(coll.bounds.min.y <= playBounds.bounds.min.y)
                {
                    sectorSide2.x = playBounds.bounds.max.x;
                }
                else
                {
                    sectorSide2.x = playBounds.bounds.min.x;
                }
                if(coll.bounds.max.y >= playBounds.bounds.max.y)
                {
                    sectorSide1.x = playBounds.bounds.max.x;
                }
                else
                {
                    sectorSide1.x = playBounds.bounds.min.x;
                }
            }
            // Top side
            else if(coll.bounds.center.y > playBounds.bounds.max.y)
            {
                sectorSide1.x = playBounds.bounds.min.x;
                sectorSide2.x = playBounds.bounds.max.x;
                if (coll.bounds.min.x <= playBounds.bounds.min.x)
                {
                    sectorSide1.y = playBounds.bounds.min.y;
                }
                else
                {
                    sectorSide1.y = playBounds.bounds.max.y;
                }
                if (coll.bounds.max.x >= playBounds.bounds.max.x)
                {
                    sectorSide2.y = playBounds.bounds.min.y;
                }
                else
                {
                    sectorSide2.y = playBounds.bounds.max.y;
                }
            }
            // Right side
            else if(coll.bounds.center.x > playBounds.bounds.max.x)
            {
                sectorSide2.y = playBounds.bounds.min.y;
                sectorSide1.y = playBounds.bounds.max.y;
                if (coll.bounds.min.y <= playBounds.bounds.min.y)
                {
                    sectorSide2.x = playBounds.bounds.min.x;
                }
                else
                {
                    sectorSide2.x = playBounds.bounds.max.x;
                }
                if (coll.bounds.max.y >= playBounds.bounds.max.y)
                {
                    sectorSide1.x = playBounds.bounds.min.x;
                }
                else
                {
                    sectorSide1.x = playBounds.bounds.max.x;
                }
            }
            // Bottom side
            else
            {
                sectorSide2.x = playBounds.bounds.min.x;
                sectorSide1.x = playBounds.bounds.max.x;
                if (coll.bounds.min.x <= playBounds.bounds.min.x)
                {
                    sectorSide2.y = playBounds.bounds.max.y;
                }
                else
                {
                    sectorSide2.y = playBounds.bounds.min.y;
                }
                if (coll.bounds.max.x >= playBounds.bounds.max.x)
                {
                    sectorSide1.y = playBounds.bounds.max.y;
                }
                else
                {
                    sectorSide1.y = playBounds.bounds.min.y;
                }
            }
            return GetDirectionFromArcSectors(sectorSide1, sectorSide2);
        }
        // If entire asteroid in play bounds without touching edges
        else if (coll.bounds.min.x > playBounds.bounds.min.x && coll.bounds.max.x < playBounds.bounds.max.x &&
                 coll.bounds.min.y > playBounds.bounds.min.y && coll.bounds.max.y < playBounds.bounds.max.y)
        {
            Vector2 randomDir = Random.insideUnitCircle;
            return new Vector3(randomDir.x, randomDir.y, 0f);
        }
        // If asteroid center in play bounds but touching edges
        else
        {
            Vector3 sectorSide1, sectorSide2;
            sectorSide1 = sectorSide2 = Vector3.zero;
            // Get intersection coordinates
            Vector3 intersectionMin = new Vector3(Mathf.Max(coll.bounds.min.x, playBounds.bounds.min.x), Mathf.Max(coll.bounds.min.y, playBounds.bounds.min.y), 0f);
            Vector3 intersectionMax = new Vector3(Mathf.Min(coll.bounds.max.x, playBounds.bounds.max.x), Mathf.Min(coll.bounds.max.y, playBounds.bounds.max.y), 0f);

            // If intersecting left play bounds
            if(intersectionMin.x == playBounds.bounds.min.x)
            {
                sectorSide2.y = intersectionMin.y;
                sectorSide1.y = intersectionMax.y;
                // If also intersecting adjacent play bounds, move sector side out in that direction
                // Else only include intersecting line segment
                if(intersectionMin.y == playBounds.bounds.min.y)
                {
                    sectorSide2.x = intersectionMax.x;
                }
                else
                {
                    sectorSide2.x = intersectionMin.x;
                }
                if(intersectionMax.y == playBounds.bounds.max.y)
                {
                    sectorSide1.x = intersectionMax.x;
                }
                else
                {
                    sectorSide1.x = intersectionMin.x;
                }
            }
            // If intersecting top play bounds
            else if(intersectionMax.y == playBounds.bounds.max.y)
            {
                sectorSide2.x = intersectionMin.x;
                sectorSide1.x = intersectionMax.x;
                if(intersectionMin.x == playBounds.bounds.min.x)
                {
                    sectorSide2.y = intersectionMin.y;
                }
                else
                {
                    sectorSide2.y = intersectionMax.y;
                }
                if(intersectionMax.x == playBounds.bounds.max.x)
                {
                    sectorSide1.y = intersectionMin.y;
                }
                else
                {
                    sectorSide1.y = intersectionMax.y;
                }
            }
            // If intersecting right play bounds
            else if(intersectionMax.x == playBounds.bounds.max.x)
            {
                sectorSide2.y = intersectionMin.y;
                sectorSide1.y = intersectionMax.y;
                if(intersectionMin.y == playBounds.bounds.min.y)
                {
                    sectorSide2.x = intersectionMin.x;
                }
                else
                {
                    sectorSide2.x = intersectionMax.x;
                }
                if(intersectionMax.y == playBounds.bounds.max.y)
                {
                    sectorSide1.x = intersectionMin.x;
                }
                else
                {
                    sectorSide1.x = intersectionMax.x;
                }
            }
            // If intersecting bottom play bounds
            else
            {
                sectorSide1.x = intersectionMin.x;
                sectorSide2.x = intersectionMax.x;
                if(intersectionMin.x == playBounds.bounds.min.x)
                {
                    sectorSide1.y = intersectionMax.y;
                }
                else
                {
                    sectorSide1.y = intersectionMin.y;
                }
                if(intersectionMax.x == playBounds.bounds.max.x)
                {
                    sectorSide2.y = intersectionMax.y;
                }
                else
                {
                    sectorSide2.y = intersectionMin.y;
                }
            }
            return GetDirectionFromArcSectors(sectorSide1, sectorSide2);
        }
    }

    Vector3 GetDirectionFromArcSectors(Vector3 sector1, Vector3 sector2)
    {
        float startAngle = Mathf.Atan2((sector1 - coll.bounds.center).y, (sector1 - coll.bounds.center).x);
        // For ease debugging convert angles to range 0 to 2 * PI
        if(startAngle < 0f)
        {
            startAngle = 2 * Mathf.PI + startAngle;
        }
        float endAngle = Mathf.Atan2((sector2 - coll.bounds.center).y, (sector2 - coll.bounds.center).x);
        if(endAngle < 0f)
        {
            endAngle = 2 * Mathf.PI + endAngle;
        }
        float minGuess = startAngle;
        startAngle = Mathf.Min(startAngle, endAngle);
        if(minGuess != startAngle)
        {
            endAngle = minGuess;
        }

        float intersectAngle = Mathf.Atan2((playBounds.bounds.center - coll.bounds.center).y, (playBounds.bounds.center - coll.bounds.center).x);
        if(intersectAngle < 0f)
        {
            intersectAngle = 2 * Mathf.PI + intersectAngle;
        }
        if(!(intersectAngle >= startAngle && intersectAngle <= endAngle))
        {
            float temp = startAngle;
            startAngle = -2 * Mathf.PI + endAngle;
            endAngle = temp;
        }

        float randomAngle = Random.Range(startAngle, endAngle);
        return new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);
    }
}
