/* 
 * This file contains an attempted implementation of DJI live video streaming using DJI Video Parser.
 * 
 * Implementation is incomplete due to a parser bug, preventing decoding and redering of live video
 * frames. This functionality is broken and cannot be resolved due to DJI Windows SDK being no longer
 * maintained and supported.
 *
 * Link: https://github.com/DJI-Windows-SDK-Tutorials/Windows-FPVDemo/issues/2
 */

#if false
using DJI.WindowsSDK;
using DJIVideoParser;
using System;
using Windows.UI.Xaml.Controls;


namespace UAV_Assistive_Operation.Services
{
    public class DJIVideoService_Deprecated
    {

        private DJIVideoParser.Parser _videoParser;

        //Decoded video frame events for services to subscribe to
        public event Action<byte[], int, int> FrameDecoded;


        public void SetSwapChainPanel(SwapChainPanel panel)
        {
            _videoParser?.SetSurfaceAndVideoCallback(0, 0, panel, ReceiveDecodeData);
        }

        //Called when aircraft is connected and video feed is ready
        public void Start()
        {
            if (_videoParser != null)
                return;

            _videoParser = new DJIVideoParser.Parser();
            _videoParser.Initialize(delegate (byte[] data)
            {
                return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
            });

            var videoFeed = DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0);
            videoFeed.VideoDataUpdated += VideoPush;
            DJISDKManager.Instance.ComponentManager.GetCameraHandler(0,0).CameraTypeChanged += CameraTypeChanged;
        }

        //Manages aircraft disconnection and app closing
        public void Stop()
        {
            if (_videoParser == null)
                return;

            var videoFeed = DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0);
            videoFeed.VideoDataUpdated -= VideoPush;
            DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged -= CameraTypeChanged;
            _videoParser = null;
        }

        //Raw data
        private void VideoPush(VideoFeed sender, byte[] bytes)
        {
            _videoParser?.PushVideoData(0, 0, bytes, bytes.Length);
        }

        //Decode data. Used to return a bytes array with image data in RGBA format.
        private void ReceiveDecodeData(byte[] data, int width, int height)
        {
            FrameDecoded?.Invoke(data, width, height);
        }

        private void CameraTypeChanged(object sender, CameraTypeMsg? value)
        {
            if (value == null)
                return;

            switch (value.Value.value)
            {
                case CameraType.MAVIC_2_ZOOM:
                    this._videoParser.SetCameraSensor(AircraftCameraType.Mavic2Zoom); 
                    break;
                case CameraType.MAVIC_2_PRO:
                    this._videoParser.SetCameraSensor(AircraftCameraType.Mavic2Pro);
                    break;
                default:
                    this._videoParser.SetCameraSensor(AircraftCameraType.Others);
                    break;
            }

        }
    }
}
#endif