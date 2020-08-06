using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

// AI agent class that maintains an ordered list of waypoints and navigates among them
// Intended to work in conjunction with a NavMeshAgent component, but will lerp around without one
public class PathingAgent : MonoBehaviour
{
    // Class storing a list of waypoints that a given waypoint can randomly connect to in addition to the next one in the main waypoints list
    // Serialized as its own class to allow nested reordable lists in custom inspector
    [System.Serializable]
    public class ConnectedWaypointsListContainer
    {
        public List<WaypointObject> connections = new List<WaypointObject>();
    }

    // Controlling whether a pathing agent's up axis is aligned to that of the local surface on slopes
    [SerializeField]
    bool orientParallelToSlopes = false;
    [SerializeField]
    [Range(0f, 180f)]
    float minSlopeReorientAngle = 45f;

    // Tethering a pathing agent to stay within a radius of another gameobject's transform
    [SerializeField]
    Transform tetherObjectTransform;
    [SerializeField]
    float tetherRadius = 1f;
    [SerializeField]
    [Range(0, 180f)]
    float tetherDirectionStartAngle = 90f;
    [SerializeField]
    [Range(-180f, 0f)]
    float tetherDirectionEndAngle = -90f;

    public enum PathingMode
    {
        SINGLE_RUN,
        LOOP,
        PING_PONG,
        RANDOM
    }
    public PathingMode pathingMode = PathingMode.LOOP;

    public List<WaypointObject> waypoints = new List<WaypointObject>();
    public List<ConnectedWaypointsListContainer> connectedWaypoints = new List<ConnectedWaypointsListContainer>();

    [SerializeField]
    int nextWaypointIndex;
    [SerializeField]
    WaypointObject currentWaypointTarget;
    [SerializeField]
    bool rotateAtWaypoints = true;
    [SerializeField]
    float turnDegreesPerSec = 120;
    [SerializeField]
    bool beginPathingOnStart = true;
    [Range(0, 1)]
    [SerializeField]
    float goRndWaypointChance = 0.5f;

    // Events for all waypoints
    [SerializeField]
    WaypointChangeEvent allWaypointsOnEnter;
    [SerializeField]
    WaypointChangeEvent allWaypointsOnExit;

    // Variables for pathing without a NavMesh
    public enum NavFailureResponse
    {
        STOP,
        LERP,
        SKIP
    }
    [SerializeField]
    NavFailureResponse navFailureResponse = NavFailureResponse.SKIP;
    [SerializeField]
    bool inheritSpeedFromNavAgent = true;
    [SerializeField]
    float moveSpeed = 3.5f;

    NavMeshAgent navAgentRef;
    bool hasNavMeshAgent;
    float nonNavMeshStoppingDistSq;
    bool finishedPathing;
    bool isReversingThroughWaypoints;
    bool isLerpingOnFailure;
    bool isRotating;
    bool isTurningToMarina;
    float marinaLerp = 0;
    bool isAtWaypoint;
    float waitAtWaypointTime;
    int startWaypointIndex;
    int currentWaypointIndex = -1;
    bool isGrounded = true; // As opposed to flying
    bool shouldStopAtNextWaypoint;
    float tetherRadiusSq;
    bool isStoppedForTether;
    Quaternion targetMarina;
    Quaternion baseMarina;
    Animator anim;

#if UNITY_EDITOR
    [HideInInspector]
    public static PathingAgent lastSelectedPathingAgent;
#endif

    // Function to offer a void-return interface for LoadWaypointCollection for UnityEvents
    public void LoadWaypoints(SCR_WaypointCollection waypointCollection)
    {
        LoadWaypointCollection(waypointCollection);
    }

