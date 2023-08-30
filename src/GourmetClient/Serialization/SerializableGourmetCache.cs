namespace GourmetClient.Serialization
{
    using System;
    using Model;

    internal class SerializableGourmetCache
    {
        public SerializableGourmetCache()
        {
            // Used for deserialization
            Timestamp = DateTime.MinValue;
        }

        public SerializableGourmetCache(GourmetCache menuCache)
        {
            menuCache = menuCache ?? throw new ArgumentNullException(nameof(menuCache));

            Version = 1;
            Timestamp = menuCache.Timestamp;
            Menu = new SerializableGourmetMenu(menuCache.Menu);
            OrderedMenu = new SerializableOrderedGourmetMenu(menuCache.OrderedMenu);

            if (menuCache.UserData != null)
            {
                UserData = new SerializableGourmetUserData(menuCache.UserData);
            }
        }

        public int? Version { get; set; }

        public DateTime Timestamp { get; set; }

        public SerializableGourmetUserData UserData { get; set; }

        public SerializableGourmetMenu Menu { get; set; }

        public SerializableOrderedGourmetMenu OrderedMenu { get; set; }

        public GourmetCache ToGourmetMenuCache()
        {
            if (Version is not 1)
            {
                throw new InvalidOperationException($"Unsupported version of serialized data: {Version}");
            }

            return new GourmetCache(Timestamp, UserData?.ToGourmetUserData(), Menu?.ToGourmetMenu(), OrderedMenu?.ToOrderedGourmetMenu());
        }
    }
}
