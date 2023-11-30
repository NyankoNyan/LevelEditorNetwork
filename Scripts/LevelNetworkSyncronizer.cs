using UnityEngine;
using Unity.Netcode;
using LevelView;
using System.Collections.Generic;
using System;

namespace Level.API
{

    public delegate void BlockProtoRequestCallback(IEnumerable<BlockProtoInfo> blockProtos);
    public delegate void GridSettingsRequestCallback(IEnumerable<GridSettingsInfo> gridSettings);
    public delegate void GridStateRequestCallback(IEnumerable<GridStateInfo> gridStates);

    public class NetcodeLevelSyncronizer : NetworkBehaviour, ILevelSyncronizer
    {

        private LevelStorage _levelStorage;

        void Awake()
        {
            _levelStorage = LevelStorage.Instance;
            if (_levelStorage == null) {
                throw new Exception();
            }
        }

        #region Blocks
        public void GetAllBlockProtoRequest(BlockProtoRequestCallback callback)
        {

        }

        [ServerRpc]
        private void GetAllBlockProtoServerRpc()
        {

        }

        [ClientRpc]
        private void GetAllBlockProtoClientRpc(){

        }

        public void BlockProtoChangesSend(
            IEnumerable<BlockProto> changed, 
            IEnumerable<BlockProto> added, 
            IEnumerable<uint> removed){

        }

        [ClientRpc]
        private void BlockProtoChangesClientRpc(){

        }

        public void BlockProtoModifyRequest(){

        }

        [ServerRpc]
        private void BlockProtoModifyServerRpc(){

        }
        #endregion

        #region GridSettings
        public void GridSettingsRequest(){

        }
        #endregion

    }
}