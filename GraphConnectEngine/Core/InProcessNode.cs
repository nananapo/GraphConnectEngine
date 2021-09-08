using System;

namespace GraphConnectEngine.Core
{
    public class InProcessNode : GraphParentResolver
    {


        public InProcessNode(GraphBase parentGraph, NodeConnector connector) : base(parentGraph,connector) 
        {
        }

        public void OnCalled(object sender, ProcessCallArgs args)
        {
            ParentGraph.Invoke(sender, args, out var results);
        }

        public override bool IsAttachableGraphType(Type type)
        {
            var dt = typeof(OutProcessNode);
            return !(type != dt && !type.IsSubclassOf(dt));
        }

        public override bool CanAttach(GraphParentResolver resolver)
        {
            if (resolver is OutProcessNode outNode)
            {
                return Connector.GetOtherNodes(this).Length == 0;
            }
            return false;
        }
    }
}