    // Function to replace the PathingAgent's current set of waypoints with onces loaded in from a SCR_WaypointCollection ScriptableObject asset
    // Calls ResetPathing on a successful load to begin pathing at the beginning of the new collection
    // Returns whether all waypoints in the collection were successfully loaded
    public bool LoadWaypointCollection(SCR_WaypointCollection waypointCollection)
    {
        List<WaypointObject> loadedWaypoints = new List<WaypointObject>();
        if (!TryToLoadWaypointsIntoList(waypointCollection.waypoints, loadedWaypoints))
        {
            return false;
        }
        List<ConnectedWaypointsListContainer> loadedConnectedWaypoints = new List<ConnectedWaypointsListContainer>();
        foreach (SCR_WaypointCollection.ConnectedWaypointNamesListContainer waypointGroup in waypointCollection.randWaypointConnections)
        {
            ConnectedWaypointsListContainer connectedWaypointsList = new ConnectedWaypointsListContainer();
            if (!TryToLoadWaypointsIntoList(waypointGroup.names, connectedWaypointsList.connections))
            {
                return false;
            }
            loadedConnectedWaypoints.Add(connectedWaypointsList);
        }
        waypoints = loadedWaypoints;
        connectedWaypoints = loadedConnectedWaypoints;
        ResetPathing();
        return true;
    }

    // Function to navigate to a specific waypoint, regardless of whether it is in this agent's list
    // Parameter waitAtWaypointTime adds additional time to the newly selected currentWaypointTarget
    // Returns whether the agent can access the newly selected currentWaypointTarget
    public bool NavigateToWaypoint(WaypointObject waypoint, float waitAtWaypointTime = 0f)
    {
        CancelCurrentWaypoint();

        int waypointIndex = waypoints.IndexOf(waypoint);
        if (waypointIndex >= 0)
        {
            nextWaypointIndex = waypointIndex;
            return NavigateToNextWaypoint(false, waitAtWaypointTime);
        }
        else
        {
            Debug.Log("Navigating to waypoint not in list");
            currentWaypointTarget = waypoint;
            return BeginMovingToCurrentWaypointTarget(waitAtWaypointTime);
        }
    }

    // Helper Function to navigate to waypoint from the editor
    public void NavigateToWaypointHelper(WaypointObject waypoint)
    {
        NavigateToWaypoint(waypoint);
    }

    // Publicly accessible function also used internally to update currentWaypointTarget
    // Parameter allowRandomSelection determines whether to also consider random waypoints in the connected, inner list
    // Parameter waitAtWaypointTime adds additional time to the newly selected currentWaypointTarget
    // Returns whether the agent can access the newly selected currentWaypointTarget
    public bool NavigateToNextWaypoint(bool allowRandomSelection = true, float waitAtWaypointTime = 0)
    {
        CancelCurrentWaypoint();

        if (allowRandomSelection && currentWaypointIndex >= 0)
        {
            if (connectedWaypoints[currentWaypointIndex].connections.Count > 0 && UnityEngine.Random.Range(0f, 1f) <= goRndWaypointChance)
            {
                int rndIndex = UnityEngine.Random.Range(0, connectedWaypoints[currentWaypointIndex].connections.Count);
                currentWaypointTarget = connectedWaypoints[currentWaypointIndex].connections[rndIndex];
            }
            else
            {
                currentWaypointTarget = waypoints[nextWaypointIndex];
            }
        }
        else
        {
            currentWaypointTarget = waypoints[nextWaypointIndex];
        }

        return BeginMovingToCurrentWaypointTarget(waitAtWaypointTime);
    }

    // Function to stop the agent upon reaching the next waypoint
    public void StopAtNextWaypointReached()
    {
        shouldStopAtNextWaypoint = true;
    }

    // Function to stop the agent moving
    public void StopPathing()
    {
        finishedPathing = true;
        if (hasNavMeshAgent)
        {
            navAgentRef.isStopped = true;
        }
    }

    // Function to continue the agent's pathing as it was before a call to StopPathing
    public void ResumePathing()
    {
        finishedPathing = false;
        if(hasNavMeshAgent)
        {
            navAgentRef.isStopped = false;
        }
    }

    // Function to offer a parameter-less interface for NavigateToNextWaypoint for UnityEvents
    public void ResumePathingAtNextWaypoint()
    {
        NavigateToNextWaypoint();
    }

    // Function to make the agent path to the closest waypoint and immediately start moving
    public void ResumePathingAtClosestWaypoint()
    {
        if (waypoints.Count <= 0)
        {
            return;
        }

        int closestWaypointIndex = 0;
        float closestDistSq = (transform.position - waypoints[0].transform.position).sqrMagnitude;
        for (int w = 1; w < waypoints.Count; ++w)
        {
            float distSq = (transform.position - waypoints[w].transform.position).sqrMagnitude;
            if (distSq < closestDistSq)
            {
                closestDistSq = distSq;
                closestWaypointIndex = w;
            }
        }
        nextWaypointIndex = closestWaypointIndex;
        NavigateToNextWaypoint(false);
    }

