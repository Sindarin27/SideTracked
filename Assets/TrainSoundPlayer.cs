using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class TrainSoundPlayer : MonoBehaviour
    {
        public AudioClip[] collission;
        [SerializeField] private float collisionLowPitch = 0.75f, collisionHighPitch = 1.2f;
        [SerializeField] private float collisionLowVolume = 0.75f, collisionHighVolume = 1.2f;
        [SerializeField] private float collisionSpeedWeightVolume = 50, collisionSpeedWeightPitch = 5;
        [SerializeField] private float volumeBias = 0.5f, pitchBias = 0.5f;

        public AudioClip[] honk;
        [SerializeField] private float honkLowPitch = 0.75f, honkHighPitch = 1.2f;
        public AudioSource audioSource;

        public AudioClip[] catchFire;
        [SerializeField] private float catchFireLowPitch = 0.75f, catchFireHighPitch = 1.2f;

        
        void Start()
        {
        }

        private void PlayRandomFrom(AudioClip[] collection, float minPitch, float maxPitch, float minVolume = 1, float maxVolume = 1)
        {
            if (collection.Length == 0)
            {
                Debug.LogError("No sounds to play, skipping");
                return;
            }
            int index = Random.Range(0, collection.Length);
            audioSource.clip = collection[index];
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.volume = Random.Range(minVolume, maxVolume);
            audioSource.Play();
        }
        
        public void PlayCollission(float tDiff, float currentSplineLength)
        {
            float speed = Math.Abs(tDiff * currentSplineLength);
            PlayRandomFrom(collission, collisionLowPitch * (speed * collisionSpeedWeightPitch + pitchBias), 
                collisionHighPitch * (speed * collisionSpeedWeightPitch + pitchBias), 
                collisionLowVolume * (speed * collisionSpeedWeightVolume + volumeBias), 
                collisionHighVolume * (speed * collisionSpeedWeightVolume + volumeBias));
        }

        public void Honk()
        {
            PlayRandomFrom(honk, honkLowPitch, honkHighPitch);
        }

        public void PlayCatchFire()
        {
            PlayRandomFrom(catchFire, catchFireLowPitch, catchFireHighPitch);
        }
        
    }
}