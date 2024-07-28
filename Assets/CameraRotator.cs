using System;
using System.Collections;
using System.Collections.Generic;
using MoreLinq.Extensions;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class CameraRotator : MonoBehaviour
    {
        private Queue<int> lockTickets = new Queue<int>();
        public Quaternion look;
        public AnimationCurve PitchResponse = new AnimationCurve(new Keyframe(-0.5f, -4), new Keyframe(0.5f, 4));
        public AnimationCurve YawResponse = new AnimationCurve(new Keyframe(-0.5f, -2), new Keyframe(0.5f, 2));
        private DepthOfField dof;
        public AudioListener audioListener;
        public Transform focusObject;
        public float dofStep = 0.01f;
        
        private void Start()
        {
            look = GetComponent<Camera>().transform.rotation;
            if (!GetComponent<PostProcessVolume>().profile.TryGetSettings<DepthOfField>(out dof))
                Debug.LogError("Could not find dof");;
        }

        private void Update()
        {
            float pitch = PitchResponse.Evaluate(-Input.mousePosition.y / Screen.height + 0.5f);
            float yaw = YawResponse.Evaluate(Input.mousePosition.x / Screen.width - 0.5f);
            // Debug.Log($"p{pitch} y{yaw}");
            Transform transform1 = transform;
            transform1.rotation = Quaternion.Euler(new Vector3(pitch, yaw, 0.0f)) * look;

            Vector3 focusPos = focusObject.position;
            dof.focusDistance.value = Mathf.MoveTowards(dof.focusDistance.value, (focusPos - transform1.position).magnitude, dofStep * Time.deltaTime);

            audioListener.transform.position = (transform.position + focusPos) * 0.5f;
        }
        
        public IEnumerator MoveCamera(Transform to, AnimationCurve PositionTransition, AnimationCurve RotationTransition)
        {
            int lockTicket = Random.Range(int.MinValue, int.MaxValue);
            lockTickets.Enqueue(lockTicket);
            
            // Wait for our turn
            while (lockTickets.Peek() != lockTicket)
            {
                yield return null;
            }
            
            // Our turn
            
            DraggableAlongSpline.AllowDragging = false;
            Vector3 startPos = transform.position;
            Quaternion startRotation = look;
            float elapsedTime = 0;
            float endTime = Math.Max(RotationTransition[RotationTransition.length - 1].time,
                PositionTransition[PositionTransition.length - 1].time);
            while (elapsedTime < endTime)
            {
                elapsedTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, to.position, PositionTransition.Evaluate(elapsedTime));
                look = Quaternion.Slerp(startRotation, to.rotation, RotationTransition.Evaluate(elapsedTime));
                yield return null;
            }

            // Release lock
            lockTickets.Dequeue();
            DraggableAlongSpline.AllowDragging = true;
        }
    }
}