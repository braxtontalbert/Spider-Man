using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spider_Man.Management;
using Spider_Man.Webshooter.Animation;
using ThunderRoad;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace Spider_Man.Webshooter.Gadgets
{
    public class SwingGadget : MonoBehaviour, IGadget
    {
        //Required Members
        public string Name { get; set; }
        public int PressCount { get; set; }
        public Coroutine Coroutine { get; set; }
        public Item Item { get; set; }
        public bool ItemAttached { get; set; }
        public RagdollHand Hand { get; set; }

        //Custom members
        public bool AttachedToCreature { get; set; }
        public bool AttachedToItem { get; set; }
        public Creature hitCreature { get; set; }
        public Item hitItem { get; set; }
        private RagdollHand OriginalHand { get; set; }
        private bool SpawningHandle { get; set; }
        private RaycastHit webHitSpot;
        private RaycastHit GlobalHit { get; set; }
        public Item SwingingHandle { get; set; }
        private Vector3 CurrentWebPosition { get; set; }
        private AudioSource SwingSFX { get; set; }
        public bool IsSwinging { get; set; }
        private LineRenderer LineRenderer { get; set; }
        private LineRenderer HandleRenderer { get; set; }
        private HandPoseData HandPoseData { get; set; }
        private bool WebConnectedToRb { get; set; }
        private SpringJoint MainJoint { get; set; }
        private Vector3 CurrentAnchorPoint { get; set; }
        public Vector3 WorldAnchorPoint { get; set; }
        private const string ThwipHandleString = "ThwipHandle";
        
        private Spring Spring { get; set; }
        private AnimationCurve AffectCurve { get; set; }

        public void Activate(Item item, RagdollHand hand, ref bool itemAttached)
        {
            Name = "Web Swing";
            SpawningHandle = false;
            Item = item;
            Hand = hand;
            OriginalHand = Hand;
            ItemAttached = itemAttached;
            SetupSpring();
            HandPoseData = Catalog.GetData<HandPoseData>(ThwipHandleString + Hand.side.ToString());
            
            InstantiateSFX();
            StartSwingCheck();
        }
        
        void SetupSpring()
        {
            AffectCurve = new AnimationCurve();
            AffectCurve.AddKey(0, 0);
            AffectCurve.AddKey(0.3f, 0.7f);
            AffectCurve.AddKey(1f, 0);
            Spring = new Spring();
            Spring.SetTarget(0);
        }

        void InstantiateSFX()
        {
            if (!SwingSFX)
            {
                Catalog.InstantiateAsync("swingaudio", Item.transform.position, Item.transform.rotation,
                    Item.transform,
                    sfx =>
                    {
                        SwingSFX = sfx.GetComponent<AudioSource>(); 
                    }, "SwingSFX");
            }
        }

        public IEnumerator WaitWindow(IGadget currentGadget)
        {
            currentGadget.DisallowItemGrab = true;
            yield return new WaitForSeconds(0.5f);
            currentGadget.DisallowItemGrab = false;
            PressCount = 0;
            Coroutine = null;
        }

        public bool DisallowItemGrab { get; set; }

        public bool AlreadyAboveWhenSwinging { get; set; }
        
        void StartSwingCheck()
        {
            Ray ray = new Ray(Item.flyDirRef.transform.position + (Item.flyDirRef.transform.forward * 0.4f), Item.flyDirRef.transform.forward);
            if (Physics.SphereCast(ray, 0.25f, out var hit,
                    80f, ((Physics.DefaultRaycastLayers) | (1 << 9) | (1 << 3) ), QueryTriggerInteraction.Ignore) && !SpawningHandle)
            {
                webHitSpot = hit;
                SpawningHandle = true;
                SpawnSwingHandle(hit);
            }
        }
        void SpawnSwingHandle(RaycastHit hit)
        {
            Catalog.GetData<ItemData>("InvisHandle").SpawnAsync(callback =>
            {
                callback.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
                callback.IgnoreItemCollision(Item);

                GlobalHit = hit;
                SetupSwing(callback, GlobalHit);
            });
        }
        
        void DisableColliders(Item handle)
        {
            foreach (var group in handle.colliderGroups)
            {
                foreach (var collider in group.colliders)
                {
                    collider.enabled = false;
                }
            }
        }
        
        void CreateOrValidateLineRenderer()
        {
            Spring.Reset();
            LineRenderer = new GameObject().AddComponent<LineRenderer>();
            LineRenderer.enabled = true;
            LineRenderer.textureMode = LineTextureMode.Tile;
            LineRenderer.widthMultiplier = 0.1f;
            LineRenderer.material = WebShooterPersistence.local.GetWebMaterial(ModOptions.webLineType);
            LineRenderer.positionCount = 0;

            HandleRenderer = new GameObject().AddComponent<LineRenderer>();
            HandleRenderer.enabled = true;
            HandleRenderer.textureMode = LineTextureMode.Tile;
            HandleRenderer.widthMultiplier = 0.1f;
            HandleRenderer.material = WebShooterPersistence.local.GetWebMaterial(ModOptions.webLineType);
            HandleRenderer.positionCount = 2;
        }
        
        void SetupSwing(Item callback, RaycastHit hit)
        {
            Item.GetMainHandle(Hand.side).SetTouchPersistent(false);
            Item.handles[0].gameObject.SetActive(false);
            
            CreateOrValidateLineRenderer();
            
            SwingingHandle = callback.GetComponent<Item>();
            DisableColliders(SwingingHandle);
            
            SwingingHandle.transform.position = Hand.transform.position;
            CurrentWebPosition = SwingingHandle.flyDirRef.transform.position;
            SwingingHandle.OnUngrabEvent -= UnGrabbedSwinging;
            SwingingHandle.OnUngrabEvent += UnGrabbedSwinging;
            if (!SetSpringJoint(hit)) return;

            Hand.UnGrab(false);
            Handle handle = SwingingHandle.mainHandleRight;
            Hand.Grab(handle, handle.GetDefaultOrientation(Hand.side), handle.GetDefaultAxisLocalPosition(),
                true);
            Hand.playerHand.controlHand.gripPressed = true;
            SwingSFX.pitch = Random.Range(0.9f, 1.1f);
            SwingSFX.Play();
            
            IsSwinging = true;
            SpawningHandle = false;
        }
        
        private void UnGrabbedSwinging(Handle handle, RagdollHand ragdollhand, bool throwing)
        {
            BreakSwing();
        }

        private GameObject anchorObj;
        private SpringJoint jnt;
        bool SetSpringJoint(RaycastHit hit)
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.GetComponentInParent<Item>() is Item item)
                {
                    AttachedToItem = true;
                    hitItem = item;

                    hitItem.OnBreakStart += OnBreakStart;
                    
                    WebConnectedToRb = true;
                    CurrentAnchorPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                    if (hit.point.y > Player.local.head.transform.position.y)
                    {
                        AlreadyAboveWhenSwinging = true;
                    }

                    anchorObj = new GameObject("WebAnchor");
                    anchorObj.transform.position = SwingingHandle.flyDirRef.transform.position;
                    Rigidbody anchorRb = anchorObj.AddComponent<Rigidbody>();
                    anchorRb.isKinematic = true;   // this makes it an immovable anchor

// Add the joint on the creature
                    jnt = hitItem.gameObject.AddComponent<SpringJoint>();
                    jnt.autoConfigureConnectedAnchor = false;

// Connect to the static anchor instead of the player
                    jnt.connectedBody = anchorRb;
                    jnt.anchor = hitItem.gameObject.transform.InverseTransformPoint(hit.point);
                    jnt.connectedAnchor = Vector3.zero;

// Spring parameters
                    float dist = Vector3.Distance(
                        hitItem.gameObject.transform.position,
                        anchorObj.transform.position
                    );
                    jnt.minDistance = 0f;
                    jnt.maxDistance = dist * 1f;
                    jnt.spring = 250f;
                    jnt.damper = 10f;
                    jnt.massScale = 1f;
                    jnt.breakForce = Mathf.Infinity;
                    jnt.breakTorque = Mathf.Infinity;
                }
                else if (hit.collider.gameObject.GetComponentInParent<Creature>() is Creature creature && !creature.isPlayer)
                {
                    AttachedToCreature = true;
                    hitCreature = creature;
                    
                    if (hitCreature.gameObject.GetComponent<CreatureWebTracker>() is CreatureWebTracker tracker)
                    {
                        if (tracker.stuckToWall)
                        {
                            return false;
                        }
                    }
                    
                    WebConnectedToRb = true;
                    CurrentAnchorPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                    if (hit.point.y > Player.local.head.transform.position.y)
                    {
                        AlreadyAboveWhenSwinging = true;
                    }

                    anchorObj = new GameObject("WebAnchor");
                    anchorObj.transform.position = SwingingHandle.flyDirRef.transform.position;
                    Rigidbody anchorRb = anchorObj.AddComponent<Rigidbody>();
                    anchorRb.isKinematic = true;   // this makes it an immovable anchor

// Add the joint on the creature
                    jnt = hitCreature.ragdoll.targetPart.gameObject.AddComponent<SpringJoint>();
                    jnt.autoConfigureConnectedAnchor = false;

// Connect to the static anchor instead of the player
                    jnt.connectedBody = anchorRb;
                    jnt.anchor = hitCreature.ragdoll.targetPart.transform.InverseTransformPoint(hit.point);
                    jnt.connectedAnchor = Vector3.zero;

// Spring parameters
                    float dist = Vector3.Distance(
                        hitCreature.ragdoll.targetPart.transform.position,
                        anchorObj.transform.position
                    );
                    jnt.minDistance = 0f;
                    jnt.maxDistance = dist * 1f;
                    jnt.spring = 4000f;
                    jnt.damper = 80f;
                    jnt.massScale = 1f;
                    jnt.breakForce = Mathf.Infinity;
                    jnt.breakTorque = Mathf.Infinity;
                }
                else {
                    WebConnectedToRb = true;
                    CurrentAnchorPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                    if (hit.point.y > Player.local.head.transform.position.y)
                    {
                        AlreadyAboveWhenSwinging = true;
                    }

                    MainJoint = SwingingHandle.gameObject.AddComponent<SpringJoint>();
                    MainJoint.autoConfigureConnectedAnchor = false;
                    MainJoint.connectedAnchor = hit.collider.transform.TransformPoint(CurrentAnchorPoint);

                    MainJoint.maxDistance =
                        Vector3.Distance(MainJoint.connectedAnchor, SwingingHandle.flyDirRef.transform.position) *
                        0.95f;
                    MainJoint.minDistance =
                        Vector3.Distance(MainJoint.connectedAnchor, SwingingHandle.flyDirRef.transform.position) * 0f;
                    MainJoint.spring = 130.5f;
                    MainJoint.damper = 20f;
                    MainJoint.massScale = Hand.ragdoll.totalMass / Hand.ragdolledMass;


                    MainJoint.breakForce = Mathf.Infinity;
                    MainJoint.breakTorque = Mathf.Infinity;
                    AttachedToCreature = false;
                    AttachedToItem = false;
                }
            }
            else
            {
                MainJoint = SwingingHandle.gameObject.AddComponent<SpringJoint>();
                MainJoint.autoConfigureConnectedAnchor = false;
                CurrentAnchorPoint = hit.point;
                
                if (hit.point.y > Player.local.head.transform.position.y)
                {
                    AlreadyAboveWhenSwinging = true;
                }
                
                MainJoint.connectedAnchor = WorldAnchorPoint;

                MainJoint.maxDistance =
                    Vector3.Distance(WorldAnchorPoint, SwingingHandle.flyDirRef.transform.position) * 0.95f;
                MainJoint.minDistance =
                    Vector3.Distance(WorldAnchorPoint, SwingingHandle.flyDirRef.transform.position) * 0f;
                MainJoint.spring = 130.5f;
                MainJoint.damper = 20f;
                MainJoint.massScale = Hand.ragdoll.totalMass / Hand.ragdolledMass;


                MainJoint.breakForce = Mathf.Infinity;
                MainJoint.breakTorque = Mathf.Infinity;
                AttachedToCreature = false;
                AttachedToItem = false;
            }

            return true;
        }

        private void OnBreakStart(Breakable breakable)
        {
            /*foreach (var handler in SwingingHandle.handlers)
            {
                handler.UnGrab(false);
            }*/
            this.Hand.UnGrab(false);
            if(this.Hand.otherHand.grabbedHandle == this.SwingingHandle.GetMainHandle(this.Hand.side))
                this.Hand.otherHand.UnGrab(false);
            if(breakable.IsBroken) BreakSwing();
            breakable.LinkedItem.OnBreakStart -= OnBreakStart;
        }

        void BreakSwing()
        {
            Hand.poser.SetTargetPose(HandPoseData, true, true, true, true, true);
            Item.handles[0].gameObject.SetActive(true);
            IsSwinging = false;
            LineRenderer.enabled = false;
            HandleRenderer.enabled = false;
            Hand = OriginalHand;
            WebConnectedToRb = false;
            if(MainJoint) Destroy(MainJoint);
            Destroy(LineRenderer.gameObject);
            Destroy(HandleRenderer.gameObject);
            Destroy(SwingingHandle.gameObject);
            if(anchorObj) Destroy(anchorObj);
            if (hitCreature)
            {
                Destroy(jnt);
                hitCreature = null;
            }
            if (hitItem)
            {
                Destroy(jnt);
                hitItem = null;
            }
            AlreadyAboveWhenSwinging = false;
            AttachedToItem = false;
            AttachedToCreature = false;
        }
        void DrawWeb()
        {
            if (LineRenderer.positionCount == 0)
            {
                Spring.SetVelocity(ModOptions.velocity);
                Spring.SetValue(0);
                LineRenderer.positionCount = ModOptions.quality + 1;
            }
            
            Spring.SetDamper(ModOptions.damper);
            Spring.SetStrength(Mathf.Clamp(ModOptions.strength * Vector3.Distance(WorldAnchorPoint, SwingingHandle.flyDirRef.transform.position),ModOptions.strength, ModOptions.strength * 5));
            Spring.Update(Time.deltaTime);

            var grapplePoint = WorldAnchorPoint;
            var grappleStartPoint = SwingingHandle.flyDirRef.transform.position;
            var up = Quaternion.LookRotation((grapplePoint - grappleStartPoint).normalized) * Vector3.up;
            if (Spring.Value > 0.01f)
            {
                CurrentWebPosition = Vector3.Lerp(CurrentWebPosition, grapplePoint, Time.deltaTime * 12f);
            }
            else
            {
                CurrentWebPosition = grapplePoint;
            }

            for (int i = 0; i < ModOptions.quality + 1; i++)
            {
                var delta = i / (float) ModOptions.quality;
                var offset = up * Mathf.Clamp(ModOptions.waveHeight * Vector3.Distance(WorldAnchorPoint, SwingingHandle.flyDirRef.transform.position),ModOptions.waveHeight, ModOptions.waveHeight * 2) * Mathf.Sin(delta * Mathf.Clamp(ModOptions.waveCount * Vector3.Distance(WorldAnchorPoint, SwingingHandle.flyDirRef.transform.position),ModOptions.waveCount, ModOptions.waveCount * 2) * Mathf.PI) * Spring.Value * AffectCurve.Evaluate(delta);
               
                float rotationAngle = delta * ModOptions.rotation;
                Quaternion rotation = Quaternion.AngleAxis(rotationAngle, (CurrentWebPosition - grappleStartPoint).normalized);
                offset = Vector3.Lerp(offset, rotation * offset, 1f); 

                LineRenderer.SetPosition(i, Vector3.Lerp(grappleStartPoint, CurrentWebPosition, delta) + offset);
            }
        }
        
        void CheckForWeblineIntersections()
        {
            RaycastHit[] hits;
            if (Physics.Raycast(LineRenderer.GetPosition(0), (LineRenderer.GetPosition(LineRenderer.positionCount - 1) - LineRenderer.GetPosition(0)).normalized, out RaycastHit hit,
                    Vector3.Distance(LineRenderer.GetPosition(0), LineRenderer.GetPosition(LineRenderer.positionCount - 1)),
                    ~((1 << 31) | (1 << 22)), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Distance(hit.point, LineRenderer.GetPosition(LineRenderer.positionCount - 1)) > 0.001f && !hit.collider.gameObject.GetComponentInParent<Creature>())
                {
                    if (hit.collider)
                    {
                        var distanceBetweenPoints = Vector3.Distance(hit.point, CurrentAnchorPoint);
                        var distanceToWebAnchor = Vector3.Distance(MainJoint.connectedAnchor,
                            SwingingHandle.flyDirRef.transform.position);
                        webHitSpot = hit;
                        WebConnectedToRb = true;
                        CurrentAnchorPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                        MainJoint.autoConfigureConnectedAnchor = false;
                        MainJoint.connectedAnchor = hit.collider.transform.TransformPoint(CurrentAnchorPoint);
                                
                        MainJoint.maxDistance =
                            distanceToWebAnchor * 0.95f;
                        MainJoint.minDistance =
                            distanceToWebAnchor * 0f;
                        MainJoint.spring = 130.5f;
                        MainJoint.damper = 20f;
                        MainJoint.massScale = Hand.ragdoll.totalMass / Hand.ragdolledMass;


                        MainJoint.breakForce = Mathf.Infinity;
                        MainJoint.breakTorque = Mathf.Infinity;
                    }
                    else
                    {
                        MainJoint.autoConfigureConnectedAnchor = false;
                        CurrentAnchorPoint = hit.point;
                        MainJoint.connectedAnchor = WorldAnchorPoint;

                        MainJoint.maxDistance =
                            Vector3.Distance(WorldAnchorPoint, SwingingHandle.flyDirRef.transform.position) * 0.95f;
                        MainJoint.minDistance =
                            Vector3.Distance(WorldAnchorPoint, SwingingHandle.flyDirRef.transform.position) * 0f;
                        MainJoint.spring = 130.5f;
                        MainJoint.damper = 20f;
                        MainJoint.massScale = Hand.ragdoll.totalMass / Hand.ragdolledMass;
                        
                        MainJoint.breakForce = Mathf.Infinity;
                        MainJoint.breakTorque = Mathf.Infinity;
                    }
                }
            }
        }
        

        bool AllowIntersections() => LineRenderer && ModOptions.realisticWeblines && LineRenderer.positionCount > 0;

        private bool ReelIn() => SwingingHandle && SwingingHandle.mainHandler &&
                                 SwingingHandle.mainHandler.Equals(Hand) &&
                                 Hand.playerHand.controlHand.usePressed;

        bool ReelOut() => SwingingHandle && SwingingHandle.mainHandler &&
                          SwingingHandle.mainHandler.Equals(Hand) &&
                          Hand.playerHand.controlHand.alternateUsePressed;
        
        bool HandIsPoser(HandPoseData data1, HandPoseData data2) => data1.Equals(data2);
        
        Vector3 previousVelocity = Vector3.zero;
        Vector3 currentVelocity = Vector3.zero;
        private void Update()
        {
            if (IsSwinging)
            {
                currentVelocity = Hand.Velocity();

                if (AllowIntersections()) CheckForWeblineIntersections();
                if (ReelIn())
                {
                    if (MainJoint && MainJoint.maxDistance > MainJoint.minDistance)
                        MainJoint.maxDistance -= ModOptions.reelInPower * Time.deltaTime;
                    else if(jnt && jnt.maxDistance > jnt.minDistance)
                        jnt.maxDistance -= ModOptions.reelInPower * Time.deltaTime;
                }

                if (ReelOut())
                {
                    if (MainJoint && MainJoint.maxDistance >= MainJoint.minDistance)
                        MainJoint.maxDistance += ModOptions.reelOutPower / 2f * Time.deltaTime;
                    else if(jnt && jnt.maxDistance >= jnt.minDistance)
                        jnt.maxDistance += ModOptions.reelInPower / 2f * Time.deltaTime;
                }

                bool isAccelerating = currentVelocity.magnitude > previousVelocity.magnitude;
                var targetDirection = SwingingHandle.transform.position - webHitSpot.point;

                if (isAccelerating)
                {
                    bool isAcceleratingStagger = currentVelocity.magnitude > previousVelocity.magnitude + 0.5f;
                    bool isAcceleratingCreature = currentVelocity.magnitude > previousVelocity.magnitude + 0.5f;
                    if (AttachedToCreature)
                    {
                        if(isAcceleratingStagger) hitCreature.ForceStagger(currentVelocity.normalized, BrainModuleHitReaction.PushBehaviour.Effect.StaggerMedium);
                        if (isAcceleratingCreature)
                        {
                            hitCreature.ragdoll.SetState(Ragdoll.State.Destabilized);
                            hitCreature.AddForce(
                                currentVelocity.normalized * 30f *
                                Mathf.Clamp(hitCreature.currentLocomotion.velocity.magnitude, 1f, Single.MaxValue),
                                ForceMode.Impulse);
                        }
                    }
                    else if (AttachedToItem)
                    {
                        if (isAcceleratingCreature)
                        {
                            if(hitItem.mainHandler != null) hitItem.mainHandler.UnGrab(false);
                            hitItem.AddForce(currentVelocity.normalized * 2f * hitItem.totalCombinedMass, ForceMode.Impulse);
                        }
                    }
                    else if(currentVelocity.magnitude > previousVelocity.magnitude + 0.3f)
                    {
                        // Check if velocity direction aligns with target direction
                        float alignment = Vector3.Dot(currentVelocity.normalized, -targetDirection.normalized);
                        if (alignment < -0.65f)
                        {
                            Hand.creature.AddForce(-targetDirection.normalized * ModOptions.webZipPower,
                                ForceMode.Impulse);
                        }
                    }
                }

                if (AttachedToCreature && hitCreature != null)
                {
                    if (hitCreature.gameObject.GetComponent<CreatureWebTracker>() is CreatureWebTracker tracker)
                    {
                        if (tracker.stuckToWall)
                        {
                            this.Hand.UnGrab(false);
                            BreakSwing();
                        }
                    }
                    if (anchorObj)
                    {
                        anchorObj.transform.position = SwingingHandle.flyDirRef.transform.position;
                    }
                    WorldAnchorPoint = hitCreature.ragdoll.targetPart.transform.TransformPoint(CurrentAnchorPoint);
                    var distance = Vector3.Distance(SwingingHandle.flyDirRef.transform.position, WorldAnchorPoint);
                    if (distance > jnt.maxDistance)
                    {
                        hitCreature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    }
                    
                }
                else if (AttachedToItem && hitItem != null)
                {
                    if (anchorObj)
                    {
                        anchorObj.transform.position = SwingingHandle.flyDirRef.transform.position;
                    }
                    WorldAnchorPoint = hitItem.gameObject.transform.TransformPoint(CurrentAnchorPoint);
                }
                else if (MainJoint && WebConnectedToRb)
                {
                    if (webHitSpot.collider)
                    {
                        WorldAnchorPoint = webHitSpot.collider.transform.TransformPoint(CurrentAnchorPoint);
                        MainJoint.connectedAnchor = WorldAnchorPoint;
                    }
                }
                
                DrawWeb();
                if (HandleRenderer)
                {
                    HandleRenderer.SetPosition(0,
                        SwingingHandle.flyDirRef.transform.position +
                        (-SwingingHandle.flyDirRef.transform.forward * 0.0706314f));
                    HandleRenderer.SetPosition(1, SwingingHandle.flyDirRef.transform.position);
                }

                previousVelocity = currentVelocity;
            }
            else
            {
                if (LineRenderer)
                {
                    Spring.Reset();
                    if (LineRenderer.positionCount > 0)
                    {
                        LineRenderer.positionCount = 0;
                    }
                }
                
                if (Hand && Hand.playerHand.isFist && ItemAttached)
                {
                    if(HandIsPoser(Hand.poser.targetHandPoseData, HandPoseData)) Hand.poser.ResetTargetPose();
                }
                else if (ItemAttached)
                {
                    if(!HandIsPoser(Hand.poser.targetHandPoseData, HandPoseData)) Hand.poser.SetTargetPose(HandPoseData, true, true, true, true, true);
                }
            }
        }
    }
}