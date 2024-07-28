using System;
using System.Collections.Generic;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Splines;

namespace DefaultNamespace
{
    public class RubbleClearer : AbstractTrackEvent
    {
        [SerializeField] private float maximumT = 0.5f;
        private bool beingTriggered = false;
        public List<GameObject> rubble;
        public float rubbleMoveDist = 100, rubbleMoveTime = 10;
        
        [CanBeNull] public AbstractTrackEvent OtherEventToDisable = null;
        public List<TrackDefinition> TrackBeforeBlock, TrackAfterBlock;

        public override bool CheckCondition(DraggableAlongSpline triggerer, float t)
        {
            if (t > maximumT)
            {
                if (!beingTriggered)
                {
                    beingTriggered = true;
                    return true;
                }
            }
            else
            {
                if (t < maximumT) beingTriggered = false;
            }

            return false;
        }

        protected override void activate(DraggableAlongSpline triggerer, float t)
        {
            triggerer.SoundPlayer.PlayCollission(0.1f, 1);
            foreach (TrackDefinition trackBefore in TrackBeforeBlock)
            {
                foreach (TrackDefinition trackAfter in TrackAfterBlock)
                {
                    trackBefore.Next.Add(trackAfter);
                    trackAfter.Previous.Add(trackBefore);
                }
            }
            foreach (GameObject rock in rubble)
            {
                rock.transform.DOMove(
                    (rock.transform.position - triggerer.transform.position).normalized * rubbleMoveDist + rock.transform.position, rubbleMoveTime);
            }
        }
    }
}