using GraphConnectEngine.Core;
using GraphConnectEngine.Node;

namespace GraphConnectEngine.Graph.Value
{
    public class ValueFuncGraph<T> : GraphBase
    {
        public delegate bool ValueFunc(out T result);
        
        private ValueFunc _valueFunc;

        public ValueFuncGraph(NodeConnector connector, ValueFunc valueFunc) : base(connector)
        {
            _valueFunc = valueFunc;
            AddNode(new OutItemNode(this, typeof(T), 0,"Value"));
        }

        protected override bool OnProcessCall(ProcessCallArgs args, out object[] results, out OutProcessNode nextNode)
        {
            if (!_valueFunc(out T result))
            {
                results = null;
                nextNode = null;
                return false;
            }

            results = new object[] {result};
            nextNode = OutProcessNode;
            return true;
        }

        public override string GetGraphName() => "ValueFunc<" + typeof(T).Name + "> Graph";
    }
}