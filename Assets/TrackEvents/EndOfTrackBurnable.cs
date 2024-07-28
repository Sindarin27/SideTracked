using System;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class EndOfTrackBurnable : AbstractTrackEvent
    {
        [SerializeField] private float epsilonForEdgeDetection = 0.0001f;
        [CanBeNull] public AbstractTrackEvent OtherEventToDisable = null;
        private bool beingTriggered = false;

        public GameObject ToBurn;
        public BurnableObject BurnAnimation;
        public TrackDefinition TrackBeforeBlock, TrackAfterBlock;
        public float BurnDuration = 0.5f;
        
        public override bool CheckCondition(DraggableAlongSpline triggerer, float t)
        {
            if (!triggerer.IsOnFire) return false; // Cannot set something on fire if we're not on fire
            if (t > 1 - epsilonForEdgeDetection)
            {
                if (!beingTriggered)
                {
                    beingTriggered = true;
                    return true;
                }
            }
            else
            {
                if (t < 1 - epsilonForEdgeDetection) beingTriggered = false;
            }

            return false;
        }

        protected override void activate(DraggableAlongSpline triggerer, float t)
        {
            ToBurn.transform.DOScale(Vector3.zero, BurnDuration).OnComplete(() => ToBurn.SetActive(false));
            BurnAnimation.Trigger();
            TrackBeforeBlock.Next.Add(TrackAfterBlock);
            TrackAfterBlock.Previous.Add(TrackBeforeBlock);
            if (OtherEventToDisable != null) OtherEventToDisable.enabled = false;
            if (triggerer)
            {
                triggerer.Extinguish();
            }
        }
    }
}