using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

// ScriptableWizard class for parenting selected objects to a new emtpy gameobject at a desired rotation
public class PLG_NavMeshWallWizard : ScriptableWizard
{
    public Vector3 newRotation = new Vector3(90f, 0f, 0f);
    public string holderObjectName = "EnvironmentWallHolder";
    public bool rotateChildren = true;

    [MenuItem("Tools/Level/Rotated NavMesh Wizard")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<PLG_NavMeshWallWizard>("Create Rotated NavMesh", "Create");
    }

    void OnSelectionChange()
    {
        OnWizardUpdate();
    }

    void OnWizardUpdate()
    {
        // Validate if there are any selected objects to combine into a navmesh
        if(Selection.transforms.Length <= 0)
        {
            errorString = "Cannot create a NavMesh without selected game objects";
            isValid = false;
        }
        else
        {
            errorString = "";
            isValid = true;
        }
    }

    void OnWizardCreate()
    {
        GameObject holder = new GameObject(holderObjectName);
        Undo.RegisterCreatedObjectUndo(holder, "Created holder");
        holder.transform.rotation = Quaternion.Euler(newRotation);

        // Selection.transforms returns the top-level selection, excluding prefabs
        Vector3 averagePosition = Selection.transforms[0].position;
        for(int i = 1; i < Selection.transforms.Length; ++i)
        {
            averagePosition += Selection.transforms[i].position;
        }
        averagePosition /= Selection.transforms.Length;
        // Position the holder object at the center of the selected objects so the NavMeshSurface bounds don't include empty area
        holder.transform.position = averagePosition;

        foreach(Transform trans in Selection.transforms)
        {
            Quaternion originalRotation = trans.rotation;
            Vector3 originalPosition = trans.position - averagePosition;
            Undo.SetTransformParent(trans, holder.transform, "Parented " + trans.gameObject.name + " to holder");
            // Undo.SetTransformParent does not support preventing the world position from staying like transform.SetParent does
            //  so the local position and rotation must be manually adjusted if desired
            if(rotateChildren)
            {
                Undo.RecordObject(trans, "Adjusted " + trans.gameObject.name + " transform");
                trans.localRotation = originalRotation;
                trans.localPosition = originalPosition;
            }
        }

        NavMeshSurface navMeshSurface = holder.AddComponent<NavMeshSurface>();
        // Only include the newly added children as surfaces to bake into the navmesh
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.BuildNavMesh();

        // Set the newly created holder object as selected for convenience so the navmesh can be seen
        Selection.activeObject = holder;
    }
}
