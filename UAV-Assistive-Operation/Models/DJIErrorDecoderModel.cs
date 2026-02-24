using DJI.WindowsSDK;
using System;

namespace UAV_Assistive_Operation.Models
{
    public class DJIErrorDecoderModel
    {
        public static String GetErrorMessage(SDKError error)
        {
            switch (error)
            {
                case SDKError.NO_ERROR:
                    return null;
                case SDKError.REQUEST_TIMEOUT:
                    return ": request timeout";
                case SDKError.DISCONNECTED:
                    return ": disconnected during execution";
                case SDKError.SYSTEM_ERROR:
                    return ": system error";
                case SDKError.COMMAND_INTERRUPTED:
                    return ": command interrupted";
                case SDKError.EXECUTION_FAILED:
                    return ": execution failed";
                case SDKError.UNKNOWN:
                    return ": unknown error occurred";
                default:
                    return " during execution";
            }
        }
    }
}
