using Silk.NET.Vulkan;

namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public static class VulkanUtils
    {
        /// <summary>
        /// Inticates if the passed result is a success or an error as defined by the Vulkan spec.
        /// </summary>
        /// <param name="result"></param>
        /// <returns>True if success; otherwise false. Defaults to true for unknown result types.</returns>
        public static bool ResultIsSuccess(Result result)
        {
            // From: https://www.khronos.org/registry/vulkan/specs/1.2-extensions/man/html/VkResult.html
            switch (result)
            {
                // Success Codes
                default:
                case Result.Success:
                case Result.NotReady:
                case Result.Timeout:
                case Result.EventSet:
                case Result.EventReset:
                case Result.Incomplete:
                case Result.SuboptimalKhr:
                case Result.ThreadIdleKhr:
                case Result.ThreadDoneKhr:
                case Result.OperationDeferredKhr:
                case Result.OperationNotDeferredKhr:
                case Result.PipelineCompileRequired:
                    return true;

                // Error codes
                case Result.ErrorOutOfHostMemory:
                case Result.ErrorOutOfDeviceMemory:
                case Result.ErrorInitializationFailed:
                case Result.ErrorDeviceLost:
                case Result.ErrorMemoryMapFailed:
                case Result.ErrorLayerNotPresent:
                case Result.ErrorExtensionNotPresent:
                case Result.ErrorFeatureNotPresent:
                case Result.ErrorIncompatibleDriver:
                case Result.ErrorTooManyObjects:
                case Result.ErrorFormatNotSupported:
                case Result.ErrorFragmentedPool:
                case Result.ErrorSurfaceLostKhr:
                case Result.ErrorNativeWindowInUseKhr:
                case Result.ErrorOutOfDateKhr:
                case Result.ErrorIncompatibleDisplayKhr:
                case Result.ErrorInvalidShaderNV:
                case Result.ErrorOutOfPoolMemory:
                case Result.ErrorInvalidExternalHandle:
                case Result.ErrorFragmentation:
                case Result.ErrorInvalidDeviceAddressExt:
                // NOTE: Same as above
                //case VK_ERROR_INVALID_OPAQUE_CAPTURE_ADDRESS:
                case Result.ErrorFullScreenExclusiveModeLostExt:
                case Result.ErrorUnknown:
                    return false;
            }
        }

        /// <summary>
        /// Returns the string representation of result.
        /// </summary>
        /// <param name="result">The result to get the string for.</param>
        /// <returns>The error code and extended error message in string form. Defaults to success for unknown result types.</returns>
        public static string FormattedResult(Result result)
        {
            // From: https://www.khronos.org/registry/vulkan/specs/1.2-extensions/man/html/VkResult.html
            // Success Codes
            switch (result)
            {
                default:
                case Result.Success:
                    return "VK_SUCCESS Command successfully completed";
                case Result.NotReady:
                    return "VK_NOT_READY A fence or query has not yet completed";
                case Result.Timeout:
                    return "VK_TIMEOUT A wait operation has not completed in the specified time";
                case Result.EventSet:
                    return "VK_EVENT_SET An event is signaled";
                case Result.EventReset:
                    return "VK_EVENT_RESET An event is unsignaled";
                case Result.Incomplete:
                    return "VK_INCOMPLETE A return array was too small for the result";
                case Result.SuboptimalKhr:
                    return "VK_SUBOPTIMAL_KHR A swapchain no longer matches the surface properties exactly, but can still be used to present to the surface successfully.";
                case Result.ThreadIdleKhr:
                    return "VK_THREAD_IDLE_KHR A deferred operation is not complete but there is currently no work for this thread to do at the time of this call.";
                case Result.ThreadDoneKhr:
                    return "VK_THREAD_DONE_KHR A deferred operation is not complete but there is no work remaining to assign to additional threads.";
                case Result.OperationDeferredKhr:
                    return "VK_OPERATION_DEFERRED_KHR A deferred operation was requested and at least some of the work was deferred.";
                case Result.OperationNotDeferredKhr:
                    return "VK_OPERATION_NOT_DEFERRED_KHR A deferred operation was requested and no operations were deferred.";
                case Result.PipelineCompileRequiredExt:
                    return "VK_PIPELINE_COMPILE_REQUIRED_EXT A requested pipeline creation would have required compilation, but the application requested compilation to not be performed.";

                // Error codes
                case Result.ErrorOutOfHostMemory:
                    return "VK_ERROR_OUT_OF_HOST_MEMORY A host memory allocation has failed.";
                case Result.ErrorOutOfDeviceMemory:
                    return "VK_ERROR_OUT_OF_DEVICE_MEMORY A device memory allocation has failed.";
                case Result.ErrorInitializationFailed:
                    return "VK_ERROR_INITIALIZATION_FAILED Initialization of an object could not be completed for implementation-specific reasons.";
                case Result.ErrorDeviceLost:
                    return "VK_ERROR_DEVICE_LOST The logical or physical device has been lost. See Lost Device";
                case Result.ErrorMemoryMapFailed:
                    return "VK_ERROR_MEMORY_MAP_FAILED Mapping of a memory object has failed.";
                case Result.ErrorLayerNotPresent:
                    return "VK_ERROR_LAYER_NOT_PRESENT A requested layer is not present or could not be loaded.";
                case Result.ErrorExtensionNotPresent:
                    return "VK_ERROR_EXTENSION_NOT_PRESENT A requested extension is not supported.";
                case Result.ErrorFeatureNotPresent:
                    return "VK_ERROR_FEATURE_NOT_PRESENT A requested feature is not supported.";
                case Result.ErrorIncompatibleDriver:
                    return "VK_ERROR_INCOMPATIBLE_DRIVER The requested version of Vulkan is not supported by the driver or is otherwise incompatible for implementation-specific reasons.";
                case Result.ErrorTooManyObjects:
                    return "VK_ERROR_TOO_MANY_OBJECTS Too many objects of the type have already been created.";
                case Result.ErrorFormatNotSupported:
                    return "VK_ERROR_FORMAT_NOT_SUPPORTED A requested format is not supported on this device.";
                case Result.ErrorFragmentedPool:
                    return "VK_ERROR_FRAGMENTED_POOL A pool allocation has failed due to fragmentation of the pool’s memory. This must only be returned if no attempt to allocate host or device memory was made to accommodate the new allocation. This should be returned in preference to VK_ERROR_OUT_OF_POOL_MEMORY, but only if the implementation is certain that the pool allocation failure was due to fragmentation.";
                case Result.ErrorSurfaceLostKhr:
                    return "VK_ERROR_SURFACE_LOST_KHR A surface is no longer available.";
                case Result.ErrorNativeWindowInUseKhr:
                    return "VK_ERROR_NATIVE_WINDOW_IN_USE_KHR The requested window is already in use by Vulkan or another API in a manner which prevents it from being used again.";
                case Result.ErrorOutOfDateKhr:
                    return "VK_ERROR_OUT_OF_DATE_KHR A surface has changed in such a way that it is no longer compatible with the swapchain, and further presentation requests using the swapchain will fail. Applications must query the new surface properties and recreate their swapchain if they wish to continue presenting to the surface.";
                case Result.ErrorIncompatibleDisplayKhr:
                    return "VK_ERROR_INCOMPATIBLE_DISPLAY_KHR The display used by a swapchain does not use the same presentable image layout, or is incompatible in a way that prevents sharing an image.";
                case Result.ErrorInvalidShaderNV:
                    return "VK_ERROR_INVALID_SHADER_NV One or more shaders failed to compile or link. More details are reported back to the application via VK_EXT_debug_report if enabled.";
                case Result.ErrorOutOfPoolMemory:
                    return "VK_ERROR_OUT_OF_POOL_MEMORY A pool memory allocation has failed. This must only be returned if no attempt to allocate host or device memory was made to accommodate the new allocation. If the failure was definitely due to fragmentation of the pool, VK_ERROR_FRAGMENTED_POOL should be returned instead.";
                case Result.ErrorInvalidExternalHandle:
                    return "VK_ERROR_INVALID_EXTERNAL_HANDLE An external handle is not a valid handle of the specified type.";
                case Result.ErrorFragmentation:
                    return "VK_ERROR_FRAGMENTATION A descriptor pool creation has failed due to fragmentation.";
                case Result.ErrorInvalidDeviceAddressExt:
                    return "VK_ERROR_INVALID_DEVICE_ADDRESS_EXT A buffer creation failed because the requested address is not available.";
                // NOTE: Same as above
                //case VK_ERROR_INVALID_OPAQUE_CAPTURE_ADDRESS:
                //    return "VK_ERROR_INVALID_OPAQUE_CAPTURE_ADDRESS A buffer creation or memory allocation failed because the requested address is not available. A shader group handle assignment failed because the requested shader group handle information is no longer valid.";
                case Result.ErrorFullScreenExclusiveModeLostExt:
                    return "VK_ERROR_FULL_SCREEN_EXCLUSIVE_MODE_LOST_EXT An operation on a swapchain created with VK_FULL_SCREEN_EXCLUSIVE_APPLICATION_CONTROLLED_EXT failed as it did not have exlusive full-screen access. This may occur due to implementation-dependent reasons, outside of the application’s control.";
                case Result.ErrorUnknown:
                    return "VK_ERROR_UNKNOWN An unknown error has occurred; either the application has provided invalid input, or an implementation failure has occurred.";
            }
        }
    }
}
