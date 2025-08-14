using ThunderRoad;

namespace Spider_Man.Webshooter
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