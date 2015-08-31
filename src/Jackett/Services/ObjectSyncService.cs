using Jackett.Models.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IObjectSyncService
    {
        void Register(SyncObjectBase item);
        void Sync(SyncObjectBase item);
        void Announce(List<Change> c);
        void Announce(Change c);
        StateRoot Root { get; }
    }

    public class ObjectSyncService : IObjectSyncService
    {
        const int DATASTORE_STATE_INCREMENT = 100000;
        uint nextId = 0;
        SyncObjectBase[] objects = new SyncObjectBase[DATASTORE_STATE_INCREMENT];
        object syncLock = new object();
        StateRoot root = new StateRoot();

        public ObjectSyncService()
        {
            root.Networks.Add(new Models.Irc.Network()
            {
                Address = "a",
                Name = "Test"
            });
        }

        public StateRoot Root
        {
            get { return root; }
        }

        public void Register(SyncObjectBase item)
        {
            if (item.Id == 0)
            {
                lock (syncLock)
                {
                    item.Id = ++nextId;

                    if (item.Id > objects.Length + 1)
                    {
                        // Extend known object array
                        var newArray = new SyncObjectBase[objects.Length + DATASTORE_STATE_INCREMENT];
                        for(int i = 0; i < objects.Length; i++)
                        {
                            newArray[i] = objects[i];
                        }
                        objects = newArray;
                    }

                    objects[item.Id] = item;
                }
            }
        }

        public void Sync(SyncObjectBase item)
        {
            if (item.Id == 0)
            {
                Register(item);
            }

            Announce(item.Sync(this));
        }

        public void Announce(List<Change> c)
        {

        }

        public void Announce(Change c)
        {
            Announce(new List<Change>() { c });
        }
    }
}
