using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using ScottPlot;
using ScottPlot.WinForms;

// ALIASING
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;
using Label = System.Windows.Forms.Label;
using Image = System.Drawing.Image;
using System.Numerics;

namespace GasTurbineFaultDetector
{
    public partial class Form1 : Form
    {
        // --- SETTINGS ---
        private const int HISTORY_LEN = 300;
        private const int FFT_LEN = 256;
        private const string IMAGE_FILENAME = "turbine.image.png";

        // --- PLOT OBJECTS ---
        private FormsPlot pAccelTime, pAccelFFT, pSDomain;
        private FormsPlot pStrainTime, pStrainFFT, pZDomain;
        private FormsPlot pMicTime, pMicFFT, pTachoTime;
        private FormsPlot pTempTime, pTempFFT, pTachoFFT;

        // --- DATA HANDLERS ---
        private ScottPlot.Plottables.DataStreamer streamAccel;
        private ScottPlot.Plottables.DataStreamer streamStrain;
        private ScottPlot.Plottables.DataStreamer streamMic;
        private ScottPlot.Plottables.DataStreamer streamTemp;
        private ScottPlot.Plottables.DataStreamer streamTacho;

        // FFT Arrays
        private double[] fftAccelData = new double[FFT_LEN];
        private double[] fftStrainData = new double[FFT_LEN];
        private double[] fftMicData = new double[FFT_LEN];
        private double[] fftTempData = new double[FFT_LEN];
        private double[] fftTachoData = new double[FFT_LEN];

        // Markers
        private ScottPlot.Plottables.Marker[] sPoles = new ScottPlot.Plottables.Marker[2];
        private ScottPlot.Plottables.Marker[] zPoles = new ScottPlot.Plottables.Marker[2];

        // Controls
        private TrackBar tbRPM, tbVibration, tbStrain, tbMic, tbTemp;
        private Label lblValRPM, lblValVib, lblValStrain, lblValMic, lblValTemp;
        private System.Windows.Forms.Timer guiTimer;

        // State Variables
        private double timeTotal = 0;
        private Random rnd = new Random();
        private bool isUpdating = false;

        public Form1()
        {
            InitializeDashboardUI();
            SetupSimulation();
        }

