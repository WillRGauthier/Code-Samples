using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

// ScriptableWizard class for automatically generating a NavMeshLink between two NavMeshSurfaces
// Note: The NavMeshLink should be manually fine-tuned
public class PLG_AutoNavMeshLinkWizard : ScriptableWizard
{
    public NavMeshSurface navMeshSurface1;
    public NavMeshSurface navMeshSurface2;

    [MenuItem("Tools/Level/Auto-Generate NavMesh Links Wizard")]
    static void CreateWizard()
    {
        PLG_AutoNavMeshLinkWizard wizard = ScriptableWizard.DisplayWizard<PLG_AutoNavMeshLinkWizard>("Automatically Generate NavMesh Links", "Generate");

        // Try to populate the NavMeshSurface references from the selection when the wizard is opened
        bool navMeshSurfaceFound = false;
        foreach (Transform trans in Selection.transforms)
        {
            NavMeshSurface navMeshSurface = trans.gameObject.GetComponent<NavMeshSurface>();
            if (navMeshSurface != null)
            {
                if (navMeshSurfaceFound)
                {
                    wizard.navMeshSurface2 = navMeshSurface;
                    break;
                }
                else
                {
                    wizard.navMeshSurface1 = navMeshSurface;
                    navMeshSurfaceFound = true;
                }
            }
        }

        // Perform validation on open in case the NavMeshSurface references were found in the selection
        wizard.OnWizardUpdate();
    }

    void OnWizardUpdate()
    {
        // Ensure that the NavMeshSurface references are different
        if (navMeshSurface1 == navMeshSurface2)
        {
            navMeshSurface2 = null;
        }

        // Validate that the NavMeshSurface references have been set
        if (navMeshSurface1 != null && navMeshSurface2 != null)
        {
            isValid = true;
            errorString = "";
        }
        else
        {
            isValid = false;
            errorString = "Set the NavMeshSurface references at the top of the window to two valid, separate navmeshes";
        }
    }

    void OnWizardCreate()
    {
        // Temporarily set the area of each navmesh to a unique area
        LayerMask oldLayerMask1 = navMeshSurface1.defaultArea;
        LayerMask oldLayerMask2 = navMeshSurface2.defaultArea;
        int newLayerMask1 = NavMesh.GetAreaFromName("AutoLinkSurface1");
        int newLayerMask2 = NavMesh.GetAreaFromName("AutoLinkSurface2");
        if (newLayerMask1 == -1 || newLayerMask2 == -1)
        {
            Debug.LogError("Ensure that there are navigation areas called 'AutoLinkSurface1' and 'AutoLinkSurface2' before using this tool");
            return;
        }
        else
        {
            navMeshSurface1.defaultArea = newLayerMask1;
            // NavMeshSurfaces must be rebuilt for new area layer to take effect
            navMeshSurface1.BuildNavMesh();
            navMeshSurface2.defaultArea = newLayerMask2;
            navMeshSurface2.BuildNavMesh();
        }

        // Transform the NavMeshSurface bounds into world space
        Vector3 surface1TransformedCenter = navMeshSurface1.transform.TransformPoint(navMeshSurface1.center);
        Vector3 surface2TransformedCenter = navMeshSurface2.transform.TransformPoint(navMeshSurface2.center);
        Vector3 directionBetweenNavMeshes = surface2TransformedCenter - surface1TransformedCenter;
        directionBetweenNavMeshes.Normalize();

        Vector3 surface1TransformedSize = navMeshSurface1.transform.TransformVector(navMeshSurface1.size);
        Vector3 surface2TransformedSize = navMeshSurface2.transform.TransformVector(navMeshSurface2.size);
        Vector3 boundingBoxEdgeCloseToSurface1 = GetIntersectionOnBoundingBoxFromCenterInDirection(new Bounds(surface1TransformedCenter, surface1TransformedSize), directionBetweenNavMeshes);
        Vector3 boundingBoxEdgeCloseToSurface2 = GetIntersectionOnBoundingBoxFromCenterInDirection(new Bounds(surface2TransformedCenter, surface2TransformedSize), -directionBetweenNavMeshes);

        Vector3 surface1LinkVertexPosition, surface2LinkVertexPosition;
        float surface1LinkWidth = GetClosestVertexToPositionOnNavMeshSurfaceAndAdjacentWidth(boundingBoxEdgeCloseToSurface1, newLayerMask1, out surface1LinkVertexPosition, navMeshSurface1.transform.right);
        float surface2LinkWidth = GetClosestVertexToPositionOnNavMeshSurfaceAndAdjacentWidth(boundingBoxEdgeCloseToSurface2, newLayerMask2, out surface2LinkVertexPosition, navMeshSurface2.transform.right);
        Vector3 length = surface2LinkVertexPosition - surface1LinkVertexPosition;
        Vector3 center = surface1LinkVertexPosition + length / 2f;

        GameObject linkHolder = new GameObject(navMeshSurface1.name + navMeshSurface2.name + "Link");
        Undo.RegisterCreatedObjectUndo(linkHolder, "Created link");
        NavMeshLink link = linkHolder.AddComponent<NavMeshLink>();
        linkHolder.transform.position = center;
        link.startPoint = -length / 2f;
        link.endPoint = length / 2f;
        link.width = Mathf.Min(surface1LinkWidth, surface2LinkWidth);

        // Reset the NavMeshSurface areas
        navMeshSurface1.defaultArea = oldLayerMask1;
        navMeshSurface1.BuildNavMesh();
        navMeshSurface2.defaultArea = oldLayerMask2;
        navMeshSurface2.BuildNavMesh();
    }

