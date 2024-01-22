using SpinnakerNET;
using SpinnakerNET.GenApi;
using SpinnakerNET.GUI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SpinnakerNET.GUI.WPFControls;
using System.Threading;
using System.Drawing;
using Pen = System.Drawing.Pen;

namespace SpinnakerTest
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {

        PropertyGridControl gridControl;

        Thread thMainView = null;

        CameraSelectionWindow camSelControl;

        // Min, Max icon
        MeasureSpotValue maxSpot;
        MeasureSpotValue minSpot;

        // ROI 
        MeasureBoxValue roiBox;


        #region PRIVATE

        const int int640480 = 640 * 480;
        const int int464348 = 464 * 348;
        const int int320256 = 320 * 256;

        private int stIntCamFrameArray = int320256;

        private int mCurWidth = 464;
        private int mCurHeight = 348;

        private bool measureSpot = false;
        private int mPointIndex = 0;

        private bool measureSpot2 = false;
        private int mPointIndex2 = 0;

        private bool bProcessing = false;

        // Offset Value 
        private const float mOffsetVal_001 = 0.01f;
        private const float mOffsetVal_01 = 0.1f;
        private const float mOffsetVal_004 = 0.04f;
        private const float mOffsetVal_04 = 0.4f;

        private float mConvertOffsetVal = mOffsetVal_001;

        private double minBox = 0;
        private double maxBox = 0;

        private Bitmap bmp = null;
        private int step = 256 / 4; // 총 4개의 간격으로 나눔
        //private int step = 348 / 4; // 총 4개의 간격으로 나눔

        #endregion

        #region PUBLIC
        public class MeasureSpotValue
        {
            int mPointIdx;
            ushort mTempValue;

            Pen mPen = new Pen(System.Drawing.Color.AliceBlue);

            public MeasureSpotValue(System.Drawing.Color cl)
            {
                mPen.Color = cl;
            }

            public void SetXY(Graphics gr, int nX, int nY)
            {
                gr.DrawLine(mPen, nX - 10, nY, nX + 10, nY);  // 수평
                gr.DrawLine(mPen, nX, nY - 10, nX, nY + 10);  // 수직
            }

            public void SetPointIndex(int nIndex)
            {
                mPointIdx = nIndex;
            }

            public int GetPointIndex()
            {
                return mPointIdx;
            }

            public void SetTempVal(ushort usTempValue)
            {
                mTempValue = usTempValue;
            }
        }

        // 측정 영역 Box
        public class MeasureBoxValue
        {
            int mX;
            int mY;
            int mWidth;
            int mHeight;
            int mPointIdx;
            ushort mTempValue;
            bool mIsVisible = false;

            // Box 영역 내의 최대 최소 위치
            int mMax_X;
            int mMax_Y;
            int mMin_X;
            int mMin_Y;

            // Box 영역 내의 최대, 최소 온도값
            ushort mMax = 0;
            ushort mMin = 65535;

            Pen mPen = new Pen(System.Drawing.Color.AliceBlue);
            Pen mPenMax = new Pen(System.Drawing.Color.Red);
            Pen mPenMin = new Pen(System.Drawing.Color.Blue);

            public MeasureBoxValue(System.Drawing.Color cl, int nX, int nY, int nWidth, int nHeight)
            {
                mPen.Color = cl;

                mX = nX;
                mY = nY;
                mWidth = nWidth;
                mHeight = nHeight;
            }

            public void ResetMinMax()
            {
                mMax_X = 0;
                mMax_Y = 0;
                mMin_X = 0;
                mMin_Y = 0;

                mMax = 0;
                mMin = 65535;
            }

            public void SetXYWH(Graphics gr)
            {
                gr.DrawRectangle(mPen, mX, mY, mWidth, mHeight);  // Box
            }

            public void SetMax(Graphics gr)
            {
                gr.DrawRectangle(mPenMax, mMax_X - 3, mMax_Y - 3, 6, 6);  // Box Max
            }

            public void SetMin(Graphics gr)
            {
                gr.DrawRectangle(mPenMin, mMin_X - 3, mMin_Y - 3, 6, 6);  // Box Min
            }

            public void GetMinMax(out ushort usMin, out ushort usMax)
            {
                usMin = mMin;
                usMax = mMax;
            }

            public void SetPointIndex(int nIndex)
            {
                mPointIdx = nIndex;
            }

            public int GetPointIndex()
            {
                return mPointIdx;
            }

            public void SetTempVal(ushort usTempValue)
            {
                mTempValue = usTempValue;
            }

            public bool GetIsVisible()
            {
                return mIsVisible;
            }

            public void SetIsVisible(bool bVal)
            {
                mIsVisible = bVal;
            }

            public bool CheckXYinBox(int nX, int nY, ushort tempVal)
            {
                bool rValue = false;

                if ((mX <= nX) && ((mX + mWidth) >= nX))   // X 좌표가 범위 내에 있는지
                {
                    if ((mY <= nY) && ((mY + mHeight) >= nY))   // Y 좌표가 범위 내에 있는지
                    {
                        rValue = true;

                        // 최대 최소 온도 체크 후 백업
                        if (mMin >= tempVal)
                        {
                            mMin = tempVal;
                            mMin_X = nX;
                            mMin_Y = nY;
                        }
                        else if (mMax < tempVal)
                        {
                            mMax = tempVal;
                            mMax_X = nX;
                            mMax_Y = nY;
                        }
                    }
                }
                return rValue;
            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            gridControl = new PropertyGridControl();
            Grid.SetRow(gridControl, 1);

            maxSpot = new MeasureSpotValue(System.Drawing.Color.White);
            minSpot = new MeasureSpotValue(System.Drawing.Color.Yellow);


        }

        #region STEP01. CAMERA SELECT / EVENT
        private void ConnectSpinnaker_Click(object sender, RoutedEventArgs e)
        {
            // Spinnaker 연결 방식 
            camSelControl = new CameraSelectionWindow();

            camSelControl.OnDeviceDoubleClicked += ConditionalStreamingEvent;
            camSelControl.OnDeviceClicked += ConnectControls;
            camSelControl.RegisterStartStopContextMenuEvent(ConditionalStreamingEvent);

            camSelControl.ShowModal(true);

        }

        /// <summary>
        /// Event handler for RegisterStartStopContextMenuEvent and OnDeviceDoubleClicked
        /// Note that ConnectControls is invoked prior to this function call
        /// </summary>
        /// <param name="sender">CameraSelectionControl</param>
        /// <param name="args">DeviceEventArgs</param>
        void ConditionalStreamingEvent(object sender, CameraSelectionWindow.DeviceEventArgs args)
        {

        }


        void ConnectControls(object sender, CameraSelectionWindow.DeviceEventArgs args)
        {
            // Check whether an Interface is selected
            if (args.IsCamera == false && args.Interface != null)
            {
                ResetWindowControl(); // Disconnect previous device
            }
            // Check whether a System is selected
            else if (args.IsSystem == true && args.System != null)
            {
                ResetWindowControl(); // Disconnect previous device
            }
            else // Connect a camera (Previous device was interface || Camera is first device connected)
            {
                SetCamera(args.Camera, false, args.Interface);
            }

            camSelControl.Close();
        }
        private void ResetWindowControl()
        {
            try
            {
                // Disconnect controls

            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }
        #endregion

        #region STEP02. CAMERA CONNECT
        /// <summary>
        /// Connect ImageDrawingControl and PropertyGridControl with IManagedCamera
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="startStreaming">Boolean indicating whether to start streaming</param>
        /// <param name="parentInterface">Parent interface </param>
        void SetCamera(IManagedCamera cam, bool startStreaming = false, IManagedInterface parentInterface = null)
        {
            try
            {
                cam.Init();

                // 카메라 연결 
                camPlayOne(cam);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("There was a problem connecting to IManagedCamera.\n{0}", ex.Message));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        void camPlayOne(IManagedCamera cam)
        {
            try
            {
                ConfigurationCam(cam);

                // Begin acquiring images
                cam.BeginAcquisition();

                // Retrieve TL device nodemap; please see NodeMapInfo_CSharp
                // example for additional information on TL device nodemaps
                INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

                Console.Write("\tDevice {0} ", 0);

                Thread.Sleep(1000);

                thMainView = new Thread(() => threadProc(cam));
                thMainView.Start();

            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        void ConfigurationCam(IManagedCamera cam)
        {
            try
            {
                INodeMap nodeMap = cam.GetNodeMap();

                StringReg iModelName = nodeMap.GetNode<StringReg>("DeviceModelName");
                string modelname = iModelName.ToString();

                // Ax5
                if (modelname.Contains("AX5"))
                {
                    stIntCamFrameArray = int320256;
                    mCurWidth = 320;
                    mCurHeight = 256;

                    bmp = new Bitmap(mCurWidth, mCurHeight);


                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono14 = iPixelFormat.GetEntryByName("Mono14");
                        iPixelFormat.Value = iPixelFormatMono14.Value;
                        Console.WriteLine("iPixelFormatMono14 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    IEnum iDigitalOutput = nodeMap.GetNode<IEnum>("DigitalOutput");
                    if (iDigitalOutput != null && iDigitalOutput.IsWritable)
                    {
                        IEnumEntry iDigitalOutput14bit = iDigitalOutput.GetEntryByName("bit14bit");
                        iDigitalOutput.Value = iDigitalOutput14bit.Value;
                        Console.WriteLine("iDigitalOutput14bit : " + nodeMap.GetNode<IEnum>("DigitalOutput").ToString());
                    }

                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("TemperatureLinearMode");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearModeOn = iTemperatureLinearMode.GetEntryByName("On");
                        iTemperatureLinearMode.Value = iTemperatureLinearModeOn.Value;

                        IEnum iTemperatureLinearResolution = nodeMap.GetNode<IEnum>("TemperatureLinearResolution");
                        if (iTemperatureLinearResolution != null && iTemperatureLinearResolution.IsWritable)
                        {
                            IEnumEntry iTemperatureLinearResolutionHigh = iTemperatureLinearResolution.GetEntryByName("High");
                            iTemperatureLinearResolution.Value = iTemperatureLinearResolutionHigh.Value;
                            mConvertOffsetVal = mOffsetVal_004;

                            Console.WriteLine("iTemperatureLinearModeOn : " + nodeMap.GetNode<IEnum>("TemperatureLinearMode").ToString());
                            Console.WriteLine("iTemperatureLinearResolution : " + nodeMap.GetNode<IEnum>("TemperatureLinearResolution").ToString());
                        }
                    }
                }
                else if (modelname.Contains("PT1000")) // FLIR Axx
                {
                    stIntCamFrameArray = int640480;
                    mCurWidth = 640;
                    mCurHeight = 480;

                    bmp = new Bitmap(mCurWidth, mCurHeight);

                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono16 = iPixelFormat.GetEntryByName("Mono16");
                        iPixelFormat.Value = iPixelFormatMono16.Value;
                        Console.WriteLine("iPixelFormatMono16 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("IRFormat");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearMode100mk = iTemperatureLinearMode.GetEntryByName("TemperatureLinear100mK");
                        iTemperatureLinearMode.Value = iTemperatureLinearMode100mk.Value;

                        mConvertOffsetVal = mOffsetVal_01;

                        Console.WriteLine("iTemperatureLinearMode 100mk : " + nodeMap.GetNode<IEnum>("IRFormat").ToString());
                    }
                }
                else if (modelname.Contains("A50")) // A50, A500
                {
                    stIntCamFrameArray = int464348;
                    mCurWidth = 464;
                    mCurHeight = 348;

                    bmp = new Bitmap(mCurWidth, mCurHeight);

                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono16 = iPixelFormat.GetEntryByName("Mono16");
                        iPixelFormat.Value = iPixelFormatMono16.Value;
                        Console.WriteLine("iPixelFormatMono16 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("IRFormat");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearMode100mk = iTemperatureLinearMode.GetEntryByName("TemperatureLinear100mK");
                        iTemperatureLinearMode.Value = iTemperatureLinearMode100mk.Value;

                        mConvertOffsetVal = mOffsetVal_01;

                        Console.WriteLine("iTemperatureLinearMode 100mk : " + nodeMap.GetNode<IEnum>("IRFormat").ToString());
                    }
                }
                else if (modelname.Contains("A70")) // A70, A700
                {
                    stIntCamFrameArray = int640480;
                    mCurWidth = 640;
                    mCurHeight = 480;

                    bmp = new Bitmap(mCurWidth, mCurHeight);

                    IEnum iPixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    if (iPixelFormat != null && iPixelFormat.IsWritable)
                    {
                        IEnumEntry iPixelFormatMono16 = iPixelFormat.GetEntryByName("Mono16");
                        iPixelFormat.Value = iPixelFormatMono16.Value;
                        Console.WriteLine("iPixelFormatMono16 : " + nodeMap.GetNode<IEnum>("PixelFormat").ToString());
                    }

                    IEnum iTemperatureLinearMode = nodeMap.GetNode<IEnum>("IRFormat");
                    if (iTemperatureLinearMode != null && iTemperatureLinearMode.IsWritable)
                    {
                        IEnumEntry iTemperatureLinearMode100mk = iTemperatureLinearMode.GetEntryByName("TemperatureLinear100mK");
                        iTemperatureLinearMode.Value = iTemperatureLinearMode100mk.Value;

                        mConvertOffsetVal = mOffsetVal_01;

                        Console.WriteLine("iTemperatureLinearMode 100mk : " + nodeMap.GetNode<IEnum>("IRFormat").ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        #endregion

        #region STEP03. CAMERA DATA RECEIVE / SHOW RESULT
        /// <summary>
        /// MdsSdkControl 데이터 Receiver delegate function
        /// </summary>
        /// <param name="data">수신 데이터</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="minval">minimum value</param>
        /// <param name="maxval">maximum value</param>
        delegate void DelegateCtrlData_Receiver(UInt16[] data, int w, int h, ushort minval, ushort maxval);
        /// <summary>
        /// MdsSdkControl 데이터 Receiver
        /// 열화상 카메라로 부터 받은 데이터로 화면을 구성
        /// </summary>
        /// <param name="data">수신 데이터</param>
        /// <param name="w">width</param>
        /// <param name="h">height</param>
        /// <param name="minval">minimum value</param>
        /// <param name="maxval">maximum value</param>
        void CtrlData_Receiver(UInt16[] data, int w, int h, ushort minval, ushort maxval)
        {
            if (data == null)
                return;

            lock (this)
            {
                //SetImage를 수행중이면 리턴.(화면 갱신 skip)
                if (bProcessing)
                {
                    return;
                }
            }

            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke(new DelegateCtrlData_Receiver(CtrlData_Receiver), new object[] { data, w, h, minval, maxval });
                return;
            }

            try
            {
                lock (this)
                {
                    bProcessing = true;
                }

                System.Drawing.Color col;

                IntPtr hBitmap = IntPtr.Zero;

                //x 는 image의 width
                //y 는 image의 hediht
                int x, y;

                // Box 내 영역의 최대 최소 온도값 초기화
                if (roiBox != null && roiBox.GetIsVisible())
                {
                    roiBox.ResetMinMax();
                }

                // Rainbow colors
                for (int a = 0; a < data.Length; a++)
                {
                    getXY(a, mCurWidth, out x, out y);

                    int rVal = (int)((data[a] - minval) * 255 / (maxval - minval));

                    if (rVal < step) //Blue to Cyan
                    {
                        col = Color.FromArgb(0, rVal * 4, 255);
                    }
                    else if (rVal < step * 2) //Cyan to Green
                    {
                        col = Color.FromArgb(0, 255, 255 - (rVal - step) * 4);
                    }
                    else if (rVal < step * 3) //Green to Yellow
                    {
                        col = Color.FromArgb((rVal - step * 2) * 4, 255, 0);
                    }
                    else //Yellow to Red
                    {
                        col = Color.FromArgb(255, 255 - (rVal - step * 3) * 4, 0);
                    }

                    // Box 내 영역의 최대 최소 온도값 체크
                    bmp.SetPixel(x, y, col);

                    if (roiBox != null && roiBox.GetIsVisible())
                    {
                        roiBox.CheckXYinBox(x, y, data[a]);
                    }
                }



                Graphics gr = Graphics.FromImage(bmp);

                int maxX = 0;
                int maxY = 0;
                int minX = 0;
                int minY = 0;

                // max spot get x, y;
                getXY(maxSpot.GetPointIndex(), mCurWidth, out maxX, out maxY);
                getXY(minSpot.GetPointIndex(), mCurWidth, out minX, out minY);

                maxSpot.SetXY(gr, maxX, maxY);
                minSpot.SetXY(gr, minY, minY);

                // ROI Box
                if (roiBox != null && roiBox.GetIsVisible())
                {
                    roiBox.SetXYWH(gr);
                    roiBox.SetMax(gr);
                    roiBox.SetMin(gr);

                    ushort usMin = 0;
                    ushort usMax = 0;

                    roiBox.GetMinMax(out usMin, out usMax);

                    minBox = (((float)(usMin) * mConvertOffsetVal) - 273.15f);
                    maxBox = (((float)(usMax) * mConvertOffsetVal) - 273.15f);

                }


                // Bitmap is ready - update image control
                hBitmap = bmp.GetHbitmap();
                BitmapSource bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                if (bmpSrc.CanFreeze)
                    bmpSrc.Freeze();

                this.backgroundImageBrush.ImageSource = bmpSrc;

                DeleteObject(hBitmap);

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[CtrlEvent] " + e.ToString());
            }
            lock (this)
                bProcessing = false;

            return;
        }


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);



        void threadProc(IManagedCamera cam)
        {
            while (true)
            {
                if (((Thread.CurrentThread.ThreadState & ThreadState.SuspendRequested) == ThreadState.SuspendRequested) ||
                ((Thread.CurrentThread.ThreadState & ThreadState.Suspended) == ThreadState.Suspended))
                {
                    break;
                }

                try
                {
                    if (cam.IsValid() != true && cam != null)
                    {
                        Console.WriteLine("cam is not valid");
                        break;
                    }

                    // Retrieve next received image and ensure image completion
                    using (IManagedImage rawImage = cam.GetNextImage())
                    {
                        if (rawImage.IsIncomplete)
                        {
                            Console.WriteLine("Image incomplete with status {0}...", rawImage.ImageStatus);
                            rawImage.Release();

                            Thread.Sleep(10);
                        }
                        else
                        {
                            double minValue = 0;
                            double maxValue = 0;
                            // [0] convert to byte array to uint16

                            int uint16Count = 0;
                            ushort max16 = 0;
                            ushort min16 = 65535;
                            double measurePoint = 0;
                            double measurePoint2 = 0;

                            UInt16[] imgArray = new UInt16[stIntCamFrameArray];

                            //for (int a = 0; a < int640480x2; a += 2)
                            for (int a = 0; a < stIntCamFrameArray * 2; a += 2)
                            //for (int a = 0; a < stIntCamFrameArray ; a++)
                            {
                                ushort sample = BitConverter.ToUInt16(rawImage.ManagedData, a);

                                if (min16 >= sample)
                                {
                                    minValue = ((float)(sample) * mConvertOffsetVal) - 273.15;
                                    min16 = sample;
                                    minSpot.SetPointIndex(a / 2);
                                    minSpot.SetTempVal(sample);

                                }
                                else if (max16 < sample)
                                {
                                    maxValue = ((float)(sample) * mConvertOffsetVal) - 273.15;
                                    max16 = sample;
                                    maxSpot.SetPointIndex(a / 2);
                                    maxSpot.SetTempVal(sample);

                                }

                                imgArray[uint16Count] = sample;
                                uint16Count++;
                            }

                            if (measureSpot)
                            {
                                measurePoint = ((float)(imgArray[mPointIndex]) * mConvertOffsetVal) - 273.15;
                            }

                            if (measureSpot2)
                            {
                                measurePoint2 = ((float)(imgArray[mPointIndex2]) * mConvertOffsetVal) - 273.15;
                            }

                            CtrlData_Receiver(imgArray, mCurWidth, mCurHeight, min16, max16);

                            CompositionTarget_Rendering(minValue, maxValue, minBox, maxBox);
                            rawImage.Release();
                        }
                    }
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                    break;
                }
                catch (SpinnakerException ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                }

                Thread.Sleep(1);

            }

        }

        // Point index에서 X, Y, 좌표를 알아낸다.
        private void getXY(int sourceIndex, int sourceWidth, out int x, out int y)
        {
            y = sourceIndex / sourceWidth;
            x = sourceIndex % sourceWidth;
        }

        delegate void DelegateCompositionTarget_Rendering(double minval, double maxval, double measurePoint, double measurePoint2);
        void CompositionTarget_Rendering(double minval, double maxval, double roiminval, double roimaxval)
        {
            if (!this.CheckAccess())
            {
                this.Dispatcher.Invoke(new DelegateCompositionTarget_Rendering(CompositionTarget_Rendering), new object[] { minval, maxval, roiminval, roimaxval });
                return;
            }

            // 전체 화면 온도 값 표시 
            MinTemp.Content = string.Format("{0:F1}", minval);
            MaxTemp.Content = string.Format("{0:F1}", maxval);

            // ROI 영역 온도 값 표시 
            ROIMinTemp.Content = string.Format("{0:F1}", roiminval);
            ROIMaxTemp.Content = string.Format("{0:F1}", roimaxval);

        }
        #endregion

        #region ROI EVENT
        private void SetROIBox_Click(object sender, RoutedEventArgs e)
        {
            // 측정 영역 박스 좌표, 크기 설정
            roiBox = new MeasureBoxValue(System.Drawing.Color.Yellow, 100, 100, 100, 100);
            roiBox.SetIsVisible(true);

        }
        #endregion

        #region Close
        private void Window_Closed(object sender, EventArgs e)
        {
            if (thMainView != null)
            {
                thMainView.Abort();
            }
        }
        #endregion
    }
}
