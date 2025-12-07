42343edx543edxs# Multi-Sensor Fault Detection & Digital Twin Simulation for Gas Turbine Blades 🚀

![Status](https://img.shields.io/badge/Status-Completed-success) ![Language](https://img.shields.io/badge/Language-C%23-blue) ![Framework](https://img.shields.io/badge/Framework-.NET%20Windows%20Forms-purple)

## 📖 Overview
This project is a **Digital Twin Simulation** framework designed to monitor and detect faults in Gas Turbine blades. It simulates a multi-sensor environment to capture critical operational parameters and uses advanced signal processing techniques to identify incipient failures (e.g., blade cracks).

## 🎥 Preview
![Dashboard Preview](dashboard_full.png)


## ✨ Key Features
* **Physics-Based Modeling:** Simulates real-world behavior of 5 industrial sensors:
    * `CTC AC192` (Accelerometer/Vibration)
    * `TML Series F` (Strain Gauge)
    * `INMP441` (MEMS Microphone)
    * `PT1000` (RTD Temperature)
    * `Optical Tachometer` (RPM)
* **Advanced Signal Processing:**
    * **FFT (Fast Fourier Transform):** Frequency domain analysis to detect harmonic distortions.
    * **Pole-Zero Analysis:** Dynamic stability assessment in S-Domain & Z-Domain.
* **Fault Simulation:** Capable of simulating "Blade Crack" scenarios to demonstrate system response under critical failure.
* **Interactive Dashboard:** Real-time data visualization using **ScottPlot 5**.

## 🛠️ Technology Stack
* **Language:** C#
* **GUI Framework:** Windows Forms (.NET)
* **Charting Library:** ScottPlot 5.0 (WinForms)
* **Math Library:** System.Numerics

## 🚀 How to Run
1.  Clone this repository:
    ```bash
    git clone [https://github.com/kemed99/Gas-Turbine-Digital-Twin-Fault-Detection.git](https://github.com/kemed99/Gas-Turbine-Digital-Twin-Fault-Detection.git)
    ```
2.  Open the `.sln` file in **Visual Studio 2022**.
3.  Ensure all NuGet packages (ScottPlot.WinForms) are restored.
4.  **Important:** Make sure the image file `turbine.image.png` is present in the `bin/Debug` folder after building.
5.  Click **Start** to run the simulation.

## 📊 Scientific Background
This project implements the concepts of **Condition-Based Monitoring (CBM)**. The fault detection logic relies on the principle that mechanical faults (like cracks) introduce specific spectral signatures (sidebands) and reduce system damping, which can be visualized via Pole-Zero mapping.

## 👨‍💻 Author
**[Zhafran Ahmed]**
* Institution: Institut Teknologi Sepuluh Nopember (ITS)
* Department: Instrumentation Engineering

---
*Created for Final Project / Thesis Research.*
