using Spider_Man.Management;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter
{
    public class SnapCheck : MonoBehaviour
    {
        private float snapDistance = 0.1f;
        private Item item;
        private RagdollHand hand;
        private bool leftTriggerSet = false;
        private WsRagdollHand triggerLEft;
        private bool rightTriggerSet = false;
        private WsRagdollHand triggerRight;
        private ManageAutoAlignment autoAlign;
        private void Start()
        {
            item = GetComponent<Item>();
            item.OnUngrabEvent += unGrabEvent;
        }

        private void OnDestroy()
        {
            item.OnUngrabEvent -= unGrabEvent;
        }

        private void unGrabEvent(Handle handle, RagdollHand ragdollhand, bool throwing)
        {
            if (ManageAutoAlignment.local.right.hand.Equals(ragdollhand.otherHand) &&
                ManageAutoAlignment.local.right.itemAttached) return;
            
            if (ManageAutoAlignment.local.left.hand.Equals(ragdollhand.otherHand) &&
                ManageAutoAlignment.local.left.itemAttached) return;
            
            if (Vector3.Distance(item.transform.position, ragdollhand.otherHand.transform.position) <= snapDistance)
            {
                Snap(ragdollhand.otherHand, item);
            }
        }

        public void Snap(RagdollHand hand, Item item)
        {
            switch (hand.side)
            {
                case Side.Right:
                    ManageAutoAlignment.local.right.ActivateItem(item);
                    ManageAutoAlignment.local.right.itemAttached = true; 
                    ManageAutoAlignment.local.right.item.DisallowDespawn = true;
                    Player.fallDamage = false;
                    GameManager.SetFreeClimb(true);
                    hand.caster.DisableSpellWheel(this);
                    hand.caster.telekinesis.Disable(ManageAutoAlignment.local.right);
                    break;
                case Side.Left:
                    ManageAutoAlignment.local.left.ActivateItem(item);
                    ManageAutoAlignment.local.left.itemAttached = true;
                    ManageAutoAlignment.local.left.item.DisallowDespawn = true;
                    Player.fallDamage = false;
                    GameManager.SetFreeClimb(true);
                    hand.caster.DisableSpellWheel(this);
                    hand.caster.telekinesis.Disable(ManageAutoAlignment.local.left);
                    break;
            }

            item.colliderGroups[0].colliders[0].enabled = false;
            item.GetMainHandle(hand.side).SetTouchPersistent(false);
            if (hand.otherHand.side == Side.Left)
                item.GetMainHandle(hand.side).allowedHandSide = Interactable.HandSide.Left;
            if (hand.otherHand.side == Side.Right)
                item.GetMainHandle(hand.side).allowedHandSide = Interactable.HandSide.Right;
            item.physicBody.isKinematic = true;
            //up and down -> up is negative down is positive
            item.transform.position = hand.transform.position + (-hand.transform.forward * 0.03f);
            //Forward and backwards -> Postive is backwards - is forwards
            item.transform.position = item.transform.position;//+ (-hand.transform.right * 0.01f);
            //Left and right
            if(hand.side == Side.Left) item.transform.position += (hand.transform.up * 0.0051f);
            if(hand.side == Side.Right) item.transform.position += (-hand.transform.up * 0.005f);
            item.transform.rotation = hand.transform.rotation;
            if(hand.side == Side.Right) item.transform.Rotate(Vector3.right, 85);
            if(hand.side == Side.Left) item.transform.Rotate(Vector3.right, 90);
            if(hand.side == Side.Right)item.transform.Rotate(Vector3.down, 90);
            if(hand.side == Side.Left)item.transform.Rotate(Vector3.down, 90);
            item.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
            item.transform.parent = hand.transform;
        }
    }
}