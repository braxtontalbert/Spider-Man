using System.Collections;
using System.Collections.Generic;
using Spider_Man.Webshooter.Gadgets.WebNet;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Management
{
    public class CreatureWebTracker : MonoBehaviour
    {
        public readonly int maxWebbingCount = 100;
        public float hitNumber;
        private readonly int valueOfHit = 5;
        private bool decayWebbing = false;
        private float decayWebbingRate;
        private Creature creature;
        private bool allowContactEvent;
        public bool stuckToWall;
        private bool materialChanged;
        private bool slowedCreature;
        public Queue<Material[]> originalCreatureMaterial = new Queue<Material[]>();
        public ConfigurableJoint rightJoint;
        public ConfigurableJoint leftJoint;
        private bool webCompletedRefreshed = true;
        private GameObject webBase;

        private bool runUpdate = true;
        private void Start()
        {
            creature = GetComponent<Creature>();
            creature.ragdoll.targetPart.collisionHandler.OnCollisionStartEvent += CollisionEvent;
            creature.OnDespawnEvent += OnDespawn;
        }

        private void OnDespawn(EventTime eventtime)
        {
            if (eventtime == EventTime.OnStart)
            {
                if(webBase) Destroy(webBase);
                if(rightJoint) Destroy(rightJoint);
                if(leftJoint) Destroy(leftJoint);
                ResetCreatureMaterial();
                allowContactEvent = false;
                Destroy(this);
            }
        }

        public void OnWebMax()
        {
            this.runUpdate = false;
        }

        private void CollisionEvent(CollisionInstance collisioninstance)
        {
            if (!collisioninstance.targetCollider.gameObject.GetComponentInParent<Item>() &&
                !collisioninstance.targetCollider.gameObject.GetComponentInParent<Creature>() && allowContactEvent &&
                !stuckToWall)
            {
                creature.Kill();
                creature.ragdoll.targetPart.ragdoll.rootPart.physicBody.rigidBody.isKinematic = true;
                creature.brain.SetState(Brain.State.Idle);
                foreach (var part in creature.ragdoll.parts)
                {
                    part.physicBody.rigidBody.isKinematic = true;
                }

                creature.locomotion.isGrounded = false;
                stuckToWall = true;
                var contactNormal = collisioninstance.contactNormal;
                var direction = collisioninstance.impactVelocity.normalized;
                var dotProduct = Vector3.Dot(collisioninstance.contactNormal, direction);
                if (dotProduct > 0.95f) contactNormal = -contactNormal;
                var spawnPoint = collisioninstance.contactPoint + (-contactNormal * 1f);
                Catalog.InstantiateAsync("webNet", spawnPoint,
                    creature.ragdoll.targetPart.transform.rotation, null,
                    callback =>
                    {
                        webBase = callback;
                        callback.transform.LookAt(creature.ragdoll.targetPart.transform.position);
                        var item = callback.GetComponent<Item>();
                        var center = item.GetCustomReference("Center");
                        List<GameObject> nodes = new List<GameObject>();
                        foreach (var go in item.customReferences)
                        {
                            foreach (var go2 in item.customReferences)
                            {
                                if (!go2.Equals(go))
                                {
                                    Physics.IgnoreCollision(go.transform.gameObject.GetComponent<Collider>(),
                                        go2.transform.gameObject.GetComponent<Collider>());
                                }
                            }

                            go.transform.gameObject.GetComponent<Rigidbody>().useGravity = true;
                            if (!go.name.Equals("Center"))
                            {
                                nodes.Add(go.transform.gameObject);
                            }

                            switch (go.name)
                            {
                                case "TopMiddle":
                                    var list = new List<GameObject>();
                                    list.Add(item.GetCustomReference("TopLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list);
                                    break;
                                case "TopRight":
                                    var list1 = new List<GameObject>();
                                    list1.Add(item.GetCustomReference("TopMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list1);
                                    break;
                                case "TopLeft":
                                    var list2 = new List<GameObject>();
                                    list2.Add(item.GetCustomReference("CenterLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list2);
                                    break;
                                case "CenterRight":
                                    var list3 = new List<GameObject>();
                                    list3.Add(item.GetCustomReference("TopRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list3);
                                    break;
                                case "CenterLeft":
                                    var list4 = new List<GameObject>();
                                    list4.Add(item.GetCustomReference("BottomLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list4);
                                    break;
                                case "BottomMiddle":
                                    var list5 = new List<GameObject>();
                                    list5.Add(item.GetCustomReference("BottomRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list5);
                                    break;
                                case "BottomLeft":
                                    var list6 = new List<GameObject>();
                                    list6.Add(item.GetCustomReference("BottomMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list6);
                                    break;
                                case "BottomRight":
                                    var list7 = new List<GameObject>();
                                    list7.Add(item.GetCustomReference("CenterRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list7);
                                    break;
                                case "Ring1TopRight":
                                    var list8 = new List<GameObject>();
                                    list8.Add(item.GetCustomReference("Ring1TopMiddle").transform.gameObject);
                                    list8.Add(item.GetCustomReference("Ring2TopRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list8);
                                    break;
                                case "Ring2TopRight":
                                    var list9 = new List<GameObject>();
                                    list9.Add(item.GetCustomReference("Ring2TopMiddle").transform.gameObject);
                                    list9.Add(item.GetCustomReference("TopRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list9);
                                    break;
                                case "Ring1TopLeft":
                                    var list10 = new List<GameObject>();
                                    list10.Add(item.GetCustomReference("Ring1CenterLeft").transform.gameObject);
                                    list10.Add(item.GetCustomReference("Ring2TopLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list10);
                                    break;
                                case "Ring2TopLeft":
                                    var list11 = new List<GameObject>();
                                    list11.Add(item.GetCustomReference("Ring2CenterLeft").transform.gameObject);
                                    list11.Add(item.GetCustomReference("TopLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list11);
                                    break;
                                case "Ring1TopMiddle":
                                    var list12 = new List<GameObject>();
                                    list12.Add(item.GetCustomReference("Ring1TopLeft").transform.gameObject);
                                    list12.Add(item.GetCustomReference("Ring2TopMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list12);
                                    break;
                                case "Ring2TopMiddle":
                                    var list13 = new List<GameObject>();
                                    list13.Add(item.GetCustomReference("Ring2TopLeft").transform.gameObject);
                                    list13.Add(item.GetCustomReference("TopMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list13);
                                    break;
                                case "Ring1BottomRight":
                                    var list14 = new List<GameObject>();
                                    list14.Add(item.GetCustomReference("Ring1CenterRight").transform.gameObject);
                                    list14.Add(item.GetCustomReference("Ring2BottomRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list14);
                                    break;
                                case "Ring1BottomLeft":
                                    var list15 = new List<GameObject>();
                                    list15.Add(item.GetCustomReference("Ring1BottomMiddle").transform.gameObject);
                                    list15.Add(item.GetCustomReference("Ring2BottomLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list15);
                                    break;
                                case "Ring2BottomRight":
                                    var list16 = new List<GameObject>();
                                    list16.Add(item.GetCustomReference("Ring2CenterRight").transform.gameObject);
                                    list16.Add(item.GetCustomReference("BottomRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list16);
                                    break;
                                case "Ring2BottomLeft":
                                    var list17 = new List<GameObject>();
                                    list17.Add(item.GetCustomReference("Ring2BottomMiddle").transform.gameObject);
                                    list17.Add(item.GetCustomReference("BottomLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list17);
                                    break;
                                case "Ring1BottomMiddle":
                                    var list18 = new List<GameObject>();
                                    list18.Add(item.GetCustomReference("Ring1BottomRight").transform.gameObject);
                                    list18.Add(item.GetCustomReference("Ring2BottomMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list18);
                                    break;
                                case "Ring2BottomMiddle":
                                    var list19 = new List<GameObject>();
                                    list19.Add(item.GetCustomReference("Ring2BottomRight").transform.gameObject);
                                    list19.Add(item.GetCustomReference("BottomMiddle").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list19);
                                    break;
                                case "Ring1CenterRight":
                                    var list20 = new List<GameObject>();
                                    list20.Add(item.GetCustomReference("Ring1TopRight").transform.gameObject);
                                    list20.Add(item.GetCustomReference("Ring2CenterRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list20);
                                    break;
                                case "Ring2CenterRight":
                                    var list21 = new List<GameObject>();
                                    list21.Add(item.GetCustomReference("Ring2TopRight").transform.gameObject);
                                    list21.Add(item.GetCustomReference("CenterRight").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list21);
                                    break;
                                case "Ring1CenterLeft":
                                    var list22 = new List<GameObject>();
                                    list22.Add(item.GetCustomReference("Ring1BottomLeft").transform.gameObject);
                                    list22.Add(item.GetCustomReference("Ring2CenterLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list22);
                                    break;
                                case "Ring2CenterLeft":
                                    var list23 = new List<GameObject>();
                                    list23.Add(item.GetCustomReference("Ring2BottomLeft").transform.gameObject);
                                    list23.Add(item.GetCustomReference("CenterLeft").transform.gameObject);
                                    go.transform.gameObject.AddComponent<WebConnector>().Setup(list23);
                                    break;
                                default:
                                    break;
                            }

                        }

                        center.gameObject.AddComponent<WebConnector>().Setup(nodes, collisioninstance.contactNormal);
                    }, "WebCreatureHandler");
            }
        }

        IEnumerator RefreshCoroutine()
        {
            webCompletedRefreshed = false;
            yield return new WaitForSeconds(5f);
            if (percentageWebbed < 1f)
            {
                webCompletedRefreshed = true;
            }
            else yield return RefreshCoroutine();
        }

        void WebbedCompleted(bool maxWebbed = false)
        {
            if (!creature) return;
            //creature.brain.instance.Stop();
            var position = creature.ragdoll.targetPart.transform.position +
                           (creature.ragdoll.targetPart.transform.forward * 0.1f);

            rightJoint = creature.handRight.gameObject.AddComponent<ConfigurableJoint>();
            rightJoint.autoConfigureConnectedAnchor = false;
            rightJoint.connectedBody = creature.ragdoll.targetPart.physicBody.rigidBody;

// Proper Anchor Configuration
            Vector3 worldAnchor = position;
            rightJoint.anchor = creature.handRight.transform.InverseTransformPoint(worldAnchor);
            rightJoint.connectedAnchor =
                creature.ragdoll.targetPart.physicBody.rigidBody.transform.InverseTransformPoint(worldAnchor);

// Set the Target Position Relative to the Connected Anchor
            rightJoint.targetPosition = rightJoint.connectedAnchor - rightJoint.anchor;

// Motion Constraints
            rightJoint.xMotion = ConfigurableJointMotion.Limited;
            rightJoint.yMotion = ConfigurableJointMotion.Limited;
            rightJoint.zMotion = ConfigurableJointMotion.Limited;

            rightJoint.angularXMotion = ConfigurableJointMotion.Limited;
            rightJoint.angularYMotion = ConfigurableJointMotion.Limited;
            rightJoint.angularZMotion = ConfigurableJointMotion.Limited;

// Linear Limit (Allow More Freedom)
            SoftJointLimit linearLimit = new SoftJointLimit();
            linearLimit.limit = 0.5f; // Increase the limit for movement
            rightJoint.linearLimit = linearLimit;

// Drive Settings (Stronger Force)
            JointDrive drive = new JointDrive
            {
                positionSpring = 10000, // Strong force to pull the hand
                positionDamper = 500, // Damping to prevent jitter
                maximumForce = 5000 // High enough to allow movement
            };

            rightJoint.xDrive = drive;
            rightJoint.yDrive = drive;
            rightJoint.zDrive = drive;
            rightJoint.angularXDrive = drive;
            rightJoint.angularYZDrive = drive;

// Rigidbody Mass Balance
            Rigidbody rb = creature.handRight.GetComponent<Rigidbody>();
            rb.mass = 10f;
            creature.ragdoll.targetPart.physicBody.rigidBody.mass = 10f;

// Solver Iterations for Stability
            rb.solverIterations = 30;
            rb.solverVelocityIterations = 15;

// Break Force (High)
            rightJoint.breakForce = 10000;
            rightJoint.breakTorque = 10000;

            //LEFT JOINT
            leftJoint = creature.handLeft.gameObject.AddComponent<ConfigurableJoint>();
            leftJoint.autoConfigureConnectedAnchor = false;
            leftJoint.connectedBody = creature.ragdoll.targetPart.physicBody.rigidBody;

            leftJoint.anchor = creature.handLeft.transform.InverseTransformPoint(worldAnchor);
            leftJoint.connectedAnchor =
                creature.ragdoll.targetPart.physicBody.rigidBody.transform.InverseTransformPoint(worldAnchor);

// Set the Target Position Relative to the Connected Anchor
            leftJoint.targetPosition = leftJoint.connectedAnchor - leftJoint.anchor;

// Motion Constraints
            leftJoint.xMotion = ConfigurableJointMotion.Limited;
            leftJoint.yMotion = ConfigurableJointMotion.Limited;
            leftJoint.zMotion = ConfigurableJointMotion.Limited;

            leftJoint.angularXMotion = ConfigurableJointMotion.Limited;
            leftJoint.angularYMotion = ConfigurableJointMotion.Limited;
            leftJoint.angularZMotion = ConfigurableJointMotion.Limited;

// Linear Limit (Allow More Freedom)
            leftJoint.linearLimit = linearLimit;

// Drive Settings (Stronger Force)
            JointDrive driveLeft = new JointDrive
            {
                positionSpring = 10000, // Strong force to pull the hand
                positionDamper = 500, // Damping to prevent jitter
                maximumForce = 5000 // High enough to allow movement
            };

            leftJoint.xDrive = driveLeft;
            leftJoint.yDrive = driveLeft;
            leftJoint.zDrive = driveLeft;
            leftJoint.angularXDrive = driveLeft;
            leftJoint.angularYZDrive = driveLeft;

// Rigidbody Mass Balance
            Rigidbody rbLeft = creature.handLeft.GetComponent<Rigidbody>();
            rbLeft.mass = 10f;

// Solver Iterations for Stability
            rbLeft.solverIterations = 30;
            rbLeft.solverVelocityIterations = 15;

// Break Force (High)
            leftJoint.breakForce = 10000;
            leftJoint.breakTorque = 10000;

            if (creature.handLeft.grabbedHandle)
            {
                creature.handLeft.UnGrab(false);
            }

            if (creature.handRight.grabbedHandle)
            {
                creature.handLeft.UnGrab(false);
            }

            if(!maxWebbed) GameManager.local.StartCoroutine(RefreshCoroutine());
        }

        private void OnDestroy()
        {
            Destroy(rightJoint);
            Destroy(leftJoint);
            ResetCreatureMaterial();
        }

        public void ResetCreatureMaterial()
        {
            int i = 0;
            foreach (var renderer in creature.renderers)
            {
                if(!originalCreatureMaterial.IsNullOrEmpty()) renderer.renderer.materials = originalCreatureMaterial.Dequeue();
            }
        }


        private float percentageWebbed = 0f;

        public void MaxWebbed()
        {
            if (!materialChanged)
            {
                if (creature && creature.renderers != null)
                {
                    foreach (var renderer in creature.renderers)
                    {
                        Material webMatSkin = ManageAutoAlignment.local.materialWeb.DeepCopyByExpressionTree();
                        Material webMatElevated =
                            ManageAutoAlignment.local.materiaLWebElevated.DeepCopyByExpressionTree();
                        originalCreatureMaterial.Enqueue(renderer.renderer.materials);
                        Material[] myMaterials = renderer.renderer.materials;
                        Material[] matDefGood = new Material[myMaterials.Length + 2];

                        matDefGood[0] = myMaterials[0];
                        matDefGood[1] = webMatSkin;
                        matDefGood[2] = webMatElevated;

                        renderer.renderer.materials = matDefGood;
                    }

                    materialChanged = true;
                }
            }
            else
            {
                ResetCreatureMaterial();
                if (creature && creature.renderers != null)
                {
                    foreach (var renderer in creature.renderers)
                    {
                        Material webMatSkin = ManageAutoAlignment.local.materialWeb.DeepCopyByExpressionTree();
                        Material webMatElevated =
                            ManageAutoAlignment.local.materiaLWebElevated.DeepCopyByExpressionTree();
                        originalCreatureMaterial.Enqueue(renderer.renderer.materials);
                        Material[] myMaterials = renderer.renderer.materials;
                        Material[] matDefGood = new Material[myMaterials.Length + 2];

                        matDefGood[0] = myMaterials[0];
                        matDefGood[1] = webMatSkin;
                        matDefGood[2] = webMatElevated;

                        renderer.renderer.materials = matDefGood;
                    }

                    materialChanged = true;
                }
                
            }

            if (creature && creature.renderers != null)
            {
                foreach (var renderer in creature.renderers)
                {
                    foreach (var mat in renderer.renderer.materials)
                    {
                        var multiplier = 1f;
                        if (mat.HasFloat("_fresnelPower"))
                        {
                            if (mat.HasFloat("_overlap") && mat.GetFloat("_overlap") == 1f)
                            {
                                multiplier = 1f;
                            }

                            mat.SetFloat("_fresnelPower", 100f);
                        }
                    }
                }
            }

            if (creature && !slowedCreature)
            {
                creature.locomotion?.SetSpeedModifier(this, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
                creature.animator.speed = 0.5f;
                slowedCreature = true;
            }

            if (!allowContactEvent)
            {
                allowContactEvent = true;
                WebbedCompleted(true);
            }
        }
        
        private void Update()
        {

            if (!runUpdate) return;
            if (webCompletedRefreshed)
            {
                if (decayWebbing)
                {
                    decayWebbingRate += (1 / Time.deltaTime);
                    hitNumber -= decayWebbingRate;

                    Mathf.Clamp(hitNumber, 0, maxWebbingCount / valueOfHit);
                }

                percentageWebbed = Mathf.Clamp((hitNumber * valueOfHit) / maxWebbingCount, 0, 1f);

                if (!slowedCreature && percentageWebbed >= 0.5f)
                {
                    creature.locomotion.SetSpeedModifier(this, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f);
                    creature.animator.speed = 0.5f;
                    slowedCreature = true;
                }

                if (slowedCreature && percentageWebbed <= 0.5f)
                {
                    creature.locomotion.SetSpeedModifier(this);
                    creature.animator.speed = 1f;
                    slowedCreature = false;
                }

                if (!materialChanged)
                {
                    if (creature.renderers != null)
                    {
                        foreach (var renderer in creature.renderers)
                        {
                            Material webMatSkin = ManageAutoAlignment.local.materialWeb.DeepCopyByExpressionTree();
                            Material webMatElevated =
                                ManageAutoAlignment.local.materiaLWebElevated.DeepCopyByExpressionTree();
                            originalCreatureMaterial.Enqueue(renderer.renderer.materials);
                            Material[] myMaterials = renderer.renderer.materials;
                            Material[] matDefGood = new Material[myMaterials.Length + 2];

                            matDefGood[0] = myMaterials[0];
                            matDefGood[1] = webMatSkin;
                            matDefGood[2] = webMatElevated;

                            renderer.renderer.materials = matDefGood;
                        }

                        materialChanged = true;
                    }
                }
                else
                {
                    if (percentageWebbed < 1f)
                    {
                        foreach (var renderer in creature.renderers)
                        {
                            foreach (var mat in renderer.renderer.materials)
                            {
                                var multiplier = 1f;
                                if (mat.HasFloat("_fresnelPower"))
                                {
                                    if (mat.HasFloat("_overlap") && mat.GetFloat("_overlap") == 1f)
                                    {
                                        multiplier = 1f;
                                    }

                                    mat.SetFloat("_fresnelPower", percentageWebbed * multiplier);
                                }
                            }
                        }
                    }
                }

                if (percentageWebbed <= 0)
                {
                    percentageWebbed = 0;
                    ResetCreatureMaterial();
                }

                allowContactEvent = percentageWebbed >= 1f;
                if (allowContactEvent)
                {
                    WebbedCompleted();
                }
            }
        }

    }
}