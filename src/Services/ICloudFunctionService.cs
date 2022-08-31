using System.Collections.Generic;

namespace PipServices3.Gcp.Services
{
    /// <summary>
    /// An interface that allows to integrate Google Function services into Google Function containers
    /// and connect their actions to the function calls.
    /// </summary>
    public interface ICloudFunctionService
    {
        /// <summary>
        /// Get all actions supported by the service.
        /// </summary>
        /// <returns>an array with supported actions.</returns>
        List<CloudFunctionAction> GetActions();
    }
}
