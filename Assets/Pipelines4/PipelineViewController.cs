using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Pipelines4
{
    internal class PipelineViewController
    {
        private volatile ViewState _state;
        private List<PipeMeshGenJobDispatcher> _workingDispatchers;
        private List<MeshRenderer> _renderers;
        public ViewState CurrentState => _state;
            
        public void PassCreateViewRequest(IPipelineModel model) // Invoked from Task thread
        {
            _state = ViewState.WaitingForDispatching;
            
            //TODO
        }

        public void PassRecycleRequest() // Invoked from Task thread
        {
            _state = ViewState.WaitingForRecycling;
        }

        public void Update(ConcurrentBag<PipeMeshGenJobDispatcher> dispatchers) // Invoked from Main thread
        {
            _state = ViewState.Dispatched;
        }

        public void LateUpdate() // Invoked from Main thread
        {
            _state = ViewState.Active;
        }

        public bool HandleRecycling(ConcurrentBag<PipeMeshGenJobDispatcher> dispatchersPool) // Invoked from Main thread
        {
            if (_state != ViewState.WaitingForRecycling) 
                return false;

            foreach (var dispatcher in _workingDispatchers)
                dispatchersPool.Add(dispatcher);
                
            _workingDispatchers.Clear();
            _state = ViewState.Idle;
            
            return true;
        }
            
            
        public enum ViewState
        {
            WaitingForDispatching,
            Dispatched,
            Active,
            WaitingForRecycling,
            Idle,
        }
    }
}