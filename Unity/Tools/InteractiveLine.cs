using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class InteractiveLine : Editor {
    public Vector3 point1, point2;
    XYGrid containingGrid;

    public enum Orientation
    {
        HORIZONTAL,
        VERTICAL
    }
    public Orientation orientation;

    public enum SelectState
    {
        NONE,
        HOVER,
        SELECTED,
        IMMOBILE
    }
    public SelectState selectState;

    public void Init(Vector3 inPoint1, Vector3 inPoint2, XYGrid inGrid, Orientation inOrientation = Orientation.HORIZONTAL)
    {
        point1 = inPoint1;
        point2 = inPoint2;
        containingGrid = inGrid;
        selectState = SelectState.NONE;
        orientation = inOrientation;
    }

    public void RegisterForDisplay(bool shouldRegister)
    {
        if(shouldRegister)
        {
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
        }
        else
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        }
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate += this.OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        switch(selectState)
        {
            case SelectState.NONE:
                Handles.color = Color.white;
                break;
            case SelectState.HOVER:
                Handles.color = Color.cyan;
                break;
            case SelectState.SELECTED:
                Handles.color = Color.green;
                break;
            case SelectState.IMMOBILE:
                Handles.color = Color.black;
                break;
        }
        Handles.DrawLine(point1, point2);

        if(selectState == SelectState.SELECTED)
        {
            if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            {
                Event.current.Use();
                containingGrid.NotifyLineDeleting(this);
                DestroyImmediate(this);
                return;
            }

            Vector3 lineMiddle = (point1 + point2) / 2f;
            Vector3 movedPos;

            if(orientation == Orientation.HORIZONTAL)
            {
                movedPos = Handles.Slider(lineMiddle, Vector3.up);
                float deltaY = movedPos.y - lineMiddle.y;
                if(deltaY != 0f)
                {
                    float oldY = point1.y;
                    point1.y += deltaY;
                    point2.y += deltaY;
                    containingGrid.NotifyLineMoved(this, oldY);
                }
            }
            else
            {
                movedPos = Handles.Slider(lineMiddle, Vector3.right);
                float deltaX = movedPos.x - lineMiddle.x;
                if(deltaX != 0f)
                {
                    float oldX = point1.x;
                    point1.x += deltaX;
                    point2.x += deltaX;
                    containingGrid.NotifyLineMoved(this, oldX);
                }   
            }
        }
    }

    public void SetSelectState(SelectState state)
    {
        selectState = state;
    }

    public void SetContainingGrid(XYGrid grid)
    {
        containingGrid = grid;
    }
}
