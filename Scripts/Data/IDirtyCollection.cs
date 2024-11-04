namespace LevelNet.Data
{
    public interface IDirtyCollection
    {
        void RejectChanges();

        void ApplyChanges();

        float GetDirtnessRatio();
    }
}
