using System.Collections.Generic;

namespace ECSLite
{
    public class Feature
    {
        private readonly List<IInitializeSystem> initializes = new List<IInitializeSystem>();
        private readonly List<IUpdateSystem> updates = new List<IUpdateSystem>();
        private readonly List<ICleanupSystem> cleanups = new List<ICleanupSystem>();
        private readonly List<ITearDownSystem> tearDowns = new List<ITearDownSystem>();

        protected void AddSystem(ISystem system)
        {
            if (system is IInitializeSystem initialize)
            {
                initializes.Add(initialize);
            }
            if (system is IUpdateSystem update)
            {
                updates.Add(update);
            }
            if (system is ICleanupSystem cleanup)
            {
                cleanups.Add(cleanup);
            }
            if (system is ITearDownSystem tearDown)
            {
                tearDowns.Add(tearDown);
            }
        }
        public void OnInitialize()
        {
            foreach (var sys in initializes)
            {
                sys.OnInitialize();
            }
        }

        public void OnUpdate()
        {
            foreach (var sys in updates)
            {
                sys.OnUpdate();
            }
        }

        public void OnCleanup()
        {
            foreach (var sys in cleanups)
            {
                sys.OnCleanup();
            }
        }

        public void OnDestroy()
        {
            foreach (var sys in tearDowns)
            {
                sys.OnTearDown();
            }
        }
    }
}
