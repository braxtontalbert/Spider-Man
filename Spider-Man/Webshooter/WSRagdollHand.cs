using System;
using System.Collections;
using Spider_Man.Management;
using Spider_Man.Webshooter.Animation;
using Spider_Man.Webshooter.Gadgets.ImpactWeb;
using Spider_Man.Webshooter.Gadgets.WebBall;
using Spider_Man.Webshooter.Gadgets.WebBomb;
using ThunderRoad;
using UnityEngine;
using Pointer = ThunderRoad.Pointer;
using Random = UnityEngine.Random;

namespace Spider_Man.Webshooter
{
    enum Gadget
    {
        WebBalls,
        ImpactWeb
        //WebBomb
    }
    public class WSRagdollHand : MonoBehaviour
    {
        private Gadget activeGadget = Gadget.WebBalls;
        private RagdollHand originalHand;
        public RagdollHand hand;
        public Item item;
        public bool itemAttached;
        public bool swinging;
        private SpringJoint mainJoint;
        private FixedJoint interimJoint;
        private LineRenderer lr;
        public static Material webtexture;
        public Item swingingHandle;
        private Item nextHandle;
        public bool activated = true;
        public WSRagdollHand otherHandMono;
        private AudioSource swingSfx;
        private AudioSource alternateSfx;
        private GameObject webBallSfx;
        public RaycastHit webHitSpot;
        private bool spawningHandle;
        private bool spawningWebBall;
        private bool spawningImpactWeb;
        private bool spawningWebBomb;
        private bool spawnTimeReset = true;
        private bool webConnectedToRb;

        private bool allowClimbing = true;
        public AnimationCurve affectCurve = new AnimationCurve();
        private Spring spring;
        
        private float maxTimeBetweenTapsSwing = 0.3f;
        private float maxTimeBetweenTapsShoot = 0.3f;
        private float lastTapTimeSwing = 0f;
        private float lastTapTimeShoot = 0f;
        private int tapCountSwing = 0;
        private int tapCountShoot = 0;

        private LineRenderer handleRenderer;

        public Vector3 currentAnchorPoint;
        public Vector3 worldAnchorPoint;
        public HandPoseData thwipHandlePose;

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

