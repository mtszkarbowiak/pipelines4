using System.Threading;
using System.Threading.Tasks;

namespace Pipelines4
{
    public interface IPipelineViewFactory
    {
        /// <summary>
        /// Creates a view for a model or rebuilds the view if one has already been 
        /// created for this model by the factory
        /// </summary>
        /// <param name="model">Model for which view should be created</param>
        /// <param name="cancellationToken">Cancellation Token used to cancel 
        /// drawing request</param>
        /// <returns>Awaitable Task</returns>
        Task CreateView( IPipelineModel model, CancellationToken cancellationToken );

        /// <summary>
        /// Destroys a view for a model if a view has already been created
        /// for this model
        /// </summary>
        /// <param name="model">Model for which view should be destroyed</param>
        /// <returns>True if view has been destroyed, False if no view is present 
        /// for this model</returns>
        Task<bool> DestroyView( IPipelineModel model );
    }
}