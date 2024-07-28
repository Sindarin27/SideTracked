using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using DG.Tweening;
using JetBrains.Annotations;
using MoreLinq;
using MoreLinq.Experimental;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.Splines;

public class DraggableAlongSpline : MonoBehaviour
{
    public static bool AllowDragging = true;
    public bool IsPlayer = false;
    [SerializeField, Tooltip("The target track to follow.")]
    public TrackDefinition currentTrack;
    public TrackDefinition previousTrack;
    public TrainSoundPlayer SoundPlayer;

    public bool IsOnFire = false;
    public GameObject Fire;
    
    // The track we're moving towards, if not already on it
    [CanBeNull] private (TrackDefinition target, float t, float score)[] possibleTargetTracks = null;
    public bool movingToDifferentTrack = false;
    private bool targetTrackIsForwards;
    private CameraRotator rotator;

    public float t = 0;
    public float MaxSpeed = 0.1f;
    public float SpeedCasual = 0.1f;
    public float CurrentSpeed;
    public float TargetT = 0;
    public float MinimumTDiffBeforeMoving = 0.001f;
    public float MinimumTDiffBeforeStopping = 0.0001f;
    public float WaitTimeBeforeStopping = 0.1f;
    public float MaxSqDistToPreviousTargetToAllowDragging = 1;
    public float MaxSqDistToTrackToAllowDragging = 1;
    public float epsilonForTrackTransfer = 0.00001f;
    public int CurveIterationCount = 6;
    public LayerMask maskForObjectsToDragAlong;
    
    public Vector3 honkPunchScale;
    public float honkPunchDuration;
    public int honkPunchVibrato = 1;
    
    public Vector3 beginDragPunchScale;
    public float beginDragPunchDuration;
    public int beginDragPunchVibrato = 1;
    
    private SplinePath<Spline> splinePath;
    private float timeWaitedForStopping = 0;
    private bool isMoving;
    private Vector3 currentDragOffset;
    public float CurrentSplineLength { get; private set; } = 1;
    public float CurrentSplineLengthInv { get; private set; } = 1;
    public float TDiff { get; private set; }= 0;
    
    // Start is called before the first frame update
    void Start()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener((data) => { OnDragDelegate((PointerEventData)data); });
        trigger.triggers.Add(dragEntry);

        EventTrigger.Entry startEntry = new EventTrigger.Entry();
        startEntry.eventID = EventTriggerType.BeginDrag;
        startEntry.callback.AddListener((data) => { OnBeginDragDelegate((PointerEventData)data); });
        trigger.triggers.Add(startEntry);

        EventTrigger.Entry releaseEntry = new EventTrigger.Entry();
        releaseEntry.eventID = EventTriggerType.PointerUp;
        releaseEntry.callback.AddListener((data) => { OnMouseRelease((PointerEventData)data); });
        trigger.triggers.Add(releaseEntry);
        CurrentSpeed = SpeedCasual;

        rotator = Camera.main!.GetComponent<CameraRotator>();
        
