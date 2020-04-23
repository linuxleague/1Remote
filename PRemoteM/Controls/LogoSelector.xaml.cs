﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using PRM.Core.Annotations;
using PRM.Core.Model;
using Shawn.Ulits;

namespace PRM.Core.Resources.Controls
{
    /// <summary>
    /// LogoSelector.xaml 的交互逻辑
    /// </summary>
    public partial class LogoSelector : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion


        /*
        public static readonly DependencyProperty LogoSourceProperty =
            DependencyProperty.Register("LogoSource", typeof(BitmapSource), typeof(LogoSelector),
                new PropertyMetadata(null, OnDataChanged));

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (BitmapSource)e.NewValue;
            ((LogoSelector)d).OnPropertyChanged(nameof(LogoSource));
        }


        public BitmapSource LogoSource
        {
            get => (BitmapSource)GetValue(LogoSourceProperty);
            set
            {
                SetValue(LogoSourceProperty, value);
                //SetImg(value);
                OnPropertyChanged(nameof(LogoSource));
            }
        }*/






        private double _scaling = 1.0;
        public double Scaling
        {
            get => _scaling;
            set
            {
                if (Math.Abs(value - _scaling) < 0.05)
                    return;

                if (value < 0.1)
                    _scaling = 0.1;
                if (value > 2)
                    _scaling = 2;
                else
                    _scaling = value;
                if (Img?.Source != null)
                {
                    double wl = ((BitmapSource)Img.Source).PixelWidth * Scaling - CanvasImage.Width;
                    double hl = ((BitmapSource)Img.Source).PixelHeight * Scaling - CanvasImage.Height;
                    CanvasImage.Width += wl;
                    CanvasImage.Height += hl;
                    // 缩放后保持图像中心不变
                    CanvasImage.SetValue(Canvas.LeftProperty, Canvas.GetLeft(CanvasImage) - wl / 2.0);
                    CanvasImage.SetValue(Canvas.TopProperty, Canvas.GetTop(CanvasImage) - hl / 2.0);
                    ReplaceChild(ref CanvasImage, ref CanvasWhiteBoard);
                }
                OnPropertyChanged(nameof(Scaling));
            }
        }

        public LogoSelector()
        {
            InitializeComponent();

            DataContext = this;

            CanvasWhiteBoard.Background = ChessboardBrushHelper.ChessboardBrush(16);

            //var img = Image.FromFile()
            //Img.Source = new ImageSource();
            //Close();
            //PRMSqliteHelper psh = new PRMSqliteHelper();
            //psh.CreateDb();
            //SetImg(GetBitmapSource(@"D:\Users\Desktop\LOGO\hp-logo-png-1.png"));
        }

        public void SetImg(BitmapSource img)
        {
            Scaling = 1.0;
            Img.Source = img;
            if (img != null)
            {
                //Img.Source = GetBitmapSource(@"E:\数据存档\5160\老算法错误样本图例\Id02889___SN10125124___20190126___5160A1_Cbc5Diff.5160xml.jpg");
                CanvasImage.Width = ((BitmapSource)Img.Source).PixelWidth * Scaling;
                CanvasImage.Height = ((BitmapSource)Img.Source).PixelHeight * Scaling;
                // pos at center parent
                CanvasImage.SetValue(Canvas.LeftProperty, (CanvasWhiteBoard.Width - CanvasImage.Width) / 2);
                CanvasImage.SetValue(Canvas.TopProperty, (CanvasWhiteBoard.Height - CanvasImage.Height) / 2);

                Scaling = Math.Min(CanvasWhiteBoard.Width / CanvasImage.Width,
                    CanvasWhiteBoard.Height / CanvasImage.Height);
            }
        }