        private void InitializeDashboardUI()
        {
            this.Text = "Gas Turbine Fault Simulator (With Scenario Buttons)";
            this.Size = new Size(1800, 1000);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.WhiteSmoke;

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.Controls.Add(mainLayout);

            // === PANEL KIRI ===
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            mainLayout.Controls.Add(leftPanel, 0, 0);

            // 1. Gambar 3D
            PictureBox picTurbine = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 300,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            leftPanel.Controls.Add(picTurbine);
            LoadTurbineImage(picTurbine);

            // 2. TOMBOL SKENARIO (BARU)
            GroupBox grpScenario = new GroupBox
            {
                Text = "SIMULATION SCENARIOS",
                Dock = DockStyle.Top,
                Height = 80,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            // Tombol Fault
            Button btnFault = new Button
            {
                Text = "TRIGGER FAULT",
                BackColor = Color.Red,
                ForeColor = Color.White,
                Location = new Point(10, 25),
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat
            };
            btnFault.Click += (s, e) => TriggerFaultScenario();

            // Tombol Normal
            Button btnNormal = new Button
            {
                Text = "RESET NORMAL",
                BackColor = Color.Green,
                ForeColor = Color.White,
                Location = new Point(180, 25),
                Size = new Size(160, 40),
                FlatStyle = FlatStyle.Flat
            };
            btnNormal.Click += (s, e) => TriggerNormalScenario();

            grpScenario.Controls.Add(btnFault);
            grpScenario.Controls.Add(btnNormal);
            // Tambahkan di bawah gambar
            leftPanel.Controls.Add(grpScenario);
            grpScenario.BringToFront(); // Pastikan terlihat

            // 3. Input Controls (Scrollable)
            FlowLayoutPanel inputPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            // Geser panel input ke bawah agar tidak menutupi tombol
            inputPanel.Padding = new Padding(10, 380, 10, 10);
            leftPanel.Controls.Add(inputPanel);

            // Tambahkan Slider
            AddSensorControl(inputPanel, "Optical Tachometer", "Input RPM", 1000, 20000, 15000, out tbRPM, out lblValRPM);
            AddSensorControl(inputPanel, "AC192 Accelerometer", "Vibration (g)", 0, 50, 5, out tbVibration, out lblValVib);
            AddSensorControl(inputPanel, "Foil Strain Gauge", "Strain (µε)", 0, 500, 100, out tbStrain, out lblValStrain);
            AddSensorControl(inputPanel, "INMP441 Microphone", "Noise Level (dB)", 0, 100, 50, out tbMic, out lblValMic);
            AddSensorControl(inputPanel, "RTD PT1000", "Temp (°C)", 0, 1000, 650, out tbTemp, out lblValTemp);

            // === PANEL KANAN (GRID) ===
            TableLayoutPanel chartGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(0)
            };
            for (int i = 0; i < 3; i++) chartGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            for (int i = 0; i < 4; i++) chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            mainLayout.Controls.Add(chartGrid, 1, 0);

            // --- INIT PLOTS ---
            pAccelTime = CreatePlot("AC192 Acceleration", "mV");
            streamAccel = SetupStreamer(pAccelTime, 1000, 2000);
            chartGrid.Controls.Add(pAccelTime, 0, 0);

            pAccelFFT = CreatePlot("Vibration FFT", "Mag");
            var sigAccel = pAccelFFT.Plot.Add.Signal(fftAccelData);
            sigAccel.Color = Colors.Blue; sigAccel.LineWidth = 2;
            pAccelFFT.Plot.Axes.SetLimitsY(0, 300);
            chartGrid.Controls.Add(pAccelFFT, 1, 0);

            pSDomain = CreateSDomainPlot();
            chartGrid.Controls.Add(pSDomain, 2, 0);

            pStrainTime = CreatePlot("Strain Gauge", "µε");
            streamStrain = SetupStreamer(pStrainTime, -300, 300);
            chartGrid.Controls.Add(pStrainTime, 0, 1);

            pStrainFFT = CreatePlot("Strain FFT", "Mag");
            var sigStrain = pStrainFFT.Plot.Add.Signal(fftStrainData);
            sigStrain.Color = Colors.Green; sigStrain.LineWidth = 2;
            pStrainFFT.Plot.Axes.SetLimitsY(0, 200);
            chartGrid.Controls.Add(pStrainFFT, 1, 1);

            pZDomain = CreateZDomainPlot();
            chartGrid.Controls.Add(pZDomain, 2, 1);

            pMicTime = CreatePlot("INMP441 Audio", "dBFS");
            streamMic = SetupStreamer(pMicTime, -15000, 15000);
            chartGrid.Controls.Add(pMicTime, 0, 2);

            pMicFFT = CreatePlot("Audio FFT", "Mag");
            var sigMic = pMicFFT.Plot.Add.Signal(fftMicData);
            sigMic.Color = Colors.Orange; sigMic.LineWidth = 2;
            pMicFFT.Plot.Axes.SetLimitsY(0, 300);
            chartGrid.Controls.Add(pMicFFT, 1, 2);

            pTachoTime = CreatePlot("Tacho Pulse", "V");
            streamTacho = SetupStreamer(pTachoTime, -1, 6);
            chartGrid.Controls.Add(pTachoTime, 2, 2);

            pTempTime = CreatePlot("PT1000 Temp", "Ω");
            streamTemp = SetupStreamer(pTempTime, 3300, 3600);
            chartGrid.Controls.Add(pTempTime, 0, 3);

            pTempFFT = CreatePlot("Temp Noise FFT", "Mag");
            var sigTemp = pTempFFT.Plot.Add.Signal(fftTempData);
            sigTemp.Color = Colors.Red; sigTemp.LineWidth = 2;
            pTempFFT.Plot.Axes.SetLimitsY(0, 50);
            chartGrid.Controls.Add(pTempFFT, 1, 3);

            pTachoFFT = CreatePlot("RPM Spectrum", "Mag");
            var sigTacho = pTachoFFT.Plot.Add.Signal(fftTachoData);
            sigTacho.Color = Colors.Purple; sigTacho.LineWidth = 2;
            pTachoFFT.Plot.Axes.SetLimitsY(0, 200);
            chartGrid.Controls.Add(pTachoFFT, 2, 3);
        }

