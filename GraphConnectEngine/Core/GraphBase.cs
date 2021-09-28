using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphConnectEngine.Node;

namespace GraphConnectEngine.Core
{
    public abstract class GraphBase : IProcessCall,IDisposable
    {

        public readonly NodeConnector Connector;

        /// <summary>
        /// Nodes
        /// </summary>
        public InProcessNode InProcessNode => InProcessNodes[0];
        public OutProcessNode OutProcessNode => OutProcessNodes[0];

        public readonly List<InProcessNode> InProcessNodes = new List<InProcessNode>();
        public readonly List<OutProcessNode> OutProcessNodes = new List<OutProcessNode>();
        public readonly List<InItemNode> InItemNodes = new List<InItemNode>();
        public readonly List<OutItemNode> OutItemNodes = new List<OutItemNode>();

        private Tuple<ProcessCallArgs, ProcessCallResult> _cache;

        public string UniqueId => GetHashCode().ToString();

        /// <summary>
        /// 実行ステータスのリス名
        /// </summary>
        public event EventHandler<GraphStatusEventArgs> OnStatusChanged;

        public GraphBase(NodeConnector connector)
        {
            Connector = connector;

            AddNode(new InProcessNode(this));
            AddNode(new OutProcessNode(this));
        }

        public async Task<InvokeResult> Invoke(object sender,ProcessCallArgs args)
        {
            string myHash = GetHashCode().ToString();
            string myName = $"{GetGraphName()}[{myHash}]";

            //イベント
            Logger.Debug($"{myName} is Invoked with\n{args}");
            OnStatusChanged?.Invoke(this,new GraphStatusEventArgs()
            {
                Type = GraphStatusEventArgs.EventType.InvokeCalled,
                Args = args
            });
            
            //キャッシュチェック
            if (_cache != null && args.CanGetItemOf(_cache.Item1))
            {
                //イベント
                Logger.Debug($"{myName} is Returning Cache\n{_cache.Item1} : {_cache.Item2}");
                OnStatusChanged?.Invoke(this, new GraphStatusEventArgs()
                {
                    Type = GraphStatusEventArgs.EventType.CacheUsed,
                    Args = args
                });

                return _cache.Item2.ToInvokeResult();
            }

            ProcessCallArgs nargs;

            if (IsConnectedInProcessNode())
            {
                if (sender is OutItemNode)
                {
                    //キャッシュがないとおかしい
                    //イベント
                    Logger.Debug($"{myName} is Returning NO Item : back with no cache");
                    OnStatusChanged?.Invoke(this, new GraphStatusEventArgs()
                    {
                        Type = GraphStatusEventArgs.EventType.CacheError,
                        Args = args
                    });

                    return InvokeResult.Fail();
                }

                //ループ検知
                if (!args.TryAdd(myHash, true, out nargs))
                {
                    //イベント
                    Logger.Debug($"{myName} invoke Failed : Loop detected");
                    OnStatusChanged?.Invoke(this, new GraphStatusEventArgs()
                    {
                        Type = GraphStatusEventArgs.EventType.LoopDetected,
                        Args = args
                    });
                    
                    return InvokeResult.Fail();
                }
            }
            else
            {
                if (sender is OutProcessNode)
                {
                    //イベント
                    //繋がってないのに呼ばれるってどういうこと？
                    Logger.Debug($"{myName} Unknown Error");
                    OnStatusChanged?.Invoke(this, new GraphStatusEventArgs()
                    {
                        Type = GraphStatusEventArgs.EventType.UnknownError,
                        Args = args
                    });

                    return InvokeResult.Fail();
                }
                
                //ループ検知
                if (!args.TryAdd(GetHashCode().ToString(), false, out nargs))
                {
                    //イベント
                    Logger.Debug($"{myName} is Returning NO Item : Loop detected");
                    OnStatusChanged?.Invoke(this, new GraphStatusEventArgs()
                    {
                        Type = GraphStatusEventArgs.EventType.LoopDetected,
                        Args = args
                    });
                    
                    return InvokeResult.Fail();
                }
            }
            
            //イベント
            Logger.Debug($"{myName} Invoke OnProcessCall in GraphBase with\n{nargs}");
            OnStatusChanged?.Invoke(this, new GraphStatusEventArgs()
            {
                Type = GraphStatusEventArgs.EventType.ProcessStart,
                Args = nargs
            });

            //Invoke
            ProcessCallResult procResult = await OnProcessCall(nargs);

            //キャッシュする
            _cache = new Tuple<ProcessCallArgs, ProcessCallResult>(nargs, procResult);

            //イベント
            Logger.Debug($"{myName} Invoked OnProcessCall with result {procResult}");
            OnStatusChanged?.Invoke(this, new GraphStatusEventArgs()
            {
                Type = procResult.IsSucceeded ? GraphStatusEventArgs.EventType.ProcessSuccess : GraphStatusEventArgs.EventType.ProcessFail,
                Args = nargs
            });
            
            //OutProcessなら実行する
            if (procResult.IsSucceeded && sender is OutProcessNode)
            {
                await procResult.NextNode.CallProcess(args);
            }

            return procResult.ToInvokeResult();
        }

        public abstract Task<ProcessCallResult> OnProcessCall(ProcessCallArgs args);
        
        /// <summary>
        /// グラフ名を取得する
        /// </summary>
        /// <returns></returns>
        public abstract string GetGraphName();

        protected void AddNode(InItemNode node)
        {
            InItemNodes.Add(node);
        }

        protected void AddNode(OutItemNode node)
        {
            OutItemNodes.Add(node);
        }

        protected void AddNode(InProcessNode node)
        {
            InProcessNodes.Add(node);
        }

        protected void AddNode(OutProcessNode node)
        {
            OutProcessNodes.Add(node);
        }

        /// <summary>
        /// InProcessNodeが繋がれているかどうかを確認する
        /// </summary>
        /// <returns></returns>
        public virtual bool IsConnectedInProcessNode()
        {
            return InProcessNode.Connector.TryGetAnotherNode(InProcessNode, out var _);
        }

        public void Dispose()
        {
            foreach (var n in InProcessNodes)
                n.Dispose();
            foreach (var n in OutProcessNodes)
                n.Dispose();
            foreach (var n in InItemNodes)
                n.Dispose();
            foreach (var n in OutItemNodes)
                n.Dispose();
        }

        public class InvokeResult
        {
            public bool IsSucceeded;
            public object[] Results;

            private InvokeResult()
            {
                
            }
            
            public static InvokeResult Create(bool isSucceeded,object[] results)
            {
                return new InvokeResult()
                {
                    IsSucceeded = isSucceeded,
                    Results = results
                };
            }

            public static InvokeResult Fail() => Create(false,null);

            public static InvokeResult Success(object[] results) => Create(true, results);
        }
    }

    public class GraphStatusEventArgs : EventArgs
    {
        public enum EventType
        {
            InvokeCalled,
            ProcessStart,
            ProcessSuccess,
            ProcessFail,
            CacheUsed,
            CacheError,
            LoopDetected,
            UnknownError
        }

        public EventType Type;
        public ProcessCallArgs Args;
    }
}