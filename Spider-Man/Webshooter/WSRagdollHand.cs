using Common;
using Spider_Man.Webshooter.Gadgets;
using Spider_Man.Webshooter.Gadgets.WebBall;
using ThunderRoad;
using UnityEngine;
using Pointer = ThunderRoad.Pointer;

namespace Spider_Man.Webshooter
{
    public class WsRagdollHand : MonoBehaviour
    {

        private static readonly string ThwipHandleString = "ThwipClose"; //has a
        public RagdollHand hand; // has a
        public Item item; // has a 
        public bool itemAttached;
        private RagdollHand originalHand;
        public Item swingingHandle;
        public bool activated = true;
        public WsRagdollHand otherHandMono;
        private AudioSource alternateSfx;
        private HandPoseData ThwipHandlePose { get; set; } // has a
        
        private CycleList<IGadget> gadgets; // has a
        public SwingGadget swing; // has a

        private void Start()
        {
            originalHand = hand;
            swing = new GameObject().AddComponent<SwingGadget>();
            ThwipHandlePose = Catalog.GetData<HandPoseData>(ThwipHandleString + hand.side.ToString());
        }
        
        void InstantiateGadgets()
        {
            gadgets = new CycleList<IGadget>();
            var webBallGadget = new GameObject().AddComponent<WebBallGadget>();
            gadgets.Add(webBallGadget);
            var impactWebGadget = new GameObject().AddComponent<ImpactWebGadget>();
            gadgets.Add(impactWebGadget);
        }

        void InstantiateSFX()
        {
            if (!alternateSfx)
            {
                Catalog.InstantiateAsync("alternateGadget", this.item.transform.position, this.item.transform.rotation,
                    this.item.gameObject.transform,
                    go => { alternateSfx = go.GetComponent<AudioSource>(); }, "AlternateSoundHandler");
            }
            else
            {
                alternateSfx.Play();
            }
        }

        public void ActivateHand(RagdollHand hand)
        {
            this.hand = hand;
            this.originalHand = hand;
        }
        public void ActivateItem(Item item)
        {
            this.item = item;
            item.OnGrabEvent += OnGrab;

            hand.poser.SetTargetPose(ThwipHandlePose, true, true, true, true, true);
            hand.playerHand.controlHand.OnButtonPressEvent += ButtonPressEvent;
            InstantiateSFX();
            InstantiateGadgets();
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

            if (hand.poser.targetHandPoseData.Equals(this.ThwipHandlePose)) hand.poser.ResetTargetPose();
        }

        void CheckDoubleTapPerGadget(IGadget currentGadget)
        {
            currentGadget.PressCount++;
            if (currentGadget.PressCount == 1)
            {
                currentGadget.Coroutine = StartCoroutine(currentGadget.WaitWindow());
            }
            else if (currentGadget.PressCount == 2)
            {
                if (currentGadget.Coroutine != null) StopCoroutine(currentGadget.Coroutine);
                currentGadget.Coroutine = null;
                currentGadget.PressCount = 0;
                currentGadget.Activate(this.item, this.hand, ref this.itemAttached);
            }
        }

        void CycleGadget()
        {
            if (gadgets.Count > 1)
            {
                gadgets.CycleLeft();
                alternateSfx.Play();
            }
        }

        private void ButtonPressEvent(PlayerControl.Hand.Button button, bool pressed)
        {
            if (button == PlayerControl.Hand.Button.Grip && activated && !this.hand.grabbedHandle &&
                this.itemAttached && this.item && pressed)
            {
               CheckDoubleTapPerGadget(swing);
            }

            if (button == PlayerControl.Hand.Button.Use && activated && !this.hand.grabbedHandle &&
                this.itemAttached && this.item && !swing.IsSwinging && pressed && !Pointer.GetActive().isPointingUI &&
                !hand.playerHand.isFist)
            {
                CheckDoubleTapPerGadget(gadgets[0]);
            }

            if (button == PlayerControl.Hand.Button.AlternateUse && !this.hand.grabbedHandle && this.itemAttached &&
                this.item && !swing.IsSwinging && pressed)
            {
                CycleGadget();
            }
        }
        private void OnGrab(Handle handle, RagdollHand ragdollhand)
        {
            if (this.itemAttached) UnSnap(this.item);
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
        
        private void OnDestroy()
        {
            this.item.OnGrabEvent -= OnGrab;
        }
    }
}