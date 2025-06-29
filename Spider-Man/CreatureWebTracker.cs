using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man
{
    public class CreatureWebTracker : MonoBehaviour
    {
        private readonly int maxWebbingCount = 100;
        public float hitNumber;
        private int valueOfHit = 5;
        private bool decayWebbing = false;
        private float decayWebbingRate;
        private Creature creature;
        private bool allowContactEvent;
        private FixedJoint joint;
        private bool stuckToWall;
        private GameObject wallWeb;
        private Material[] originalMaterialArray;
        private bool materialChanged;
        private Material webMaterial;
        private Material webMaterialElevated;
        private bool slowedCreature;
        public List<Material[]> originalCreatureMaterial = new List<Material[]>();
        public ConfigurableJoint rightJoint;
        public ConfigurableJoint leftJoint;
        private bool webCompletedRefreshed = true;
        private void Start()
        {
            creature = GetComponent<Creature>();
            creature.ragdoll.OnContactStartEvent += CollisionStart;
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
        void WebbedCompleted()
        {
            //creature.brain.instance.Stop();
            var position = creature.ragdoll.targetPart.transform.position +
                           (creature.ragdoll.targetPart.transform.forward * 0.1f);

            rightJoint = creature.handRight.gameObject.AddComponent<ConfigurableJoint>();
            rightJoint.autoConfigureConnectedAnchor = false;
            rightJoint.connectedBody = creature.ragdoll.targetPart.physicBody.rigidBody;

// Proper Anchor Configuration
            Vector3 worldAnchor = position;
            rightJoint.anchor = creature.handRight.transform.InverseTransformPoint(worldAnchor);
            rightJoint.connectedAnchor = creature.ragdoll.targetPart.physicBody.rigidBody.transform.InverseTransformPoint(worldAnchor);

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
            linearLimit.limit = 0.5f;  // Increase the limit for movement
            rightJoint.linearLimit = linearLimit;

// Drive Settings (Stronger Force)
            JointDrive drive = new JointDrive
            {
                positionSpring = 10000,  // Strong force to pull the hand
                positionDamper = 500,    // Damping to prevent jitter
                maximumForce = 5000      // High enough to allow movement
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
            leftJoint.connectedAnchor = creature.ragdoll.targetPart.physicBody.rigidBody.transform.InverseTransformPoint(worldAnchor);

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
                positionSpring = 10000,  // Strong force to pull the hand
                positionDamper = 500,    // Damping to prevent jitter
                maximumForce = 5000      // High enough to allow movement
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
            GameManager.local.StartCoroutine(RefreshCoroutine());
        }

        private void OnDestroy()
        {
            Destroy(joint);
            Destroy(rightJoint);
            Destroy(leftJoint);
            Destroy(wallWeb);
            ResetCreatureMaterial();
        }

        public void ResetCreatureMaterial()
        {
            for (int i = 0; i < creature.renderers.Count; i++) {
                creature.renderers[i].renderer.materials = originalCreatureMaterial[i];
            }
        }


        private float percentageWebbed;
        private void Update()
        {
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
                    foreach (var renderer in creature.renderers)
                    {
                        Material webMatSkin = ManageAutoAlignment.local.materialWeb.DeepCopyByExpressionTree();
                        Material webMatElevated =
                            ManageAutoAlignment.local.materiaLWebElevated.DeepCopyByExpressionTree();
                        originalCreatureMaterial.Add(renderer.renderer.materials);
                        Material[] myMaterials = renderer.renderer.materials;
                        Material[] matDefGood = new Material[myMaterials.Length + 2];

                        matDefGood[0] = myMaterials[0];
                        matDefGood[1] = webMatSkin;
                        matDefGood[2] = webMatElevated;

                        renderer.renderer.materials = matDefGood;
                    }

                    materialChanged = true;
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

        private void CollisionStart(CollisionInstance collisioninstance, RagdollPart ragdollpart)
        {
            if (!collisioninstance.targetCollider.gameObject.GetComponentInParent<Item>() &&
                !collisioninstance.targetCollider.gameObject.GetComponentInParent<Creature>() && allowContactEvent && !stuckToWall)
            {
                joint = creature.ragdoll.targetPart.gameObject.AddComponent<FixedJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = collisioninstance.contactPoint;
                creature.locomotion.isGrounded = false;
                stuckToWall = true;
                Catalog.InstantiateAsync("Assets/Spider-Man/cobwebs/WebHitMeshCreature.prefab", collisioninstance.contactPoint, creature.ragdoll.targetPart.transform.rotation, null,
                    callback =>
                    {
                        if (callback != null)
                        {

                            wallWeb = callback;
                            Vector3 collisionNormal = collisioninstance.contactNormal;

                            Vector3 forwardVector = Vector3.Cross(Vector3.right, collisionNormal).normalized;

                            if (forwardVector == Vector3.zero)
                            {
                                forwardVector = Vector3.Cross(Vector3.up, collisionNormal).normalized;
                            }

                            Vector3 upVector = -collisionNormal; 
                            forwardVector = -forwardVector;
                            
                            Quaternion rotation = Quaternion.LookRotation(forwardVector, upVector);

                            callback.transform.rotation = rotation;

                            callback.transform.position = callback.transform.position + (callback.transform.up * 0.1f);
                            Debug.Log("GameObject instantiated and oriented correctly.");

                            var cloth = callback.gameObject.GetComponentInChildren<Cloth>();
                            List<CapsuleCollider> colliders = new List<CapsuleCollider>();
                            foreach (var part in creature.ragdoll.parts)
                            {
                                foreach (var collider in part.colliderGroup.colliders)
                                {
                                    if (collider.GetType() == typeof(CapsuleCollider))
                                    {
                                        colliders.Add((CapsuleCollider)collider);
                                    }
                                }
                            }

                            cloth.capsuleColliders = colliders.ToArray();
                        }
                        else
                        {
                            Debug.LogError("Prefab instantiation callback returned null!");
                        }
                    }, "WebCreatureHandler");
            }
        }
    }
}