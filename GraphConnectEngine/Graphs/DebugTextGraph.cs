using System;
using System.Threading.Tasks;
using GraphConnectEngine.Core;
using GraphConnectEngine.Node;

namespace GraphConnectEngine.Graphs
{
    public class DebugTextGraph : Core.Graph
    {
        
        private Func<string, Task<bool>> _updateText;

        public DebugTextGraph(NodeConnector connector,Func<string, Task<bool>> updateText) : base(connector)
        {
            AddNode(new InItemNode(this, typeof(object),"Object"));
            AddNode(new OutItemNode(this, typeof(string),1,"Text"));
            _updateText = updateText;
        }

        public override async Task<ProcessCallResult> OnProcessCall(ProcessCallArgs args, object[] parameters)
        {
            var obj = parameters[0];
            var str = obj.ToString();
            
            //実行
            if (!await _updateText(str))
                return ProcessCallResult.Fail();

            return ProcessCallResult.Success(new []
            {
                obj,
                str
            }, OutProcessNode);
        }

        public override string GetGraphName() => "Debug Text Graph";
    }
}