    // Function to reset the agent's path to that of game start-up and immediately start moving
    public void ResetPathing()
    {
        isReversingThroughWaypoints = false;
        currentWaypointIndex = -1;
        nextWaypointIndex = startWaypointIndex;
        NavigateToNextWaypoint(false);
    }

    // Function to add wait time to the current waitAtWaypointTime
    public void AddWaitAtWaypointTime(float duration)
    {
        waitAtWaypointTime += duration;
    }

    private void Awake()
    {
        // Remove null waypoints
        for (int w = waypoints.Count - 1; w >= 0; --w)
        {
            if (waypoints[w] == null)
            {
                waypoints.RemoveAt(w);
            }
        }
        for (int c = connectedWaypoints.Count - 1; c >= 0; --c)
        {
            for (int cw = connectedWaypoints[c].connections.Count - 1; cw >= 0; --cw)
            {
                if (connectedWaypoints[c].connections[cw] == null)
                {
                    connectedWaypoints[c].connections.RemoveAt(cw);
                }
            }
        }

        navAgentRef = GetComponentInChildren<NavMeshAgent>();
        hasNavMeshAgent = navAgentRef != null && navAgentRef.enabled;

        if (hasNavMeshAgent && inheritSpeedFromNavAgent)
        {
            moveSpeed = navAgentRef.speed;
        }

        startWaypointIndex = nextWaypointIndex;
        tetherRadiusSq = tetherRadius * tetherRadius;

        if (waypoints.Count <= 0)
        {
            finishedPathing = true;
        }
    }

    private void Start()
    {
        NavMeshAgent navAgent = GetComponent<NavMeshAgent>();
        navAgent.Warp(this.transform.position);

        anim = GetComponentInChildren<Animator>();
        if (beginPathingOnStart && !finishedPathing)
        {
            NavigateToNextWaypoint(false);
        }
        else
        {
            StopPathing();
        }
    }

