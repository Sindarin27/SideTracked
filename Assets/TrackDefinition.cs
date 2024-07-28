using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

namespace DefaultNamespace
{
    public class TrackDefinition : MonoBehaviour
    {
        [CanBeNull] private SplineContainer splineContainerCached;

        public List<TrackDefinition> Previous;
        public List<TrackDefinition> Next;
        public List<AbstractTrackEvent> Events;

        public DraggableAlongSpline OccupiedBy;
        public List<TrackDefinition> SharesOccupationWith;
        
        // public bool IsInverted;

        public float StartT => 0; //IsInverted ? 1 : 0;
        public float EndT => 1; //IsInverted ? 0 : 1;

        public SplineContainer SplineContainer
        {
            get
            {
                return splineContainerCached ??= GetComponentInChildren<SplineContainer>();
            }
        }

        public bool IsFree()
        {
            return OccupiedBy == null && SharesOccupationWith.All(t => t.OccupiedBy == null);
        }
        
        public bool HasNext()
        {
            return Next != null && Next.Any(track => track != null && track.isActiveAndEnabled);
        }
        
        public bool HasPrevious()
        {
            return Previous != null && Previous.Any(track => track != null && track.isActiveAndEnabled);
        }

        public bool ShouldTransitionForward(float t, float epsilonForTransition)
        {
            return t > 1 - epsilonForTransition; // && !IsInverted) || (t < epsilonForTransition && IsInverted);
        }
        
        public bool ShouldTransitionBackward(float t, float epsilonForTransition)
        {
            return t < epsilonForTransition;
        }
    }
}