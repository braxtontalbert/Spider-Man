using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.WebBall
{
    public class WebBall : MonoBehaviour
    {
        private Item item;
        private Vector3 spawnPoint;
        private Transform webBallTexture;
        private string webTypeAddition = "";
        public void Setup(Vector3 spawnPoint, Transform webBallTexture)
        {
            this.spawnPoint = spawnPoint;
            this.webBallTexture = webBallTexture;
        }
        private void Start()
        {
            item = GetComponent<Item>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            item.Despawn();
            if (ModOptions.webColor == "Black")
            {
                webTypeAddition = "Black";
            }
            Catalog.InstantiateAsync("webSplat" + webTypeAddition, collision.contacts[0].point, item.transform.rotation,
                null,
                go =>
                {
                }, "WebHitSplat");
            
            if (collision.gameObject.GetComponentInParent<Creature>() is Creature creature)
            {
                var direction = -collision.relativeVelocity.normalized;
                if(!creature.isKilled) creature.ForceStagger(direction, BrainModuleHitReaction.PushBehaviour.Effect.StaggerFull);
                if (creature.gameObject.GetComponent<CreatureWebTracker>() is CreatureWebTracker tracker)
                {
                    tracker.hitNumber += 1;
                }
                else
                {
                    var trackerAdd = creature.gameObject.AddComponent<CreatureWebTracker>();
                    trackerAdd.hitNumber += 1;
                }
            }
            else if (collision.gameObject.GetComponent<Item>() is Item hitItem)
            {
                if (hitItem.mainHandler != null)
                {
                    var hitItemCreature = hitItem.mainHandler.creature;
                    hitItem.mainHandler.UnGrab(false);
                    hitItem.AddForce(collision.relativeVelocity.normalized * (hitItem.totalCombinedMass + 4f), ForceMode.Impulse);
                }
            }
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
                var vector = new Vector3(localScaleRef.x, localScaleRef.y, localScaleRef.z + 0.3f);
                webBallTexture.transform.localScale = Vector3.Lerp(localScaleRef, vector, Time.deltaTime * 300f);
                if (Vector3.Distance(item.transform.position, spawnPoint) > 20f)
                {
                    item.Despawn();
                }
            }
        }
    }
}