        // --- SCENARIO LOGIC ---
        private void TriggerFaultScenario()
        {
            // Set Slider ke Nilai Ekstrim (Fault Condition)
            tbRPM.Value = 13500;        // RPM drop/unstable
            tbVibration.Value = 45;     // 4.5g (Very High Vibration)
            tbStrain.Value = 450;       // High Strain
            tbMic.Value = 95;           // Very Loud Noise
            tbTemp.Value = 850;         // Overheat (850°C)

            UpdateLabels(); // Update teks angka di sebelah slider
        }

        private void TriggerNormalScenario()
        {
            // Set Slider ke Nilai Aman (Normal Condition)
            tbRPM.Value = 15000;        // Optimal RPM
            tbVibration.Value = 5;      // 0.5g (Low Vib)
            tbStrain.Value = 100;       // Low Strain
            tbMic.Value = 50;           // Normal Sound
            tbTemp.Value = 650;         // Normal Temp

            UpdateLabels();
        }

        private void UpdateLabels()
        {
            // Update teks label agar sesuai dengan posisi slider baru
            if (lblValRPM != null) lblValRPM.Text = tbRPM.Value.ToString();
            if (lblValVib != null) lblValVib.Text = tbVibration.Value.ToString();
            if (lblValStrain != null) lblValStrain.Text = tbStrain.Value.ToString();
            if (lblValMic != null) lblValMic.Text = tbMic.Value.ToString();
            if (lblValTemp != null) lblValTemp.Text = tbTemp.Value.ToString();
        }

