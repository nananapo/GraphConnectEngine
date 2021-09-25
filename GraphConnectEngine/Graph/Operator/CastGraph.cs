using System;
using GraphConnectEngine.Core;
using GraphConnectEngine.Node;

namespace GraphConnectEngine.Graph.Operator
{
    public class CastGraph<T> : GraphBase
    {

        private Func<object, object> _lambda;
        
        public CastGraph(NodeConnector connector) : base(connector)
        {
            AddNode(new InItemNode(this,typeof(object),"Object"));
            AddNode(new OutItemNode(this,typeof(T),0,"Object"));

            var t = typeof(T);
            if (t == typeof(bool))
            {
                _lambda = o => Convert.ToBoolean(0);
            }
            else if (t == typeof(byte))
            {
                _lambda = o => Convert.ToByte(o);
            }
            else if (t == typeof(char))
            {
                _lambda = o => Convert.ToChar(o);
            }
            else if (t == typeof(decimal))
            {
                _lambda = o => Convert.ToDecimal(o);
            }
            else if (t == typeof(double))
            {
                _lambda = o => Convert.ToDouble(o);
            }
            else if (t == typeof(string))
            {
                _lambda = o => o.ToString();
            }
            else if (t == typeof(int))
            {
                _lambda = o => Convert.ToInt32(o);
            }
            else if (t == typeof(float))
            {
                _lambda = o => Convert.ToSingle(o);
            }
            else if (t == typeof(DateTime))
            {
                _lambda = o => Convert.ToDateTime(o);
            }
            else
            {
                //TODO 他のConvert対応のやつも追加する
                _lambda = o => (T) o;
            }
        }

        protected override bool OnProcessCall(ProcessCallArgs args, out object[] results, out OutProcessNode nextNode)
        {
            if (!InItemNodes[0].GetItemFromConnectedNode(args, out object value))
            {
                Logger.Debug("AAA");
                results = null;
                nextNode = null;
                return false;
            }

            // cast
            T a;
            bool isSuccess = false;
            
            try
            {
                var r = _lambda(value);
                if (r is T t)
                {
                    a = t;
                    isSuccess = true;
                }
                else
                {
                    a = default;
                }
            }
            catch (InvalidCastException)
            {
                a = default;
            }

            if (isSuccess)
            {
                results = new object[] {a};
                nextNode = OutProcessNode;
                return true;
            }
            else
            {
                results = null;
                nextNode = null;
                return false;
            }

        }

        public override string GetGraphName() => "CastGraph<"+typeof(T).Name+">";
    }
}