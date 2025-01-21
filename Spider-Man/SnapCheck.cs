using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man
{
    public class SnapCheck : MonoBehaviour
    {
        private float snapDistance = 0.1f;
        private Item item;
        private RagdollHand hand;
        private bool leftTriggerSet = false;
        private TriggerColliderMono triggerLEft;
        private bool rightTriggerSet = false;
        private TriggerColliderMono triggerRight;
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
            if (Vector3.Distance(item.transform.position, ragdollhand.otherHand.transform.position) <= snapDistance)
            {
                Snap(ragdollhand.otherHand, item);
            }
        }

        void Snap(RagdollHand hand, Item item)
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
                    break;
                case Side.Left:
                    ManageAutoAlignment.local.left.ActivateItem(item);
                    ManageAutoAlignment.local.left.itemAttached = true;
                    ManageAutoAlignment.local.left.item.DisallowDespawn = true;
                    Player.fallDamage = false;
                    GameManager.SetFreeClimb(true);
                    hand.caster.DisableSpellWheel(this);
                    break;
            }

            item.colliderGroups[0].colliders[0].enabled = false;
            item.GetMainHandle(hand.side).SetTouchPersistent(false);
            if (hand.otherHand.side == Side.Left)
                item.GetMainHandle(hand.side).allowedHandSide = Interactable.HandSide.Left;
            if (hand.otherHand.side == Side.Right)
                item.GetMainHandle(hand.side).allowedHandSide = Interactable.HandSide.Right;
            item.physicBody.isKinematic = true;
            item.transform.position = hand.transform.position + (-hand.transform.forward * 0.03f);
            item.transform.position = item.transform.position + (hand.transform.right * 0.01f);
            item.transform.rotation = hand.transform.rotation;
            item.transform.Rotate(Vector3.right, 90);
            item.transform.Rotate(Vector3.down, 90);
            item.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
            item.transform.parent = hand.transform;
        }
    }
}