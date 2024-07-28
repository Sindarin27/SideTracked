using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class CameraTransitionTrack : AbstractTrackEvent
    {
        public Transform cameraPositionA;
        public Transform cameraPositionB;
        public float tToMoveBackToA = 0.3f;
        public float tToMoveToB = 0.7f;
        public bool currentlyAtA = true;

        public int unlockLevelIndex = 0;
        public bool doUnlockLevel = false;

        public bool continueMovingForwardsIfTransitionForward = true;
        public bool continueMovingBackwardsIfTransitionBackward = true;
        
        [FormerlySerializedAs("transition")] public AnimationCurve PositionTransition;
        [FormerlySerializedAs("transition")] public AnimationCurve RotationTransition;
        [CanBeNull] public CameraTransitionTrack buddy;

        [FormerlySerializedAs("removePreviousTrackOnTransition")] [FormerlySerializedAs("removePreviousTrackAfterTransition")] [FormerlySerializedAs("removePreviousTrack")] public bool makeOneWayTrackOnTransition = false;
        public override bool CheckCondition(DraggableAlongSpline triggerer, float t)
        {
            // Debug.Log("Checked!");
            return (currentlyAtA && t > tToMoveToB) || (!currentlyAtA && t < tToMoveBackToA);
        }

        protected override void activate(DraggableAlongSpline triggerer, float t)
        {
            // Debug.Log("Activated!");
            currentlyAtA = !currentlyAtA;
            if (buddy != null) buddy.currentlyAtA = currentlyAtA;
            StartCoroutine(Camera.main!.GetComponent<CameraRotator>().MoveCamera(currentlyAtA ? cameraPositionA : cameraPositionB, PositionTransition, RotationTransition));
            if (makeOneWayTrackOnTransition) triggerer.currentTrack.Previous.Clear();

            if (continueMovingBackwardsIfTransitionBackward && currentlyAtA)
            {
                triggerer.TargetT = triggerer.currentTrack.StartT;
                triggerer.CurrentSpeed = triggerer.SpeedCasual;
            }
            else if (continueMovingForwardsIfTransitionForward && !currentlyAtA)
            {
                triggerer.TargetT = triggerer.currentTrack.EndT;
                triggerer.CurrentSpeed = triggerer.SpeedCasual;
            }

            if (doUnlockLevel && unlockLevelIndex > PlayerPrefs.GetInt(LevelSelector.MAX_LEVEL_KEY, 0) && triggerer.IsPlayer)
            {
                PlayerPrefs.SetInt(LevelSelector.MAX_LEVEL_KEY, unlockLevelIndex);
                PlayerPrefs.Save();
            }
        }
    }
}