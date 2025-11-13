using System;
using System.Collections;
using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spider_Man.Webshooter.Gadgets.WebBall
{
    public class WebBallGadget : MonoBehaviour, IGadget
    {
        //Required Members
        public string Name { get; set; }
        public int PressCount { get; set; }
        public Coroutine Coroutine { get; set; }
        public Item Item { get; set; }
        public bool ItemAttached { get; set; }
        public RagdollHand Hand { get; set; }
        
        //Custom members
        private bool SpawningWebBall { get; set; }

        private string webTypeAddition = "";

        public void Activate(Item item, RagdollHand hand, ref bool itemAttached)
        {
            Item = item;
            Hand = hand;
            ItemAttached = itemAttached;
            SpawnWebBall();
        }

        public IEnumerator WaitWindow(IGadget gadget)
        {
            yield return new WaitForSeconds(0.5f);
            PressCount = 0;
            Coroutine = null;
        }

        public bool DisallowItemGrab { get; set; }

        void SpawnWebBall()
        {
            if (!SpawningWebBall)
            {
                if (ModOptions.webColor == "Black")
                {
                    webTypeAddition = "Black";
                }
                SpawningWebBall = true;
                Catalog.GetData<ItemData>("WebBall"+webTypeAddition).SpawnAsync(callback =>
                {
                    webTypeAddition = "";
                    callback.transform.position = Item.flyDirRef.transform.position;
                    callback.transform.rotation = Item.flyDirRef.transform.rotation;
                    var webbBall = callback.GetComponent<Item>();
                    webbBall.Throw();
                    webbBall.IgnoreItemCollision(Item);
                    webbBall.IgnoreRagdollCollision(Hand.ragdoll);
                    var transformFound = callback.gameObject.transform.Find("webballRounded");
                    var renderer = transformFound.GetComponentInChildren<MeshRenderer>();
                    renderer.enabled = false;
                    webbBall.gameObject.AddComponent<WebBall>()
                        .Setup(Item.flyDirRef.transform.position, transformFound);
                    webbBall.physicBody.rigidBody.useGravity = false;
                    webbBall.physicBody.rigidBody.AddForce(
                        Item.flyDirRef.transform.forward *
                        Mathf.Clamp(Math.Abs(Hand.physicBody.rigidBody.velocity.magnitude), 85f, 130f),
                        ForceMode.Impulse);

                    
                    
                    Catalog.InstantiateAsync("WebBallSFX", Item.flyDirRef.transform.position,
                        Item.flyDirRef.transform.rotation,
                        Item.gameObject.transform,
                        sfx =>
                        {
                            var audio = sfx.GetComponent<AudioSource>();
                            audio.pitch = Random.Range(0.9f, 1.1f);
                            audio.Play();
                            sfx.AddComponent<DestroyAudioAfterPlay>();
                        }, "WebBallSFX");
                    SpawningWebBall = false;
                });
            }
        }
    }
}