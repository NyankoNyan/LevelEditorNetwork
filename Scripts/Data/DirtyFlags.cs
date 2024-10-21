namespace LevelNet.Data
{
    public class DirtyFlags : IDirtyCollection
    {
        public bool IsDirty => _isDirty;

        private DirtyStruct _root;
        private bool _isDirty;

        public DirtyFlags(object data)
        {
            _root = new(data.GetType());
            _root.Init(data);
        }

        public void RejectChanges() => _root.RejectChanges();

        public void ApplyChanges() => _root.ApplyChanges();

        internal DirtyStruct Root => _root;

        internal void SetDirty()
        {
            _isDirty = true;
        }
    }
}
