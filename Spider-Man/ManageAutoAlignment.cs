using System;
using ThunderRoad;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spider_Man
{
    public class ManageAutoAlignment : ThunderScript
    {
        public static ManageAutoAlignment local;
        public TriggerColliderMono left;
        public TriggerColliderMono right;


        private float elapsedTimeSwingStart = 0f;
        private float elapsedTimeSwingEnd = 0f;
        private bool firstDirectionSet = false;
        private Vector3 firstDirection;
        private Vector3 lastDirectionAfterSwingEnd;
        private bool lastDirectionSet;
        
        private Vector3 autoAlignDefault;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            if (local == null)
            {
                local = this;
            }

            Player.onSpawn += OnSpawn;
        }

        private void OnSpawn(Player player)
        {
            player.onCreaturePossess += Possess;
        }

        private void Possess(Creature obj)
        {
            if (!left)
            {
                var handLeft = obj.handLeft;
                GameObject colliderObject = new GameObject();
                var sphereCollider = colliderObject.AddComponent<SphereCollider>();
                sphereCollider.center = new Vector3(0, 0, 0);
                sphereCollider.radius = 0.1f;
                sphereCollider.isTrigger = true;
                sphereCollider.gameObject.layer = obj.data.overlapMask;
                var toAdd = Object.Instantiate(colliderObject, handLeft.transform);
                toAdd.transform.position = handLeft.transform.position;
                toAdd.transform.parent = handLeft.transform;
                toAdd.AddComponent<TriggerColliderMono>().ActivateHand(handLeft);
                left = toAdd.GetComponent<TriggerColliderMono>();
                Player.currentCreature.handLeft.playerHand.controlHand.OnButtonPressEvent += PressedEvent;
            }

            if (!right)
            {
                var handRight = obj.handRight;
                GameObject colliderObject = new GameObject();
                var sphereCollider = colliderObject.AddComponent<SphereCollider>();
                sphereCollider.center = new Vector3(0, 0, 0);
                sphereCollider.radius = 0.1f;
                sphereCollider.isTrigger = true;
                sphereCollider.gameObject.layer = obj.data.overlapMask;
                var toAdd = Object.Instantiate(colliderObject, handRight.transform);
                toAdd.transform.position = handRight.transform.position;
                toAdd.transform.parent = handRight.transform;
                toAdd.AddComponent<TriggerColliderMono>().ActivateHand(handRight);
                right = toAdd.GetComponent<TriggerColliderMono>();
                Player.currentCreature.handRight.playerHand.controlHand.OnButtonPressEvent += PressedEvent;
            }
            
            if (!right.otherHandMono)
            {
                right.otherHandMono = left;
            }

            if (!left.otherHandMono)
            {
                left.otherHandMono = right;
            }
        }

        private bool alignDive;
        private void PressedEvent(PlayerControl.Hand.Button button, bool pressed)
        {
            if (button == PlayerControl.Hand.Button.AlternateUse && pressed && (!right.swinging || !left.swinging))
            {
                alignDive = true;
            }
            else if (button == PlayerControl.Hand.Button.AlternateUse && !pressed)
            {
                alignDive = false;
            }
        }

        public override void ScriptFixedUpdate()
        {
            base.ScriptFixedUpdate();
            Update();
        }
        float speed = 10f;
        Vector3 direction = Vector3.up;
        Vector3 targetDirection = Vector3.up;
        private bool alignPlayerWhileClimbing = false;
        private bool vectorSet;
        private Vector3 perpendicularVector;
        private Vector3 previousNormal;
        private Vector3 smoothedNormal;
        private void Update()
        {
            if (ModOptions.alignPlayerWhileSwinging && right && left)
            {
                if (Player.local.autoAlign) Player.local.autoAlign = false;
                if (!Player.local.locomotion.isGrounded)
                {
                    if (left.swinging && right.swinging)
                    {
                        targetDirection = (right.worldAnchorPoint - left.worldAnchorPoint).normalized;
                        float distanceHalved = Vector3.Distance(right.worldAnchorPoint, left.worldAnchorPoint) / 2f;
                        var position = left.worldAnchorPoint + (direction * distanceHalved);
                        targetDirection = (position - Player.currentCreature.ragdoll.headPart.transform.position)
                            .normalized;
                    }
                    else if (left.swinging && !right.swinging)
                        targetDirection = (left.worldAnchorPoint - left.swingingHandle.transform.position).normalized;
                    else if (right.swinging && !left.swinging)
                        targetDirection = (right.worldAnchorPoint - right.swingingHandle.transform.position).normalized;
                    else
                    {
                        targetDirection = alignDive ? Vector3.down : Vector3.up;
                    }
                    if (Vector3.Dot(Player.local.transform.forward, targetDirection) < 0 && alignDive)
                    {
                        // Flip the targetDirection if it points backward
                        targetDirection = Vector3.Reflect(targetDirection, Player.local.transform.forward);
                    }

                    if (!vectorSet)
                    {
                        direction = Vector3.Slerp(direction, targetDirection, ModOptions.alignmentSpeed * Time.deltaTime);
                        Quaternion rotation = Quaternion.FromToRotation(Player.local.transform.up, direction);
                        if (Quaternion.Angle(Player.local.transform.rotation,
                                rotation * Player.local.transform.rotation) <
                            0.1f) return;
                        Player.local.transform.rotation = Quaternion.Slerp(Player.local.transform.rotation,
                            rotation * Player.local.transform.rotation, speed * Time.deltaTime);
                    }
                }
            }

            if (alignPlayerWhileClimbing && right && left)
            {
                if (right.swinging || left.swinging) return;
                var playerClimbLeft = Player.currentCreature.handLeft.climb;
                var playerClimbRight = Player.currentCreature.handRight.climb;
                var leftCheck = (!playerClimbLeft.gripRagdollPart &&
                                 !playerClimbLeft.gripItem);
                var rightCheck = (!playerClimbRight.gripRagdollPart &&
                                  !playerClimbRight.gripItem);
                if (playerClimbLeft.isGripping)
                {
                    if (leftCheck)
                    {
                        if (playerClimbLeft.gripCollider)
                        {
                            var collider = playerClimbLeft.gripCollider;
                            collider.gameObject.layer = playerClimbRight.ragdollHand.creature.data.groundMask;
                            RaycastHit hit;
                            if (Physics.Raycast(playerClimbLeft.ragdollHand.palmCollider.transform.position,
                                    playerClimbLeft.ragdollHand.PalmDir, out hit, 2f, Physics.DefaultRaycastLayers,
                                    QueryTriggerInteraction.Ignore) && !vectorSet)
                            {
                                var normal = hit.normal;
                                smoothedNormal = Vector3.Slerp(previousNormal, normal, Time.deltaTime * 10f);
                                float blendFactor = Mathf.Clamp01((Mathf.Abs(normal.y) - 0.3f) / 0.7f);
                                Vector3 referenceVector = Vector3.ProjectOnPlane(playerClimbLeft.ragdollHand.transform.right, smoothedNormal).normalized;
                                perpendicularVector = Vector3.Cross(smoothedNormal, referenceVector).normalized;
                                previousNormal = smoothedNormal;
                                vectorSet = true;
                            }
                        }
                    }   
                }
                if(playerClimbRight.isGripping && !vectorSet)
                {
                    if (rightCheck)
                    {
                        if (playerClimbRight.gripCollider)
                        {
                            var collider = playerClimbRight.gripCollider;
                            collider.gameObject.layer = playerClimbRight.ragdollHand.creature.data.groundMask;
                            RaycastHit hit;
                            if (Physics.Raycast(playerClimbRight.ragdollHand.palmCollider.transform.position,
                                    playerClimbRight.ragdollHand.PalmDir, out hit, 2f, Physics.DefaultRaycastLayers,
                                    QueryTriggerInteraction.Ignore) && !vectorSet)
                            {
                                var normal = hit.normal;
                                smoothedNormal = Vector3.Slerp(previousNormal, normal, Time.deltaTime * 10f);
                                float blendFactor = Mathf.Clamp01((Mathf.Abs(normal.y) - 0.3f) / 0.7f);
                                Vector3 referenceVector = Vector3.ProjectOnPlane(playerClimbRight.ragdollHand.transform.right, smoothedNormal).normalized;
                                perpendicularVector = Vector3.Cross(smoothedNormal, referenceVector).normalized;
                                previousNormal = smoothedNormal;
                                vectorSet = true;
                            }
                        }
                    }
                }

                if (!playerClimbLeft.isGripping && !playerClimbRight.isGripping)
                {
                    vectorSet = false;
                }

                if (vectorSet)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(perpendicularVector, smoothedNormal);
                    float angleDifference = Quaternion.Angle(Player.local.transform.rotation, targetRotation);

                    if (angleDifference > 10f)
                    {
                        Player.local.transform.rotation = Quaternion.Slerp(
                            Player.local.transform.rotation,
                            targetRotation,
                            speed * Time.deltaTime
                        );
                    }
                }
            }
        }
    }
}