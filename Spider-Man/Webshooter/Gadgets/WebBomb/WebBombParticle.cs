using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man
{
    public class WebBombParticle : MonoBehaviour
    {
        private void OnParticleCollision(GameObject other)
        {
            Debug.Log("Particle Collision occurred");
            if (other.GetComponentInParent<Creature>() is Creature creature && !creature.isPlayer)
            {
                if (creature.gameObject.GetComponent<CreatureWebTracker>() is CreatureWebTracker tracker)
                {
                    tracker.MaxWebbed();
                }
                else
                {
                    var tracked = creature.gameObject.AddComponent<CreatureWebTracker>();
                    tracked.MaxWebbed();
                }
            }
        }
    }
}