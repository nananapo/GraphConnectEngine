using System.Reflection;
using GraphConnectEngine.Core;
using GraphConnectEngine.Node;

namespace GraphConnectEngine.Graph.Generative
{
    public abstract class GenerativeGraph : GraphBase
    {

        public MethodInfo MethodInfo { get; }

        protected ParameterInfo[] Parameters;

        public GenerativeGraph(NodeConnector connector, MethodInfo methodInfo) : base(connector)
        {
            if (methodInfo == null || !methodInfo.IsPublic)
                return;//TODO Exception
            
            MethodInfo = methodInfo;

            //Return Node
            AddNode(new OutItemNode(this, MethodInfo.ReturnType, 0,"Result"));
            
            //Parameter
            Parameters = MethodInfo.GetParameters();
            for(int i=0;i<Parameters.Length;i++)
            {
                ParameterInfo parameterInfo = Parameters[i];
                InItemNode iNode = new InItemNode(this, parameterInfo.ParameterType,parameterInfo.Name);
                AddNode(iNode);
                
                OutItemNode oNode = new OutItemNode(this, parameterInfo.ParameterType, i+1,parameterInfo.Name);
                AddNode(oNode);
            }
        }
        
        /// <summary>
        /// パラメーターにあたるノードからあたいをとってくる
        /// </summary>
        /// <param name="args"></param>
        /// <param name="results">結果</param>
        /// <returns>成功したかどうか</returns>
        private bool TryGetParameterValues(ProcessCallArgs args,out object[] results)
        {
            object[] param = new object[Parameters.Length];
            
            for (int i = 0; i < Parameters.Length; i++)
            {
                ParameterInfo parameterInfo = Parameters[i];
                if (InItemNodes[i].GetItemFromConnectedNode(args, out object oitem))
                {
                    param[i] = oitem;
                    continue;
                }
                
                if (parameterInfo.HasDefaultValue)
                {
                    param[i] = parameterInfo.DefaultValue;
                }
                else
                {
                    results = null;
                    return false;
                }
            }

            results = param;
            return true;
        }

        protected override bool OnProcessCall(ProcessCallArgs args, out object[] results, out OutProcessNode nextNode)
        {
            if (!TryGetParameterValues(args, out var param))
            {
                results = null;
                nextNode = null;
                return false;
            }
            
            var procResult = InvokeMethod(args,param,out var invokeResult);
           
            results = new object[param.Length + 1];
            results[0] = invokeResult;
            for (int i = 0; i < param.Length; i++)
            {
                results[i + 1] = param[i];
            }
            nextNode = OutProcessNode;
            
            return procResult;
        }

        /// <summary>
        /// 実行する
        /// </summary>
        /// <returns></returns>
        protected abstract bool InvokeMethod(ProcessCallArgs args,object[] param,out object result);

    }
}