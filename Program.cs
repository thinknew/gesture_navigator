using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;



namespace gesture_viewer.cs
{
    class Program
    {
       

        static void OnGesure(ref PXCMGesture.Gesture data)
        {
            //Console.WriteLine("[gesture] label={0}", data.label);
            if (Convert.ToString(data.label) == "LABEL_NAV_SWIPE_LEFT")
            {
                Console.WriteLine("LEFT");
                SendKeys.SendWait("{LEFT}");
                Console.WriteLine("Left Arrow pressed");
               // ConsoleKeyInfo kb = new ConsoleKeyInfo();
               // kb=Console.ReadKey(ConsoleKey.LeftArrow);// Console.ReadKey();
               // if (kb.Key == ConsoleKey.LeftArrow)
                   
                //Console.WriteLine("Left Arrow pressed");
            }
            if (Convert.ToString(data.label) == "LABEL_NAV_SWIPE_RIGHT")
            {
                Console.WriteLine("RIGHT");
                SendKeys.SendWait("{RIGHT}");
                Console.WriteLine("Right Arrow pressed");
            }


        }

        static void Main(string[] args)
        {
            // Create session
            PXCMSession session;
            pxcmStatus sts=PXCMSession.CreateInstance(out session);
            if (sts<pxcmStatus.PXCM_STATUS_NO_ERROR) {
                Console.WriteLine("Failed to create the SDK session");
                return;
            }

            // Gesture Module
            PXCMBase gesture_t;
            sts = session.CreateImpl(PXCMGesture.CUID, out gesture_t);
            if (sts<pxcmStatus.PXCM_STATUS_NO_ERROR) {
                Console.WriteLine("Failed to load any gesture recognition module");
                session.Dispose();
                return;
            }
            PXCMGesture gesture=(PXCMGesture)gesture_t;

            PXCMGesture.ProfileInfo pinfo;
            sts=gesture.QueryProfile(0,out pinfo);

            UtilMCapture capture = new UtilMCapture(session);
            sts = capture.LocateStreams(ref pinfo.inputs);
            if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Console.WriteLine("Failed to locate a capture module");
                gesture.Dispose();
                capture.Dispose();
                session.Dispose();
                return;
            }
            sts=gesture.SetProfile(ref pinfo);
            sts=gesture.SubscribeGesture(100,OnGesure);

            bool device_lost=false;
            PXCMImage[] images=new PXCMImage[PXCMCapture.VideoStream.STREAM_LIMIT];
            PXCMScheduler.SyncPoint[] sps=new PXCMScheduler.SyncPoint[2];
            for (int nframes=0;nframes<50000;nframes++) {
                sts=capture.ReadStreamAsync(images,out sps[0]);
                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    if (sts == pxcmStatus.PXCM_STATUS_DEVICE_LOST)
                    {
                        if (!device_lost) Console.WriteLine("Device disconnected");
                        device_lost = true; nframes--;
                        continue;
                    }
                    Console.WriteLine("Device failed\n");
                    break;
                }
                if (device_lost)
                {
                    Console.WriteLine("Device reconnected");
                    device_lost = false;
                }

                sts=gesture.ProcessImageAsync(images,out sps[1]);
                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                PXCMScheduler.SyncPoint.SynchronizeEx(sps);
                if (sps[0].Synchronize(0) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    PXCMGesture.GeoNode data;
                    sts = gesture.QueryNodeData(0, PXCMGesture.GeoNode.Label.LABEL_BODY_HAND_PRIMARY | PXCMGesture.GeoNode.Label.LABEL_HAND_MIDDLE, out data);
                    if (sts >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                        Console.WriteLine("[node] {0}, {1}, {2}", data.positionImage.x, data.positionImage.y, data.timeStamp);
                }

                foreach (PXCMScheduler.SyncPoint s in sps) if (s!=null) s.Dispose();
                foreach (PXCMImage i in images) if (i!=null) i.Dispose();
            }

            gesture.Dispose();
            capture.Dispose();
            session.Dispose();
        }

        public static PXCMGesture.Gesture.OnGesture OnGesture { get; set; }
        
    }
}