            if (this.hand.side == Side.Left)
            {
                this.thwipHandlePose = Catalog.GetData<HandPoseData>("ThwipCloseLeft");
            }
            else
            {
                this.thwipHandlePose = Catalog.GetData<HandPoseData>("ThwipCloseRight");
            }
        }
        void HandleTap(int tapMax, string type)
        {
            
                switch (type)
                {
                    case "Swing":
                        float currentTimeSwing = Time.time;
                        if (currentTimeSwing - lastTapTimeSwing <= maxTimeBetweenTapsSwing)
                        {
                            tapCountSwing++;
                            if (tapCountSwing == tapMax)
                            {
                                StartSwingCheck();
                            }
                        }
                        else
                        {
                            tapCountSwing = 0;
                        }
                        lastTapTimeSwing = currentTimeSwing;
                        break;
                    case "Shoot":
                        float currentTimeShoot = Time.time;
                        if (currentTimeShoot - lastTapTimeShoot > maxTimeBetweenTapsShoot)
                        {
                            tapCountShoot = 0; // Reset if too slow
                        }

                        tapCountShoot++;
                        lastTapTimeShoot = currentTimeShoot;

                        if (tapCountShoot == tapMax)
                        {
                            SpawnWebBall();
                            tapCountShoot = 0;
                        }
                        break;
                }
                
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
                        var transformFound = callback.gameObject.transform.Find("webballRounded");
                        var renderer = transformFound.GetComponentInChildren<MeshRenderer>();
                        renderer.enabled = false;
                        webbBall.gameObject.AddComponent<WebBall>().Setup(item.flyDirRef.transform.position, transformFound);
                        webbBall.physicBody.rigidBody.useGravity = false;
                        webbBall.physicBody.rigidBody.AddForce(item.flyDirRef.transform.forward  * Mathf.Clamp(Math.Abs(hand.physicBody.rigidBody.velocity.magnitude), 85f, 130f),
                            ForceMode.Impulse);
                        
                        Catalog.InstantiateAsync("WebBallSFX", item.flyDirRef.transform.position, item.flyDirRef.transform.rotation,
                                item.gameObject.transform,
                                sfx =>
                                {
                                    var audio = sfx.GetComponent<AudioSource>();
                                    audio.pitch = Random.Range(0.9f, 1.1f);
                                    audio.Play();
                                    sfx.AddComponent<DestroyAudioAfterPlay>();
                                }, "WebBallSFX");
                        
                        spawningWebBomb = false;
                        spawningWebBall = false;
                    });
            }
        }

        IEnumerator ImpactWebTimer()
        {
            yield return new WaitForSeconds(3f);
            if (!this.reloadSound)
            {
                Catalog.InstantiateAsync("reloadSound", this.item.transform.position, this.item.transform.rotation,
                    this.item.transform,
                    go => { this.reloadSound = go.GetComponent<AudioSource>(); }, "ReloadSoundHandler");
            }
            else
            {
                this.reloadSound.Play();
            }

            spawnTimeReset = true;
        }
        void SpawnImpactWeb()
        {
            if (!spawningImpactWeb && spawnTimeReset)
            {
                spawnTimeReset = false;
                StartCoroutine(ImpactWebTimer());
                spawningImpactWeb = true;
                Catalog.InstantiateAsync("impactWeb", item.flyDirRef.transform.position, item.flyDirRef.transform.rotation, null, callback =>
                {
                    callback.transform.position = item.flyDirRef.transform.position;
                    callback.transform.rotation = item.flyDirRef.transform.rotation;
                    var webbBall = callback.GetComponent<Item>();

                    webbBall.IgnoreItemCollision(item);
                    webbBall.IgnoreRagdollCollision(hand.ragdoll);
                    var transformFound = callback.gameObject.transform.Find("webballRounded");
                    var renderer = transformFound.GetComponentInChildren<MeshRenderer>();
                    renderer.enabled = false;
                    webbBall.gameObject.AddComponent<ImpactWeb>().Setup(item.flyDirRef.transform.position, transformFound, this.item);
                    webbBall.physicBody.rigidBody.useGravity = false;
                    webbBall.physicBody.rigidBody.AddForce(item.flyDirRef.transform.forward * Mathf.Clamp(hand.physicBody.velocity.magnitude, 95, 150f),
                        ForceMode.Impulse);
                        
                    Catalog.InstantiateAsync("WebBallSFX", item.flyDirRef.transform.position, item.flyDirRef.transform.rotation,
                        item.gameObject.transform,
                        sfx =>
                        {
                            var audio = sfx.GetComponent<AudioSource>();
                            audio.pitch = Random.Range(0.9f, 1.1f);
                            audio.Play();
                            sfx.AddComponent<DestroyAudioAfterPlay>();
                        }, "WebBallSFX");
                        
                    spawningWebBomb = false;
                    spawningWebBall = false;
                    spawningImpactWeb = false;
                }, "ImpactWebHandler");
            }
        }

        void SpawnWebBomb()
        {
            if (!spawningWebBomb)
            {
                spawningWebBall = true;
                Catalog.InstantiateAsync("webBomb", item.flyDirRef.transform.position,
                    item.flyDirRef.transform.rotation, null, callback =>

                    { 
                        var collider = callback.GetComponent<Collider>();
                        foreach (var colliderGroup in item.colliderGroups)
                        {
                            foreach (var colliderIn in colliderGroup.colliders)
                            {
                                Physics.IgnoreCollision(colliderIn, collider);
                            }
                        }
                    
                    foreach (var part in hand.ragdoll.parts)
                    {
                        foreach (var colliders in part.colliderGroup.colliders)
                        {
                            Physics.IgnoreCollision(colliders, collider);
                        }
                    }
                    //var transformFound = callback.gameObject.transform.Find("webballRounded");
                    //var renderer = transformFound.GetComponentInChildren<MeshRenderer>();
                    //renderer.enabled = false;
                    callback.AddComponent<WebBomb>();
                    callback.GetComponent<Rigidbody>().AddForce(item.flyDirRef.transform.forward * 20f * Mathf.Clamp(hand.Velocity().magnitude, 1, 100f),
                        ForceMode.Impulse);
                    callback.layer = GameManager.GetLayer(LayerName.MovingItem);
                    /*Catalog.InstantiateAsync("WebBallSFX", item.flyDirRef.transform.position, item.flyDirRef.transform.rotation,
                        item.gameObject.transform,
                        sfx =>
                        {
                            sfx.AddComponent<DestroyAudioAfterPlay>();
                        }, "WebBallSFX");*/
                        
                    spawningWebBomb = false;
                    spawningWebBall = false;
                    }, "WebBombHandler");
            }
        }
        public void ActivateItem(Item item)
        {
            this.item = item;
            item.OnGrabEvent += OnGrab;
            
            hand.poser.SetTargetPose(thwipHandlePose, true, true, true, true, true);
            hand.playerHand.controlHand.OnButtonPressEvent += ButtonPressEvent;

        }
        
        private void ButtonPressEvent(PlayerControl.Hand.Button button, bool pressed)
        {
            if (button == PlayerControl.Hand.Button.Grip && activated && !this.hand.grabbedHandle && this.itemAttached && this.item)
            {
                Debug.Log("Target Pose: " + hand?.poser?.targetHandPoseData.id);
                HandleTap(2, "Swing");
            }

            if (button == PlayerControl.Hand.Button.Use && activated && !this.hand.grabbedHandle &&
                this.itemAttached && this.item && !swinging && pressed && !Pointer.GetActive().isPointingUI && !hand.playerHand.isFist)
            {
                
                switch (activeGadget)
                {
                    case Gadget.WebBalls:
                        HandleTap(2, "Shoot");//SpawnWebBall();
                        break;
                    case Gadget.ImpactWeb:
                        SpawnImpactWeb();
                        break;
                    /*case Gadget.WebBomb:
                        SpawnWebBomb();
                        break;*/
                }
            }

            if (button == PlayerControl.Hand.Button.AlternateUse && !this.hand.grabbedHandle && this.itemAttached &&
                this.item && !swinging && pressed)
            {
                
                Gadget[] values = (Gadget[])Enum.GetValues((typeof(Gadget)));
                int index = Array.IndexOf(values, activeGadget);
                index = (index + 1) % values.Length;
                activeGadget = values[index];
                if (!alternateSfx)
                {
                    Catalog.InstantiateAsync("alternateGadget", this.item.transform.position, this.item.transform.rotation, this.item.gameObject.transform,
                        go =>
                        {
                            alternateSfx = go.GetComponent<AudioSource>();
                            alternateSfx.Play();
                        }, "AlternateSoundHandler");
                }
                else
                {
                    alternateSfx.Play();
                }
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
            item.GetMainHandle(hand.side).SetTouchPersistent(true);
            this.hand.playerHand.controlHand.OnButtonPressEvent -= ButtonPressEvent;
            this.hand.caster.telekinesis.Enable(this);
            item.OnGrabEvent -= OnGrab;
            item.DisallowDespawn = false;
            if (!otherHandMono.itemAttached)
            {
                Player.fallDamage = true;
                GameManager.SetFreeClimb(false);
            }
            if(hand.poser.targetHandPoseData.Equals(this.thwipHandlePose)) hand.poser.ResetTargetPose();
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

        private SpringJoint creatureConnectedJoint;
        void SetSpringJoint(RaycastHit hit)
        {
            if (hit.collider)
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
                if (hit.collider.gameObject.GetComponentInParent<Creature>() is Creature creature)
                {
                    creature.locomotion.flyDrag = Player.local.locomotion.flyDrag;
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
            this.item.GetMainHandle(hand.side).SetTouchPersistent(false);
            this.item.handles[0].gameObject.SetActive(false);
            if (!lr)
            {
                lr = new GameObject().AddComponent<LineRenderer>();
                lr.enabled = true;
                lr.textureMode = LineTextureMode.Tile;
                lr.widthMultiplier = 0.1f;
                lr.material = webtexture;
                lr.positionCount = 0;

                handleRenderer = new GameObject().AddComponent<LineRenderer>();
                handleRenderer.enabled = true;
                handleRenderer.textureMode = LineTextureMode.Tile;
                handleRenderer.widthMultiplier = 0.1f;
                handleRenderer.material = webtexture;
                handleRenderer.positionCount = 2;
            }
            else
            {
                lr.enabled = true;
                handleRenderer.enabled = true;
            }
            swingingHandle = callback.GetComponent<Item>();
            foreach (var group in swingingHandle.colliderGroups)
            {
                foreach (var collider in group.colliders)
                {
                    collider.enabled = false;
                }
            }
            swingingHandle.transform.position = hand.transform.position;
            currentWebPosition = swingingHandle.flyDirRef.transform.position;
            swingingHandle.OnUngrabEvent += UnGrabbedSwinging;
            
            SetSpringJoint(hit);
            Handle handle = swingingHandle.mainHandleRight;
            hand.Grab(handle, handle.GetDefaultOrientation(hand.side), handle.GetDefaultAxisLocalPosition(),
                true);
            if (!swingSfx && !swinging)
            {
                Catalog.InstantiateAsync("swingaudio", item.transform.position, item.transform.rotation,
                    item.transform,
                    sfx =>
                    {
                        this.swingSfx = sfx.GetComponent<AudioSource>(); 
                        swingSfx.pitch = Random.Range(0.9f, 1.1f);
                        swingSfx.Play();
                    }, "SwingSFX");
            }
            else if(!swinging)
            {
                swingSfx.pitch = Random.Range(0.9f, 1.1f);
                swingSfx.Play();
            }
            swinging = true;
            spawningHandle = false;
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
        private AudioSource reloadSound;

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
                    item.GetMainHandle(hand.side).SetTouchPersistent(true);
                    if (hand.otherHand.side == Side.Left)
                        item.GetMainHandle(hand.side).allowedHandSide = Interactable.HandSide.Left;
                    if (hand.otherHand.side == Side.Right)
                        item.GetMainHandle(hand.side).allowedHandSide = Interactable.HandSide.Right;
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
            hand.poser.SetTargetPose(thwipHandlePose, true, true, true, true, true);
            this.item.handles[0].gameObject.SetActive(true);
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

        void CheckForWeblineIntersections()
        {
            RaycastHit[] hits;
            if (Physics.Raycast(lr.GetPosition(0), (lr.GetPosition(lr.positionCount - 1) - lr.GetPosition(0)).normalized, out RaycastHit hit,
                    Vector3.Distance(lr.GetPosition(0), lr.GetPosition(lr.positionCount - 1)),
                    ~((1 << 31) | (1 << 22)), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Distance(hit.point, lr.GetPosition(lr.positionCount - 1)) > 0.001f && !hit.collider.gameObject.GetComponentInParent<Creature>())
                {
                    if (hit.collider)
                    {
                        var distanceBetweenPoints = Vector3.Distance(hit.point, currentAnchorPoint);
                        var distanceToWebAnchor = Vector3.Distance(mainJoint.connectedAnchor,
                            swingingHandle.flyDirRef.transform.position);
                        webHitSpot = hit;
                        webConnectedToRb = true;
                        currentAnchorPoint = hit.collider.transform.InverseTransformPoint(hit.point);
                        //Destroy(mainJoint);
                        //mainJoint = swingingHandle.gameObject.AddComponent<SpringJoint>();
                        mainJoint.autoConfigureConnectedAnchor = false;
                        mainJoint.connectedAnchor = hit.collider.transform.TransformPoint(currentAnchorPoint);
                                
                        mainJoint.maxDistance =
                            distanceToWebAnchor * 0.95f;
                        mainJoint.minDistance =
                            distanceToWebAnchor * 0f;
                        mainJoint.spring = 130.5f;
                        mainJoint.damper = 20f;
                        mainJoint.massScale = hand.ragdoll.totalMass / hand.ragdolledMass;


                        mainJoint.breakForce = Mathf.Infinity;
                        mainJoint.breakTorque = Mathf.Infinity;
                    }
                    else
                    {
                       // Destroy(mainJoint);
                        //mainJoint = swingingHandle.gameObject.AddComponent<SpringJoint>();
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
            }
        }

        private void Update()
        {
            if (this.swinging)
            {
                if (lr)
                {
                    if(ModOptions.realisticWeblines && lr.positionCount > 0) CheckForWeblineIntersections();
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
            else
            {
                if (hand.playerHand.isFist && this.itemAttached)
                {
                    if(hand.poser.targetHandPoseData.Equals(this.thwipHandlePose)) hand.poser.ResetTargetPose();
                }
                else if (this.itemAttached)
                {
                    if(!hand.poser.targetHandPoseData.Equals(this.thwipHandlePose)) hand.poser.SetTargetPose(thwipHandlePose, true, true, true, true, true);
                }
            }
        }
    }
}