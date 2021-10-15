using System.Threading.Tasks;
using GraphConnectEngine.Node;

namespace GraphConnectEngine.Graphs.Value
{
    /// <summary>
    /// OutItemNode[T]が1つあるグラフ
    /// コンストラクタで値のデフォルト値を設定できる。
    /// 実行時はValueをOutItemNode[0]に渡す
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueGraph<T> : Graph
    {
        /// <summary>
        /// 実行時に返される値
        /// </summary>
        public T Value;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="defaultValue">Tのデフォルト値</param>
        public ValueGraph(NodeConnector connector,T defaultValue) : base(connector)
        {
            Value = defaultValue;
            AddNode(new OutItemNode(this, typeof(T), 0,"Value"));
        }

        public override Task<ProcessCallResult> OnProcessCall(ProcessCallArgs args, object[] parameters)
        {
            return Task.FromResult(ProcessCallResult.Success(new object[]{Value},OutProcessNode));
        }

        public override string GetGraphName() => "Value<" + typeof(T).Name + "> Graph";
    }
}