    // Function to find the point where a line from the center of a bounds in direction intersects the edges of the bounds
    Vector3 GetIntersectionOnBoundingBoxFromCenterInDirection(Bounds bounds, Vector3 direction)
    {
        // Ray intersects box plane parallel to x-axis
        float tMin = (bounds.min.x - bounds.center.x) / direction.x;
        float tMax = (bounds.max.x - bounds.center.x) / direction.x;
        if(tMin > tMax)
        {
            float temp = tMin;
            tMin = tMax;
            tMax = temp;
        }
        // Ray intersects box plane parallel to y-axis
        float tYMin = (bounds.min.y - bounds.center.y) / direction.y;
        float tYMax = (bounds.max.y - bounds.center.y) / direction.y;
        if(tYMin > tYMax)
        {
            float temp = tYMin;
            tYMin = tYMax;
            tYMax = temp;
        }
        if(tYMin > tMin)
        {
            tMin = tYMin;
        }
        if(tYMax < tMax)
        {
            tMax = tYMax;
        }
        // Ray intersects box plane parallel to z-axis
        float tZMin = (bounds.min.z - bounds.center.z) / direction.z;
        float tZMax = (bounds.max.z - bounds.center.z) / direction.z;
        if(tZMin > tZMax)
        {
            float temp = tZMin;
            tZMin = tZMax;
            tZMax = temp;
        }
        if(tZMin > tMin)
        {
            tMin = tZMin;
        }
        if(tZMax < tMax)
        {
            tMax = tZMax;
        }

        return bounds.center + tMax * direction;
    }

    // Function to find the closest vertex to position on a NavMeshSurface with area layer areaMask
    //  and return the width of that vertex's two edges paralleling navMeshRight
    float GetClosestVertexToPositionOnNavMeshSurfaceAndAdjacentWidth(Vector3 position, int areaMask, out Vector3 closestVertex, Vector3 navMeshRight)
    {
        float adjacentWidth = 0f;

        closestVertex = Vector3.zero;
        float minDistance = Mathf.Infinity;

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        for(int i = 0; i < triangulation.areas.Length; ++i)
        {
            // The triangulation includes all NavMeshSurfaces, so only consider triangles with the appropriate area layer mask
            if(triangulation.areas[i] == areaMask)
            {
                int triangleIndex = 3 * i;
                for(int j = 0; j < 3; ++j)
                {
                    float distance = Vector3.Distance(position, triangulation.vertices[triangulation.indices[triangleIndex + j]]);
                    if(distance < minDistance)
                    {
                        minDistance = distance;
                        closestVertex = triangulation.vertices[triangulation.indices[triangleIndex + j]];
                    }
                }
            }
        }

        // For unknown reasons a vertex can appear more than once in the triangulation's vertices array
        List<int> vertexIndices = new List<int>();
        for(int i = 0; i < triangulation.vertices.Length; ++i)
        {
            if(triangulation.vertices[i] == closestVertex)
            {
                vertexIndices.Add(i);
            }
        }

        float smallestDiffFromParallel = Mathf.Infinity;
        float smallestDiffFromOppositeParallel = Mathf.NegativeInfinity;
        Vector3 bestParallelVertex = Vector3.zero;
        Vector3 bestOppositeParallelVertex = Vector3.zero;
        for(int i = 0; i < triangulation.indices.Length; ++i)
        {
            if(vertexIndices.Contains(triangulation.indices[i]))
            {
                int neighborIndex1, neighborIndex2;
                if (i % 3 == 0)
                {
                    neighborIndex1 = triangulation.indices[i + 1];
                    neighborIndex2 = triangulation.indices[i + 2];
                }
                else if (i % 3 == 1)
                {
                    neighborIndex1 = triangulation.indices[i - 1];
                    neighborIndex2 = triangulation.indices[i + 1];
                }
                else
                {
                    neighborIndex1 = triangulation.indices[i - 2];
                    neighborIndex2 = triangulation.indices[i - 1];
                }

                Vector3 neighborDirection1, neighborDirection2;
                neighborDirection1 = (triangulation.vertices[neighborIndex1] - closestVertex).normalized;
                neighborDirection2 = (triangulation.vertices[neighborIndex2] - closestVertex).normalized;
                float dot1 = Vector3.Dot(neighborDirection1, navMeshRight);
                float dot2 = Vector3.Dot(neighborDirection2, navMeshRight);

                if (1f - dot1 < smallestDiffFromParallel)
                {
                    smallestDiffFromParallel = 1f - dot1;
                    bestParallelVertex = triangulation.vertices[neighborIndex1];
                }
                if(1f - dot2 < smallestDiffFromParallel)
                {
                    smallestDiffFromParallel = 1f - dot2;
                    bestParallelVertex = triangulation.vertices[neighborIndex2];
                }
                if(-1f - dot1 > smallestDiffFromOppositeParallel)
                {
                    smallestDiffFromOppositeParallel = -1f - dot1;
                    bestOppositeParallelVertex = triangulation.vertices[neighborIndex1];
                }
                if(-1f - dot2 > smallestDiffFromOppositeParallel)
                {
                    smallestDiffFromOppositeParallel = -1f - dot2;
                    bestOppositeParallelVertex = triangulation.vertices[neighborIndex2];
                }
            }
        }

        // Debug drawing to show the widths added into the returned adjacentWidth
        //Debug.DrawLine(bestOppositeParallelVertex, closestVertex, Color.white, 2f);
        //Debug.DrawLine(bestParallelVertex, closestVertex, Color.white, 2f);

        if(smallestDiffFromParallel < Mathf.Infinity)
        {
            adjacentWidth += Vector3.Distance(bestParallelVertex, closestVertex);
        }
        if(smallestDiffFromOppositeParallel > Mathf.NegativeInfinity)
        {
            adjacentWidth += Vector3.Distance(bestOppositeParallelVertex, closestVertex);
        }
        return adjacentWidth;
    }
}
