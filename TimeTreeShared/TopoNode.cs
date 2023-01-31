using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace TimeTreeShared
{
    public class TopoNode : ExtendedNode
    {
        protected TopoNode(SerializationInfo info, StreamingContext ctx) : base(info, ctx)
        {
            UniqueID = info.GetInt32("UniqueID");
            PartitionData = (SplitData)info.GetValue("PartitionData", typeof(SplitData));
            RepresentsPhylum = info.GetString("RepresentsPhylum");
            RepresentsClass = info.GetString("RepresentsClass");
            RepresentsOrder = info.GetString("RepresentsOrder");
            RepresentsFamily = info.GetString("RepresentsFamily");
            RepresentsGenus = info.GetString("RepresentsGenus");
        }
    }
}
