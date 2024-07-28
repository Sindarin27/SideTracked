using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class RandomScaleAndRotation : MonoBehaviour
    {
        public float minScaleMultiplier, maxScaleMultiplier;

        private void Start()
        {
            transform.localScale *= Random.Range(minScaleMultiplier, maxScaleMultiplier);
            transform.Rotate(transform.up, Random.Range(0, 360));
        }
    }
}