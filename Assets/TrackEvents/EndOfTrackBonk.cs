using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class EndOfTrackBonk : AbstractTrackEvent
    {
        [SerializeField] private float epsilonForEdgeDetection = 0.0001f;
        private bool beingTriggered = false;
        public override bool CheckCondition(DraggableAlongSpline triggerer, float t)
        {
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
            float speed = Math.Abs(triggerer.TDiff * triggerer.CurrentSplineLength);
            triggerer.SoundPlayer.PlayCollission(triggerer.TDiff, triggerer.CurrentSplineLength);
        }
    }
}