using System.Collections;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.WebBomb
{
    public class WebBomb : MonoBehaviour
    {
        private ParticleSystem particleSystem;
        private void OnCollisionEnter(Collision other)
        {
            this.gameObject.transform.parent = other.gameObject.transform;
            this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            particleSystem = this.gameObject.GetComponentInChildren<ParticleSystem>();
            Debug.Log("Particle System: " + particleSystem);
            if(particleSystem) particleSystem.gameObject.AddComponent<WebBombParticle>();
            StartCoroutine(BombTimer());
        }

        IEnumerator BombTimer()
        {
            yield return new WaitForSeconds(2f);
            ExplodeWebBomb();
        }

        private void ExplodeWebBomb()
        {
            particleSystem.gameObject.transform.parent = null;
            particleSystem.gameObject.transform.rotation = Quaternion.identity;
            particleSystem.Play();
        }
    }
}