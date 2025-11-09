using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.WebBomb
{
    public class WebBombGadget : MonoBehaviour, IGadget
    {
        public string Name { get; set; }
        public int PressCount { get; set; }
        public Coroutine Coroutine { get; set; }
        public Item Item { get; set; }
        public bool ItemAttached { get; set; }
        public RagdollHand Hand { get; set; }

        private bool SpawnTimeReset = false;
        bool SpawningWebBomb = false;
        private AudioSource ReloadSound;
        public void Activate(Item item, RagdollHand hand, ref bool itemAttached)
        {
            if (Item == null && Hand == null)
            {
                Name = "Impact Web";
                Item = item;
                Hand = hand;
                PressCount = 0;
                ItemAttached = itemAttached;
                SpawnTimeReset = true;
                SpawningWebBomb = false;
                Coroutine = null;
            }
            SpawnWebBomb();
        }
        
        IEnumerator WebBombTimer()
        {
            yield return new WaitForSeconds(2f);
            if (!ReloadSound)
            {
                Catalog.InstantiateAsync("reloadSound", Item.transform.position, Item.transform.rotation,
                    Item.transform,
                    go => { ReloadSound = go.GetComponent<AudioSource>(); }, "ReloadSoundHandler");
            }
            else
            {
                ReloadSound.Play();
            }

            SpawnTimeReset = true;
        }
        
        void SpawnWebBomb()
        {
            if (!SpawningWebBomb && SpawnTimeReset)
            {
                SpawnTimeReset = false;
                StartCoroutine(WebBombTimer());
                SpawningWebBomb = true;
                Catalog.InstantiateAsync("webBomb", Item.flyDirRef.transform.position, Item.flyDirRef.transform.rotation, null, callback =>
                {
                    var webbBall = callback.GetComponent<Item>();
                    webbBall.Throw(flyDetection: Item.FlyDetection.Forced);
                    webbBall.IgnoreItemCollision(Item);
                    webbBall.IgnoreRagdollCollision(Hand.ragdoll);
                    webbBall.gameObject.AddComponent<WebBomb>().Setup(Item, Hand);
                    webbBall.physicBody.rigidBody.useGravity = true;
                    webbBall.physicBody.rigidBody.AddForce(Item.flyDirRef.transform.forward * Mathf.Clamp(Hand.physicBody.velocity.magnitude, 15f, 30f),
                        ForceMode.Impulse);
                        
                    Catalog.InstantiateAsync("webBombShootSFX", Item.flyDirRef.transform.position, Item.flyDirRef.transform.rotation,
                        Item.gameObject.transform,
                        sfx =>
                        {
                            var audio = sfx.GetComponent<AudioSource>();
                            audio.pitch = Random.Range(0.9f, 1.1f);
                            audio.Play();
                            sfx.AddComponent<DestroyAudioAfterPlay>();
                        }, "WebBombSFX");
                    SpawningWebBomb = false;
                }, "WebBombHandler");
            }
        }

        public IEnumerator WaitWindow(IGadget gadget)
        {
            yield return new WaitForSeconds(0.5f);

            PressCount = 0;
            Coroutine = null;
        }

        public bool DisallowItemGrab { get; set; }
    }
}