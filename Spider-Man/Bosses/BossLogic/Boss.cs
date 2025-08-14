namespace Spider_Man.Bosses.BossLogic
{
    public interface IBoss
    {
        void InitializeBoss();
        void StartBossFight();
        void StopBossFight();
        void ChangePhase();
    }
}