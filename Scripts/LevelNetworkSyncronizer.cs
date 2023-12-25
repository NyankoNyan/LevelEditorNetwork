using UnityEngine;
using Unity.Netcode;
using LevelView;
using System.Collections.Generic;
using System;

namespace Level.API.Network
{

    public delegate void BlockProtoRequestCallback(IEnumerable<BlockProtoInfo> blockProtos);
    public delegate void BlockProtoModifyCallback(IEnumerable<string> errors);
    public delegate void GridSettingsRequestCallback(IEnumerable<GridSettingsInfo> gridSettings);
    public delegate void GridStateRequestCallback(IEnumerable<GridStateInfo> gridStates);

    public class NetcodeLevelSyncronizer : NetworkBehaviour, ILevelSyncronizer
    {
        private Dictionary<ulong, object> _waitResponces = new();
        private ulong _responceCounter = 0;

        private LevelStorage _levelStorage;

        void Awake()
        {
            _levelStorage = LevelStorage.Instance;
            if (_levelStorage == null) {
                throw new Exception();
            }
        }

        private ulong PushRequest(object callback)
        {
            _responceCounter++;
            _waitResponces.Add(_responceCounter, callback);
            return _responceCounter;
        }

        private object PopCallback(ulong callbackId)
        {
            object callback;
            if (_waitResponces.Remove(callbackId, out callback)) {
                return callback;
            } else {
                throw new LevelAPIException($"Unknown callback {callbackId}");
            }
        }

        #region Blocks
        #region Blocks: request current list
        public void GetAllBlockProtoRequest(BlockProtoRequestCallback callback)
        {
            GetAllBlockProtoServerRpc(PushRequest(callback));
        }

        [ServerRpc]
        private void GetAllBlockProtoServerRpc(ulong requestId, ServerRpcParams serverRpcParams = default)
        {
            GetAllBlockProtoClientRpc(
                _levelStorage.API.BlockProtoCollection.Select(x => (BlockProtoInfoNetSerialize)x.Info).ToArray(),
                requestId,
                NetworkTools.ResponceClient(serverRpcParams)
            );

        }

        [ClientRpc]
        private void GetAllBlockProtoClientRpc(BlockProtoInfoNetSerialize[] blockProtoInfoList, ulong requestId, ClientRpcParams clientRpcParams)
        {
            var callback = PopCallback(requestId) as BlockProtoRequestCallback;
            callback?.Invoke(blockProtoInfoList.Select(x => (BlockProtoInfo)x));
        }

        public void BlockProtosRequestAndAdd()
        {
            GetAllBlockProtoRequest(blockProtos => {
                foreach (var blockProto in blockProtos) {
                    _levelStorage.API.BlockProtoCollection.Add(blockProto.content, blockProto.id);
                }
            });
        }
        #endregion

        #region Blocks: change block's protos from server
        public void BlockProtoChangesSend(
            IEnumerable<BlockProtoInfo> changed = null,
            IEnumerable<BlockProtoInfo> added = null,
            uint[] removed = null)
        {
            var changes = new ContentChangeInfo<BlockProtoInfoNetSerialize>() {
                change = changed.Select(x => (BlockProtoInfoNetSerialize)x).ToArray(),
                add = added.Select(x => (BlockProtoInfoNetSerialize)x).ToArray()
            };
            BlockProtoChangesClientRpc(changes, removed);
        }

        [ClientRpc]
        private void BlockProtoChangesClientRpc(ContentChangeInfo<BlockProtoInfoNetSerialize> changes, uint[] removed)
        {
            var blockProtos = _levelStorage.API.BlockProtoCollection;

            if (removed != null) {
                foreach (var id in removed) {
                    blockProtos.Remove(id);
                }
            }
            if (changes.add != null) {
                foreach (var info in changes.add) {
                    blockProtos.Add(info.content, info.id);
                }
            }
            if (changes.change != null) {
                foreach (var info in changes.change) {
                    var blockProto = blockProtos[info.id];
                    blockProto.Settings = info.content;
                }
            }
        }
        #endregion

        #region Blocks: change block's protos from client

        public void BlockProtoModifyRequest(
            BlockProtoModifyCallback callback,
            IEnumerable<BlockProtoInfo> changed = null,
            IEnumerable<BlockProtoInfo> added = null,
            uint[] removed = null)
        {
            var changes = new ContentChangeInfo<BlockProtoInfoNetSerialize>() {
                change = changed.Select(x => (BlockProtoInfoNetSerialize)x).ToArray(),
                add = added.Select(x => (BlockProtoInfoNetSerialize)x).ToArray()
            };
            BlockProtoModifyServerRpc(PushRequest(callback), changes, removed);
        }

        [ServerRpc]
        private void BlockProtoModifyServerRpc(ulong requestId,
                                               ContentChangeInfo<BlockProtoInfoNetSerialize> changes,
                                               uint[] removed,
                                               ServerRpcParams serverRpcParams = default)
        {
            List<string> errors = new();

            List<uint> confirmRemove = null;
            if (removed != null) {
                confirmRemove = new();
                foreach (uint id in removed) {
                    try {
                        _levelStorage.API.BlockProtoCollection.Remove(id);
                    } catch (LevelAPIException e) {
                        errors.Add(e.Message);
                        continue;
                    }
                    confirmRemove.Add(id);
                }
            }

            List<BlockProtoInfo> confirmAdd = null;
            if (changes.add != null) {
                confirmAdd = new();
                foreach (BlockProtoInfo info in changes.add) {
                    BlockProto blockProto;
                    try {
                        blockProto = _levelStorage.API.BlockProtoCollection.Add(info.content);
                    } catch (LevelAPIException e) {
                        errors.Add(e.Message);
                        continue;
                    }

                    confirmAdd.Add(blockProto.Info);
                }
            }

            List<BlockProtoInfo> confirmChange = null;
            if (changes.change != null) {
                confirmChange = new();
                foreach (BlockProtoInfo info in changes.change) {
                    var blockProto = _levelStorage.API.BlockProtoCollection[info.id];
                    blockProto.Settings = info.content;
                }
            }
            BlockProtoChangesSend(confirmChange, confirmAdd, confirmRemove.ToArray());
            BlockProtoModifyResponceClientRpc(requestId, new StringListWrap(errors.ToArray()), NetworkTools.ResponceClient(serverRpcParams));
        }

        [ClientRpc]
        private void BlockProtoModifyResponceClientRpc(ulong requestId, StringListWrap errors, ClientRpcParams clientRpcParams)
        {
            var callback = (BlockProtoModifyCallback)PopCallback(requestId);
            callback?.Invoke(errors.Values);
        }
        #endregion
        #endregion Blocks

        #region GridSettings
        #region GridSettings: request list from client
        public void AllGridSettingsRequest(GridSettingsRequestCallback callback)
        {


        }

        [ServerRpc]
        private void AllGridSettingsRequestServerRpc(ulong requestId, ServerRpcParams serverRpcParams = default){

        }

        [ClientRpc]
        private void AllGridSettingsResponceClientRpc(ulong requestId, ClientRpcParams clientRpcParams){

        }
        #endregion GridSettings

    }
}