        public BitmapSource Logo
        {
            set => SetImg(value);
            get
            {
                if (Img.Source == null)
                    return null;

                ReplaceChild(ref CanvasImage, ref CanvasWhiteBoard);

                BitmapSource resize;
                if (Math.Abs(Scaling - 1) < 0.01)
                    resize = (Img.Source as BitmapSource);
                else
                    resize = (Img.Source as BitmapSource).Resize(Scaling, Scaling);

                // calc roi
                double x = (double)CanvasImage.GetValue(Canvas.LeftProperty);
                double y = (double)CanvasImage.GetValue(Canvas.TopProperty);

                var startPoint = new PointF(0, 0);
                double roiWidth = 0;
                double roiHeight = 0;
                var drawPoint = new PointF(0, 0);
                if (x < 0)
                {
                    drawPoint.X = 0;
                    startPoint.X = (float)Math.Abs(x);
                }
                else
                {
                    drawPoint.X = (float)Math.Abs(x);
                    startPoint.X = 0;
                }
                if (y < 0)
                {
                    drawPoint.Y = 0;
                    startPoint.Y = (float)Math.Abs(y);
                }
                else
                {
                    drawPoint.Y = (float)Math.Abs(y);
                    startPoint.Y = 0;
                }

                if (x + resize.PixelWidth > CanvasWhiteBoard.Width)
                {
                    roiWidth = CanvasWhiteBoard.Width - drawPoint.X;
                }
                else
                {
                    roiWidth = resize.PixelWidth - startPoint.X;
                }

                if (y + resize.PixelHeight > CanvasWhiteBoard.Height)
                {
                    roiHeight = CanvasWhiteBoard.Height - drawPoint.Y;
                }
                else
                {
                    roiHeight = resize.PixelHeight - startPoint.Y;
                }

                if (roiWidth > 0 && roiHeight > 0)
                {
                    var roi = resize.Roi(new Rectangle((int) startPoint.X, (int) startPoint.Y, (int) roiWidth, (int) roiHeight));
                    var bitmap = new Bitmap((int) CanvasWhiteBoard.Width, (int) CanvasWhiteBoard.Height);
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.Save();
                        g.DrawImage(roi, drawPoint);
                    }
                    bitmap.MakeTransparent(System.Drawing.Color.Transparent);
                    //double imgAccWidth = CanvasImage.
                    return bitmap.ToBitmapSource();
                }
                else
                {
                    var bitmap = new Bitmap((int) CanvasWhiteBoard.Width, (int) CanvasWhiteBoard.Height);
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        g.Save();
                    }
                    bitmap.MakeTransparent(System.Drawing.Color.Transparent);
                    //double imgAccWidth = CanvasImage.
                    return bitmap.ToBitmapSource();
                }
            }
        }


        #region 拖动选区
        //鼠标相对于被拖动的Canvas控件CanvasImage的坐标
        private System.Windows.Point mouseStartPosition = new System.Windows.Point();
        //鼠标相对于作为容器的Canvas控件CanvasWhiteBoard的坐标
        private System.Windows.Point mouseNowPosition = new System.Windows.Point();

        private bool dragStarted = false;
        private DateTime dragStartTime = DateTime.MinValue;
        /// <summary>
        /// 记录移动起点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStarted = true;
            dragStartTime = DateTime.Now;
            mouseStartPosition = e.GetPosition(CanvasImage);
        }

        private void CanvasWhiteBoard_OnMouseLeave(object sender, MouseEventArgs e)
        {
            dragStarted = false;
        }

        /// <summary>
        /// 计算移动终点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragStarted = false;
            ReplaceChild(ref CanvasImage, ref CanvasWhiteBoard);
            CanvasImage.ReleaseMouseCapture();
        }

        /// <summary>
        /// 实现拖动图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasImage_Move(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // 消抖
                if (dragStarted && (DateTime.Now - dragStartTime).Milliseconds > 50)
                {
                    Canvas c = CanvasImage;
                    mouseNowPosition = e.GetPosition(CanvasWhiteBoard);
                    if (!ReplaceChild(ref c, ref CanvasWhiteBoard))
                    {
                        //if (Settings.Instance.MoveHorizontalEnabled)
                        c.SetValue(Canvas.LeftProperty, mouseNowPosition.X - mouseStartPosition.X);
                        //if (Settings.Instance.MoveVerticalEnabled)
                        c.SetValue(Canvas.TopProperty, mouseNowPosition.Y - mouseStartPosition.Y);
                        c.CaptureMouse();
                    }
                    else
                    {
                        c.ReleaseMouseCapture();
                    }
                }
            }
            e.Handled = true;
        }
        #endregion



        /// <summary>
        /// 上一次使用滚轮放大时间
        /// </summary>
        private int LastMouseWhellTimestamp = 0;
        /// <summary>
        /// 滚轮加速度
        /// </summary>
        private double k = 1;
        private void Window_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Img?.Source == null)
                return;

            //if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                // 连续滚轮加速度
                if (e.Timestamp - LastMouseWhellTimestamp < 200)
                {
                    k += 1.5;
                    if (k > 15)
                        k = 15;
                }
                else if (e.Timestamp - LastMouseWhellTimestamp < 1000)
                {
                    k -= 1;
                    if (k < 1)
                        k = 1;
                }
                else
                {
                    k = 1;
                }
                LastMouseWhellTimestamp = e.Timestamp;

                double tmp = Scaling;
                tmp *= 1 + e.Delta * 0.0001 * k;

                if (CanvasImage.Width * Scaling < 5)
                    tmp = 5.0 / CanvasImage.Width;
                if (CanvasImage.Height * Scaling < 5)
                    tmp = 5.0 / CanvasImage.Height;
                Scaling = tmp;
            }
        }




        /// <summary>
        ///  防止 child 跑到 parent 外面
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private bool ReplaceChild(ref Canvas child, ref Canvas parent)
        {
            const double span = 10;
            bool ret = false;
            if (parent.ActualHeight > span * 2 && parent.ActualWidth > span * 2)
            {
                double l = Canvas.GetLeft(child);
                double t = Canvas.GetTop(child);
                double r = l + child.Width;
                double b = t + child.Height;
                if (l > parent.ActualWidth - span)
                {
                    child.SetValue(Canvas.LeftProperty, parent.ActualWidth - span);
                    ret = true;
                }
                if (r < span)
                {
                    child.SetValue(Canvas.LeftProperty, span - child.ActualWidth);
                    ret = true;
                }

                if (t > parent.ActualHeight - span)
                {
                    child.SetValue(Canvas.TopProperty, parent.ActualHeight - span);
                    ret = true;
                }

                if (b < span)
                {
                    child.SetValue(Canvas.TopProperty, span - child.ActualHeight);
                    ret = true;
                }
            }
            return ret;
        }


        public static BitmapSource GetBitmapSource(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = File.OpenRead(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public static bool? Show(Microsoft.Win32.FileDialog fileDialog)
        {
            Window win = new Window
            {
                ResizeMode = System.Windows.ResizeMode.NoResize,
                WindowStyle = System.Windows.WindowStyle.None,
                Topmost = true,
                Visibility = System.Windows.Visibility.Hidden,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = fileDialog
            };
            bool? result = false;
            win.Loaded += (s, e) =>
            {
                result = fileDialog.ShowDialog();
            };
            win.ShowDialog();
            win.Close();
            return result;

        }
        private void BtnOpenImg_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = SystemConfig.GetInstance().Language.GetText("logo_selecter_open_file_dialog_title"),
                Filter = "image|*.jpg;*.png;*.bmp|all files|*.*",
                RestoreDirectory = true
            };
            //ofd.InitialDirectory = Application.StartupPath;
            //ofd.FilterIndex = 2;
            if (Show(ofd) == true)
            {
                SetImg(GetBitmapSource(ofd.FileName));
            }
        }

        private void BtnZoomIn_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Scaling += 0.1;
        }

        private void BtnZoomOut_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Scaling -= 0.1;
        }

    }



    #region ChessboardBrushHelper
    static class ChessboardBrushHelper
    {
        private static readonly object obj = new object();
        private static Dictionary<int,ImageBrush> chessbordBrushes = new Dictionary<int, ImageBrush>();
        public static ImageBrush ChessboardBrush(int blockPixSize = 32)
        {
            lock (obj)
            {
                if (chessbordBrushes.ContainsKey(blockPixSize))
                    return chessbordBrushes[blockPixSize];
                // 绘制透明背景
                var wpen = System.Drawing.Brushes.White;
                var gpen = System.Drawing.Brushes.LightGray;
                int span = blockPixSize;
                var bg = new System.Drawing.Bitmap(span * 2, span * 2);
                using (var g = System.Drawing.Graphics.FromImage(bg))
                {
                    g.FillRectangle(wpen, new System.Drawing.Rectangle(0, 0, bg.Width, bg.Height));
                    for (var v = 0; v < span * 2; v += span)
                    {
                        for (int h = (v / (span)) % 2 == 0 ? 0 : span; h < span * 2; h += span * 2)
                        {
                            g.FillRectangle(gpen, new System.Drawing.Rectangle(h, v, span, span));
                        }
                    }
                }
                return new ImageBrush(bg.ToBitmapImage())
                {
                    Stretch = Stretch.UniformToFill,
                    TileMode = TileMode.Tile,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    Viewport = new Rect(new System.Windows.Point(0, 0), new System.Windows.Point(span * 2, span * 2)),
                    ViewportUnits = BrushMappingMode.Absolute
                };
            }
        }
    } 
    #endregion
}