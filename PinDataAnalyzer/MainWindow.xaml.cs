using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;

using System.IO;

namespace PinDataAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonProcess_Click(object sender, RoutedEventArgs e)
        {
            string readPath = @"C:\Users\YuOko\OneDrive\Desktop\Work\ROBATEST\ТЗ_Robat1.txt";
            DateTime start = DateTime.Now;
            string text = "";
            using (StreamReader sr = new StreamReader(readPath))
            {
                
                while (!sr.EndOfStream)
                {
                    string fileLine = sr.ReadLine();
                    text += fileLine;
                }
            }
            DateTime end = DateTime.Now;
            MessageBox.Show(string.Format("start {0}, end {1}", start, end));
        }
    }
}
