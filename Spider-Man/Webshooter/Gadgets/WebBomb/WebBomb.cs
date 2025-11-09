using System;
using System.Collections;
using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace Spider_Man.Webshooter.Gadgets.WebBomb
{
    public class WebBomb : MonoBehaviour
    {
        private VisualEffect vfx;
        private bool started;
        private Item item;
        private RagdollHand hand;
        private bool collisionFirst;
        private GameObject sound;
        private void OnCollisionEnter(Collision other)
        {
            if(!collisionFirst) 
            {
                collisionFirst = true;
            }
            else
            {
                return;
            }
            this.gameObject.transform.parent = other.gameObject.transform;
            this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
            vfx = this.gameObject.GetComponentInChildren<VisualEffect>();
            StartCoroutine(BombTimer());
        }

        public void Setup(Item item, RagdollHand hand)
        {
            this.item = item;
            this.hand = hand;
        }
        IEnumerator BombTimer()
        {
            yield return new WaitForSeconds(1f);
            ExplodeWebBomb();
            yield return new WaitForSeconds(3f);
            Destroy(this.gameObject);
        }

        private void OnDestroy()
        {
            Destroy(sound);
        }

        IEnumerator DelayWeb(Creature creature)
        {
            creature.gameObject.AddComponent<CreatureWebTracker>().OnWebMax();
            var trackerAdd = creature.gameObject.GetComponent<CreatureWebTracker>();
            yield return null;
            trackerAdd.MaxWebbed();
        }

        private void ExplodeWebBomb()
        {
            if (vfx)
            {
                vfx.Play();
                Catalog.InstantiateAsync("webBombExplodeSFX", this.gameObject.transform.position, this.item.transform.rotation,
                    null,
                    sfx =>
                    {
                        sound = sfx;
                        var audio = sfx.GetComponent<AudioSource>();
                        audio.pitch = Random.Range(0.9f, 1.1f);
                        audio.Play();
                        sfx.AddComponent<DestroyAudioAfterPlay>();
                    }, "WebBombExplodeSFX");
                foreach (var creature in Creature.allActive)
                {
                    if (creature.isPlayer) continue;
                    float distance = Vector3.Distance(creature.ragdoll.targetPart.transform.position, this.gameObject.transform.position);
                    Vector3 direction = (creature.ragdoll.targetPart.transform.position - this.gameObject.transform.position).normalized;
                    if (distance <= 5f)
                    {
                        creature.ForceStagger(direction, BrainModuleHitReaction.PushBehaviour.Effect.StaggerFull);
                        if (creature.gameObject.GetComponent<CreatureWebTracker>() is CreatureWebTracker tracker)
                        {
                            tracker.MaxWebbed();
                        }
                        else
                        {
                            StartCoroutine(DelayWeb(creature));
                        }
                    }
                }
            }
        }
    }
}