using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Webshooter.Gadgets
{
    public interface IGadget
    {
        string Name { get; set; }
        int PressCount { get; set; }
        Coroutine Coroutine { get; set; }
        Item Item { get; set; }
        bool ItemAttached { get; set; }
        RagdollHand Hand { get; set; }
        void Activate(Item item, RagdollHand hand, ref bool itemAttached);
        IEnumerator WaitWindow(IGadget gadget);
        bool DisallowItemGrab { get; set; }
    }
}