using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class Level : MonoBehaviour
    {
        [SerializeField] private bool jumpHere = false;
        [SerializeField] private bool previewHere = false;
        
        public DraggableAlongSpline player;
        public List<GameObject> components;
        public int index;
        
        public List<AbstractTrackEvent> eventsToAutoComplete;

        public TrackDefinition startTrack;
        public float TOnStartTrack;
        public float TargetTOnStartTrack;
        
        public AnimationCurve PositionTransition;
        public AnimationCurve RotationTransition;

        public Transform startCameraPosition;

        public void StartHere()
        {
            Camera cam = Camera.main;
            CameraRotator rotator = cam!.GetComponent<CameraRotator>();
            rotator.focusObject = player.transform;
            StartCoroutine(rotator.MoveCamera(startCameraPosition, PositionTransition, RotationTransition));

            player.gameObject.SetActive(true);
            player.currentTrack = startTrack;
            player.t = TOnStartTrack;
            player.TargetT = TargetTOnStartTrack;
            player.movingToDifferentTrack = false;
            player.OnTrackChanged();
            player.EvaluatePositionAndRotation();
            player.CurrentSpeed = player.SpeedCasual;
            
            foreach (Level level in FindObjectsOfType<Level>(true))
            {
                if (level.index < index)
                {
                    level.AutoComplete();
                }
            }
        }

        public void AutoComplete()
        {
            foreach (AbstractTrackEvent trackEvent in eventsToAutoComplete)
            {
                trackEvent.Activate(player, 0);
            }
        }

        public void Preview()
        {
            Camera cam = Camera.main;
            CameraRotator rotator = cam!.GetComponent<CameraRotator>();
            rotator.focusObject = startTrack.transform;
            StartCoroutine(rotator.MoveCamera(startCameraPosition, PositionTransition, RotationTransition));
        }

        private void OnValidate()
        {
            if (jumpHere) StartHere();
            jumpHere = false;
            if (previewHere) Preview();
            previewHere = false;
        }
    }
}