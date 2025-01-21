using System;
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
        public Vector3 webHitSpot;
        private bool spawningHandle;
        private bool spawningWebBall;

        private bool allowClimbing = true;
        public AnimationCurve affectCurve = new AnimationCurve();
        private Spring spring;
        
        //double tap variables
        private float maxTimeBetweenTaps = 0.3f;
        private float lastTapTime = 0f;
        private int tapCount = 0;

        private LineRenderer handleRenderer;

        public void ActivateHand(RagdollHand hand)
        {
            this.hand = hand;
            this.originalHand = hand;
        }

        private Mesh webMesh;
        private GameObject webMeshObject;
        private GameObject instantiatedObjectMesh;
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
                    webMeshObject = new GameObject();
                    var meshFilter = webMeshObject.GetComponent<MeshFilter>();
                    if (meshFilter == null)
                    {
                        meshFilter = webMeshObject.AddComponent<MeshFilter>();
                    }

                    var meshRenderer = webMeshObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                    {
                        meshRenderer = webMeshObject.AddComponent<MeshRenderer>();
                    }

                    // Initialize the mesh
                    webMesh = new Mesh();
                    meshFilter.mesh = webMesh;

                    meshRenderer.material = webtexture;
                    // Optionally assign a default material
                    if (meshRenderer.material == null)
                    {
                        meshRenderer.material = new Material(Shader.Find("ThunderRoad/LitMoss"));
                    }
                }, "Webmaterial");
            }
            
        }
            private void DrawDynamicWebMesh()
            {
            // Ensure the web object is instantiated
            if (!instantiatedObjectMesh)
            {
                instantiatedObjectMesh = Instantiate(webMeshObject);
                instantiatedObjectMesh.transform.position = swingingHandle.transform.position;
            }

            // Update currentWebPosition for animation
            currentWebPosition = Vector3.Lerp(currentWebPosition, webHitSpot, Time.deltaTime * 12f);
            if (Vector3.Distance(currentWebPosition, webHitSpot) < 0.01f)
            {
                currentWebPosition = webHitSpot;
            }

            // Prepare parameters for variation
            float randomWaveHeight = ModOptions.waveHeight * Random.Range(0.8f, 1.2f);
            float randomWaveFrequency = ModOptions.waveCount * Random.Range(0.8f, 1.2f);
            float randomRotationMultiplier = ModOptions.rotation * Random.Range(0.8f, 1.2f);

            // Web appearance customization
            float baseWidth = 0.02f; // Narrower base width for Spider-Man-like web
            float taperingFactor = 0.01f; // End width

            int vertexCount = (ModOptions.quality + 1) * 2;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[ModOptions.quality * 6];
            var uvs = new Vector2[vertexCount];

            Vector3 grappleStartPoint = swingingHandle.flyDirRef.transform.position;
            Vector3 up = Quaternion.LookRotation((webHitSpot - grappleStartPoint).normalized) * Vector3.up;
            currentWebPosition = Vector3.Lerp(currentWebPosition, webHitSpot, Time.deltaTime * 12f);
            for (int i = 0; i <= ModOptions.quality; i++)
            {
                float delta = i / (float)ModOptions.quality;

                // Position along the web, animating toward currentWebPosition
                Vector3 segmentPosition = Vector3.Lerp(grappleStartPoint, currentWebPosition, delta);

                // Tangled offset effect: Add spiraling or jittering
                float tangleStrength = Mathf.Lerp(0.005f, 0.015f, delta);
                Vector3 tangleOffset = Quaternion.AngleAxis(delta * 360 * randomRotationMultiplier, (webHitSpot - grappleStartPoint).normalized) * up * tangleStrength;

                // Wave effect for the web
                float waveMagnitude = Mathf.Clamp(randomWaveHeight * Vector3.Distance(webHitSpot, grappleStartPoint), randomWaveHeight, randomWaveHeight * 2);
                Vector3 waveOffset = up * waveMagnitude * Mathf.Sin(delta * randomWaveFrequency * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

                // Combine offsets
                Vector3 totalOffset = waveOffset + tangleOffset;

                // Tapering width
                float width = Mathf.Lerp(baseWidth, taperingFactor, delta);

                // Define vertices for this segment
                Vector3 depthOffset = (currentWebPosition - grappleStartPoint).normalized * Mathf.Lerp(0.05f, 0.1f, delta);
                Vector3 left = segmentPosition - (up * (width / 2)) + totalOffset;
                Vector3 right = segmentPosition + (up * (width / 2)) + totalOffset;

                vertices[i * 2] = left;
                vertices[i * 2 + 1] = right;

                // UV mapping
                uvs[i * 2] = new Vector2(0, delta);
                uvs[i * 2 + 1] = new Vector2(1, delta);

                // Define triangles
                if (i < ModOptions.quality)
                {
                    int baseIndex = i * 2;
                    int triangleIndex = i * 6;

                    // First triangle
                    triangles[triangleIndex] = baseIndex;
                    triangles[triangleIndex + 1] = baseIndex + 2;
                    triangles[triangleIndex + 2] = baseIndex + 1;

                    // Second triangle
                    triangles[triangleIndex + 3] = baseIndex + 1;
                    triangles[triangleIndex + 4] = baseIndex + 2;
                    triangles[triangleIndex + 5] = baseIndex + 3;
                }
            }

            // Update the mesh
            webMesh.Clear();
            webMesh.vertices = vertices;
            webMesh.triangles = triangles;
            webMesh.uv = uvs;
            webMesh.RecalculateNormals();
        }
        
        void HandleTap(int tapMax, string type)
        {
            float currentTime = Time.time;

            if (currentTime - lastTapTime <= maxTimeBetweenTaps)
            {
                // Double tap detected
                tapCount++;
                if (tapCount == tapMax)
                {
                    Debug.LogError("Before swing check");
                    if(type == "Swing") StartSwingCheck();
                    //if (type == "Shoot") SpawnWebBall();
                    tapCount = 0; // Reset tap count after executing
                }
            }
            else
            {
                // Too much time has passed; reset tap count
                tapCount = 0; // Start a new tap sequence
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
                        webbBall.gameObject.AddComponent<WebBall>();
                        webbBall.physicBody.rigidBody.useGravity = true;
                        webbBall.physicBody.rigidBody.AddForce(item.flyDirRef.transform.forward * 70f,
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
            Debug.Log("PRESSING BUTTONS");
            if (button == PlayerControl.Hand.Button.Grip && activated && !this.hand.grabbedHandle && this.itemAttached && this.item)
            {
                HandleTap(2, "Swing");
            }

            /*if (button == PlayerControl.Hand.Button.Use && activated && !this.hand.grabbedHandle &&
                this.itemAttached && this.item && !swinging)
            {
                HandleTap(2, "Shoot");
            }*/
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
            item.OnGrabEvent -= OnGrab;
            Debug.Log(this.otherHandMono.activated);
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
                                                     (webHitSpot - this.swingingHandle.transform.position).normalized *
                                                     0.3f;
                
                interimJoint = nextHandle.gameObject.AddComponent<FixedJoint>();
                interimJoint.autoConfigureConnectedAnchor = false;
                interimJoint.breakForce = Mathf.Infinity;
                interimJoint.breakTorque = Mathf.Infinity;
                this.nextHandle.OnUngrabEvent += UnGrabbedSwinging;
            });
        }
        void SpawnSwingHandle(RaycastHit hit)
        {
            Catalog.GetData<ItemData>("InvisHandle").SpawnAsync(callback =>
            {
                callback.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
                callback.IgnoreItemCollision(item);
                SetupSwing(callback, hit);
                if(ModOptions.allowClimbing) SpawnNextHandle();
            });
        }
        
        void SetSpringJoint(Vector3 hit)
        {
            mainJoint = swingingHandle.gameObject.AddComponent<SpringJoint>();
            mainJoint.autoConfigureConnectedAnchor = false;
            mainJoint.connectedAnchor = hit;

            mainJoint.maxDistance =
                Vector3.Distance(hit, swingingHandle.flyDirRef.transform.position) * 0.95f;
            mainJoint.minDistance =
                Vector3.Distance(hit, swingingHandle.flyDirRef.transform.position) * 0f;
            mainJoint.spring = 130.5f;
            mainJoint.damper = 20f;
            mainJoint.massScale = hand.ragdoll.totalMass / hand.ragdolledMass;


            mainJoint.breakForce = Mathf.Infinity;
            mainJoint.breakTorque = Mathf.Infinity;
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
            
            SetSpringJoint(hit.point);
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
            Debug.Log("Before ray cast");
            if (Physics.Raycast(item.flyDirRef.transform.position + (item.flyDirRef.transform.forward * 0.4f), item.flyDirRef.transform.forward, out var hit,
                    60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && !spawningHandle)
            {
                Debug.LogError("In raycast hit is: " + hit);
                webHitSpot = hit.point;
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
            spring.SetStrength(Mathf.Clamp(ModOptions.strength * Vector3.Distance(webHitSpot, swingingHandle.flyDirRef.transform.position),ModOptions.strength, ModOptions.strength * 5));
            spring.Update(Time.deltaTime);

            var grapplePoint = webHitSpot;
            var grappleStartPoint = swingingHandle.flyDirRef.transform.position;
            var up = Quaternion.LookRotation((grapplePoint - grappleStartPoint).normalized) * Vector3.up;
            currentWebPosition = Vector3.Lerp(currentWebPosition, grapplePoint, Time.deltaTime * 12f);
        
            for (int i = 0; i < ModOptions.quality + 1; i++)
            {
                var delta = i / (float) ModOptions.quality;
                var offset = up * Mathf.Clamp(ModOptions.waveHeight * Vector3.Distance(webHitSpot, swingingHandle.flyDirRef.transform.position),ModOptions.waveHeight, ModOptions.waveHeight * 2) * Mathf.Sin(delta * Mathf.Clamp(ModOptions.waveCount * Vector3.Distance(webHitSpot, swingingHandle.flyDirRef.transform.position),ModOptions.waveCount, ModOptions.waveCount * 2) * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);
               
                // Add rotation to the offset
                float rotationAngle = delta * ModOptions.rotation; // Full rotation over the length
                Quaternion rotation = Quaternion.AngleAxis(rotationAngle, (currentWebPosition - grappleStartPoint).normalized); // Rotate around the main axis of the web
                offset = Vector3.Lerp(offset, rotation * offset, 1f); // Apply rotation to the offset

                lr.SetPosition(i, Vector3.Lerp(grappleStartPoint, currentWebPosition, delta) + offset);
            }

        }
        
        private void DrawWebMesh()
        {

            if (!instantiatedObjectMesh)
            {
                instantiatedObjectMesh = Instantiate(webMeshObject);
                instantiatedObjectMesh.transform.position = swingingHandle.transform.position;
            }
        // Prepare vertices and triangles
            int vertexCount = (ModOptions.quality + 1) * 2;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[ModOptions.quality * 6];
            var uvs = new Vector2[vertexCount];

            Vector3 grappleStartPoint = swingingHandle.flyDirRef.transform.position;
            Vector3 up = Quaternion.LookRotation((webHitSpot - grappleStartPoint).normalized) * Vector3.up;
            currentWebPosition = Vector3.Lerp(currentWebPosition, webHitSpot, Time.deltaTime * 12f);
            for (int i = 0; i <= ModOptions.quality; i++)
            {
                float delta = i / (float)ModOptions.quality;

                // Offset for wave effect
                float waveMagnitude = Mathf.Clamp(ModOptions.waveHeight * Vector3.Distance(webHitSpot, grappleStartPoint), ModOptions.waveHeight, ModOptions.waveHeight * 2);
                float waveFrequency = Mathf.Clamp(ModOptions.waveCount * Vector3.Distance(webHitSpot, grappleStartPoint), ModOptions.waveCount, ModOptions.waveCount * 2);
                Vector3 offset = up * waveMagnitude * Mathf.Sin(delta * waveFrequency * Mathf.PI) * spring.Value * affectCurve.Evaluate(delta);

                // Rotation effect
                float rotationAngle = delta * ModOptions.rotation;
                Quaternion rotationQuat = Quaternion.AngleAxis(rotationAngle, (currentWebPosition - grappleStartPoint).normalized);
                offset = rotationQuat * offset;

                // Position of the web at this segment
                Vector3 segmentPosition = Vector3.Lerp(grappleStartPoint, currentWebPosition, delta);

                // Tapering width
                float width = Mathf.Lerp(0.2f, 0.05f, delta);

                // Define vertices for this segment
                Vector3 depthOffset = (currentWebPosition - grappleStartPoint).normalized * Mathf.Lerp(0.05f, 0.1f, delta);
                Vector3 left = segmentPosition - (rotationQuat * up) * (width / 2) + depthOffset;
                Vector3 right = segmentPosition + (rotationQuat * up) * (width / 2) - depthOffset;

                vertices[i * 2] = left;
                vertices[i * 2 + 1] = right;

                // UV mapping
                uvs[i * 2] = new Vector2(0, delta);
                uvs[i * 2 + 1] = new Vector2(1, delta);

                // Define triangles
                if (i < ModOptions.quality)
                {
                    int baseIndex = i * 2;
                    int triangleIndex = i * 6;

                    // First triangle
                    triangles[triangleIndex] = baseIndex;
                    triangles[triangleIndex + 1] = baseIndex + 2;
                    triangles[triangleIndex + 2] = baseIndex + 1;

                    // Second triangle
                    triangles[triangleIndex + 3] = baseIndex + 1;
                    triangles[triangleIndex + 4] = baseIndex + 2;
                    triangles[triangleIndex + 5] = baseIndex + 3;
                }
            }

            // Update the mesh
            webMesh.Clear();
            webMesh.vertices = vertices;
            webMesh.triangles = triangles;
            webMesh.uv = uvs;
            webMesh.RecalculateNormals();
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
            if (swinging && false)
            {
                DrawWeb();
                DrawWebMesh();
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

                if (instantiatedObjectMesh)
                {
                    Destroy(instantiatedObjectMesh);
                }
            }
        }
       

        void BreakSwing()
        {
            swinging = false;
            lr.enabled = false;
            handleRenderer.enabled = false;
            this.hand = originalHand;
            Destroy(mainJoint);
            Destroy(swingingHandle.gameObject);
        }
        
        private float transitionTime = 0f; // Manages the transition animation
        private float connectionProgress = 0f; // Tracks the progress of the web connecting to the target
        private bool isSwinging = false; // Tracks if the web is actively swinging

        private void DrawRopeMesh()
        {
            // Ensure the web object is instantiated
            if (!instantiatedObjectMesh)
            {
                instantiatedObjectMesh = Instantiate(webMeshObject);
                instantiatedObjectMesh.transform.position = swingingHandle.transform.position;
            }

            // Reset state for new swings
            if (!isSwinging)
            {
                connectionProgress = 0f;
                transitionTime = 0f;
                currentWebPosition = swingingHandle.flyDirRef.transform.position;
                isSwinging = true; // Set swinging state
            }

            // Speed of connection animation
            float connectionSpeed = 0.8f; // Slightly slower for the connection
            bool hasReachedTarget = connectionProgress >= 1f;

            if (!hasReachedTarget)
            {
                connectionProgress = Mathf.Clamp01(connectionProgress + Time.deltaTime * connectionSpeed);
                currentWebPosition = Vector3.Lerp(swingingHandle.flyDirRef.transform.position, webHitSpot, connectionProgress);
            }

            // Transition factor for uncoiling
            float transitionDuration = 2.0f; // Longer duration for slower uncoiling
            float uncoilFactor = hasReachedTarget ? Mathf.Clamp01(transitionTime / transitionDuration) : 0f;

            if (hasReachedTarget)
            {
                transitionTime += Time.deltaTime;
            }
            else
            {
                transitionTime = 0f;
            }

            // Rope parameters
            int radialSegments = 8; // Number of circular segments
            int quality = ModOptions.quality;
            float baseRadius = 0.012f; // Base radius for the rope
            float taperingFactor = 0.006f; // Tapering radius toward the end
            float sagMagnitude = Mathf.Lerp(0.08f, 0.02f, uncoilFactor); // Reduce sag as transition progresses
            float rotationSpeed = hasReachedTarget ? 0f : 3f; // Coiling stops after transition

            // Initialize arrays
            int vertexCount = (quality + 1) * (radialSegments + 1);
            int triangleCount = quality * radialSegments * 6;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[triangleCount];
            var uvs = new Vector2[vertexCount];

            // Directions
            Vector3 grappleStartPoint = swingingHandle.flyDirRef.transform.position;
            Vector3 direction = (webHitSpot - grappleStartPoint).normalized;
            Vector3 upDirection = Quaternion.LookRotation(direction) * Vector3.up;

            // Generate vertices and triangles
            int vertexIndex = 0;
            int triangleIndex = 0;

            for (int i = 0; i <= quality; i++)
            {
                float delta = i / (float)quality;

                // Position along the rope
                Vector3 segmentPosition = Vector3.Lerp(grappleStartPoint, currentWebPosition, delta);

                // Direction-based sag
                float sagFactor = Mathf.Lerp(0.8f, 1f, delta); // Stiffness fades along the length
                float sag = Mathf.Sin(delta * Mathf.PI) * sagMagnitude * sagFactor;
                segmentPosition += upDirection * sag;

                // Radius tapering
                float radius = Mathf.Lerp(baseRadius, taperingFactor, delta);

                // Rotational coiling (diminishes during transition)
                float coilRotation = Mathf.Lerp(rotationSpeed * delta * 360f, 0f, uncoilFactor);
                Quaternion coilQuaternion = Quaternion.AngleAxis(coilRotation, direction);

                for (int j = 0; j <= radialSegments; j++)
                {
                    float angle = (j / (float)radialSegments) * Mathf.PI * 2;
                    Vector3 radialOffset = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, direction) * upDirection * radius;

                    // Apply coiling rotation
                    radialOffset = coilQuaternion * radialOffset;

                    // Vertex position
                    vertices[vertexIndex] = segmentPosition + radialOffset;

                    // UV mapping
                    uvs[vertexIndex] = new Vector2(j / (float)radialSegments, delta);

                    // Triangles
                    if (i < quality && j < radialSegments)
                    {
                        int nextSegment = vertexIndex + radialSegments + 1;

                        triangles[triangleIndex++] = vertexIndex;
                        triangles[triangleIndex++] = nextSegment;
                        triangles[triangleIndex++] = vertexIndex + 1;

                        triangles[triangleIndex++] = vertexIndex + 1;
                        triangles[triangleIndex++] = nextSegment;
                        triangles[triangleIndex++] = nextSegment + 1;
                    }

                    vertexIndex++;
                }
            }

            // Update the mesh
            webMesh.Clear();
            webMesh.vertices = vertices;
            webMesh.triangles = triangles;
            webMesh.uv = uvs;
            webMesh.RecalculateNormals();

            // Reset the state when animation finishes
            if (hasReachedTarget && uncoilFactor >= 1f)
            {
                isSwinging = false; // Ready for the next swing
            }
        }
        
       

        private void Update()
        {
            if (this.swinging)
            {
                DrawRopeMesh();
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
                                                   (webHitSpot - this.swingingHandle.transform.position).normalized *
                                                   0.3f;
                    nextHandle.transform.rotation = Quaternion.LookRotation((webHitSpot - swingingHandle.transform.position));
                }
            }
        }
    }
}