using System.Collections.Generic;
using UnityEngine;

namespace Pipelines4
{
    public interface IPipelineModel
    {
        /// <summary>
        /// Model ID
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// Diameter of the pipes
        /// </summary>
        float Diameter { get; }

        /// <summary>
        /// Number of pipes in the pipeline
        /// </summary>
        int PipesCount { get; }

        /// <summary>
        /// World-space node positions
        /// </summary>
        IReadOnlyList<Vector3> NodePositions { get; }
    }
}