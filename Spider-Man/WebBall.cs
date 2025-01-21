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
        private MeshRenderer renderer;
        private void Start()
        {
            item = GetComponent<Item>();
            //spawnPoint = item.transform.position;
            //renderer = item.gameObject.transform.Find("webbaltextured").gameObject.GetComponent<MeshRenderer>();
            //item.mainCollisionHandler.OnCollisionStartEvent += OnCollisionStart;
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Collision detected on web ball");
            Debug.Log(item.colliderGroups[0].colliders[0].material);
            item.Despawn();
        }

        private void Update()
        {
            /*if (Vector3.Distance(spawnPoint, item.transform.position) < 0.3f)
            {
                renderer.enabled = false;
            }
            else if(!renderer.enabled)
            {
                renderer.enabled = true;
            }*/
        }
    }
}