        OnTrackChanged();
        EvaluatePositionAndRotation();
    }

    private void OnMouseRelease(PointerEventData _)
    {
        if (!isMoving)
        {
            Honk();
        }
    }

    private void Honk()
    {
        SoundPlayer.Honk();
        transform.DOComplete();
        transform.DOPunchScale(honkPunchScale, honkPunchDuration, honkPunchVibrato, 0);
        Extinguish(); // Honking blows off some air
    }

    // Update is called once per frame
    void Update()
    {
    }
    private void FixedUpdate()
    {
        isMoving |= movingToDifferentTrack || Math.Abs(t - TargetT) > MinimumTDiffBeforeMoving;
        if (isMoving)
        {
            float movingToT = movingToDifferentTrack ? (targetTrackIsForwards ? currentTrack.EndT : currentTrack.StartT) : TargetT;
            if (Math.Abs(t - movingToT) < MinimumTDiffBeforeStopping && AllowDragging) // Only count down if draggable
            {
                timeWaitedForStopping += Time.deltaTime;
                if (timeWaitedForStopping > WaitTimeBeforeStopping) isMoving = false;
            }
            else
            {
                timeWaitedForStopping = 0;
            }
            
            
            float newT = Mathf.MoveTowards(t, movingToT, CurrentSpeed * Time.deltaTime * CurrentSplineLengthInv);
            TDiff = newT - t;
            t = newT;
            // Check if ready for a track transfer
            if (movingToDifferentTrack && ((currentTrack.ShouldTransitionBackward(t, epsilonForTrackTransfer) && !targetTrackIsForwards)
                                           || (currentTrack.ShouldTransitionForward(t, epsilonForTrackTransfer) && targetTrackIsForwards)))
            {
                TransferToTargetTrack();
            }
            EvaluatePositionAndRotation();
            currentTrack.Events.ForEach(trackEvent => trackEvent.CheckAndTrigger(this, t));
        }
    }

    private void TransferToTargetTrack()
    {
        // If moving forwards, start at the beginning of the new track.
        // If moving backwards, start at the end of the new track
        bool hasTarget;
        (TrackDefinition, float, float)? tuple;
        (hasTarget, tuple) = possibleTargetTracks!.Where(a => a.target.IsFree())
            .Maxima(a => a.score)
            .Take(1)
            .TrySingle(false, true, false); // Many will never happen
        
        // Clear moving no matter what
        possibleTargetTracks = null;
        movingToDifferentTrack = false;

        // If any valid target, move to it
        if (hasTarget)
        {
            (currentTrack, TargetT, _) = tuple.Value;
            t = targetTrackIsForwards ? currentTrack.StartT : currentTrack.EndT;
            OnTrackChanged();
        }
        else
        {
            SoundPlayer.PlayCollission(TDiff, CurrentSplineLength);
            isMoving = false;
        }
    }
    
    public void EvaluatePositionAndRotation()
    {
        currentTrack.SplineContainer.Evaluate(splinePath, t, out float3 pos, out float3 forward, out float3 up);
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(((Vector3)forward).normalized, ((Vector3)up).normalized);
    }
    
    private void OnBeginDragDelegate(PointerEventData eventData)
    {
        // currentDragOffset = eventData.pointerPressRaycast.worldPosition - transform.position;
        // TODO(polishing) maybe figure out how to use this better
        // Debug.Log(currentDragOffset);

        rotator.focusObject = transform;
        transform.DOComplete();
        transform.DOPunchScale(beginDragPunchScale, beginDragPunchDuration, beginDragPunchVibrato, 0);
    }

    private void OnDragDelegate(PointerEventData eventData)
    {
        if (!AllowDragging) return;
        
        // Find position of mouse
        Ray ray = eventData.pressEventCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, float.PositiveInfinity, maskForObjectsToDragAlong)) return;
        // Vector3 direction = hitInfo.point - transform.position;
        // transform.position += direction;

        // If the train is actually getting dragged, allow it to move at full speed
        CurrentSpeed = MaxSpeed;
        
        if (!movingToDifferentTrack)
        {
            float? candidateTargetT = FindTOnTrack(currentTrack, hitInfo.point, TargetT, eventData.pressEventCamera);
            if (candidateTargetT != null) TargetT = candidateTargetT.Value;

            if (TargetT + epsilonForTrackTransfer >= t && currentTrack.ShouldTransitionForward(TargetT, epsilonForTrackTransfer) && currentTrack.HasNext())
            {
                targetTrackIsForwards = true;
                // targetTrack = currentTrack.Next.First(track => track.isActiveAndEnabled);
                movingToDifferentTrack = true;
                // Add all target tracks at their start (because moving forward) and without calculating distance (that will be done immediately after)
                possibleTargetTracks = currentTrack.Next.Select(track => (track, track.StartT, float.NaN)).ToArray();
            }
            else if (TargetT - epsilonForTrackTransfer < t && currentTrack.ShouldTransitionBackward(TargetT, epsilonForTrackTransfer) && currentTrack.HasPrevious())
            {
                targetTrackIsForwards = false;
                // targetTrack = currentTrack.Previous.First(track => track.isActiveAndEnabled);
                movingToDifferentTrack = true;
                // Add all target tracks at their end (because moving backward) and without calculating distance (that will be done immediately after)
                possibleTargetTracks = currentTrack.Previous.Select(track => (track, track.EndT, float.NaN)).ToArray();
            }
            
        }

        if (movingToDifferentTrack)
        {
            // Evaluate target t for all possible track switches and save the distance from the cursor to that point
            for (int i = 0; i < possibleTargetTracks!.Length; i++)
            {
                float? candidateTargetT = FindTOnTrack(possibleTargetTracks[i].target, hitInfo.point, possibleTargetTracks[i].t, eventData.pressEventCamera);
                float score;
                // If the new point is in a bad position, use the value from the previous drag instead
                if (!candidateTargetT.HasValue)
                {
                    candidateTargetT = possibleTargetTracks[i].t;
                    score = possibleTargetTracks[i].score;
                }
                else
                {
                    // Evaluate a point halfway between the target and the center of this track.
                    // We'll use that to calculate a distance for the track to the pointer
                    Vector3 halfwayTangent = possibleTargetTracks[i].target.SplineContainer
                        .EvaluateTangent((candidateTargetT.Value + 0.5f) * 0.5f);
                    if (!targetTrackIsForwards) halfwayTangent *= -1; // If the track is backwards, so is the tangent
                    Vector3 mouseDirection = hitInfo.point - transform.position;
                    score = Vector3.Dot(halfwayTangent.normalized, mouseDirection.normalized);
                }
                possibleTargetTracks[i] = (possibleTargetTracks[i].target, candidateTargetT.Value, score);
            }
        }
    }
    
    private float? FindTOnTrack(TrackDefinition track, Vector3 fromPoint, float previousT, Camera camera)
    {
        // Find position on curve
        float3 hitPointCurveLocal = track.SplineContainer.transform.InverseTransformPoint(fromPoint);
        SplineUtility.GetNearestPoint(splinePath, hitPointCurveLocal, out float3 candidatePointCurveLocal, out float candidateTargetT, iterations:CurveIterationCount);
        Vector3 candidateTarget = track.SplineContainer.transform.TransformPoint(candidatePointCurveLocal); // Cast once, use later
        Debug.DrawLine(fromPoint, candidateTarget, Color.cyan, 0, false);

        // Find if the new position on the curve is close enough to the old position on the curve for it to be seen as valid dragging
        Vector3 previousTarget = track.SplineContainer.EvaluatePosition(splinePath, previousT);

        // float sqDistToTrack = (fromPoint - candidateTarget).sqrMagnitude;
        // if (sqDistToTrack > MaxSqDistToTrackToAllowDragging)
        // {
        //     Debug.DrawLine(fromPoint, candidateTarget, Color.red, 0.1f, false);
        //     return null;
        // }
        
        if ((previousTarget - candidateTarget).sqrMagnitude > MaxSqDistToPreviousTargetToAllowDragging)
        {
            Debug.DrawLine(previousTarget, candidatePointCurveLocal, Color.red, 0.1f, false);
            return null;
        }

        // Check if the point is visible on the camera, to prevent dragging out of sight
        Vector3 positionOfCandidateOnCamera = camera.WorldToViewportPoint(candidateTarget);
        if (positionOfCandidateOnCamera.x < 0 || positionOfCandidateOnCamera.x > 1 ||
            positionOfCandidateOnCamera.y < 0 || positionOfCandidateOnCamera.y > 1 || positionOfCandidateOnCamera.z < 0)
        {
            return null;
        }

        return candidateTargetT;
    }

    public void OnTrackChanged()
    {
        if (previousTrack != null) previousTrack.OccupiedBy = null;
        splinePath = new SplinePath<Spline>(currentTrack.SplineContainer.Splines);
        CurrentSplineLength = splinePath.GetLength();
        CurrentSplineLengthInv = 1 / splinePath.GetLength();
        currentTrack.OccupiedBy = this;
        previousTrack = currentTrack;
    }

    public void Ignite()
    {
        if (IsOnFire) return; // Already on fire


        IsOnFire = true;
        Fire.SetActive(true);
        SoundPlayer.PlayCatchFire();
    }

    public void Extinguish()
    {
        if (!IsOnFire) return; // Not on fire

        IsOnFire = false;
        Fire.SetActive(false);
    }
}