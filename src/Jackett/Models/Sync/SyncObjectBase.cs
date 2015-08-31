using Jackett.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Sync
{
    public abstract class SyncObjectBase
    {
        uint id { get; set; }
        Dictionary<string, object> lastSync = new Dictionary<string, object>();

        public uint Id
        {
            get { return id; }
            set { id = value; }
        }

        public List<Change> Sync(IObjectSyncService s, List<Change> changeList = null, bool deep = false, bool includeNonChanges = false)
        {
            if (id == 0)
            {
                s.Register(this);
            }

            if (changeList == null)
            {
                changeList = new List<Change>();
            }

            var changes = new Dictionary<string, object>();
            foreach(var property in GetType().GetProperties()){
                var value = property.GetValue(this);
                var name = property.Name;

                if (name == "Id")
                    continue;

                if (value is IList)
                {
                    var subItems = new List<ObjectPointer>();
                    foreach(var item in value as IList)
                    {
                        var subModel = item as SyncObjectBase;
                        if (deep && subModel != null)
                        {
                            subModel.Sync(s, changeList, deep, includeNonChanges);
                            subItems.Add(new ObjectPointer() { Id = subModel.Id });
                        }
                    }

                    changes[name] = subItems;
                }
                else if(property.PropertyType.IsPrimitive || value is string)
                {
                    object prevValue = null;
                    if (lastSync.ContainsKey(name))
                    {
                        prevValue = lastSync[name];
                    }

                    if (includeNonChanges)
                    {
                        changes[name] = value;
                    }
                    else
                    {
                        if (Id == 0 || value != prevValue)
                        {
                            lastSync[name] = value;
                            changes[name] = value;
                        }
                    }
                } else if (value is object)
                {
                    var subModel = value as SyncObjectBase;
                    if (deep && subModel != null)
                    {
                        subModel.Sync(s, changeList, deep, includeNonChanges);
                        changes[name] = new ObjectPointer() { Id = subModel.Id };
                    }
                }
            }

            if (changes.Count > 0)
            {

                changeList.Add(new Change()
                {
                    Id = id,
                    Properties = changes
                });
            }

            return changeList;
        }

        public List<Change> GetData(IObjectSyncService s)
        {
            return Sync(s, null, true, true);
        }
    }
}
