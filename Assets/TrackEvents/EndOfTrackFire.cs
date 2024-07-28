using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class EndOfTrackFire : AbstractTrackEvent
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
            triggerer.Ignite();
        }
    }
}