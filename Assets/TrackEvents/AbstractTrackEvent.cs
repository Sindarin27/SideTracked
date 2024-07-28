using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace DefaultNamespace
{
    public abstract class AbstractTrackEvent : MonoBehaviour
    {
        [SerializeField] private bool disableAfterTrigger = false;

        public void CheckAndTrigger(DraggableAlongSpline triggerer, float t)
        {
            if (!enabled) return;
            
            if (CheckCondition(triggerer, t))
            {
                Activate(triggerer, t);
            }
        }

        public abstract bool CheckCondition(DraggableAlongSpline triggerer, float t);

        protected abstract void activate(DraggableAlongSpline triggerer, float t);

        public void Activate(DraggableAlongSpline triggerer, float t)
        {
            if (!enabled) return;
            
            activate(triggerer, t);
            
            if (disableAfterTrigger) enabled = false;
        }
    }
}