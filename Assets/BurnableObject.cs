using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurnableObject : MonoBehaviour
{
    private bool triggered = false;
    public float spawnEffectTime = 2;
    public float pause = 1;
    public AnimationCurve fadeIn;

    public ParticleSystem ps;
    public AudioSource audioSource;
    float timer = 0;
    Renderer _renderer;

    int shaderProperty;

    void Start ()
    {
        shaderProperty = Shader.PropertyToID("_cutoff");
        _renderer = GetComponent<Renderer>();
    }
	
    void Update ()
    {
        if (!triggered) return;
        
        if (timer < spawnEffectTime + pause)
        {
            timer += Time.deltaTime;
        }
        else
        {
            triggered = false;
            gameObject.SetActive(false);
            timer = 0;
        }


        _renderer.material.SetFloat(shaderProperty, fadeIn.Evaluate( Mathf.InverseLerp(0, spawnEffectTime, timer)));
        
    }

    public void Trigger()
    {
        gameObject.SetActive(true);
        ParticleSystem.MainModule main = ps.main;
        main.duration = spawnEffectTime;
        triggered = true;
        ps.Play();
        audioSource.Play();
    }
}