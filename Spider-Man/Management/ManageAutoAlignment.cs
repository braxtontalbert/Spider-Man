using Spider_Man.Webshooter;
using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Spider_Man.Management
{
    public class ManageAutoAlignment : ThunderScript
    {
        public static ManageAutoAlignment local;
        public WsRagdollHand left;
        public WsRagdollHand right;

        public Material materialWeb;
        public Material materiaLWebElevated;

        private float elapsedTimeSwingStart = 0f;
        private float elapsedTimeSwingEnd = 0f;
        private bool firstDirectionSet = false;
        private Vector3 firstDirection;
        private Vector3 lastDirectionAfterSwingEnd;
        private bool lastDirectionSet;
        private Vector3 autoAlignDefault;

        public GameObject slamEffect;

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            if (local == null)
            {
                local = this;
            }
            
            Catalog.LoadAssetAsync<Material>("WebbedUpMatSkin", material =>
            {
                this.materialWeb = material;
            }, "WebMaterialHandler");
            Catalog.LoadAssetAsync<Material>("WebbedUpMatElevated", material =>
            {
                this.materiaLWebElevated = material;
            }, "WebElevatedMaterialHandler");
            Catalog.LoadAssetAsync<GameObject>("groundSlam", o =>
            {
                slamEffect = o;
            }, "SlamHandler");
            Player.onSpawn += OnSpawn;
            EventManager.onCreatureDespawn += CreatureDespawn;
        }

        public override void ScriptUnload()
        {
            base.ScriptUnload();
            EventManager.onCreatureDespawn -= CreatureDespawn;
            Player.local.locomotion.OnGroundEvent -= OnLand;
        }

        private void CreatureDespawn(Creature creature1, EventTime eventtime)
        {
            if (eventtime == EventTime.OnStart && creature1.gameObject.GetComponent<CreatureWebTracker>())
            {
                var webtrackerRef = creature1.gameObject.GetComponent<CreatureWebTracker>();
                webtrackerRef.ResetCreatureMaterial();
                creature1.locomotion.SetSpeedModifier(webtrackerRef);
                creature1.animator.speed = 1f;
                Object.Destroy(webtrackerRef); 
            }
        }
        Collider[] array = new Collider[50];
        private const float SlamRadius = 10f;
        private const float ExplosionForce = 2000f;
        private const float UpwardModifier = 10f;
        private const int SlamMask = (1 << 3) | (1 << 9); // Only include layers 3 and 9
        private void OnLand(Locomotion locomotion, Vector3 groundPoint, Vector3 velocity, Collider groundCollider)
        {
            if (velocity.magnitude > 30f)
            {
                GameObject go = GameObject.Instantiate(ManageAutoAlignment.local.slamEffect);
                go.transform.position = groundPoint;
                go.GetComponent<VisualEffect>().Play();
                go.GetComponent<AudioSource>().Play();

                
                int hitCount = Physics.OverlapSphereNonAlloc(groundPoint, SlamRadius, array, SlamMask);
                
                for (int i = 0; i < hitCount; i++)
                {
                    var collider = array[i];
                    if (collider == null || !collider.attachedRigidbody) continue;
                    Rigidbody refRB = collider.GetComponentInParent<Rigidbody>();
                    if (refRB == null) continue;
                    var hitCreature = collider.GetComponentInParent<Creature>();
                    var hitItem =  collider.GetComponentInParent<Item>();
                    float mass = collider.attachedRigidbody.mass;

                    if (hitCreature != null && !hitCreature.isPlayer)
                    {
                        hitCreature.ragdoll.SetState(Ragdoll.State.Destabilized, true);

                        var rb = collider.attachedRigidbody;
                        if (rb != null && rb.isKinematic) rb.isKinematic = false;

                        rb.AddExplosionForce(ExplosionForce, groundPoint, SlamRadius, UpwardModifier, ForceMode.Impulse);
                    }
                    else if (hitItem != null)
                    {
                        collider?.attachedRigidbody?.AddExplosionForce(mass * ExplosionForce, groundPoint, SlamRadius, UpwardModifier);
                    }
                }
            }
        }

        private void OnSpawn(Player player)
        {
            player.onCreaturePossess += Possess;
        }

        private void Possess(Creature obj)
        {
            Player.local.locomotion.OnGroundEvent += OnLand;
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
                toAdd.AddComponent<WsRagdollHand>().ActivateHand(handLeft);
                left = toAdd.GetComponent<WsRagdollHand>();
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
                toAdd.AddComponent<WsRagdollHand>().ActivateHand(handRight);
                right = toAdd.GetComponent<WsRagdollHand>();
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
        private bool startWallRun;
        private Vector3 gravityNormal;
        private void PressedEvent(PlayerControl.Hand.Button button, bool pressed)
        {
            if (button == PlayerControl.Hand.Button.AlternateUse && pressed && (!right.swing.IsSwinging || !left.swing.IsSwinging) && allowWallRun)
            {
                if (Player.local.handRight.ragdollHand.climb.isGripping)
                {
                    if (Physics.Raycast(Player.local.handRight.ragdollHand.caster.magicSource.transform.position,
                            Player.local.handRight.ragdollHand.caster.magicSource.transform.forward, out RaycastHit hit, .5f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        var normal = hit.normal;
                        Player.local.locomotion.customGravity = (-normal * -9.81f).magnitude;
                        targetDirection = -hit.normal;
                        startWallRun = true;
                    }
                }
                else if (Player.local.handLeft.ragdollHand.climb.isGripping)
                {
                    if (Physics.Raycast(Player.local.handLeft.ragdollHand.caster.magicSource.transform.position,
                            Player.local.handLeft.ragdollHand.caster.magicSource.transform.forward, out RaycastHit hit, .5f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        var normal = hit.normal;
                        Player.local.locomotion.customGravity = (-normal * -9.81f).magnitude;
                        targetDirection = -hit.normal;
                        startWallRun = true;
                    }
                    
                }
            }
            /*else if (button == PlayerControl.Hand.Button.AlternateUse && !pressed)
            {
                startWallRun = false;
            }*/
        }

        public override void ScriptFixedUpdate()
        {
            base.ScriptFixedUpdate();
            Update();
        }
        float speed = 10f;
        Vector3 direction = Vector3.up;
        public Vector3 targetDirection = Vector3.up;
        private bool alignPlayerWhileClimbing = false;
        private bool allowWallRun;
        private bool vectorSet;
        private Vector3 perpendicularVector;
        private Vector3 previousNormal;
        private Vector3 smoothedNormal;
        
        private Vector3 lastRotationAxis = Vector3.zero;
        private float lastAngleToUp = 0f;
        
        private Vector3 spinAxis = Vector3.zero;
        private float currentSpinDirection = 1f; // 1 = clockwise, -1 = counter-clockwise
        private void Update()
        {
            if (ModOptions.alignPlayerWhileSwinging && right && left)
            {
                if (Player.local.autoAlign) Player.local.autoAlign = false;
                if (!Player.local.locomotion.isGrounded)
                {
                    
                    if (left.swing.IsSwinging && right.swing.IsSwinging && (left.swing.AlreadyAboveWhenSwinging || right.swing.AlreadyAboveWhenSwinging))
                    {
                        targetDirection = (right.swing.WorldAnchorPoint - left.swing.WorldAnchorPoint).normalized;
                        float distanceHalved = Vector3.Distance(right.swing.WorldAnchorPoint, left.swing.WorldAnchorPoint) / 2f;
                        var position = left.swing.WorldAnchorPoint + (direction * distanceHalved);
                        targetDirection = (position - Player.currentCreature.ragdoll.headPart.transform.position)
                            .normalized;
                    }
                    else if (left.swing.IsSwinging && !right.swing.IsSwinging && (left.swing.AlreadyAboveWhenSwinging || right.swing.AlreadyAboveWhenSwinging))
                        targetDirection = (left.swing.WorldAnchorPoint - left.swing.SwingingHandle.transform.position).normalized;
                    else if (right.swing.IsSwinging && !left.swing.IsSwinging && 
                             (left.swing.AlreadyAboveWhenSwinging || right.swing.AlreadyAboveWhenSwinging))
                        targetDirection = (right.swing.WorldAnchorPoint - right.swing.SwingingHandle.transform.position).normalized;
                    else if(allowWallRun)
                    {
                        targetDirection = alignDive ? Vector3.down : Vector3.up;
                    }
                    else
                    {
                        targetDirection = startWallRun ? targetDirection : Vector3.up;
                    }
                    if (Vector3.Dot(Player.local.transform.forward, targetDirection) < 0 && alignDive)
                    {
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

            /*if (alignPlayerWhileClimbing && right && left)
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
            }*/
        }
    }
}