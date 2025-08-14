using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Spider_Man.Management
{
    public class VFXPool : ThunderScript
    {
        public static VFXPool local;
        public Queue<GameObject> webSplatVfxPool = new Queue<GameObject>();

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            if (local == null)
            {
                local = this;
            }
        }
        public Queue<GameObject> GetPoolByReference(String reference)
        {
            switch (reference)
            {
                case "WebSplat":
                    return webSplatVfxPool;
                default:
                    return null;
            }
        }

        public void ClearVfxPools()
        {
            foreach (var vfx in webSplatVfxPool)
            {
                Catalog.ReleaseAsset(vfx);
            }
            
        }
    }
}