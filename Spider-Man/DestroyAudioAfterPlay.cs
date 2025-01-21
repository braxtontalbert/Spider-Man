using System;
using UnityEngine;

namespace Spider_Man
{
    public class DestroyAudioAfterPlay : MonoBehaviour
    {
        private AudioSource source;

        private void Start()
        {
            source = gameObject.GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (!source.isPlaying)
            {
                Destroy(this.gameObject);
            }
        }
    }
}