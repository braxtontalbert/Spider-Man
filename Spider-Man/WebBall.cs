using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man
{
    public class WebBall : MonoBehaviour
    {
        private Item item;
        private Vector3 spawnPoint;
        private Transform webBallTexture;

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
            Catalog.InstantiateAsync("webSplat", collision.contacts[0].point, item.transform.rotation,
                null,
                go =>
                {
                }, "WebHitSplat");
            if (collision.gameObject.GetComponentInParent<Creature>() is Creature creature)
            {
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
            /*else
            {
                Vector3 sum = new Vector3();
                foreach (var contact in collision.contacts)
                {
                    sum += contact.normal;
                }

                Vector3 positionSum = new Vector3();
                foreach (var contact in collision.contacts)
                {
                    sum += contact.point;
                }

                var collisionNormalDirection = sum.normalized;
                var collisionPositionAverage = sum / collision.contacts.Length;

                Catalog.InstantiateAsync("WebHitMesh", collisionPositionAverage, collision.collider.transform.rotation,
                    null,
                    go =>
                    {
                        go.transform.rotation = Quaternion.LookRotation(go.transform.up, collisionNormalDirection);
                        DecalManager.decalQueue.Enqueue(go);
                    }, "WebHitMesh");
            }*/

        }

        private float elapsedTime = 0f;
        
        private void Update()
        {
            if (Vector3.Distance(spawnPoint, item.transform.position) > 0.3f)
            {
                var renderer = webBallTexture.GetComponent<MeshRenderer>();
                renderer.enabled = true;
            }

            if (item && webBallTexture)
            {
                var localScaleRef = webBallTexture.transform.localScale;
                var vector = new Vector3(localScaleRef.x, localScaleRef.y, localScaleRef.z + 3f);
                webBallTexture.transform.localScale = Vector3.Lerp(localScaleRef, vector, Time.deltaTime * 300f);
                if (Vector3.Distance(item.transform.position, spawnPoint) > 20f)
                {
                    item.Despawn();
                }
            }
        }
    }
}