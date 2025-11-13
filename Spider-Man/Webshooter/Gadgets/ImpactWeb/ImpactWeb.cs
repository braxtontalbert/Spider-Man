using System.Collections;
using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.ImpactWeb
{
    public class ImpactWeb : MonoBehaviour
    {
        private Item item;
        private Vector3 spawnPoint;
        private Transform webBallTexture;
        private Item webshooter;
        private string webTypeAddition = "";
        
        public void Setup(Vector3 spawnPoint, Transform webBallTexture, Item webshooter)
        {
            this.spawnPoint = spawnPoint;
            this.webBallTexture = webBallTexture;
            this.webshooter = webshooter;
        }
        private void Start()
        {
            item = GetComponent<Item>();
        }

        IEnumerator DelayWeb(Creature creature)
        {
            creature.gameObject.AddComponent<CreatureWebTracker>().OnWebMax();
            var trackerAdd = creature.gameObject.GetComponent<CreatureWebTracker>();
            yield return null;
            trackerAdd.MaxWebbed();
            item.Despawn();
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (ModOptions.webColor == "Black")
            {
                webTypeAddition = "Black";
            }
            var direction = (collision.contacts[0].point - webshooter.flyDirRef.transform.position).normalized;
            Catalog.InstantiateAsync("webSplat" + webTypeAddition, collision.contacts[0].point, item.transform.rotation,
                null,
                go =>
                {
                }, "WebHitSplat");
            
            if (collision.gameObject.GetComponentInParent<Creature>() is Creature creature)
            {
                if (creature.gameObject.GetComponent<CreatureWebTracker>() is CreatureWebTracker tracker)
                {
                    tracker.MaxWebbed();
                    item.Despawn();
                }
                else
                {
                    StartCoroutine(DelayWeb(creature));
                }
                creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                foreach (var part in creature.ragdoll.parts)
                {
                    part.physicBody.rigidBody.AddForce(direction * 300f, ForceMode.Impulse);
                }
            }
            else item.Despawn();
            
        }

        private float elapsedTime = 0f;
        
        private void Update()
        {
            if (Vector3.Distance(spawnPoint, item.transform.position) > 0.3f)
            {
                var renderer = webBallTexture.GetComponentInChildren<MeshRenderer>();
                renderer.enabled = true;
            }

            if (item && webBallTexture)
            {
                var localScaleRef = webBallTexture.transform.localScale;
                var vector = new Vector3(localScaleRef.x, localScaleRef.y, localScaleRef.z + 0.1f);
                webBallTexture.transform.localScale = Vector3.Lerp(localScaleRef, vector, Time.deltaTime * 300f);
                if (Vector3.Distance(item.transform.position, spawnPoint) > 20f)
                {
                    item.Despawn();
                }
            }
        }
    }
}