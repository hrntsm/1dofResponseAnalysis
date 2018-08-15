﻿using System;
using Grasshopper.Kernel;

/// <summary>
/// コンポーネントの定義
/// </summary>
namespace GH_NewmarkBeta
{
    public class NewmarkBetaComponet : GH_Component
    {
        public NewmarkBetaComponet()
            : base("1dof Response Analysis", "1dof RA", "Response Analysis of the Single dof", "rgkr", "Response Analysis")
        {
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Mass", "M", "Lumped Mass", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stiffness", "K", "Spring Stiffness", GH_ParamAccess.item);
            pManager.AddNumberParameter("Damping ratio", "h", "Damping ratio", GH_ParamAccess.item);
            pManager.AddNumberParameter("Time Increment", "dt", "Time Increment", GH_ParamAccess.item);
            pManager.AddNumberParameter("Beta", "Beta", "Parameters of Newmark β ", GH_ParamAccess.item);
            pManager.AddIntegerParameter("N", "N", "Parameters of Newmark β ", GH_ParamAccess.item);
            pManager.AddTextParameter("WAVE", "WAVE", "Parameters of Newmark β ", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Acceleration", "Acc", "output Acceleration", GH_ParamAccess.item);
            pManager.AddNumberParameter("Velocity", "Vel", "output Velocity", GH_ParamAccess.item);
            pManager.AddNumberParameter("Displacement", "Disp", "output Displacement", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // パラメータの定義 ＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
            double M = double.NaN;    // 質量
            double K = double.NaN;    // 剛性
            double h = double.NaN;    // 減衰定数
            double dt = double.NaN;   // 時間刻み
            double beta = double.NaN; // 解析パラメータ
            int N = 0;                // 波形データ数
            string wave_str = "0";

            // grasshopper からデータ取得　＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
            if (!DA.GetData(0, ref M)) { return; }
            if (!DA.GetData(1, ref K)) { return; }
            if (!DA.GetData(2, ref h)) { return; }
            if (!DA.GetData(3, ref dt)) { return; }
            if (!DA.GetData(4, ref beta)) { return; }
            if (!DA.GetData(5, ref N)) { return; }
            if (!DA.GetData(6, ref wave_str)) { return; }

            //　地震波データの処理　＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
            //　カンマ区切りで波形を入力するので、カンマで区切り配列に入れている
            char[] delimiter = { ',' };    //分割文字
            double[] wave = new double[N];
            string[] wk;
            wk = wave_str.Split(delimiter);  //カンマで分割
            for (int i = 0; i < N; i++)
            {
                wave[i] = double.Parse(wk[i]);
            }

            //　応答解析　＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
            double[] out_a = new double[N];
            double[] out_v = new double[N];
            double[] out_d = new double[N];

            Solver.NewmarkBeta_solver slv = new Solver.NewmarkBeta_solver();
            slv.NewmarkBeta(M, K, h, dt, beta, N, wave, ref out_a, ref out_v, ref out_d);

            // grassshopper へのデータ出力　＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
            DA.SetDataList(0, out_a);
            DA.SetDataList(1, out_v);
            DA.SetDataList(2, out_d);
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("419c3a3a-cc48-4717-9cef-5f5647a5ecfc"); }
        }
    }
}
/// <summary>
/// 解析関連
/// </summary>
namespace Solver
{
    /// <summary>
    /// Newmarkβ法で応答解析を行うクラス
    /// </summary>
    public class NewmarkBeta_solver
    {
        public void NewmarkBeta(double m, double k, double h, double dt, double beta, int N, double[] Ag, ref double[] out_a, ref double[] out_v, ref double[] out_d)
        {
            // 解析関連パラメータ-----------------------------
            double a = 0.0, v = 0.0, x = 0.0, an = 0.0, vn = 0.0;
            double a0 = Ag[0];                   // 初期加速度
            double v0 = 0.0;                     // 初期速度
            double d0 = 0.0;                     // 初期変位
            double c = 2 * h * Math.Sqrt(m * k); // 粘性減衰定数

            for (int n = 0; n < N; n++)
            {
                if (n == 0)  // t = 0 の時
                {
                    a = a0;
                    v = v0;
                    x = d0;
                }
                else       //  t ≠ 0 の時
                {
                    a = -(c * (v + a * dt / 2.0) + k * (x + v * dt + a * (dt * dt) * (0.5 - beta)) + m * Ag[n])
                        / (m + c * dt / 2.0 + k * (dt * dt) * beta);
                    v = v + (1.0 / 2.0) * (a + an) * dt;
                    x = x + vn * dt + beta * (a + 2.0 * an) * (dt * dt);
                }
                // 結果を出力マトリクスに入れる。
                out_a[n] = a;
                out_v[n] = v;
                out_d[n] = x;

                an = a;
                vn = v;
            }
        }
    }
}