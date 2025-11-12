using System.Collections.Generic;
using Spider_Man.Management;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets.WebNet
{
    public class WebConnector : MonoBehaviour
    {
        public List<GameObject> connectedNodes;
        public Material lineMaterial;
        public float lineWidth = 0.15f;
        private Vector3 normal;

        private readonly List<GameObject> lineObjects = new List<GameObject>();
        private readonly List<LineRenderer> lineRenderers = new List<LineRenderer>();

        void Update()
        {
            SyncLineObjects();
            UpdateLines();
        }

        private void Start()
        {
            if (!this.gameObject.name.Equals("Center")) return;
            foreach (var node in connectedNodes)
            {
                node.GetComponent<Rigidbody>().AddForce(normal * 90f, ForceMode.Impulse);
            }
        }

        public void Setup(List<GameObject> nodes, Vector3 normal = new Vector3())
        {
            this.connectedNodes = nodes;
            foreach (var node in connectedNodes)
            {
                node.AddComponent<CollisionPerNode>();
            }
            this.lineMaterial = WebShooterPersistence.local.GetWebMaterial(ModOptions.webNetType);
            this.normal = normal;
        }

        void SyncLineObjects()
        {
            // Ensure there's one line per node
            while (lineObjects.Count < connectedNodes.Count)
            {
                GameObject lineObj = new GameObject($"Line_{lineObjects.Count}");
                lineObj.transform.SetParent(transform, false);

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
                lr.widthMultiplier = lineWidth;
                lr.useWorldSpace = true;
                lr.alignment = LineAlignment.View;
                lr.positionCount = 2;
                lr.numCapVertices = 4;

                lineObjects.Add(lineObj);
                lineRenderers.Add(lr);
            }
        }

        void UpdateLines()
        {
            Vector3 selfCenter = GetWorldCenter(gameObject);

            for (int i = 0; i < connectedNodes.Count; i++)
            {
                GameObject target = connectedNodes[i];
                LineRenderer lr = lineRenderers[i];

                if (target != null)
                {
                    Vector3 targetCenter = GetWorldCenter(target);
                    lr.enabled = true;
                    lr.SetPosition(0, selfCenter);
                    lr.SetPosition(1, targetCenter);
                }
                else
                {
                    lr.enabled = false;
                }
            }
        }

        Vector3 GetWorldCenter(GameObject obj)
        {
            if (obj.TryGetComponent<Renderer>(out var rend))
                return rend.bounds.center;
            if (obj.TryGetComponent<Collider>(out var col))
                return col.bounds.center;
            return obj.transform.position;
        }
    }

}