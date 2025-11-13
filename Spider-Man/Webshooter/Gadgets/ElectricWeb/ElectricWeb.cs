using System.Collections;
using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.ElectricWeb
{
    public class ElectricWeb : MonoBehaviour
    {
        private Item item;
        private Vector3 spawnPoint;
        private Transform webBallTexture;
        private Item webshooter;
        string webTypeAddition = "";
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
                creature.Inflict("Electrocute", this, 5f, parameter: 100f);
                item.Despawn();
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