using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VendingCarousel : MonoBehaviour
{
    [Header("Hack Boost 3D Object Spacing")]
    [SerializeField]
    float radius = 1f;
    [SerializeField]
    float vertOffset = -0.25f;
    [SerializeField]
    [Range(0f, 360f)]
    float hackBoostSpacing = 30f;

    [Header("Spin timing")]
    [SerializeField]
    float minSwipeDelta = 0.0075f;
    [SerializeField]
    float clickTimeLimit = 0.5f;
    [SerializeField]
    float snapDuration = 0.1f;
    [SerializeField]
    float dragSpeed = 100f;
    [SerializeField]
    float initSpinDrag = 10f;
    [SerializeField]
    float spinSpeedMult = 10f;

    [SerializeField]
    List<GameObject> hackBoostPrefabs = new List<GameObject>();

    List<GameObject> hackBoost3DObjs = new List<GameObject>();
    int midIndex;
    float[] angleSlots;
    int[] angleSlotIndices;
    int selectedItemIndex;

    bool isSnapping = false;
    float timeSnapping;
    float[] startSnapAngles;
    float[] snapAngleDeltas;

    bool isDragging;
    float startDragTime;
    float[] prevFramesDragPositions = new float[3];
    int prevDragFramesIndex;
    float cumulativeDragDelta;

    bool isSpinning;
    float spinSpeed;
    float spinDrag;
    float cumulativeSpinDelta;

    public void SnapToItem(GameObject item)
    {
        int index = hackBoost3DObjs.IndexOf(item);
        if (index < 0)
        {
            Debug.LogError(item + " was not found in the carousel and can't be snapped to.");
        }
        else
        {
            SetIndexAsCenteredItem(index, true);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject prefab in hackBoostPrefabs)
        {
            GameObject boost = Instantiate(prefab, transform);
            hackBoost3DObjs.Add(boost);
        }
        angleSlots = new float[hackBoost3DObjs.Count];
        angleSlotIndices = new int[hackBoost3DObjs.Count];
        startSnapAngles = new float[hackBoost3DObjs.Count];
        snapAngleDeltas = new float[hackBoost3DObjs.Count];

        midIndex = hackBoostPrefabs.Count / 2;
        if (hackBoostPrefabs.Count % 2 == 0)
        {
            --midIndex;
        }
        // Cache the slots - the initial angular rotation offset for each item
        for (int i = 0; i < hackBoost3DObjs.Count; ++i)
        {
            float angleOffset = 270f + (i - midIndex) * hackBoostSpacing;
            angleSlots[i] = angleOffset;
            angleSlotIndices[i] = i;
        }
        SetIndexAsCenteredItem(midIndex, false, true);
    }

    // Update is called once per frame
    void Update()
    {
        if (isSnapping)
        {
            timeSnapping = Mathf.Min(timeSnapping + Time.deltaTime, snapDuration);
            float normalizedTimeSnapping = timeSnapping / snapDuration;
            for (int i = 0; i < hackBoost3DObjs.Count; ++i)
            {
                float angleOffset = startSnapAngles[i] + normalizedTimeSnapping * snapAngleDeltas[i];
                Vector3 pos = new Vector3(radius * Mathf.Cos(angleOffset * Mathf.Deg2Rad),
                                        vertOffset,
                                        radius * Mathf.Sin(angleOffset * Mathf.Deg2Rad));
                hackBoost3DObjs[i].transform.localPosition = pos;
                hackBoost3DObjs[i].transform.localRotation = Quaternion.Euler(0f, 270f - angleOffset, 0f);
            }
            if (timeSnapping >= snapDuration)
            {
                isSnapping = false;
                // Update startSnapAngles to the current values
                for (int i = 0; i < hackBoost3DObjs.Count; ++i)
                {
                    startSnapAngles[i] += snapAngleDeltas[i];
                    // Normalize startSnapAngles to 0-360
                    startSnapAngles[i] %= 360f;
                    if (startSnapAngles[i] <= 0f)
                    {
                        startSnapAngles[i] += 360f;
                    }
                }

                vendingItemDisplay.SetDisplayedItem(itemsForSale[selectedItemIndex]);
                vendingItemDisplay.gameObject.SetActive(true);
            }
        }
        else if (isSpinning)
        {
            // Only drag when not clicking on UI
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                isSpinning = false;
                isDragging = true;
                startDragTime = Time.timeSinceLevelLoad;
                prevDragFramesIndex = 0;
                prevFramesDragPositions[0] = Input.mousePosition.x;
                prevFramesDragPositions[1] = Input.mousePosition.x;
                prevFramesDragPositions[2] = Input.mousePosition.x;
                cumulativeDragDelta = cumulativeSpinDelta;
            }
            else
            {
                if (spinSpeed >= 0)
                {
                    spinSpeed = Mathf.Max(spinSpeed - Time.deltaTime * spinDrag, 0f);
                }
                else
                {
                    spinSpeed = Mathf.Min(spinSpeed + Time.deltaTime * spinDrag, 0f);
                }
                spinDrag += Time.deltaTime * spinDrag;

                if (spinSpeed != 0f)
                {
                    cumulativeSpinDelta += spinSpeed * Time.deltaTime;
                    RotateCarousel(cumulativeSpinDelta);
                }
                else
                {
                    isSpinning = false;
                    SnapToClosestItemToCenter(cumulativeSpinDelta);
                }
            }
        }
        else
        {
            // Only drag when not clicking on UI
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                isDragging = true;
                startDragTime = Time.timeSinceLevelLoad;
                prevDragFramesIndex = 0;
                prevFramesDragPositions[0] = Input.mousePosition.x;
                prevFramesDragPositions[1] = Input.mousePosition.x;
                prevFramesDragPositions[2] = Input.mousePosition.x;
                cumulativeDragDelta = 0f;

                vendingItemDisplay.gameObject.SetActive(false);
            }
            else if (isDragging)
            {
                float dragDuration = Time.timeSinceLevelLoad - startDragTime;
                if (Input.GetMouseButtonUp(0))
                {
                    isDragging = false;
                    float swipeDelta = (Input.mousePosition.x - prevFramesDragPositions[prevDragFramesIndex]) / Screen.width;
                    // Fast swipe, spin
                    if (Mathf.Abs(swipeDelta) >= minSwipeDelta)
                    {
                        isSpinning = true;
                        spinSpeed = swipeDelta * spinSpeedMult;
                        cumulativeSpinDelta = cumulativeDragDelta;
                        spinDrag = initSpinDrag;
                    }
                    else if (dragDuration <= clickTimeLimit)
                    {
                        // Click, not a swipe - move to the selected item
                        RaycastHit hit;
                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity, 1 << 10))
                        {
                            SnapToItem(hit.collider.gameObject);
                        }
                        // Click not on an item, re-center
                        else
                        {
                            SetIndexAsCenteredItem(selectedItemIndex, true);
                        }
                    }
                    // Slow drag, snap to the nearest item
                    else
                    {
                        SnapToClosestItemToCenter(cumulativeDragDelta);
                    }
                }
                // Dragging
                else
                {
                    float prevFrameDragPos = prevFramesDragPositions[(prevFramesDragPositions.Length + (prevDragFramesIndex - 1) % prevFramesDragPositions.Length) % prevFramesDragPositions.Length];
                    float dragDelta = (Input.mousePosition.x - prevFrameDragPos) / Screen.width;
                    cumulativeDragDelta += dragDelta * Time.deltaTime * dragSpeed;
                    RotateCarousel(cumulativeDragDelta);
                    prevFramesDragPositions[prevDragFramesIndex] = Input.mousePosition.x;
                    prevDragFramesIndex = (prevDragFramesIndex + 1) % prevFramesDragPositions.Length;
                }
            }
        }
    }

    void SetIndexAsCenteredItem(int index, bool correctLongWay, bool instantaneous = false)
    {
        selectedItemIndex = index;

        int slotDelta = angleSlotIndices[index] - midIndex;
        int temp = slotDelta;
        // Restrict slotDelta to right rotations less than array size
        int rotations = (hackBoost3DObjs.Count + slotDelta % hackBoost3DObjs.Count) % hackBoost3DObjs.Count;
        int[] newIndices = new int[hackBoost3DObjs.Count];
        for (int i = 0; i < hackBoost3DObjs.Count; ++i)
        {
            newIndices[(i + rotations) % hackBoost3DObjs.Count] = angleSlotIndices[i];
        }

        for (int i = 0; i < hackBoost3DObjs.Count; ++i)
        {
            float angleOffset = angleSlots[newIndices[i]];
            if (instantaneous)
            {
                Vector3 pos = new Vector3(radius * Mathf.Cos(angleOffset * Mathf.Deg2Rad),
                        vertOffset,
                        radius * Mathf.Sin(angleOffset * Mathf.Deg2Rad));
                hackBoost3DObjs[i].transform.localPosition = pos;
                hackBoost3DObjs[i].transform.localRotation = Quaternion.Euler(0f, 270f - angleOffset, 0f);
                startSnapAngles[i] = angleOffset;

                vendingItemDisplay.SetDisplayedItem(itemsForSale[selectedItemIndex]);
                vendingItemDisplay.gameObject.SetActive(true);
            }
            else
            {
                snapAngleDeltas[i] = angleOffset - startSnapAngles[i];

                // Correct for rotations taking the long way around
                if (Mathf.Abs(snapAngleDeltas[i]) > 360f)
                {
                    snapAngleDeltas[i] %= 360f;
                }
                if (Mathf.Abs(snapAngleDeltas[i]) > 180f)
                {
                    float original = snapAngleDeltas[i];
                    if (snapAngleDeltas[i] >= 0f)
                    {
                        snapAngleDeltas[i] -= 360f;
                    }
                    else
                    {
                        snapAngleDeltas[i] += 360f;
                    }
                }
            }
        }

        // Correct for rotations that should go around the back
        if (correctLongWay)
        {
            if (slotDelta > 0)
            {
                for (int i = 0; slotDelta > 0; --slotDelta, ++i)
                {
                    int slotIndex = System.Array.IndexOf(angleSlotIndices, i);
                    if (startSnapAngles[slotIndex] > angleSlots[newIndices[slotIndex]])
                    {
                        snapAngleDeltas[slotIndex] = -startSnapAngles[slotIndex] + angleSlots[newIndices[slotIndex]];
                    }
                    else
                    {
                        snapAngleDeltas[slotIndex] = -startSnapAngles[slotIndex] - (360f - angleSlots[newIndices[slotIndex]]);
                    }
                }
            }
            else if (slotDelta < 0)
            {
                for (int i = hackBoost3DObjs.Count - 1; slotDelta < 0; ++slotDelta, --i)
                {
                    int slotIndex = System.Array.IndexOf(angleSlotIndices, i);
                    if (startSnapAngles[slotIndex] <= angleSlots[newIndices[slotIndex]])
                    {
                        snapAngleDeltas[slotIndex] = angleSlots[newIndices[slotIndex]] - startSnapAngles[slotIndex];
                    }
                    else
                    {
                        snapAngleDeltas[slotIndex] = 360f - startSnapAngles[slotIndex] + angleSlots[newIndices[slotIndex]];
                    }
                }
            }
        }
        angleSlotIndices = newIndices;

        if (!instantaneous)
        {
            isSnapping = true;
            timeSnapping = 0f;

            vendingItemDisplay.gameObject.SetActive(false);
        }
    }

    // Apply a cumulative number of slots shifted to the last slotted position of each boost
    void RotateCarousel(float nextSlotMultiple)
    {
        int wholeSlotMultiple = (int)nextSlotMultiple;
        int wholeSlotMultiplePlusOne = wholeSlotMultiple;
        if (nextSlotMultiple >= 0)
        {
            ++wholeSlotMultiplePlusOne;
        }
        else
        {
            --wholeSlotMultiplePlusOne;
        }
        float fractionalMultiple = Mathf.Abs(nextSlotMultiple - wholeSlotMultiple);

        for (int i = 0; i < hackBoost3DObjs.Count; ++i)
        {
            int nextSlot = (hackBoost3DObjs.Count + (angleSlotIndices[i] + wholeSlotMultiple) % hackBoost3DObjs.Count) % hackBoost3DObjs.Count;
            int nextSlotPlusOne = (hackBoost3DObjs.Count + (angleSlotIndices[i] + wholeSlotMultiplePlusOne) % hackBoost3DObjs.Count) % hackBoost3DObjs.Count;

            int revolutionsForNext = (angleSlotIndices[i] + wholeSlotMultiple) / hackBoost3DObjs.Count;
            if (angleSlotIndices[i] + wholeSlotMultiple < 0 && (angleSlotIndices[i] + wholeSlotMultiple) % hackBoost3DObjs.Count != 0)
            {
                --revolutionsForNext;
            }
            int revolutionsForNextPlus = (angleSlotIndices[i] + wholeSlotMultiplePlusOne) / hackBoost3DObjs.Count;
            if (angleSlotIndices[i] + wholeSlotMultiplePlusOne < 0 && (angleSlotIndices[i] + wholeSlotMultiplePlusOne) % hackBoost3DObjs.Count != 0)
            {
                --revolutionsForNextPlus;
            }

            float nextAngleOffset = angleSlots[nextSlot] + 360f * revolutionsForNext - angleSlots[angleSlotIndices[i]];
            float nextPlusAngleOffset = fractionalMultiple * (angleSlots[nextSlotPlusOne] + 360f * revolutionsForNextPlus - (angleSlots[nextSlot] + 360f * revolutionsForNext));
            float angleOffset = angleSlots[angleSlotIndices[i]] + nextAngleOffset + nextPlusAngleOffset;
            Vector3 pos = new Vector3(radius * Mathf.Cos(angleOffset * Mathf.Deg2Rad),
                        vertOffset,
                        radius * Mathf.Sin(angleOffset * Mathf.Deg2Rad));
            hackBoost3DObjs[i].transform.localPosition = pos;
            hackBoost3DObjs[i].transform.localRotation = Quaternion.Euler(0f, 270f - angleOffset, 0f);
        }
    }

    void SnapToClosestItemToCenter(float cumulativeDelta)
    {
        int wholeShifts = (int)cumulativeDelta;
        int wholeShiftsPlusOne = wholeShifts;
        if (cumulativeDelta >= 0f)
        {
            ++wholeShiftsPlusOne;
        }
        else
        {
            --wholeShiftsPlusOne;
        }
        float fractionalShift = Mathf.Abs(cumulativeDelta - wholeShifts);

        // Find the current closest angle to 270f
        int closestIndexToCenter = 0;
        float closestOffsetFrom270 = Mathf.Infinity;
        for (int i = 0; i < hackBoost3DObjs.Count; ++i)
        {
            int shiftedIndex = (hackBoost3DObjs.Count + (angleSlotIndices[i] + wholeShifts) % hackBoost3DObjs.Count) % hackBoost3DObjs.Count;
            int shiftedIndexPlusOne = (hackBoost3DObjs.Count + (angleSlotIndices[i] + wholeShiftsPlusOne) % hackBoost3DObjs.Count) % hackBoost3DObjs.Count;

            int revolutionsForNext = (angleSlotIndices[i] + wholeShifts) / hackBoost3DObjs.Count;
            if (angleSlotIndices[i] + wholeShifts < 0 && (angleSlotIndices[i] + wholeShifts) % hackBoost3DObjs.Count != 0)
            {
                --revolutionsForNext;
            }
            int revolutionsForNextPlus = (angleSlotIndices[i] + wholeShiftsPlusOne) / hackBoost3DObjs.Count;
            if (angleSlotIndices[i] + wholeShiftsPlusOne < 0 && (angleSlotIndices[i] + wholeShiftsPlusOne) % hackBoost3DObjs.Count != 0)
            {
                --revolutionsForNextPlus;
            }

            // Calculate and normalize current angle to 0-360
            float shiftAngleOffset = angleSlots[shiftedIndex] + 360f * revolutionsForNext - angleSlots[angleSlotIndices[i]];
            float fractionalShiftAngleOffset = fractionalShift * (angleSlots[shiftedIndexPlusOne] + 360f * revolutionsForNextPlus - (angleSlots[shiftedIndex] + 360f * revolutionsForNext));
            float angleOffset = angleSlots[angleSlotIndices[i]] + shiftAngleOffset + fractionalShiftAngleOffset;
            angleOffset %= 360f;
            if (angleOffset <= 0f)
            {
                angleOffset += 360f;
            }
            // Update the start snap angle so the drag doesn't reset before moving
            startSnapAngles[i] = angleOffset;

            float offsetFrom270 = Mathf.Abs(270f - angleOffset);
            if (offsetFrom270 < closestOffsetFrom270)
            {
                closestOffsetFrom270 = offsetFrom270;
                closestIndexToCenter = i;
            }
        }
        SetIndexAsCenteredItem(closestIndexToCenter, false);
    }
}