    private void Update()
    {
        if(orientParallelToSlopes && hasNavMeshAgent && !navAgentRef.isOnOffMeshLink)
        {
            NavMeshSurface navMeshSurface = navAgentRef.navMeshOwner as NavMeshSurface;
            if(navMeshSurface != null)
            {
                float surfaceSlope = Vector3.Angle(navMeshSurface.transform.up, Vector3.up);
                //Debug.Log("Surface slope is:" + surfaceSlope.ToString());
                if(surfaceSlope >= minSlopeReorientAngle && surfaceSlope <= 180f - minSlopeReorientAngle)
                {
                    navAgentRef.updateRotation = false;
                    transform.rotation = Quaternion.LookRotation(-navMeshSurface.transform.up, navAgentRef.velocity);
                }
                else
                {
                    navAgentRef.updateRotation = true;
                }
            }
        }

        CheckTether();
        if (finishedPathing || isStoppedForTether)
        {
            
            return;
        }

        if (isAtWaypoint)
        {
            waitAtWaypointTime = Mathf.Clamp(waitAtWaypointTime - Time.deltaTime, 0f, waitAtWaypointTime - Time.deltaTime);
            if (waitAtWaypointTime <= 0 && !isRotating)
            {
                
                LeaveWaypoint();
            }
        }

        if (isRotating)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentWaypointTarget.transform.forward, transform.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnDegreesPerSec * Time.deltaTime);
            // Done rotating
            if (transform.rotation == targetRotation)
            {
                isRotating = false;
                if(hasNavMeshAgent)
                {
                    navAgentRef.updateRotation = true;
                    navAgentRef.updateUpAxis = true;
                }
                if (waitAtWaypointTime <= 0)
                {
                    LeaveWaypoint();
                }
            }
        }
        else
        {
            if (!hasNavMeshAgent || isLerpingOnFailure)
            {
                Vector3 targetForward = (currentWaypointTarget.transform.position - transform.position).normalized;
                transform.position += targetForward * moveSpeed * Time.deltaTime;
                // Face the direction of travel
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetForward), turnDegreesPerSec * Time.deltaTime);
            }
            CheckDestinationReached();
        }
    }

    // Function used internally by LoadWaypointCollection to fill a list of WaypointObject script references from a list of names of scene game objects
    // Returns whether all waypoints in the list were successfully loaded
    bool TryToLoadWaypointsIntoList(List<string> waypointNames, List<WaypointObject> recipientList)
    {
        foreach (string waypoint in waypointNames)
        {
            GameObject waypointObj = GameObject.Find(waypoint);
            if (waypointObj == null)
            {
                Debug.LogError("Failed to find " + waypoint + "\nAborting waypoint collection load");
                return false;
            }
            else
            {
                WaypointObject waypointObjectRef = waypointObj.GetComponent<WaypointObject>();
                if (waypointObjectRef == null)
                {
                    Debug.LogError(waypoint + " has no WaypointObject script attached\nAborting waypoint collection load");
                    return false;
                }
                else
                {
                    recipientList.Add(waypointObjectRef);
                }
            }
        }
        return true;
    }

    // Function used internally by NavigateToWaypoint and NavigateToNextWaypoint to start the agent moving to currentWaypointTarget
    // Returns whether the agent can access the newly selected currentWaypointTarget
    bool BeginMovingToCurrentWaypointTarget(float waitAtWaypointTime)
    {
        this.waitAtWaypointTime = waitAtWaypointTime + currentWaypointTarget.waitTime;

        finishedPathing = false;
        if (hasNavMeshAgent)
        {
            navAgentRef.isStopped = false;
            navAgentRef.stoppingDistance = currentWaypointTarget.stoppingDist;
            nonNavMeshStoppingDistSq = currentWaypointTarget.stoppingDist * currentWaypointTarget.stoppingDist;

            navAgentRef.SetDestination(currentWaypointTarget.transform.position);
            bool setDestinationWorked = navAgentRef.path.status == NavMeshPathStatus.PathComplete && isGrounded;
            // Even if the destination is unreachable, SetDestination will return a complete path to the closest point possible
            // We double-check destination is within stoppingDistance of curentWaypointTarget's position
            if(setDestinationWorked)
            {
                float distFromDestinationToWaypoint = (currentWaypointTarget.transform.position - navAgentRef.destination).magnitude;
                setDestinationWorked &= distFromDestinationToWaypoint <= navAgentRef.stoppingDistance;
                // If CalculatePath failed, stop the movement from SetDestination
                if(!setDestinationWorked)
                {
                    navAgentRef.SetDestination(transform.position);
                }
            }
            if (setDestinationWorked)
            {
                return true;
            }
            else
            {
                // Stop the NavMeshAgent so it doesn't interfere with the failure response
                navAgentRef.ResetPath();

                NavMeshHit ignoreHit;
                // If currentWaypointTarget is within 0.5m from a NavMesh, it is on the ground
                // The NavAgent should take back control when the grounded waypoint is reached
                isGrounded = NavMesh.SamplePosition(currentWaypointTarget.transform.position, out ignoreHit, 0.5f, NavMesh.AllAreas);
                switch (navFailureResponse)
                {
                    case NavFailureResponse.STOP:
                        Debug.LogError(name + " set to stop on NavMesh failure unable to find path to waypoint " + currentWaypointTarget.name);
                        StopPathing();
                        return false;
                    case NavFailureResponse.LERP:
                        isLerpingOnFailure = true;
                        // Take away transform control from the NavAgent
                        navAgentRef.updatePosition = false;
                        navAgentRef.updateRotation = false;
                        return true;
                    case NavFailureResponse.SKIP:
                        // If agent can't reach next master waypoint, update indices
                        if (currentWaypointTarget == waypoints[nextWaypointIndex])
                        {
                            int oldNextWaypointIndex = nextWaypointIndex;
                            // Only call NavigateToNextWaypoint when the agent hasn't reached the end of its path
                            if (IncrementWaypointIndicesByPathingMode() && oldNextWaypointIndex != nextWaypointIndex)
                            {
                                return NavigateToNextWaypoint();
                            }
                            else
                            {
                                return false;
                            }
                        }
                        // If agent can't reach a random connected waypoint, try for the next master waypoint
                        else
                        {
                            return NavigateToNextWaypoint(false);
                        }
                }
                // Return that should never be reached to make the compiler happy
                return true;
            }
        }
        // Always lerp to waypoint without a NavMeshAgent component
        else
        {
            nonNavMeshStoppingDistSq = currentWaypointTarget.stoppingDist * currentWaypointTarget.stoppingDist;
            return true;
        }
    }

    // Function to test if the agent has reached its currentWaypointTarget
    void CheckDestinationReached()
    {
        if (hasNavMeshAgent && !isLerpingOnFailure)
        {
            if (!navAgentRef.pathPending && navAgentRef.remainingDistance <= navAgentRef.stoppingDistance)
            {
                OnDestinationReached();
            }
            else
            {
                
                if (anim != null)
                {
                    anim.SetBool("isWalking", true);
                }
            }
        }
        else
        {
            if ((currentWaypointTarget.transform.position - transform.position).sqrMagnitude <= nonNavMeshStoppingDistSq)
            {
                OnDestinationReached();
            }
            else
            {
                
                if (anim != null)
                {
                   
                    anim.SetBool("isWalking", true);
                }
            }
        }
    }

    // Function to handle reaching a waypoint
    void OnDestinationReached()
    {
        
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
        }
        isLerpingOnFailure = false;
        if (hasNavMeshAgent)
        {
            navAgentRef.Warp(transform.position);
            if (isGrounded)
            {
                navAgentRef.updatePosition = true;
                navAgentRef.updateRotation = true;
            }
        }

        isAtWaypoint = true;
        if (rotateAtWaypoints)
        {
            isRotating = true;
            if(hasNavMeshAgent)
            {
                navAgentRef.updateRotation = false;
                navAgentRef.updateUpAxis = false;
            }
        }

        if (shouldStopAtNextWaypoint)
        {
            shouldStopAtNextWaypoint = false;
            StopPathing();
        }

        allWaypointsOnEnter.Invoke();
        if (currentWaypointTarget == waypoints[nextWaypointIndex])
        {
            currentWaypointTarget.onWaypointEnter.Invoke();
            IncrementWaypointIndicesByPathingMode();
        }
    }

    // Function to handle leaving a waypoint
    void LeaveWaypoint()
    {
        isAtWaypoint = false;
        WaypointObject oldCurrentWaypointTarget = currentWaypointTarget;

        NavigateToNextWaypoint();

        allWaypointsOnExit.Invoke();
        if (oldCurrentWaypointTarget == waypoints[currentWaypointIndex])
        {
            oldCurrentWaypointTarget.onWaypointExit.Invoke();
        }
    }

    // Function to clean up leaving a waypoint preemptively by stopping rotating, setting wait time to 0, and calling relevant OnWaypointExit events
    // Checks and only has effect when at a waypoint
    void CancelCurrentWaypoint()
    {
        if (isAtWaypoint)
        {
            isAtWaypoint = false;
            isRotating = false;
            if(hasNavMeshAgent)
            {
                navAgentRef.updateRotation = true;
                navAgentRef.updateUpAxis = true;
            }
            waitAtWaypointTime = 0f;
            allWaypointsOnExit.Invoke();
            if (currentWaypointTarget == waypoints[nextWaypointIndex])
            {
                currentWaypointTarget.onWaypointExit.Invoke();
            }
        }
    }

    // Function to increment currentWaypointIndex and nextWaypointIndex based on pathingMode
    // Returns false when the end of the path is reached (can only happen for SINGLE_RUN mode)
    bool IncrementWaypointIndicesByPathingMode()
    {
        switch (pathingMode)
        {
            case PathingMode.SINGLE_RUN:
                if (nextWaypointIndex >= waypoints.Count - 1)
                {
                    StopPathing();
                    return false;
                }
                else
                {
                    ++currentWaypointIndex;
                    ++nextWaypointIndex;
                }
                break;
            case PathingMode.LOOP:
                if (nextWaypointIndex >= waypoints.Count - 1)
                {
                    ++currentWaypointIndex;
                    nextWaypointIndex = 0;
                }
                else if (nextWaypointIndex == 0)
                {
                    currentWaypointIndex = 0;
                    ++nextWaypointIndex;
                }
                else
                {
                    ++currentWaypointIndex;
                    ++nextWaypointIndex;
                }
                currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Count - 1);
                nextWaypointIndex = Mathf.Clamp(nextWaypointIndex, 0, waypoints.Count);
                break;
            case PathingMode.PING_PONG:
                if (isReversingThroughWaypoints)
                {
                    if (nextWaypointIndex <= 0)
                    {
                        isReversingThroughWaypoints = false;
                        --currentWaypointIndex;
                        nextWaypointIndex = 1;
                    }
                    else
                    {
                        --currentWaypointIndex;
                        --nextWaypointIndex;
                    }
                }
                else
                {
                    if (nextWaypointIndex >= waypoints.Count - 1)
                    {
                        isReversingThroughWaypoints = true;
                        ++currentWaypointIndex;
                        nextWaypointIndex = waypoints.Count - 2;
                    }
                    else
                    {
                        ++currentWaypointIndex;
                        ++nextWaypointIndex;
                    }
                }
                break;
            case PathingMode.RANDOM:
                int rndIndex;
                do
                {
                    rndIndex = UnityEngine.Random.Range(0, waypoints.Count - 1);
                } while (rndIndex == nextWaypointIndex);
                currentWaypointIndex = rndIndex;
                nextWaypointIndex = currentWaypointIndex + 1;
                break;
        }
        return true;
    }

    // Function to check the PathingAgent's position and direction with respect to its tether object and stop or resume pathing when appropriate
    void CheckTether()
    {
        if (tetherObjectTransform != null)
        {
            float sqrDist = (tetherObjectTransform.position - transform.position).sqrMagnitude;
            // Tether object is outside of tether radius
            if (sqrDist >= tetherRadiusSq)
            {
                Vector3 dirToWaypoint = (currentWaypointTarget.transform.position - transform.position).normalized;
                Vector3 dirToTetherObj = (tetherObjectTransform.position - transform.position).normalized;
                float angleDiff = Vector3.SignedAngle(dirToWaypoint, dirToTetherObj, Vector3.up);

                // Tether object is behind the PathingAgent
                if (angleDiff > tetherDirectionStartAngle || angleDiff < tetherDirectionEndAngle)
                {
                    isStoppedForTether = true;
                    if(hasNavMeshAgent)
                    {
                        navAgentRef.isStopped = true;
                    }
                }
                else
                {
                    ResumeIfStoppedForTether();
                }
            }
            else
            {
                ResumeIfStoppedForTether();
            }
        }
    }

    // Function to resume pathing if pathing was stopped due to the tether object
    void ResumeIfStoppedForTether()
    {
        if (isStoppedForTether)
        {
            isStoppedForTether = false;
            if (hasNavMeshAgent)
            {
                navAgentRef.isStopped = false;
            }
        }
    }
    
    public void FaceMarina()
    {
        transform.LookAt(GameObject.Find("Player").transform);
    }