        // --- DRAWING & PLOTTING HELPERS ---
        private void LoadTurbineImage(PictureBox pb)
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, IMAGE_FILENAME);
            if (File.Exists(imagePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        pb.Image = Image.FromStream(fs);
                    }
                }
                catch { DrawErrorPlaceholder(pb, imagePath); }
            }
            else { DrawErrorPlaceholder(pb, imagePath); }
        }

        private void DrawErrorPlaceholder(PictureBox pb, string path)
        {
            if (pb.Width <= 0) return;
            Bitmap bmp = new Bitmap(pb.Width, pb.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.DrawString("IMAGE NOT FOUND", new Font("Arial", 12, FontStyle.Bold), Brushes.Red, 10, 10);
                g.DrawString("Please put 'turbine.image.png' in bin/Debug", new Font("Arial", 8), Brushes.White, 10, 30);
                g.DrawEllipse(new Pen(Color.Cyan), 100, 100, 100, 100);
            }
            pb.Image = bmp;
        }

        private ScottPlot.Plottables.DataStreamer SetupStreamer(FormsPlot plot, double minY, double maxY)
        {
            var streamer = plot.Plot.Add.DataStreamer(HISTORY_LEN);
            streamer.ViewScrollLeft();
            streamer.LineColor = Colors.DodgerBlue;
            streamer.LineWidth = 2;
            plot.Plot.Axes.SetLimitsY(minY, maxY);
            plot.Plot.Axes.Margins(0, 0);
            return streamer;
        }

        private FormsPlot CreatePlot(string title, string yLabel)
        {
            var plot = new FormsPlot { Dock = DockStyle.Fill };
            plot.Plot.Title(title);
            plot.Plot.Axes.Title.Label.FontSize = 11;
            plot.Plot.Axes.Left.Label.Text = yLabel;
            plot.Plot.Axes.Left.Label.FontSize = 9;
            plot.Plot.Axes.Bottom.Label.Text = "";
            plot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 0;

            plot.Plot.FigureBackground.Color = Colors.White;
            plot.Plot.DataBackground.Color = Colors.White;
            plot.Plot.Axes.Color(Colors.Black);
            plot.Plot.Grid.MajorLineColor = Colors.LightGray.WithAlpha(0.5);
            return plot;
        }

        private FormsPlot CreateSDomainPlot()
        {
            var plot = CreatePlot("S-Domain", "Imag");
            plot.Plot.Axes.Bottom.Label.Text = "Real";
            plot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 8;

            var poly = plot.Plot.Add.Polygon(new Coordinates[] {
                new Coordinates(-20, -20), new Coordinates(0, -20),
                new Coordinates(0, 20), new Coordinates(-20, 20)
            });
            poly.FillStyle.Color = Colors.Green.WithAlpha(0.2);
            poly.LineStyle.Color = Colors.Transparent;

            plot.Plot.Add.VerticalLine(0, 1, Colors.Black);
            plot.Plot.Add.HorizontalLine(0, 1, Colors.Black);

            sPoles[0] = plot.Plot.Add.Marker(0, 0);
            sPoles[1] = plot.Plot.Add.Marker(0, 0);
            foreach (var m in sPoles) { m.Shape = MarkerShape.Cross; m.Size = 10; m.Color = Colors.Blue; m.LineWidth = 2; }

            plot.Plot.Axes.SetLimits(-5, 1, -5, 5);
            return plot;
        }

        private FormsPlot CreateZDomainPlot()
        {
            var plot = CreatePlot("Z-Domain", "Imag");
            plot.Plot.Axes.Bottom.Label.Text = "Real";
            plot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 8;

            var circle = plot.Plot.Add.Circle(0, 0, 1);
            circle.LineStyle.Pattern = LinePattern.Dashed;
            circle.LineColor = Colors.Gray;
            circle.FillStyle.Color = Colors.Transparent;

            plot.Plot.Add.VerticalLine(0, 1, Colors.Black);
            plot.Plot.Add.HorizontalLine(0, 1, Colors.Black);

            zPoles[0] = plot.Plot.Add.Marker(0, 0);
            zPoles[1] = plot.Plot.Add.Marker(0, 0);
            foreach (var m in zPoles) { m.Shape = MarkerShape.Cross; m.Size = 10; m.Color = Colors.Blue; m.LineWidth = 2; }

            plot.Plot.Axes.SetLimits(-1.2, 1.2, -1.2, 1.2);
            return plot;
        }

        private void AddSensorControl(FlowLayoutPanel panel, string title, string formula, int min, int max, int def, out TrackBar tb, out Label lblVal)
        {
            GroupBox gb = new GroupBox { Text = title, Size = new Size(330, 80), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            Label lblF = new Label { Text = formula, Location = new Point(10, 18), Size = new Size(310, 15), ForeColor = Color.Gray, Font = new Font("Consolas", 8) };

            tb = new TrackBar { Minimum = min, Maximum = max, Value = def, Location = new Point(5, 35), Size = new Size(260, 40), TickFrequency = (max - min) / 10 };
            lblVal = new Label { Text = def.ToString(), Location = new Point(270, 40), Size = new Size(50, 20), TextAlign = ContentAlignment.MiddleRight };

            TrackBar _tb = tb; Label _lbl = lblVal;
            tb.Scroll += (s, e) => { _lbl.Text = _tb.Value.ToString(); };

            gb.Controls.Add(lblF); gb.Controls.Add(tb); gb.Controls.Add(lblVal);
            panel.Controls.Add(gb);
        }

        // --- SIMULASI REAL-TIME ---
        private void SetupSimulation()
        {
            guiTimer = new System.Windows.Forms.Timer { Interval = 100 };
            guiTimer.Tick += UpdateSimulation;
            guiTimer.Start();
        }

        private void UpdateSimulation(object sender, EventArgs e)
        {
            if (isUpdating) return;
            isUpdating = true;

            try
            {
                if (tbRPM == null) return;

                double rpm = tbRPM.Value;
                double vibG = tbVibration.Value / 10.0;
                double strain = tbStrain.Value;
                double micDB = tbMic.Value;
                double tempC = tbTemp.Value;

                double freq = rpm / 60.0;
                double omega = 2 * Math.PI * freq;

                int points = 10;
                double[] chunkAccel = new double[points];
                double[] chunkStrain = new double[points];
                double[] chunkMic = new double[points];
                double[] chunkTemp = new double[points];
                double[] chunkTacho = new double[points];

                for (int i = 0; i < points; i++)
                {
                    timeTotal += 0.002;
                    double noise = (rnd.NextDouble() - 0.5) * 0.2;
                    double wave = Math.Sin(omega * timeTotal);

                    chunkAccel[i] = 1500 + (100 * ((vibG * wave) + noise));
                    chunkStrain[i] = (strain * wave) + (noise * 10);
                    chunkMic[i] = (micDB * 100 * Math.Sin(3 * omega * timeTotal)) + (noise * 500);
                    chunkTemp[i] = (1000 * (1 + 0.00385 * tempC)) + (noise * 5);
                    chunkTacho[i] = wave > 0.9 ? 5 : 0;
                }

                streamAccel.AddRange(chunkAccel);
                streamStrain.AddRange(chunkStrain);
                streamMic.AddRange(chunkMic);
                streamTemp.AddRange(chunkTemp);
                streamTacho.AddRange(chunkTacho);

                UpdateFFT(fftAccelData, rpm, 200);
                UpdateFFT(fftStrainData, rpm, 150);
                UpdateFFT(fftMicData, rpm * 3, 200);
                UpdateFFT(fftTempData, 0, 10);
                UpdateFFT(fftTachoData, rpm, 100);

                UpdatePoles(vibG, rpm);

                pAccelTime.Refresh(); pAccelFFT.Refresh();
                pStrainTime.Refresh(); pStrainFFT.Refresh();
                pMicTime.Refresh(); pMicFFT.Refresh();
                pTempTime.Refresh(); pTempFFT.Refresh();
                pTachoTime.Refresh(); pTachoFFT.Refresh();
                pSDomain.Refresh(); pZDomain.Refresh();
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void UpdateFFT(double[] targetArray, double targetFreqRPM, double peakHeight)
        {
            Array.Clear(targetArray, 0, targetArray.Length);
            double freqIndex = (targetFreqRPM / 60.0) / 2.0;
            int idx = (int)freqIndex;

            if (idx > 0 && idx < targetArray.Length)
                targetArray[idx] = peakHeight + rnd.Next(10);

            for (int k = 0; k < targetArray.Length; k += 5) targetArray[k] = rnd.NextDouble() * 5;
        }

        private void UpdatePoles(double vibG, double rpm)
        {
            double damping = 0.5 - (vibG * 0.08);
            double freq = rpm / 60.0 / 20.0;

            sPoles[0].Location = new Coordinates(-damping, freq);
            sPoles[1].Location = new Coordinates(-damping, -freq);

            ScottPlot.Color statusColor = (damping < 0.1) ? Colors.Red : Colors.Blue;
            sPoles[0].Color = statusColor; sPoles[1].Color = statusColor;

            double T = 0.1;
            Complex s = new Complex(-damping, freq);
            Complex z = Complex.Exp(s * T);

            zPoles[0].Location = new Coordinates(z.Real, z.Imaginary);
            zPoles[1].Location = new Coordinates(z.Real, -z.Imaginary);
            zPoles[0].Color = statusColor; zPoles[1].Color = statusColor;
        }
    }
}