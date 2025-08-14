using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.WebNet
{
    public class CollisionPerNode : MonoBehaviour
    {
        private void OnCollisionEnter(Collision other)
        {
            this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}