#if UNITY_EDITOR
    [MenuItem("Tools/AI/StopShowingAgentPath")]
    static void StopShowingAgentPath()
    {
        lastSelectedPathingAgent = null;
    }

    void OnDrawGizmos()
    {
        if(lastSelectedPathingAgent)
        {
            Color oldColor = Handles.color;
            GUIStyle style = new GUIStyle();

            for (int w = lastSelectedPathingAgent.waypoints.Count - 1; w >= 0; --w)
            {
                if (lastSelectedPathingAgent.waypoints[w])
                {
                    Handles.color = Color.cyan;
                    style.normal.textColor = Handles.color;
                    Handles.Label(lastSelectedPathingAgent.waypoints[w].transform.position + Vector3.up, (w + 1).ToString(), style);
                    int prevNonNullWaypointIndex = w - 1;
                    while (prevNonNullWaypointIndex >= 0 && lastSelectedPathingAgent.waypoints[prevNonNullWaypointIndex] == null)
                    {
                        --prevNonNullWaypointIndex;
                    }
                    if (prevNonNullWaypointIndex >= 0)
                    {
                        Handles.DrawAAPolyLine(7.5f, new Vector3[] { lastSelectedPathingAgent.waypoints[w].transform.position, lastSelectedPathingAgent.waypoints[prevNonNullWaypointIndex].transform.position });
                        //Handles.DrawLine(agent.waypoints[w].transform.position, agent.waypoints[prevNonNullWaypointIndex].transform.position);
                    }
                }
            }
            Handles.color = oldColor;
        }
    }
#endif
}
