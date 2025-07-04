﻿using System;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spider_Man
{
    public class TriggerColliderMono : MonoBehaviour
    {
        private RagdollHand originalHand;
        public RagdollHand hand;
        public Item item;
        public bool itemAttached;
        public bool swinging;
        private SpringJoint mainJoint;
        private FixedJoint interimJoint;
        private LineRenderer lr;
        private Material webtexture;
        public Item swingingHandle;
        private Item nextHandle;
        public bool activated = true;
        public TriggerColliderMono otherHandMono;
        private AudioSource swingSfx;
        private GameObject webBallSfx;
        public RaycastHit webHitSpot;
        private bool spawningHandle;
        private bool spawningWebBall;
        private bool webConnectedToRb;

        private bool allowClimbing = true;
        public AnimationCurve affectCurve = new AnimationCurve();
        private Spring spring;
        
        private float maxTimeBetweenTaps = 0.3f;
        private float lastTapTime = 0f;
        private int tapCount = 0;

        private LineRenderer handleRenderer;

        public Vector3 currentAnchorPoint;
        public Vector3 worldAnchorPoint;

        public void ActivateHand(RagdollHand hand)
        {
            this.hand = hand;
            this.originalHand = hand;
        }
        private void Start()
        {
            originalHand = hand;
            affectCurve.AddKey(0, 0);
            affectCurve.AddKey(0.3f, 0.7f);
            affectCurve.AddKey(1f, 0);
            spring = new Spring();
            spring.SetTarget(0);
            if (webtexture == null)
            {
                Catalog.LoadAssetAsync<Material>("Webtexture", callback =>
                {
                    webtexture = callback;
                }, "Webmaterial");
            }
            
        }
        void HandleTap(int tapMax, string type)
        {
            float currentTime = Time.time;

            if (currentTime - lastTapTime <= maxTimeBetweenTaps)
            {
                tapCount++;
                if (tapCount == tapMax)
                {
                    if(type == "Swing") StartSwingCheck();
                    if (type == "Shoot") SpawnWebBall();
                    tapCount = 0; 
                }
            }
            else
            {
                tapCount = 0; 
            }

            lastTapTime = currentTime;
        }

        
        void SpawnWebBall()
        {
            if (!spawningWebBall)
            {
                spawningWebBall = true;
                Catalog.GetData<ItemData>("WebBall").SpawnAsync(callback =>
                {
                        callback.transform.position = item.flyDirRef.transform.position;
                        callback.transform.rotation = item.flyDirRef.transform.rotation;
                        var webbBall = callback.GetComponent<Item>();

                        webbBall.IgnoreItemCollision(item);
                        webbBall.IgnoreRagdollCollision(hand.ragdoll);
                        var transformFound = callback.gameObject.transform.Find("web");
                        var renderer = transformFound.GetComponent<MeshRenderer>();
                        renderer.enabled = false;
                        webbBall.gameObject.AddComponent<WebBall>().Setup(item.flyDirRef.transform.position, transformFound);
                        webbBall.physicBody.rigidBody.useGravity = false;
                        webbBall.physicBody.rigidBody.AddForce(item.flyDirRef.transform.forward * 70f * Mathf.Clamp(hand.Velocity().magnitude, 1, 100f),
                            ForceMode.Impulse);
                        
                        Catalog.InstantiateAsync("WebBallSFX", item.flyDirRef.transform.position, item.flyDirRef.transform.rotation,
                                item.gameObject.transform,
                                sfx =>
                                {
                                    sfx.AddComponent<DestroyAudioAfterPlay>();
                                }, "WebBallSFX");
                        
                        spawningWebBall = false;
                    });
            }
        }

        public void ActivateItem(Item item)
        {
            this.item = item;
            item.OnGrabEvent += OnGrab;
            hand.playerHand.controlHand.OnButtonPressEvent += ButtonPressEvent;

        }
        
        private void ButtonPressEvent(PlayerControl.Hand.Button button, bool pressed)
        {
            if (button == PlayerControl.Hand.Button.Grip && activated && !this.hand.grabbedHandle && this.itemAttached && this.item)
            {
                HandleTap(2, "Swing");
            }

            if (button == PlayerControl.Hand.Button.Use && activated && !this.hand.grabbedHandle &&
                this.itemAttached && this.item && !swinging)
            {
                HandleTap(2, "Shoot");
            }
        }

        private void OnDestroy()
        {
            this.item.OnGrabEvent -= OnGrab;
        }

        private void OnGrab(Handle handle, RagdollHand ragdollhand)
        {
            if(this.itemAttached) UnSnap(this.item);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.GetComponentInParent<RagdollHand>() is RagdollHand hand && !hand.Equals(this.hand))
            {
                if (hand.Equals(this.hand.otherHand))
                { 
                    if (otherHandMono.itemAttached && this.item)
                    {
                        item.GetMainHandle(hand.side).SetTouchPersistent(true);
                        if (otherHandMono)
                        {
                            otherHandMono.activated = false;   
                        }
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.GetComponentInParent<RagdollHand>() is RagdollHand hand && !hand.Equals(this.hand))
            {
                if (hand.Equals(this.hand.otherHand))
                {
                    if (otherHandMono.itemAttached && this.item)
                    {
                        item.GetMainHandle(hand.otherHand.side).SetTouchPersistent(false);
                        otherHandMono.activated = true;  
                    }
                }
            }
        }

        void UnSnap(Item item)
        {
            this.itemAttached = false;
            item.physicBody.isKinematic = false;
            item.transform.parent = null;
            item.colliderGroups[0].colliders[0].enabled = true;
            item.GetMainHandle(hand.otherHand.side).allowedHandSide = Interactable.HandSide.Both;
            this.hand.caster.AllowSpellWheel(item.gameObject.GetComponent<SnapCheck>());
            this.hand.playerHand.controlHand.OnButtonPressEvent -= ButtonPressEvent;
            this.hand.caster.telekinesis.Enable(this);
            item.OnGrabEvent -= OnGrab;
            item.DisallowDespawn = false;
            if (!otherHandMono.itemAttached)
            {
                Player.fallDamage = true;
                GameManager.SetFreeClimb(false);
            }
        }

        void SpawnNextHandle()
        {
            Catalog.GetData<ItemData>("InvisHandle").SpawnAsync(nextHandle =>
            {
                nextHandle.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
                nextHandle.IgnoreItemCollision(item);
                nextHandle.IgnoreItemCollision(swingingHandle);
                this.nextHandle = nextHandle;
                this.nextHandle.transform.position = this.swingingHandle.transform.position +
                                                     (worldAnchorPoint - this.swingingHandle.transform.position).normalized *
                                                     0.3f;
                
                interimJoint = nextHandle.gameObject.AddComponent<FixedJoint>();
                interimJoint.autoConfigureConnectedAnchor = false;
                interimJoint.breakForce = Mathf.Infinity;
                interimJoint.breakTorque = Mathf.Infinity;
                this.nextHandle.OnUngrabEvent += UnGrabbedSwinging;
            });
        }

        private RaycastHit globalHit;
        void SpawnSwingHandle(RaycastHit hit)
        {
            Catalog.GetData<ItemData>("InvisHandle").SpawnAsync(callback =>
            {
                callback.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
                callback.IgnoreItemCollision(item);

                globalHit = hit;
                SetupSwing(callback, globalHit);
                if(ModOptions.allowClimbing) SpawnNextHandle();
            });
        }

        private ConfigurableJoint creatureConnectedJoint;
        void SetSpringJoint(RaycastHit hit)
        {

            if (hit.collider)
            {
                
                if (hit.collider.gameObject.GetComponentInParent<Creature>() is Creature creature &&
                    hit.collider.gameObject.GetComponentInParent<Rigidbody>() is Rigidbody rb)
                {
                    creature.ragdoll.SetState(Ragdoll.State.Destabilized);
                    webConnectedToRb = true;
                    currentAnchorPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                    creatureConnectedJoint = swingingHandle.gameObject.AddComponent<ConfigurableJoint>();
                    creatureConnectedJoint.autoConfigureConnectedAnchor = false;
                    creatureConnectedJoint.connectedBody = rb;
                    creatureConnectedJoint.connectedAnchor = hit.collider.transform.TransformPoint(currentAnchorPoint);
                    
                    creatureConnectedJoint.angularXMotion = ConfigurableJointMotion.Locked;
                    creatureConnectedJoint.angularYMotion = ConfigurableJointMotion.Locked;
                    creatureConnectedJoint.angularZMotion = ConfigurableJointMotion.Locked;
                    
                    creatureConnectedJoint.xMotion = ConfigurableJointMotion.Limited;
                    creatureConnectedJoint.yMotion = ConfigurableJointMotion.Limited;
                    creatureConnectedJoint.zMotion = ConfigurableJointMotion.Limited;
                    
                    SoftJointLimit limit = new SoftJointLimit();
                    limit.limit = 2f;
                    creatureConnectedJoint.linearLimit = limit;
                    JointDrive drive = new JointDrive();
                    drive.positionSpring = 0f;  
                    drive.positionDamper = 0f; 
                    drive.maximumForce = 0f; 
                    creatureConnectedJoint.xDrive = drive;
                    creatureConnectedJoint.yDrive = drive;
                    creatureConnectedJoint.zDrive = drive;
                }
                else
                {
                    webConnectedToRb = true;
                    currentAnchorPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                    mainJoint = swingingHandle.gameObject.AddComponent<SpringJoint>();
                    mainJoint.autoConfigureConnectedAnchor = false;
                    mainJoint.connectedAnchor = hit.collider.transform.TransformPoint(currentAnchorPoint);
                    
                    mainJoint.maxDistance =
                        Vector3.Distance( mainJoint.connectedAnchor, swingingHandle.flyDirRef.transform.position) * 0.95f;
                    mainJoint.minDistance =
                        Vector3.Distance( mainJoint.connectedAnchor, swingingHandle.flyDirRef.transform.position) * 0f;
                    mainJoint.spring = 130.5f;
                    mainJoint.damper = 20f;
                    mainJoint.massScale = hand.ragdoll.totalMass / hand.ragdolledMass;


                    mainJoint.breakForce = Mathf.Infinity;
                    mainJoint.breakTorque = Mathf.Infinity;
                }
            }
            else
            {
                mainJoint = swingingHandle.gameObject.AddComponent<SpringJoint>();
                mainJoint.autoConfigureConnectedAnchor = false;
                currentAnchorPoint = hit.point;
                mainJoint.connectedAnchor = worldAnchorPoint;

                mainJoint.maxDistance =
                    Vector3.Distance(worldAnchorPoint, swingingHandle.flyDirRef.transform.position) * 0.95f;
                mainJoint.minDistance =
                    Vector3.Distance(worldAnchorPoint, swingingHandle.flyDirRef.transform.position) * 0f;
                mainJoint.spring = 130.5f;
                mainJoint.damper = 20f;
                mainJoint.massScale = hand.ragdoll.totalMass / hand.ragdolledMass;


                mainJoint.breakForce = Mathf.Infinity;
                mainJoint.breakTorque = Mathf.Infinity;
            }
        }
        void SetupSwing(Item callback, RaycastHit hit)
        {
            if (!lr)
            {
                lr = new GameObject().AddComponent<LineRenderer>();
                lr.enabled = true;
                lr.textureMode = LineTextureMode.Tile;
                lr.widthMultiplier = 0.06f;
                lr.material = webtexture;
                lr.positionCount = 0;

                handleRenderer = new GameObject().AddComponent<LineRenderer>();
                handleRenderer.enabled = true;
                handleRenderer.textureMode = LineTextureMode.Tile;
                handleRenderer.widthMultiplier = 0.06f;
                handleRenderer.material = webtexture;
                handleRenderer.positionCount = 2;
            }
            else
            {
                lr.enabled = true;
                handleRenderer.enabled = true;
            }
            swingingHandle = callback.GetComponent<Item>();
            swingingHandle.transform.position = hand.transform.position;
            currentWebPosition = swingingHandle.flyDirRef.transform.position;
            swingingHandle.OnUngrabEvent += UnGrabbedSwinging;
            
            SetSpringJoint(hit);
            Handle handle = swingingHandle.mainHandleRight;
            hand.Grab(handle, handle.GetDefaultOrientation(hand.side), handle.GetDefaultAxisLocalPosition(),
                true);
            swinging = true;
            spawningHandle = false;

            if (!swingSfx)
            {
                Catalog.InstantiateAsync("swingaudio", item.transform.position, item.transform.rotation,
                    item.transform,
                    sfx => { this.swingSfx = sfx.GetComponent<AudioSource>(); }, "SwingSFX");
            }
            else
            {
                swingSfx.Play();
            }
        }
        void StartSwingCheck()
        {
            if (Physics.Raycast(item.flyDirRef.transform.position + (item.flyDirRef.transform.forward * 0.4f), item.flyDirRef.transform.forward, out var hit,
                    60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && !spawningHandle)
            {
                webHitSpot = hit;
                spawningHandle = true;
                SpawnSwingHandle(hit);
            }
        }

        private Vector3 currentWebPosition;
        void DrawWeb()
        {
            if (lr.positionCount == 0)
            {
                spring.SetVelocity(ModOptions.velocity);
                lr.positionCount = ModOptions.quality + 1;
            }
            
            spring.SetDamper(ModOptions.damper);
            spring.SetStrength(Mathf.Clamp(ModOptions.strength * Vector3.Distance(worldAnchorPoint, swingingHandle.flyDirRef.transform.position),ModOptions.strength, ModOptions.strength * 5));
            spring.Update(Time.deltaTime);

            var grapplePoint = worldAnchorPoint;
            var grappleStartPoint = swingingHandle.flyDirRef.transform.position;
            var up = Quaternion.LookRotation((grapplePoint - grappleStartPoint).normalized) * Vector3.up;
            currentWebPosition = Vector3.Lerp(currentWebPosition, grapplePoint, Time.deltaTime * 12f);
        
            for (int i = 0; i < ModOptions.quality + 1; i++)
            {
                var delta = i / (float) ModOptions.quality;
                var offset = up * Mathf.Clamp(ModOptions.waveHeight * Vector3.Distance(worldAnchorPoint, swingingHandle.flyDirRef.transform.position),ModOptions.waveHeight, ModOptions.waveHeight * 2) * Mathf.Sin(delta * Mathf.Clamp(ModOptions.waveCount * Vector3.Distance(worldAnchorPoint, swingingHandle.flyDirRef.transform.position),ModOptions.waveCount, ModOptions.waveCount * 2) * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);
               
                float rotationAngle = delta * ModOptions.rotation;
                Quaternion rotation = Quaternion.AngleAxis(rotationAngle, (currentWebPosition - grappleStartPoint).normalized);
                offset = Vector3.Lerp(offset, rotation * offset, 1f); 

                lr.SetPosition(i, Vector3.Lerp(grappleStartPoint, currentWebPosition, delta) + offset);
            }

        }
        private void UnGrabbedSwinging(Handle handle, RagdollHand ragdollhand, bool throwing)
        {
            if (handle.GetComponentInParent<Item>().Equals(swingingHandle) && ModOptions.allowClimbing)
            {
                if (nextHandle && nextHandle.mainHandler == null)
                {
                    BreakSwing();
                    Destroy(nextHandle.gameObject);
                }
                else
                {
                    this.hand = this.hand.otherHand;
                    var tempStorage = swingingHandle;
                    swingingHandle = nextHandle;
                    SetSpringJoint(webHitSpot);
                    mainJoint.maxDistance -= Vector3.Distance(tempStorage.transform.position, swingingHandle.transform.position) * 15f;
                    Destroy(swingingHandle.gameObject.GetComponent<FixedJoint>());
                    Destroy(tempStorage.gameObject);
                    SpawnNextHandle();
                }
            }
            else
            {
                BreakSwing();
            }
        }
        private void LateUpdate()
        {
            if (swinging)
            {
                DrawWeb();
                if (handleRenderer)
                {
                    handleRenderer.SetPosition(0, swingingHandle.flyDirRef.transform.position + (-swingingHandle.flyDirRef.transform.forward * 0.0706314f));
                    handleRenderer.SetPosition(1, swingingHandle.flyDirRef.transform.position);
                }
            }
            else
            {
                if (lr)
                {
                    spring.Reset();
                    if (lr.positionCount > 0)
                    {
                        lr.positionCount = 0;
                    }
                }
            }
        }


        void BreakSwing()
        {
            swinging = false;
            lr.enabled = false;
            handleRenderer.enabled = false;
            this.hand = originalHand;
            webConnectedToRb = false;
            if(mainJoint) Destroy(mainJoint);
            if(creatureConnectedJoint) Destroy(creatureConnectedJoint);
            Destroy(swingingHandle.gameObject);
        }


        private void FixedUpdate()
        {
            if (mainJoint && webConnectedToRb)
            {

                if (webHitSpot.collider)
                {
                    worldAnchorPoint = webHitSpot.collider.transform.TransformPoint(currentAnchorPoint);
                    mainJoint.connectedAnchor = worldAnchorPoint;
                }
            }
        }

        private void Update()
        {
            if (this.swinging)
            {
                if (creatureConnectedJoint)
                {
                    float distance = Vector3.Distance(swingingHandle.transform.position, currentAnchorPoint);
                    float distanceFactor = Mathf.Clamp01(distance / 2); 
                    Vector3 directionToPull = (currentAnchorPoint - swingingHandle.transform.position).normalized;
                    creatureConnectedJoint.targetPosition = directionToPull * Mathf.Clamp(distance, 0.1f, 4f);

                    JointDrive drive = creatureConnectedJoint.xDrive;
                    drive.positionSpring = 10f;
                    drive.positionDamper = 500f;
                    drive.maximumForce = 50f;
                    creatureConnectedJoint.xDrive = drive;
                    creatureConnectedJoint.yDrive = drive;
                    creatureConnectedJoint.zDrive = drive;
                }
                if (swingingHandle && swingingHandle.mainHandler && swingingHandle.mainHandler.Equals(this.hand) &&
                    this.hand.playerHand.controlHand.usePressed)
                {
                    if (mainJoint && mainJoint.maxDistance > mainJoint.minDistance)
                    {
                        mainJoint.maxDistance -= ModOptions.reelInPower * Time.deltaTime;
                    }
                }

                if (swingingHandle && swingingHandle.mainHandler && swingingHandle.mainHandler.Equals(this.hand) &&
                    this.hand.playerHand.controlHand.alternateUsePressed)
                {
                    if (mainJoint && mainJoint.maxDistance >= mainJoint.minDistance)
                    {
                        mainJoint.maxDistance += ModOptions.reelOutPower * Time.deltaTime;
                    }
                }

                if (interimJoint)
                {
                    interimJoint.connectedAnchor = this.swingingHandle.transform.position +
                                                   (worldAnchorPoint - this.swingingHandle.transform.position).normalized *
                                                   0.3f;
                    nextHandle.transform.rotation = Quaternion.LookRotation((worldAnchorPoint - swingingHandle.transform.position));
                }
            }
        }
    }
}