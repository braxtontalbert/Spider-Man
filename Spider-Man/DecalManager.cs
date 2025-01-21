using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man
{
    public class DecalManager : ThunderScript
    {
        public static Queue<GameObject> decalQueue = new Queue<GameObject>();

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();
            if (decalQueue.Count >= 10)
            {
                GameObject.Destroy(decalQueue.Dequeue());
            }
        }
    }
}