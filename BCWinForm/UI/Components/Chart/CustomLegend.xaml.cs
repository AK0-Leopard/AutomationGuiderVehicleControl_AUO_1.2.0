using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts.Wpf;

namespace com.mirle.ibg3k0.ohxc.winform.UI.Components.SubPage
{
    public partial class CustomLegend : UserControl, IChartLegend
    {
        private List<SeriesViewModel> _series;

        public CustomLegend()
        {
            InitializeComponent();
            _series = new List<SeriesViewModel>();
            _series.Add(new SeriesViewModel()
            {
                Fill = Brushes.Red,
                Title = "Down"
            }
            );
            _series.Add(new SeriesViewModel()
            {
                Fill = Brushes.Lime,
                Title = "Run"
            }
            );
            _series.Add(new SeriesViewModel()
            {
                Fill = Brushes.LightGray,
                Title = "Idle"
            }
            );
            DataContext = this;
        }

        public List<SeriesViewModel> Series
        {
            get { return _series; }
            set
            {
                //_series = value;
                //OnPropertyChanged("Series");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}