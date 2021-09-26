using System;
using System.Collections.Generic;
using GraphConnectEngine.Core;

namespace GraphConnectEngine.Node
{
    /// <summary>
    /// NodeBase同士がどのように繋がっているか管理するためのもの
    /// 
    /// ConnectNode => IsAttachableGraphType => CanAttach => Register => OnConnect
    /// </summary>
    public class NodeConnector
    {

        public EventHandler<NodeConnectEventArgs> OnConnect;
        
        public EventHandler<NodeConnectEventArgs> OnDisonnect;

        ///
        private readonly Dictionary<NodeBase, List<NodeBase>> _dict = new Dictionary<NodeBase, List<NodeBase>>();

        /// <summary>
        /// 繋がれているノードをキャストして取得する
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] GetOtherNodes<T>(NodeBase key)
        {
            if (!_dict.ContainsKey(key))
                return Array.Empty<T>();
            
            var list = new List<T>();
            foreach (var resolver in _dict[key])
                if (resolver is T t)
                    list.Add(t);

            return  list.ToArray();
        }
        /// <summary>
        /// 繋がれているノードを取得する
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public NodeBase[] GetOtherNodes(NodeBase key)
        {
            return _dict.ContainsKey(key) ? _dict[key].ToArray() : new NodeBase[0];
        }

        /// <summary>
        /// 繋がれているノードをキャストして取得する
        /// 繋がれていない場合はfalseを返す
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetOtherNodes<T>(NodeBase key, out T[] result)
        {
            result = null;
            
            if (!_dict.ContainsKey(key))
                return false;

            var list = new List<T>();
            foreach (var resolver in _dict[key])
                if (resolver is T t)
                    list.Add(t);

            result = list.ToArray();
            return list.Count != 0;
        }
        
        /// <summary>
        /// 繋がれているノードを取得する
        /// 繋がれていない場合はfalseを返す
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetOtherNodes(NodeBase key, out NodeBase[] result)
        {
            result = _dict.ContainsKey(key) ? _dict[key].ToArray() : null;
            return result != null;
        }

        /// <summary>
        /// 繋がれているノードをキャストして取得する
        /// 繋がれていない場合はfalseを返す
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetAnotherNode<T>(NodeBase key, out T result) where T : NodeBase
        {
            result = _dict.ContainsKey(key) ? _dict[key][0] as T: null;
            return result != null;
        }
        
        /// <summary>
        /// 繋がれているノードを取得する
        /// 繋がれていない場合はfalseを返す
        /// </summary>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetAnotherNode(NodeBase key, out NodeBase result)
        {
            result = _dict.ContainsKey(key) ? _dict[key][0] : null;
            return result != null;
        }

        /// <summary>
        /// node1にnode2を繋ぐ(node1のみ)
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        private void Register(NodeBase node1, NodeBase node2)
        {
            if (!_dict.ContainsKey(node1))
            {
                _dict[node1] = new List<NodeBase>();
            }

            _dict[node1].Add(node2);
        }

        /// <summary>
        /// node1からnode2を切り離す(node1のみ)
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        private void Remove(NodeBase node1, NodeBase node2)
        {
            if (_dict.ContainsKey(node1))
            {
                _dict[node1].Remove(node2);
                if (_dict[node1].Count == 0)
                {
                    _dict.Remove(node1);
                }
            }
        }

        /// <summary>
        /// node1がnode2と繋がっているかチェック(node1のみ)
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <returns></returns>
        public bool IsConnected(NodeBase node1, NodeBase node2)
        {
            if (_dict.ContainsKey(node1))
            {
                return _dict[node1].Contains(node2);
            }
            
            return false;
        }

        /// <summary>
        /// ノードとノードを繋ぐ
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <returns></returns>
        public bool ConnectNode(NodeBase node1, NodeBase node2)
        {
            Logger.Debug("[NodeConnector] Connect Node-------------------------");
            DumpNode(node1);
            DumpNode(node2);

            if (node1.Connector != this || node2.Connector != this)
            {
                Logger.Debug("[NodeConnector] Connector is not current Connector.");
                return false;
            }
            
            //接続チェック
            if (IsConnected(node1, node2) || IsConnected(node2,node1))
            {
                Logger.Debug("[NodeConnector] Already connected.");
                return false;
            }

            //つなげるResolverか確認する
            if (!node1.IsAttachableGraphType(node2.GetType()) || !node2.IsAttachableGraphType(node1.GetType()))
            {
                Logger.Debug("[NodeConnector] Node is not attachable Graph type.");
                return false;
            }

            //つながるかどうか確認
            if (!node1.CanAttach(node2) || !node2.CanAttach(node1))
            {
                Logger.Debug("[NodeConnector] Node is not attachable.");
                return false;
            }
            
            //接続
            Register(node1,node2);
            Register(node2,node1);
            
            Logger.Debug("[NodeConnector] Registered.");
            
            //TODO 下のイベントたちもsenderをつける
            OnConnect?.Invoke(node1,new NodeConnectEventArgs()
            {
                SenderNode = node1,
                OtherNode = node2
            });
            
            //イベント発火
            node1.InvokeConnectEvent(new NodeConnectEventArgs()
            {
                SenderNode = node1,
                OtherNode = node2
            });
            
            node2.InvokeConnectEvent(new NodeConnectEventArgs()
            {
                SenderNode = node2,
                OtherNode = node1
            });

            Logger.Debug("[NodeConnector] Called Connected Events.");
            return true;
        }

        /// <summary>
        /// ノードを切断する
        /// </summary>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <returns></returns>
        public bool DisconnectNode(NodeBase node1, NodeBase node2)
        {

            if (node1.Connector != this || node2.Connector != this)
            {
                return false;
            }
            
            //接続確認
            if (!IsConnected(node1, node2) && !IsConnected(node2, node1))
            {
                return false;
            }
            
            //接続を切る
            Remove(node1,node2);
            Remove(node2,node1);
            
            OnDisonnect?.Invoke(node1, new NodeConnectEventArgs()
            {
                SenderNode = node1,
                OtherNode = node2
            });
            
            //イベント発火
            node1.InvokeDisconnectEvent(new NodeConnectEventArgs()
            {
                SenderNode = node1,
                OtherNode = node2
            });
            
            node2.InvokeDisconnectEvent(new NodeConnectEventArgs()
            {
                SenderNode = node2,
                OtherNode = node1
            });
            
            return true;
        }

        public bool DisconnectAllNode(NodeBase node)
        {
            if (node == null)
            {
                return false;
            }
            
            if (node.Connector != this)
            {
                return false;
            }

            var onodes = GetOtherNodes(node);
            foreach (var other in onodes)
            {
                if (other == null)
                    continue;
                DisconnectNode(other, node);
            }

            _dict.Remove(node);

            return true;
        }

        public void DumpNode(NodeBase node)
        {
            Logger.Debug($"[NodeConnector] Dump of {node}");
            Logger.Debug($"Type : {node.GetType().FullName}");
            Logger.Debug($"Graph : {node.ParentGraph}");

            if (node.ParentGraph != null)
            {
                Logger.Debug($"Connector : {node.Connector}");
            }
            else
            {
                Logger.Debug($"Connector : Null");
            }

            if (node is InItemNode || node is OutItemNode)
            {
                Logger.Debug($"ItemType : ${((IItemTypeResolver)node).GetItemType().FullName}");
            }
        }
    }
}