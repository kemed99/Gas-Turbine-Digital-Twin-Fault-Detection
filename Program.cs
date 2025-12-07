using GasTurbineFaultDetector;
using System;
using System.Windows.Forms;

namespace GasTurbineFaultDetector // Namespace ini HARUS SAMA dengan Form1.cs
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Ini baris yang tadi error. Sekarang aman karena namespace sudah sama.
            Application.Run(new Form1());
        }
    }
}