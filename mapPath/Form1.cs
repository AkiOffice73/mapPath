using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mapPath
{
    public partial class Form1 : Form
    {
        // -----------------------------------------------------------------
        public double 航點斜率 = 10;
        public double 航線間距 = 20;
        // -----------------------------------------------------------------

        /// <summary> 紀錄航點圍籬的所有錨點 </summary>
        List<PointF> Fence = new List<PointF>();
        /// <summary> 紀錄航點圍籬每條外牆的斜率 </summary>
        List<double> slopes = new List<double>();
        /// <summary> 紀錄航點圍籬每條外牆的斜截式Y軸位移量 </summary>
        List<double> shifts = new List<double>();

        int FencePointRadius = 5; //航點顯示的半徑(用於繪圖,顯示)

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Fence.Add(new PointF(150 + 025, 150 + 000));
            Fence.Add(new PointF(150 + 272, 150 + 025));
            Fence.Add(new PointF(150 + 205, 150 + 207));
            Fence.Add(new PointF(150 + 000, 150 + 208));

            calSlopeShift();
        }
        void calSlopeShift()
        {
            slopes.Clear();
            shifts.Clear();
            int length = Fence.Count;
            //cal slope,shift
            for (int j = 0; j < length; j++)
            {
                int nextJ = (j + 1) % length;

                double slope = double.MaxValue;
                double shift = 0;
                if (Fence[nextJ].Y - Fence[j].Y != 0)
                {
                    slope = (Fence[nextJ].Y - Fence[j].Y) / (Fence[nextJ].X - Fence[j].X);
                    shift = Fence[j].Y - slope * Fence[j].X;
                }
                slopes.Add(slope);
                shifts.Add(shift);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = CreateGraphics();

            //draw fence
            DrawFence(g);

            //draw waypointLine
            DrawWaypointLine(g);

            //draw waypointLineController
            DrawWaypointLineController(g);

        }
        void DrawFence(Graphics g)
        {
            Pen pen = new Pen(Color.Red);
            int length = Fence.Count;
            for (int i = 0; i < length; i++)
            {
                g.DrawEllipse(pen, PointRect(Fence[i], FencePointRadius));
                g.DrawLine(pen, Fence[i], Fence[(i + 1) % Fence.Count]);
            }
        }
        void DrawWaypointLine(Graphics g)
        {
            Pen penBlue = new Pen(Color.Blue);
            int length = Fence.Count;
            double WaySlope = 航點斜率;
            double WaySpace = 航線間距;

            //find WaypointFindLine forloop Start/End X (boundary Y_Shift)
            double  wayShiftA = 0, 
                    wayShiftB = 0;
            int hitCountA = int.MaxValue, 
                hitCountB = int.MaxValue;
            for (int i = 0; i < length; i++)
            {
                PointF item = Fence[i];
                double tmpShift = calShift(WaySlope, item);

                //test Fence HitCount
                int hit = 0;
                for (int j = 0; j < length; j++)
                {
                    Point tempP = GetXPoint(WaySlope, tmpShift, slopes[j], shifts[j]);
                    if (InFenceRangeXY(tempP, j))
                        hit++;
                }

                //sort hitCount A < B
                if (hit < hitCountA)
                {
                    hitCountB = hitCountA;
                    wayShiftB = wayShiftA;
                    hitCountA = hit;
                    wayShiftA = tmpShift;
                }
                else if (hit < hitCountB)
                {
                    hitCountB = hit;
                    wayShiftB = tmpShift;
                }

            }
            //sort wayShift A < B
            if (wayShiftA > wayShiftB)
            {
                double tmp = wayShiftB;
                wayShiftB = wayShiftA;
                wayShiftA = tmp;
            }

            //cal WaypointFindLine shift step
            int forJ_ShiftStep = Math.Abs((int)(WaySpace * (1d / Math.Cos(Math.Atan(WaySlope)))));
            for (int j = (int)wayShiftA; j <= (int)wayShiftB; j += (int)forJ_ShiftStep)
            {
                //draw WaypointFindLine
                Point p1 = new Point(0, calYFromSlope(WaySlope, j, 0));
                Point p2 = new Point(this.Width, calYFromSlope(WaySlope, j, this.Width));
                g.DrawLine(new Pen(Color.YellowGreen), p1, p2);

                for (int i = 0; i < length; i++)
                {
                    Point TempXPoint = GetXPoint(WaySlope, j, slopes[i], shifts[i]);
                    //if inFence: draw crossPoint
                    if (InFenceRangeXY(TempXPoint, i))
                    {
                        g.DrawEllipse(penBlue, PointRect(TempXPoint, FencePointRadius / 2));
                    }
                }
            }
        }
        Point WaypointLineControllerCenter = new Point(50, 50);
        int WaypointLineControllerRadius = 45;
        void DrawWaypointLineController(Graphics g)
        {
            Pen p = new Pen(Color.Black);
            g.FillEllipse(Brushes.Azure, PointRect(WaypointLineControllerCenter, WaypointLineControllerRadius));
            double theta = Math.Atan(航點斜率);
            Point p1 = new Point();


        }

        /// <summary>
        /// 檢測指定的點是否在構成航點圍籬的兩錨點之間(未檢測是否正好在外牆上)
        /// </summary>
        /// <param name="px">欲檢測的點</param>
        /// <param name="fenceIndex">欲檢測的航點圍籬之外牆索引</param>
        /// <returns></returns>
        bool InFenceRangeXY(Point px,int fenceIndex)
        {
            int i = fenceIndex, nextI = (fenceIndex + 1) % Fence.Count;
            bool InRangeX = (px.X >= Fence[i].X && px.X <= Fence[nextI].X) || (px.X <= Fence[i].X && px.X >= Fence[nextI].X);
            bool InRangeY = (px.Y >= Fence[i].Y && px.Y <= Fence[nextI].Y) || (px.Y <= Fence[i].Y && px.Y >= Fence[nextI].Y);
            return InRangeX && InRangeY;
        }
        /// <summary>
        /// 取得兩線交點
        /// </summary>
        /// <param name="way_slope">第一點斜率</param>
        /// <param name="way_shift">第一點Y偏移</param>
        /// <param name="fence_slope">第二點斜率</param>
        /// <param name="fence_shift">第二點Y偏移</param>
        /// <returns></returns>
        Point GetXPoint(double way_slope, double way_shift, double fence_slope, double fence_shift)
        {
            Point p = new Point();

            double dSlope = (fence_slope - way_slope);
            double dShift = (fence_shift - way_shift);
            double tempX = (-dShift / dSlope);
            double tempY = (tempX * fence_slope + fence_shift);
            return new Point((int)Math.Round(tempX), (int)Math.Round(tempY));
        }
        /// <summary>
        /// 以斜截式計算Y
        /// </summary>
        /// <param name="slope"></param>
        /// <param name="shift"></param>
        /// <param name="X"></param>
        /// <returns></returns>
        int calYFromSlope(double slope, double shift, int X)
        {
            return (int)(slope * X + shift);
        }
        /// <summary>
        /// 以斜截式計算X
        /// </summary>
        /// <param name="slope"></param>
        /// <param name="shift"></param>
        /// <param name="X"></param>
        /// <returns></returns>
        int calXFromSlope(double slope, double shift, int Y)
        {
            return (int)((Y - shift) / slope);
        }
        /// <summary>求指定斜率經過指定一點所需的位移量</summary>
        /// <param name="slope"></param>
        /// <param name="pF"></param>
        /// <returns></returns>
        double calShift(double slope,PointF pF)
        {
            return pF.Y - slope * pF.X;
        }

        /// <summary>
        /// 以點座標與半徑取得外接矩形，用於DrawEllipse方法
        /// </summary>
        /// <param name="center"></param>
        /// <param name="Radius"></param>
        /// <returns></returns>
        Rectangle PointRect(PointF center,int Radius)
        {
            int r2 = Radius + Radius;
            return new Rectangle((int)center.X - Radius, (int)center.Y - Radius, r2, r2);
        }

        // --------------------------------------------------------------------------------
#region Input Event Methon
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

        }
        int SelectPointIndex = -1;
        int SelectPointShiftX = -1;
        int SelectPointShiftY = -1;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            int length = Fence.Count;
            for (int i = 0; i < length; i++)
            {
                if (Math.Abs(Fence[i].X - e.X) < FencePointRadius &&
                    Math.Abs(Fence[i].Y - e.Y) < FencePointRadius
                    )
                {
                    SelectPointIndex = i;
                    SelectPointShiftX = (int)Fence[i].X - e.X;
                    SelectPointShiftY = (int)Fence[i].Y - e.Y;
                    break;
                }

            }
        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (SelectPointIndex >= 0)
            {
                Fence[SelectPointIndex] = new PointF(e.X + SelectPointShiftX, e.Y + SelectPointShiftY);
                calSlopeShift();
                this.Refresh();
            }
        }
        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            SelectPointIndex = -1;
        }
        private void Form1_MouseLeave(object sender, EventArgs e)
        {
            SelectPointIndex = -1;
        }
#endregion
        // --------------------------------------------------------------------------------
    }
}
