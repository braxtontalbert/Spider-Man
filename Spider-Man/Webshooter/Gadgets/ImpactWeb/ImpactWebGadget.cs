using System.Collections;
using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets
{
    public class ImpactWebGadget : MonoBehaviour, IGadget
    {
        public string Name { get; set; }
        private bool SpawningImpactWeb { get; set; }
        private bool SpawnTimeReset { get; set; }
        private AudioSource ReloadSound { get; set; }
        public bool ItemAttached { get; set; }
        public RagdollHand Hand { get; set;}
        public Item Item { get; set; }
        public int PressCount { get; set; }
        public Coroutine Coroutine { get; set; }
        private string webTypeAddition = "";
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
                SpawningImpactWeb = false;
                Coroutine = null;
            }
            SpawnImpactWeb();
        }

        public IEnumerator WaitWindow(IGadget gadget)
        {
            yield return new WaitForSeconds(0.5f);

            PressCount = 0;
            Coroutine = null;
        }

        public bool DisallowItemGrab { get; set; }

        void SpawnImpactWeb()
        {
            if (!SpawningImpactWeb && SpawnTimeReset)
            {
                SpawnTimeReset = false;
                StartCoroutine(ImpactWebTimer());
                SpawningImpactWeb = true;
                if (ModOptions.webColor == "Black")
                {
                    webTypeAddition = "Black";
                }
                Catalog.InstantiateAsync("impactWeb"+webTypeAddition, Item.flyDirRef.transform.position, Item.flyDirRef.transform.rotation, null, callback =>
                {
                    webTypeAddition = "";
                    callback.transform.position = Item.flyDirRef.transform.position;
                    callback.transform.rotation = Item.flyDirRef.transform.rotation;
                    var webbBall = callback.GetComponent<Item>();

                    webbBall.gameObject.layer = GameManager.GetLayer(LayerName.MovingItem);
                    webbBall.IgnoreItemCollision(Item);
                    webbBall.IgnoreRagdollCollision(Hand.ragdoll);
                    var transformFound = callback.gameObject.transform.Find("webballRounded");
                    var renderer = transformFound.GetComponentInChildren<MeshRenderer>();
                    renderer.enabled = false;
                    webbBall.gameObject.AddComponent<ImpactWeb.ImpactWeb>().Setup(Item.flyDirRef.transform.position, transformFound, this.Item);
                    webbBall.physicBody.rigidBody.useGravity = false;
                    webbBall.physicBody.rigidBody.AddForce(Item.flyDirRef.transform.forward * Mathf.Clamp(Hand.physicBody.velocity.magnitude, 95, 150f),
                        ForceMode.Impulse);
                        
                    Catalog.InstantiateAsync("WebBallSFX", Item.flyDirRef.transform.position, Item.flyDirRef.transform.rotation,
                        Item.gameObject.transform,
                        sfx =>
                        {
                            var audio = sfx.GetComponent<AudioSource>();
                            audio.pitch = Random.Range(0.9f, 1.1f);
                            audio.Play();
                            sfx.AddComponent<DestroyAudioAfterPlay>();
                        }, "WebBallSFX");
                    SpawningImpactWeb = false;
                }, "ImpactWebHandler");
            }
        }
        IEnumerator ImpactWebTimer()
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
    }
}