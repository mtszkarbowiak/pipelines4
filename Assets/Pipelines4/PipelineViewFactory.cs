using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Pipelines4
{
    public class PipelineViewFactory : MonoBehaviour, IPipelineViewFactory
    {
        private const string ACTIVE_PIPE_RENDERERS_POOL_NAME = "ActivePipeViews";
        private const string IDLE_PIPE_RENDERERS_POOL_NAME = "IdlePipeViews";
        private const string ACTIVE_SUPPORT_RENDERERS_POOL_NAME = "ActiveSupportViews";
        private const string IDLE_SUPPORT_RENDERERS_POOL_NAME = "IdleSupportViews";
        private const int INIT_PIPE_POOL_FEED = 32;
        private const int INIT_SUPPORT_RENDERERS_POOL_FEED = 64;
        private const int DISPATCHERS_POOL_FEED = 10;
        private const int VIEWS_POOL_FEED = 128;

        [SerializeField] private Material _material;
        [SerializeField] private GameObject _supportPrefab;

        private Transform _activePipeViewsPoolParent;
        private Transform _idlePipeViewsPoolParent;
        private Transform _activeSupportViewsPoolParent;
        private Transform _idleSupportViewsPoolParent;

        private ConcurrentBag<PipeMeshGenJobDispatcher> _dispatchersPool;
        private ConcurrentDictionary<ulong, PipelineViewController> _activeViews;
        private ConcurrentBag<PipelineViewController> _viewsPool;


        
        private void Awake()
        {
            // Feed scene data.
            
            _activePipeViewsPoolParent = new GameObject(ACTIVE_PIPE_RENDERERS_POOL_NAME).transform;
            _idlePipeViewsPoolParent = new GameObject(IDLE_PIPE_RENDERERS_POOL_NAME).transform;
            _activeSupportViewsPoolParent = new GameObject(ACTIVE_SUPPORT_RENDERERS_POOL_NAME).transform;
            _idleSupportViewsPoolParent = new GameObject(IDLE_SUPPORT_RENDERERS_POOL_NAME).transform;

            _activePipeViewsPoolParent.SetParent(this.transform);
            _idlePipeViewsPoolParent.SetParent(this.transform);
            _activeSupportViewsPoolParent.SetParent(this.transform);
            _idleSupportViewsPoolParent.SetParent(this.transform);

            _idlePipeViewsPoolParent.gameObject.SetActive(false);
            _idleSupportViewsPoolParent.gameObject.SetActive(false);


            for (var i = 0; i < INIT_PIPE_POOL_FEED; i++)
            {
                var newPipeView = new GameObject($"View {i}");
                var filter = newPipeView.AddComponent<MeshFilter>();
                var renderer = newPipeView.AddComponent<MeshRenderer>();

                newPipeView.SetActive(true);
                newPipeView.transform.SetParent(_idlePipeViewsPoolParent);
                filter.sharedMesh = new Mesh();
                renderer.material = _material;
            }

            for (var i = 0; i < INIT_SUPPORT_RENDERERS_POOL_FEED; i++)
            {
                var newSupportView = Instantiate(_supportPrefab, _idleSupportViewsPoolParent);
                newSupportView.SetActive(true);
            }

            
            // Feed internal data.
            
            _dispatchersPool = new ConcurrentBag<PipeMeshGenJobDispatcher>();
            _activeViews = new ConcurrentDictionary<ulong, PipelineViewController>();
            _viewsPool = new ConcurrentBag<PipelineViewController>();

            for (var i = 0; i < DISPATCHERS_POOL_FEED; i++)
                _dispatchersPool.Add( new PipeMeshGenJobDispatcher(Allocator.Persistent));
            for (var i = 0; i < VIEWS_POOL_FEED; i++)
                _viewsPool.Add( new PipelineViewController());
        }

        private void OnDestroy()
        {
            foreach (var dispatcher in _dispatchersPool) dispatcher.Dispose();
        }

        

        private void Update()
        {
            foreach (var view in _activeViews)
                view.Value.Update(_dispatchersPool);
        }

        private void LateUpdate()
        {
            foreach (var view in _activeViews)
                view.Value.LateUpdate();
            
            
            foreach (var view in _activeViews)
            {
                if(view.Value.HandleRecycling(_dispatchersPool))
                    _viewsPool.Add(view.Value);
            }
        }

        

        public async Task CreateView(IPipelineModel model, CancellationToken cancellationToken)
        {
            if (_activeViews.TryGetValue(model.Id, out var view))
            {
                lock (view)
                {
                    view.PassCreateViewRequest(model);
                }
            }
            else if (_viewsPool.TryTake(out view))
            {
                view.PassCreateViewRequest(model);

                if (_activeViews.TryAdd(model.Id, view) == false)
                    throw new UnityException("Adding view failed!");
            }
            else throw new UnityException("Not enough available views.");
        }

        public async Task<bool> DestroyView(IPipelineModel model)
        {
            if (!_activeViews.TryRemove(model.Id, out var view))
                return false;

            view.PassRecycleRequest();

            return true;
        }
    }
}