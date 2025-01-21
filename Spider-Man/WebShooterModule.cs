using ThunderRoad;
using UnityEngine;
namespace Spider_Man
{
    public class WebShooterModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<SnapCheck>();
        }
    }
}