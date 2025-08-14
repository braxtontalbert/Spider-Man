using UnityEngine;

namespace Spider_Man.Webshooter
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