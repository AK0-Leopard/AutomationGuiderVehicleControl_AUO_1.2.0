using System;
using System.Collections.Generic;
using System.IO;
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
using com.mirle.ibg3k0.bc.winform.App;
using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.App;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;


namespace com.mirle.ibg3k0.ohxc.winform.UI.Components.SubPage
{
    /// <summary>
    /// uc_SP_Chart.xaml 的互動邏輯
    /// </summary>
    public partial class uc_SP_Chart : UserControl
    {

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public string[] TimeLabels { get; set; }
        public Func<double, string> Formatter { get; set; }
        public Func<ChartPoint, string> PointLabel { get; set; }

        PieChart[] pieCharts = null;
        //public ChartValues<CustomVm> Customs { get; set; }
        BCApplication bcApp = null;
        SCApplication scApp = null;
        //List<ACMD_MCS> cmdMCSList = null;
        //List<HCMD_MCS> hcmdMCSList = null;
        //List<ACMD_OHTC> ohtcCMDList = null;
        List<HCMD_OHTC> hisOHTCCMDList = null;
        List<ALARM> alarmList = null;
        List<RawChartData> rawDataList = null;
        RawChartData Cutleft = null;
        //List<CustomVm> vmList = null;
        public uc_SP_Chart()
        {
            InitializeComponent();
            DataContext = this;
            //InitializeComponent();
            //this.Name = "uc_SP_Chart";
            ////scApp = SCApplication.getInstance();
            //initUI();
        }
        
         public void Start(BCApplication _bcApp)
        {
            bcApp = _bcApp;
            scApp = bcApp.SCApplication;
            initUI();
            List<string> numberlist = new List<string>();
            for(int i = 1; i <= 30; i++)
            {
                numberlist.Add(i.ToString());
            }
            comboBox.ItemsSource = numberlist;
        }

        public class RawChartData
        {
            public DateTime startTime;
            public DateTime endTime;
            public string vehicle_id;
            public string status;
            public string info;
        }
        private void initUI()
        {
            #region barchart
            DateTime today = DateTime.Now.Date;
            DateTime yesterday = today.AddDays(-1).Date;
            DateTime yesterdayBeforeOneHour = yesterday.AddHours(-1);
            m_StartDCbx.SelectedDate = yesterday;
            List<AVEHICLE> vehicleList = scApp.VehicleBLL.cache.loadAllVh();
            //cmdMCSList = app.CmdBLL.GetCMDsBySetTimeClearTime(yesterdayBeforeOneHour, today);
            //hcmdMCSList = app.CmdBLL.GetHCMDsBySetTimeClearTime(yesterdayBeforeOneHour, today);
            //ohtcCMDList = app.CmdBLL.GetOHTCCMDsBySetTimeClearTime(yesterdayBeforeOneHour, today.AddHours(1));
            hisOHTCCMDList = scApp.CMDBLL.GetHisOHTCCMDsBySetTimeClearTime(yesterdayBeforeOneHour, today.AddHours(1));
            alarmList = scApp.AlarmBLL.GetALARMsBySetTimeClearTime(yesterdayBeforeOneHour, today.AddHours(1));
            SeriesCollection = new SeriesCollection();
            Labels = new string[vehicleList.Count];
            //Labels = new[] { "AGV01", "AGV02", "AGV03", "AGV04" };
            List<CustomVm>[] vmListArray = new List<CustomVm>[vehicleList.Count];
            int _index = 0;
            foreach (AVEHICLE vh in vehicleList)
            {
                Labels[_index] = vh.VEHICLE_ID.Trim();
                rawDataList = new List<RawChartData>();
                vmListArray[_index] = new List<CustomVm>();
                Cutleft = null;
                foreach (ALARM alarm in alarmList)
                {
                    if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet && alarm.RPT_DATE_TIME < DateTime.Now.AddHours(-1))
                    {
                        continue;
                    }
                    if (alarm.EQPT_ID.Trim() == vh.VEHICLE_ID.Trim())
                    {
                        if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrReset)
                        {
                            RawChartData rawChartData = new RawChartData();
                            rawChartData.startTime = alarm.RPT_DATE_TIME;
                            rawChartData.endTime = (DateTime)alarm.CLEAR_DATE_TIME;
                            rawChartData.vehicle_id = alarm.EQPT_ID.Trim();
                            rawChartData.status = "Down";
                            TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                            rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] Alarm Code:[{alarm.ALAM_CODE.Trim()}] Alarm Description:[{alarm.ALAM_DESC.Trim()}] Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";

                            rawDataList.Add(rawChartData);
                        }
                        else if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet)
                        {
                            RawChartData rawChartData = new RawChartData();
                            rawChartData.startTime = alarm.RPT_DATE_TIME;
                            rawChartData.endTime = today;
                            rawChartData.vehicle_id = alarm.EQPT_ID.Trim();
                            rawChartData.status = "Down";
                            TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                            rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] Alarm Code:[{alarm.ALAM_CODE.Trim()}] Alarm Description:[{alarm.ALAM_DESC.Trim()}] Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";
                            rawDataList.Add(rawChartData);
                        }
                    }
                }
                foreach (HCMD_OHTC cmd in hisOHTCCMDList)
                {
                    if (string.IsNullOrWhiteSpace(cmd.CMD_ID_MCS) || cmd.CMD_START_TIME == null || cmd.CMD_END_TIME == null)
                    {
                        continue;
                    }
                    if (cmd.VH_ID.Trim() == vh.VEHICLE_ID.Trim())
                    {
                        RawChartData rawChartData = new RawChartData();
                        rawChartData.startTime = (DateTime)cmd.CMD_START_TIME;
                        rawChartData.endTime = (DateTime)cmd.CMD_END_TIME;
                        rawChartData.status = "Run";
                        rawChartData.vehicle_id = vh.VEHICLE_ID.Trim();
                        TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                        rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] MCS CMD ID:[{cmd.CMD_ID_MCS.Trim()}] Vehicle CMD ID:[{cmd.CMD_ID.Trim()}]  Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";
                        rawDataList.Add(rawChartData);
                    }
                }
                rawDataList = rawDataList.OrderBy(data => data.startTime).ToList();
                RawChartData pre = null;
                foreach (RawChartData data in rawDataList)
                {
                    if (vmListArray[_index].Count > 0)
                    {
                        if (data.startTime > today) break;
                        if (data.endTime > today)//超出擷取時間
                        {
                            if (Cutleft != null)
                            {
                                if (Cutleft.startTime < data.startTime)
                                {
                                    if (Cutleft.endTime <= pre.endTime)
                                    {
                                        Cutleft = null;
                                    }
                                    else
                                    {
                                        if (pre.endTime > Cutleft.startTime)
                                        {
                                            vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                            pre.endTime = Cutleft.startTime;
                                        }
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = Cutleft.vehicle_id;
                                        vm.Status = Cutleft.status;
                                        vm.Duration = (int)Math.Round((today - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        Cutleft.startTime = pre.endTime;
                                        vm.Description = Cutleft.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        vmListArray[_index].Add(vm);
                                        pre = Cutleft;
                                        Cutleft = null;
                                    }
                                }
                            }
                            if (pre.endTime < data.startTime)
                            {
                                CustomVm idle_vm = new CustomVm();
                                idle_vm.index = _index;
                                idle_vm.VehicleName = data.vehicle_id;
                                idle_vm.Status = "Idle";
                                idle_vm.Duration = (int)Math.Round((data.startTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                TimeSpan totalTime = data.startTime - pre.endTime;
                                idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                                idle_vm.Color = Brushes.LightGray;
                                vmListArray[_index].Add(idle_vm);

                                CustomVm vm = new CustomVm();
                                vm.index = _index;
                                vm.VehicleName = data.vehicle_id;
                                vm.Status = data.status;
                                vm.Duration = (int)Math.Round((today - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                data.endTime = today;
                                vm.Description = data.info;
                                vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                if (vm.Duration > 0)
                                {
                                    vmListArray[_index].Add(vm);
                                    pre = data;
                                }
                                else
                                {

                                }
                                continue;
                            }
                            else if (pre.endTime > data.endTime)
                            {
                                //if (data.status == "Down" && pre.status == "Run")//被包到前一筆裡
                                if (data.status == "Run" && pre.status == "Down")//被包到前一筆裡
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((today - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.endTime = today;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre.startTime = data.endTime;
                                        Cutleft = pre;
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                                continue;
                            }
                            else
                            {
                                //if (data.status == "Down" && pre.status == "Run")//蓋前面
                                if (data.status == "Run" && pre.status == "Down")//蓋前面
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    pre.endTime = data.startTime;
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((today - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.endTime = today;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                                else//擺後面
                                {
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((today - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.startTime = pre.endTime;
                                    data.endTime = today;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Cutleft != null)
                            {
                                if (Cutleft.startTime < data.startTime)
                                {
                                    if (Cutleft.endTime <= pre.endTime)
                                    {
                                        Cutleft = null;
                                    }
                                    else
                                    {
                                        if (pre.endTime > Cutleft.startTime)
                                        {
                                            vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                            pre.endTime = Cutleft.startTime;
                                        }
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = Cutleft.vehicle_id;
                                        vm.Status = Cutleft.status;
                                        vm.Duration = (int)Math.Round((Cutleft.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        Cutleft.startTime = pre.endTime;
                                        vm.Description = Cutleft.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        vmListArray[_index].Add(vm);
                                        pre = Cutleft;
                                        Cutleft = null;
                                    }
                                }
                            }

                            if (pre.endTime < data.startTime)
                            {
                                CustomVm idle_vm = new CustomVm();
                                idle_vm.index = _index;
                                idle_vm.VehicleName = data.vehicle_id;
                                idle_vm.Status = "Idle";
                                idle_vm.Duration = (int)Math.Round((data.startTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                TimeSpan totalTime = data.startTime - pre.endTime;
                                idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                                idle_vm.Color = Brushes.LightGray;
                                vmListArray[_index].Add(idle_vm);

                                CustomVm vm = new CustomVm();
                                vm.index = _index;
                                vm.VehicleName = data.vehicle_id;
                                vm.Status = data.status;
                                vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                vm.Description = data.info;
                                vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                if (vm.Duration > 0)
                                {
                                    vmListArray[_index].Add(vm);
                                    pre = data;
                                }
                                else
                                {

                                }
                                continue;
                            }
                            else if (pre.endTime > data.endTime)
                            {
                                //if (data.status == "Down" && pre.status == "Run")//被包到前一筆裡
                                if (data.status == "Run" && pre.status == "Down")//被包到前一筆裡
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre.startTime = data.endTime;
                                        Cutleft = pre;
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }

                                continue;
                            }
                            else
                            {
                                //if (data.status == "Down" && pre.status == "Run")//蓋前面
                                if (data.status == "Run" && pre.status == "Down")//蓋前面
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    pre.endTime = data.startTime;
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }

                                }
                                else//擺後面
                                {
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((data.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.startTime = pre.endTime;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                    }
                    else//第一筆
                    {
                        if (data.endTime < yesterday)
                        {
                            continue;
                        }
                        else if (data.startTime < yesterday)
                        {
                            CustomVm first_vm = new CustomVm();
                            first_vm.index = _index;
                            first_vm.VehicleName = data.vehicle_id;
                            first_vm.Status = data.status;
                            first_vm.Duration = (int)Math.Round((data.endTime - yesterday).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            data.startTime = yesterday;
                            first_vm.Description = data.info;
                            first_vm.Color = first_vm.Status == "Down" ? Brushes.Red : first_vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                            vmListArray[_index].Add(first_vm);
                            pre = data;
                        }
                        else
                        {
                            CustomVm idle_vm = new CustomVm();
                            idle_vm.index = _index;
                            idle_vm.VehicleName = data.vehicle_id;
                            idle_vm.Status = "Idle";
                            idle_vm.Duration = (int)Math.Round((data.startTime - yesterday).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            TimeSpan totalTime = data.startTime - yesterday;
                            idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{yesterday.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                            idle_vm.Color = Brushes.LightGray;
                            vmListArray[_index].Add(idle_vm);


                            CustomVm first_vm = new CustomVm();
                            first_vm.index = _index;
                            first_vm.VehicleName = data.vehicle_id;
                            first_vm.Status = data.status;
                            first_vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            first_vm.Description = data.info;
                            first_vm.Color = first_vm.Status == "Down" ? Brushes.Red : first_vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                            vmListArray[_index].Add(first_vm);
                            pre = data;
                        }
                    }
                }

                if (Cutleft != null)
                {
                    if (pre.endTime > Cutleft.startTime)
                    {
                        vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                        pre.endTime = Cutleft.startTime;
                    }
                    CustomVm vm = new CustomVm();
                    vm.index = _index;
                    vm.VehicleName = Cutleft.vehicle_id;
                    vm.Status = Cutleft.status;
                    vm.Duration = (int)Math.Round((Cutleft.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                    vm.Description = Cutleft.info;
                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                    vmListArray[_index].Add(vm);
                    pre = Cutleft;
                    Cutleft = null;
                }

                if (pre == null)
                {
                    CustomVm idle_vm = new CustomVm();
                    idle_vm.index = _index;
                    idle_vm.VehicleName = vh.VEHICLE_ID.Trim();
                    idle_vm.Status = "Idle";
                    idle_vm.Duration = (int)Math.Round((today - yesterday).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                    TimeSpan totalTime = today - yesterday;
                    idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{yesterday.ToString("HH:mm:ss")}] EndTime:[{today.ToString("HH:mm:ss")}]";
                    idle_vm.Color = Brushes.LightGray;
                    vmListArray[_index].Add(idle_vm);

                }
                else if (pre.endTime < today)//看看最後要不要加IDLE
                {
                    CustomVm idle_vm = new CustomVm();
                    idle_vm.index = _index;
                    idle_vm.VehicleName = pre.vehicle_id;
                    idle_vm.Status = "Idle";
                    idle_vm.Duration = (int)Math.Round((today - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                    TimeSpan totalTime = today - pre.endTime;
                    idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{today.ToString("HH:mm:ss")}]";
                    idle_vm.Color = Brushes.LightGray;
                    vmListArray[_index].Add(idle_vm);
                }


                int count_sec = 0;
                foreach (CustomVm vm in vmListArray[_index])
                {
                    count_sec += vm.Duration;
                    SeriesCollection.Add(new StackedRowSeries
                    {
                        Values = new ChartValues<CustomVm>()
                    {
                        vm
                    },
                        Fill = vm.Color,
                        Stroke = Brushes.Transparent
                    });
                }
                _index++;
            }



            //adding series updates and animates the chart
            //SeriesCollection.Add(new StackedRowSeries
            //{
            //    Values = new ChartValues<double> { 6, 2, 7,4 },
            //    StackMode = StackMode.Values
            //});

            //adding values also updates and animates
            //SeriesCollection[2].Values.Add(4d);
            var customVmMapper = Mappers.Xy<CustomVm>()
            .Y((value, index) => value.index) // lets use the position of the item as X
            .X(value => value.Duration); //and PurchasedItems property as Y

            //lets save the mapper globally
            Charting.For<CustomVm>(customVmMapper);

            //TimeLabels = new[] { "0", "1", "2", "3","4","5", "6", "7", "8", "9", "10", 
            //    "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24" };
            TimeLabels = new string[86400];
            int __index = 0;
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 3600; j++)
                {
                    TimeLabels[__index] = i.ToString();
                    __index++;
                }
            }
            //Formatter = value => value + " Mill";
            #endregion barchart



            #region piechart
            PointLabel = chartPoint =>
string.Format("{0}h{1}m{2}s ({3:P})", (int)(chartPoint.Participation * 24), (int)(chartPoint.Participation * 1440) % 60, (int)(chartPoint.Participation * 86400) % 60, chartPoint.Participation);
            int pie_index = 0;
            pieCharts = new PieChart[vehicleList.Count];
            SeriesCollection[] pieSeriesCollections = new SeriesCollection[vehicleList.Count];

            foreach (AVEHICLE vh in vehicleList)
            {

                if (pie_index == 3) break;//因為排版限制只能放三個PieChart
                pieCharts[pie_index] = new PieChart();
                pieCharts[pie_index].LegendLocation = LiveCharts.LegendLocation.Bottom;
                pieSeriesCollections[pie_index] = new SeriesCollection();
                pieCharts[pie_index].Series = pieSeriesCollections[pie_index];
                pieCharts[pie_index].DataClick += Chart_OnDataClick;
                pieCharts[pie_index].Hoverable = false;
                pieCharts[pie_index].ToolTip = null;
                int down_time = 0;
                int run_time = 0;
                int idle_time = 0;
                foreach (CustomVm vm in vmListArray[pie_index])
                {
                    if (vm.VehicleName.Trim() == vh.VEHICLE_ID.Trim())
                    {
                        switch (vm.Status)
                        {
                            case "Down":
                                down_time += vm.Duration;
                                break;
                            case "Run":
                                run_time += vm.Duration;
                                break;
                            case "Idle":
                                idle_time += vm.Duration;
                                break;
                        }
                    }
                }
                pieSeriesCollections[pie_index].Add(new PieSeries
                {
                    Title = "Down",
                    DataLabels = true,
                    Values = new ChartValues<double>()
                    {
                        down_time
                    },
                    LabelPoint = PointLabel,
                    Fill = Brushes.Red,
                    Stroke = Brushes.Transparent
                });
                pieSeriesCollections[pie_index].Add(new PieSeries
                {
                    Title = "Run",
                    DataLabels = true,
                    Values = new ChartValues<double>()
                    {
                        run_time
                    },
                    LabelPoint = PointLabel,
                    Fill = Brushes.Lime,
                    Stroke = Brushes.Transparent
                });
                pieSeriesCollections[pie_index].Add(new PieSeries
                {
                    Title = "Idle",
                    DataLabels = true,
                    Values = new ChartValues<double>()
                    {
                        idle_time
                    },
                    LabelPoint = PointLabel,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.Transparent
                });
                DockPanel.SetDock(pieCharts[pie_index], Dock.Top);
                if (pie_index == 0)
                {
                    pieChartLabel1.Content = vh.VEHICLE_ID.Trim();
                    dockPanel2.Children.Add(pieCharts[pie_index]);
                }
                else if (pie_index == 1)
                {
                    pieChartLabel2.Content = vh.VEHICLE_ID.Trim();
                    dockPanel3.Children.Add(pieCharts[pie_index]);
                }
                else if (pie_index == 2)
                {
                    pieChartLabel3.Content = vh.VEHICLE_ID.Trim();
                    dockPanel4.Children.Add(pieCharts[pie_index]);
                }
                pie_index++;
            }
            #endregion piechart
            DataContext = this;
        }


        private void refresh()
        {
            #region barchart
            if (m_StartDCbx.SelectedDate == null) return;
            DateTime selected_day = (DateTime)m_StartDCbx.SelectedDate;
            DateTime selected_day_plus = selected_day.AddDays(1);
            DateTime yesterdayBeforeOneHour = selected_day.AddHours(-1);
            List<AVEHICLE> vehicleList = scApp.VehicleBLL.cache.loadAllVh();
            //cmdMCSList = app.CmdBLL.GetCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus);
            //hcmdMCSList = app.CmdBLL.GetHCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus);
            //ohtcCMDList = app.CmdBLL.GetOHTCCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus.AddHours(1));
            hisOHTCCMDList = scApp.CMDBLL.GetHisOHTCCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus.AddHours(1));
            alarmList = scApp.AlarmBLL.GetALARMsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus.AddHours(1));
            //SeriesCollection = null;
            SeriesCollection = new SeriesCollection();
            Labels = new string[vehicleList.Count];
            //Labels = new[] { "AGV01", "AGV02", "AGV03", "AGV04" };
            List<CustomVm>[] vmListArray = new List<CustomVm>[vehicleList.Count];
            int _index = 0;
            foreach (AVEHICLE vh in vehicleList)
            {
                Labels[_index] = vh.VEHICLE_ID.Trim();
                rawDataList = new List<RawChartData>();
                vmListArray[_index] = new List<CustomVm>();
                Cutleft = null;
                foreach (ALARM alarm in alarmList)
                {
                    if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet && alarm.RPT_DATE_TIME < DateTime.Now.AddHours(-1))
                    {
                        continue;
                    }
                    if (alarm.EQPT_ID.Trim() == vh.VEHICLE_ID.Trim())
                    {
                        if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrReset)
                        {
                            RawChartData rawChartData = new RawChartData();
                            rawChartData.startTime = alarm.RPT_DATE_TIME;
                            rawChartData.endTime = (DateTime)alarm.CLEAR_DATE_TIME;
                            rawChartData.vehicle_id = alarm.EQPT_ID.Trim();
                            rawChartData.status = "Down";
                            TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                            rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] Alarm Code:[{alarm.ALAM_CODE.Trim()}] Alarm Description:[{alarm.ALAM_DESC.Trim()}] Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";

                            rawDataList.Add(rawChartData);
                        }
                        else if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet)
                        {
                            RawChartData rawChartData = new RawChartData();
                            rawChartData.startTime = alarm.RPT_DATE_TIME;
                            rawChartData.endTime = selected_day_plus;
                            rawChartData.vehicle_id = alarm.EQPT_ID.Trim();
                            rawChartData.status = "Down";
                            TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                            rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] Alarm Code:[{alarm.ALAM_CODE.Trim()}] Alarm Description:[{alarm.ALAM_DESC.Trim()}] Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";
                            rawDataList.Add(rawChartData);
                        }
                    }
                }
                foreach (HCMD_OHTC cmd in hisOHTCCMDList)
                {
                    if (string.IsNullOrWhiteSpace(cmd.CMD_ID_MCS) || cmd.CMD_START_TIME == null || cmd.CMD_END_TIME == null)
                    {
                        continue;
                    }
                    if (cmd.VH_ID.Trim() == vh.VEHICLE_ID.Trim())
                    {
                        RawChartData rawChartData = new RawChartData();
                        rawChartData.startTime = (DateTime)cmd.CMD_START_TIME;
                        rawChartData.endTime = (DateTime)cmd.CMD_END_TIME;
                        rawChartData.status = "Run";
                        rawChartData.vehicle_id = vh.VEHICLE_ID.Trim();
                        TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                        rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] MCS CMD ID:[{cmd.CMD_ID_MCS.Trim()}] Vehicle CMD ID:[{cmd.CMD_ID.Trim()}]  Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";
                        rawDataList.Add(rawChartData);
                    }
                }
                rawDataList = rawDataList.OrderBy(data => data.startTime).ToList();
                RawChartData pre = null;
                foreach (RawChartData data in rawDataList)
                {
                    if (data.info.StartsWith("Duration:[2m:4s"))
                    {

                    }
                    if (vmListArray[_index].Count > 0)
                    {
                        if (data.startTime > selected_day_plus) break;
                        if (data.endTime > selected_day_plus)//超出擷取時間
                        {
                            if (Cutleft != null)
                            {
                                if (Cutleft.startTime < data.startTime)
                                {
                                    if (Cutleft.endTime <= pre.endTime)
                                    {
                                        Cutleft = null;
                                    }
                                    else
                                    {
                                        if (pre.endTime > Cutleft.startTime)
                                        {
                                            vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                            pre.endTime = Cutleft.startTime;
                                        }
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = Cutleft.vehicle_id;
                                        vm.Status = Cutleft.status;
                                        vm.Duration = (int)Math.Round((selected_day_plus - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        Cutleft.startTime = pre.endTime;
                                        vm.Description = Cutleft.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        vmListArray[_index].Add(vm);
                                        pre = Cutleft;
                                        Cutleft = null;
                                    }
                                }
                            }
                            if (pre.endTime < data.startTime)
                            {
                                CustomVm idle_vm = new CustomVm();
                                idle_vm.index = _index;
                                idle_vm.VehicleName = data.vehicle_id;
                                idle_vm.Status = "Idle";
                                idle_vm.Duration = (int)Math.Round((data.startTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                TimeSpan totalTime = data.startTime - pre.endTime;
                                idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                                idle_vm.Color = Brushes.LightGray;
                                vmListArray[_index].Add(idle_vm);

                                CustomVm vm = new CustomVm();
                                vm.index = _index;
                                vm.VehicleName = data.vehicle_id;
                                vm.Status = data.status;
                                vm.Duration = (int)Math.Round((selected_day_plus - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                data.endTime = selected_day_plus;
                                vm.Description = data.info;
                                vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                if (vm.Duration > 0)
                                {
                                    vmListArray[_index].Add(vm);
                                    pre = data;
                                }
                                else
                                {

                                }
                                continue;
                            }
                            else if (pre.endTime > data.endTime)
                            {
                                //if (data.status == "Down" && pre.status == "Run")//被包到前一筆裡
                                if (data.status == "Run" && pre.status == "Down")//被包到前一筆裡
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((selected_day_plus - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.endTime = selected_day_plus;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre.startTime = data.endTime;
                                        Cutleft = pre;
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                                continue;
                            }
                            else
                            {
                                //if (data.status == "Down" && pre.status == "Run")//蓋前面
                                if (data.status == "Run" && pre.status == "Down")//蓋前面
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    pre.endTime = data.startTime;
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((selected_day_plus - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.endTime = selected_day_plus;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                                else//擺後面
                                {
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((selected_day_plus - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.startTime = pre.endTime;
                                    data.endTime = selected_day_plus;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Cutleft != null)
                            {
                                if (Cutleft.startTime < data.startTime)
                                {
                                    if (Cutleft.endTime <= pre.endTime)
                                    {
                                        Cutleft = null;
                                    }
                                    else
                                    {
                                        if (pre.endTime > Cutleft.startTime)
                                        {
                                            vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                            pre.endTime = Cutleft.startTime;
                                        }
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = Cutleft.vehicle_id;
                                        vm.Status = Cutleft.status;
                                        vm.Duration = (int)Math.Round((Cutleft.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        Cutleft.startTime = pre.endTime;
                                        vm.Description = Cutleft.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        vmListArray[_index].Add(vm);
                                        pre = Cutleft;
                                        Cutleft = null;
                                    }
                                }
                            }

                            if (pre.endTime < data.startTime)
                            {
                                CustomVm idle_vm = new CustomVm();
                                idle_vm.index = _index;
                                idle_vm.VehicleName = data.vehicle_id;
                                idle_vm.Status = "Idle";
                                idle_vm.Duration = (int)Math.Round((data.startTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                TimeSpan totalTime = data.startTime - pre.endTime;
                                idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                                idle_vm.Color = Brushes.LightGray;
                                vmListArray[_index].Add(idle_vm);

                                CustomVm vm = new CustomVm();
                                vm.index = _index;
                                vm.VehicleName = data.vehicle_id;
                                vm.Status = data.status;
                                vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                vm.Description = data.info;
                                vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                if (vm.Duration > 0)
                                {
                                    vmListArray[_index].Add(vm);
                                    pre = data;
                                }
                                else
                                {

                                }
                                continue;
                            }
                            else if (pre.endTime > data.endTime)
                            {
                                //if (data.status == "Down" && pre.status == "Run")//被包到前一筆裡
                                if (data.status == "Run" && pre.status == "Down")//被包到前一筆裡
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre.startTime = data.endTime;
                                        Cutleft = pre;
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }

                                continue;
                            }
                            else
                            {
                                //if (data.status == "Down" && pre.status == "Run")//蓋前面
                                if (data.status == "Run" && pre.status == "Down")//蓋前面
                                {
                                    vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    pre.endTime = data.startTime;
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }

                                }
                                else//擺後面
                                {
                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((data.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.startTime = pre.endTime;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                    }
                    else//第一筆
                    {
                        if (data.endTime < selected_day)
                        {
                            continue;
                        }
                        else if (data.startTime < selected_day)
                        {
                            CustomVm first_vm = new CustomVm();
                            first_vm.index = _index;
                            first_vm.VehicleName = data.vehicle_id;
                            first_vm.Status = data.status;
                            first_vm.Duration = (int)Math.Round((data.endTime - selected_day).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            data.startTime = selected_day;
                            first_vm.Description = data.info;
                            first_vm.Color = first_vm.Status == "Down" ? Brushes.Red : first_vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                            vmListArray[_index].Add(first_vm);
                            pre = data;
                        }
                        else
                        {
                            CustomVm idle_vm = new CustomVm();
                            idle_vm.index = _index;
                            idle_vm.VehicleName = data.vehicle_id;
                            idle_vm.Status = "Idle";
                            idle_vm.Duration = (int)Math.Round((data.startTime - selected_day).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            TimeSpan totalTime = data.startTime - selected_day;
                            idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{selected_day.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                            idle_vm.Color = Brushes.LightGray;
                            vmListArray[_index].Add(idle_vm);


                            CustomVm first_vm = new CustomVm();
                            first_vm.index = _index;
                            first_vm.VehicleName = data.vehicle_id;
                            first_vm.Status = data.status;
                            first_vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            first_vm.Description = data.info;
                            first_vm.Color = first_vm.Status == "Down" ? Brushes.Red : first_vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                            vmListArray[_index].Add(first_vm);
                            pre = data;
                        }
                    }
                }

                if (Cutleft != null)
                {
                    if (pre.endTime > Cutleft.startTime)
                    {
                        vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                        pre.endTime = Cutleft.startTime;
                    }
                    CustomVm vm = new CustomVm();
                    vm.index = _index;
                    vm.VehicleName = Cutleft.vehicle_id;
                    vm.Status = Cutleft.status;
                    vm.Duration = (int)Math.Round((Cutleft.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                    vm.Description = Cutleft.info;
                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                    vmListArray[_index].Add(vm);
                    pre = Cutleft;
                    Cutleft = null;
                }

                if (pre == null)
                {
                    CustomVm idle_vm = new CustomVm();
                    idle_vm.index = _index;
                    idle_vm.VehicleName = vh.VEHICLE_ID.Trim();
                    idle_vm.Status = "Idle";
                    idle_vm.Duration = (int)Math.Round((selected_day_plus - selected_day).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                    TimeSpan totalTime = selected_day_plus - selected_day;
                    idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{selected_day.ToString("HH:mm:ss")}] EndTime:[{selected_day_plus.ToString("HH:mm:ss")}]";
                    idle_vm.Color = Brushes.LightGray;
                    vmListArray[_index].Add(idle_vm);

                }
                else if (pre.endTime < selected_day_plus)//看看最後要不要加IDLE
                {
                    CustomVm idle_vm = new CustomVm();
                    idle_vm.index = _index;
                    idle_vm.VehicleName = pre.vehicle_id;
                    idle_vm.Status = "Idle";
                    idle_vm.Duration = (int)Math.Round((selected_day_plus - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                    TimeSpan totalTime = selected_day_plus - pre.endTime;
                    idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{selected_day_plus.ToString("HH:mm:ss")}]";
                    idle_vm.Color = Brushes.LightGray;
                    vmListArray[_index].Add(idle_vm);
                }


                int count_sec = 0;
                foreach (CustomVm vm in vmListArray[_index])
                {
                    count_sec += vm.Duration;
                    SeriesCollection.Add(new StackedRowSeries
                    {
                        Values = new ChartValues<CustomVm>()
                    {
                        vm
                    },
                        Fill = vm.Color,
                        Stroke = Brushes.Transparent
                    });
                }
                _index++;
            }
            barchart.Series = SeriesCollection;


            //var customVmMapper = Mappers.Xy<CustomVm>()
            //.Y((value, index) => value.index) 
            //.X(value => value.Duration); 

            //Charting.For<CustomVm>(customVmMapper);

            //TimeLabels = new[] { "0", "1", "2", "3","4","5", "6", "7", "8", "9", "10", 
            //    "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24" };
            //Formatter = value => value + " Mill";
            #endregion barchart



            #region piechart
            //            PointLabel = chartPoint =>
            //string.Format("{0}h{1}m{2}s ({3:P})", (int)(chartPoint.Participation * 24), (int)(chartPoint.Participation * 1440) % 60, (int)(chartPoint.Participation * 86400) % 60, chartPoint.Participation);
            int pie_index = 0;
            pieCharts = new PieChart[vehicleList.Count];
            SeriesCollection[] pieSeriesCollections = new SeriesCollection[vehicleList.Count];

            foreach (AVEHICLE vh in vehicleList)
            {

                if (pie_index == 3) break;//因為排版限制只能放三個PieChart
                pieCharts[pie_index] = new PieChart();
                pieCharts[pie_index].LegendLocation = LiveCharts.LegendLocation.Bottom;
                pieSeriesCollections[pie_index] = new SeriesCollection();
                pieCharts[pie_index].Series = pieSeriesCollections[pie_index];
                pieCharts[pie_index].DataClick += Chart_OnDataClick;
                pieCharts[pie_index].Hoverable = false;
                pieCharts[pie_index].ToolTip = null;
                int down_time = 0;
                int run_time = 0;
                int idle_time = 0;
                foreach (CustomVm vm in vmListArray[pie_index])
                {
                    if (vm.VehicleName.Trim() == vh.VEHICLE_ID.Trim())
                    {
                        switch (vm.Status)
                        {
                            case "Down":
                                down_time += vm.Duration;
                                break;
                            case "Run":
                                run_time += vm.Duration;
                                break;
                            case "Idle":
                                idle_time += vm.Duration;
                                break;
                        }
                    }
                }
                pieSeriesCollections[pie_index].Add(new PieSeries
                {
                    Title = "Down",
                    DataLabels = true,
                    Values = new ChartValues<double>()
                    {
                        down_time
                    },
                    LabelPoint = PointLabel,
                    Fill = Brushes.Red,
                    Stroke = Brushes.Transparent
                });
                pieSeriesCollections[pie_index].Add(new PieSeries
                {
                    Title = "Run",
                    DataLabels = true,
                    Values = new ChartValues<double>()
                    {
                        run_time
                    },
                    LabelPoint = PointLabel,
                    Fill = Brushes.Lime,
                    Stroke = Brushes.Transparent
                });
                pieSeriesCollections[pie_index].Add(new PieSeries
                {
                    Title = "Idle",
                    DataLabels = true,
                    Values = new ChartValues<double>()
                    {
                        idle_time
                    },
                    LabelPoint = PointLabel,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.Transparent
                });
                DockPanel.SetDock(pieCharts[pie_index], Dock.Top);
                if (pie_index == 0)
                {
                    dockPanel2.Children.Clear();
                    dockPanel2.Children.Add(pieCharts[pie_index]);
                }
                else if (pie_index == 1)
                {
                    dockPanel3.Children.Clear();
                    dockPanel3.Children.Add(pieCharts[pie_index]);
                }
                else if (pie_index == 2)
                {
                    dockPanel4.Children.Clear();
                    dockPanel4.Children.Add(pieCharts[pie_index]);
                }
                pie_index++;
            }
            #endregion piechart
            DataContext = this;
        }


        private void export()
        {
            int day_count = 0;
            if(string.IsNullOrWhiteSpace(comboBox.Text))
            {
                System.Windows.Forms.MessageBox.Show("Please select Days boefore excute export function.");
                return;
            }
            else if(!int.TryParse(comboBox.Text,out day_count))
            {
                System.Windows.Forms.MessageBox.Show("Please input an integer.");
                return;
            }
            for (int day = 0;day<day_count; day++)
            {
                #region barchart
                if (m_StartDCbx.SelectedDate == null) return;

                DateTime pri_selected_day = (DateTime)m_StartDCbx.SelectedDate;
                DateTime selected_day = pri_selected_day.AddDays(day);
                DateTime selected_day_plus = selected_day.AddDays(1);
                DateTime yesterdayBeforeOneHour = selected_day.AddHours(-1);
                List<AVEHICLE> vehicleList = scApp.VehicleBLL.cache.loadAllVh();
                //cmdMCSList = app.CmdBLL.GetCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus);
                //hcmdMCSList = app.CmdBLL.GetHCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus);
                //ohtcCMDList = app.CmdBLL.GetOHTCCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus.AddHours(1));
                hisOHTCCMDList = scApp.CMDBLL.GetHisOHTCCMDsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus.AddHours(1));
                alarmList = scApp.AlarmBLL.GetALARMsBySetTimeClearTime(yesterdayBeforeOneHour, selected_day_plus.AddHours(1));
                //SeriesCollection = null;
                SeriesCollection = new SeriesCollection();
                Labels = new string[vehicleList.Count];
                //Labels = new[] { "AGV01", "AGV02", "AGV03", "AGV04" };
                List<CustomVm>[] vmListArray = new List<CustomVm>[vehicleList.Count];
                int _index = 0;
                foreach (AVEHICLE vh in vehicleList)
                {
                    Labels[_index] = vh.VEHICLE_ID.Trim();
                    rawDataList = new List<RawChartData>();
                    vmListArray[_index] = new List<CustomVm>();
                    Cutleft = null;
                    foreach (ALARM alarm in alarmList)
                    {
                        if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet && alarm.RPT_DATE_TIME < DateTime.Now.AddHours(-1))
                        {
                            continue;
                        }
                        if (alarm.EQPT_ID.Trim() == vh.VEHICLE_ID.Trim())
                        {
                            if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrReset)
                            {
                                RawChartData rawChartData = new RawChartData();
                                rawChartData.startTime = alarm.RPT_DATE_TIME;
                                rawChartData.endTime = (DateTime)alarm.CLEAR_DATE_TIME;
                                rawChartData.vehicle_id = alarm.EQPT_ID.Trim();
                                rawChartData.status = "Down";
                                TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                                rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] Alarm Code:[{alarm.ALAM_CODE.Trim()}] Alarm Description:[{alarm.ALAM_DESC.Trim()}] Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";

                                rawDataList.Add(rawChartData);
                            }
                            else if (alarm.ALAM_STAT == sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet)
                            {
                                RawChartData rawChartData = new RawChartData();
                                rawChartData.startTime = alarm.RPT_DATE_TIME;
                                rawChartData.endTime = selected_day_plus;
                                rawChartData.vehicle_id = alarm.EQPT_ID.Trim();
                                rawChartData.status = "Down";
                                TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                                rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] Alarm Code:[{alarm.ALAM_CODE.Trim()}] Alarm Description:[{alarm.ALAM_DESC.Trim()}] Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";
                                rawDataList.Add(rawChartData);
                            }
                        }
                    }
                    foreach (HCMD_OHTC cmd in hisOHTCCMDList)
                    {
                        if (string.IsNullOrWhiteSpace(cmd.CMD_ID_MCS) || cmd.CMD_START_TIME == null || cmd.CMD_END_TIME == null)
                        {
                            continue;
                        }
                        if (cmd.VH_ID.Trim() == vh.VEHICLE_ID.Trim())
                        {
                            RawChartData rawChartData = new RawChartData();
                            rawChartData.startTime = (DateTime)cmd.CMD_START_TIME;
                            rawChartData.endTime = (DateTime)cmd.CMD_END_TIME;
                            rawChartData.status = "Run";
                            rawChartData.vehicle_id = vh.VEHICLE_ID.Trim();
                            TimeSpan totalTime = rawChartData.endTime - rawChartData.startTime;
                            rawChartData.info = $"Duration:[{myTimeSpanToString(totalTime)}] MCS CMD ID:[{cmd.CMD_ID_MCS.Trim()}] Vehicle CMD ID:[{cmd.CMD_ID.Trim()}]  Start Time:[{rawChartData.startTime.ToString("HH:mm:ss")}] EndTime:[{rawChartData.endTime.ToString("HH:mm:ss")}]";
                            rawDataList.Add(rawChartData);
                        }
                    }
                    rawDataList = rawDataList.OrderBy(data => data.startTime).ToList();
                    RawChartData pre = null;
                    foreach (RawChartData data in rawDataList)
                    {
                        if (data.info.StartsWith("Duration:[2m:4s"))
                        {

                        }
                        if (vmListArray[_index].Count > 0)
                        {
                            if (data.startTime > selected_day_plus) break;
                            if (data.endTime > selected_day_plus)//超出擷取時間
                            {
                                if (Cutleft != null)
                                {
                                    if (Cutleft.startTime < data.startTime)
                                    {
                                        if (Cutleft.endTime <= pre.endTime)
                                        {
                                            Cutleft = null;
                                        }
                                        else
                                        {
                                            if (pre.endTime > Cutleft.startTime)
                                            {
                                                vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                                pre.endTime = Cutleft.startTime;
                                            }
                                            CustomVm vm = new CustomVm();
                                            vm.index = _index;
                                            vm.VehicleName = Cutleft.vehicle_id;
                                            vm.Status = Cutleft.status;
                                            vm.Duration = (int)Math.Round((selected_day_plus - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                            Cutleft.startTime = pre.endTime;
                                            vm.Description = Cutleft.info;
                                            vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                            vmListArray[_index].Add(vm);
                                            pre = Cutleft;
                                            Cutleft = null;
                                        }
                                    }
                                }
                                if (pre.endTime < data.startTime)
                                {
                                    CustomVm idle_vm = new CustomVm();
                                    idle_vm.index = _index;
                                    idle_vm.VehicleName = data.vehicle_id;
                                    idle_vm.Status = "Idle";
                                    idle_vm.Duration = (int)Math.Round((data.startTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    TimeSpan totalTime = data.startTime - pre.endTime;
                                    idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                                    idle_vm.Color = Brushes.LightGray;
                                    vmListArray[_index].Add(idle_vm);

                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((selected_day_plus - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    data.endTime = selected_day_plus;
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                    continue;
                                }
                                else if (pre.endTime > data.endTime)
                                {
                                    //if (data.status == "Down" && pre.status == "Run")//被包到前一筆裡
                                    if (data.status == "Run" && pre.status == "Down")//被包到前一筆裡
                                    {
                                        vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = data.vehicle_id;
                                        vm.Status = data.status;
                                        vm.Duration = (int)Math.Round((selected_day_plus - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        data.endTime = selected_day_plus;
                                        vm.Description = data.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        if (vm.Duration > 0)
                                        {
                                            vmListArray[_index].Add(vm);
                                            pre.startTime = data.endTime;
                                            Cutleft = pre;
                                            pre = data;
                                        }
                                        else
                                        {

                                        }
                                    }
                                    continue;
                                }
                                else
                                {
                                    //if (data.status == "Down" && pre.status == "Run")//蓋前面
                                    if (data.status == "Run" && pre.status == "Down")//蓋前面
                                    {
                                        vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        pre.endTime = data.startTime;
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = data.vehicle_id;
                                        vm.Status = data.status;
                                        vm.Duration = (int)Math.Round((selected_day_plus - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        data.endTime = selected_day_plus;
                                        vm.Description = data.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        if (vm.Duration > 0)
                                        {
                                            vmListArray[_index].Add(vm);
                                            pre = data;
                                        }
                                        else
                                        {

                                        }
                                    }
                                    else//擺後面
                                    {
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = data.vehicle_id;
                                        vm.Status = data.status;
                                        vm.Duration = (int)Math.Round((selected_day_plus - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        data.startTime = pre.endTime;
                                        data.endTime = selected_day_plus;
                                        vm.Description = data.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        if (vm.Duration > 0)
                                        {
                                            vmListArray[_index].Add(vm);
                                            pre = data;
                                        }
                                        else
                                        {

                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (Cutleft != null)
                                {
                                    if (Cutleft.startTime < data.startTime)
                                    {
                                        if (Cutleft.endTime <= pre.endTime)
                                        {
                                            Cutleft = null;
                                        }
                                        else
                                        {
                                            if (pre.endTime > Cutleft.startTime)
                                            {
                                                vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                                pre.endTime = Cutleft.startTime;
                                            }
                                            CustomVm vm = new CustomVm();
                                            vm.index = _index;
                                            vm.VehicleName = Cutleft.vehicle_id;
                                            vm.Status = Cutleft.status;
                                            vm.Duration = (int)Math.Round((Cutleft.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                            Cutleft.startTime = pre.endTime;
                                            vm.Description = Cutleft.info;
                                            vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                            vmListArray[_index].Add(vm);
                                            pre = Cutleft;
                                            Cutleft = null;
                                        }
                                    }
                                }

                                if (pre.endTime < data.startTime)
                                {
                                    CustomVm idle_vm = new CustomVm();
                                    idle_vm.index = _index;
                                    idle_vm.VehicleName = data.vehicle_id;
                                    idle_vm.Status = "Idle";
                                    idle_vm.Duration = (int)Math.Round((data.startTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    TimeSpan totalTime = data.startTime - pre.endTime;
                                    idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                                    idle_vm.Color = Brushes.LightGray;
                                    vmListArray[_index].Add(idle_vm);

                                    CustomVm vm = new CustomVm();
                                    vm.index = _index;
                                    vm.VehicleName = data.vehicle_id;
                                    vm.Status = data.status;
                                    vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                    vm.Description = data.info;
                                    vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                    if (vm.Duration > 0)
                                    {
                                        vmListArray[_index].Add(vm);
                                        pre = data;
                                    }
                                    else
                                    {

                                    }
                                    continue;
                                }
                                else if (pre.endTime > data.endTime)
                                {
                                    //if (data.status == "Down" && pre.status == "Run")//被包到前一筆裡
                                    if (data.status == "Run" && pre.status == "Down")//被包到前一筆裡
                                    {
                                        vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = data.vehicle_id;
                                        vm.Status = data.status;
                                        vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        vm.Description = data.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        if (vm.Duration > 0)
                                        {
                                            vmListArray[_index].Add(vm);
                                            pre.startTime = data.endTime;
                                            Cutleft = pre;
                                            pre = data;
                                        }
                                        else
                                        {

                                        }
                                    }

                                    continue;
                                }
                                else
                                {
                                    //if (data.status == "Down" && pre.status == "Run")//蓋前面
                                    if (data.status == "Run" && pre.status == "Down")//蓋前面
                                    {
                                        vmListArray[_index].Last().Duration = (int)Math.Round((data.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        pre.endTime = data.startTime;
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = data.vehicle_id;
                                        vm.Status = data.status;
                                        vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        vm.Description = data.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        if (vm.Duration > 0)
                                        {
                                            vmListArray[_index].Add(vm);
                                            pre = data;
                                        }
                                        else
                                        {

                                        }

                                    }
                                    else//擺後面
                                    {
                                        CustomVm vm = new CustomVm();
                                        vm.index = _index;
                                        vm.VehicleName = data.vehicle_id;
                                        vm.Status = data.status;
                                        vm.Duration = (int)Math.Round((data.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                        data.startTime = pre.endTime;
                                        vm.Description = data.info;
                                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                        if (vm.Duration > 0)
                                        {
                                            vmListArray[_index].Add(vm);
                                            pre = data;
                                        }
                                        else
                                        {

                                        }
                                    }
                                }
                            }
                        }
                        else//第一筆
                        {
                            if (data.endTime < selected_day)
                            {
                                continue;
                            }
                            else if (data.startTime < selected_day)
                            {
                                CustomVm first_vm = new CustomVm();
                                first_vm.index = _index;
                                first_vm.VehicleName = data.vehicle_id;
                                first_vm.Status = data.status;
                                first_vm.Duration = (int)Math.Round((data.endTime - selected_day).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                data.startTime = selected_day;
                                first_vm.Description = data.info;
                                first_vm.Color = first_vm.Status == "Down" ? Brushes.Red : first_vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                vmListArray[_index].Add(first_vm);
                                pre = data;
                            }
                            else
                            {
                                CustomVm idle_vm = new CustomVm();
                                idle_vm.index = _index;
                                idle_vm.VehicleName = data.vehicle_id;
                                idle_vm.Status = "Idle";
                                idle_vm.Duration = (int)Math.Round((data.startTime - selected_day).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                TimeSpan totalTime = data.startTime - selected_day;
                                idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{selected_day.ToString("HH:mm:ss")}] EndTime:[{data.startTime.ToString("HH:mm:ss")}]";
                                idle_vm.Color = Brushes.LightGray;
                                vmListArray[_index].Add(idle_vm);


                                CustomVm first_vm = new CustomVm();
                                first_vm.index = _index;
                                first_vm.VehicleName = data.vehicle_id;
                                first_vm.Status = data.status;
                                first_vm.Duration = (int)Math.Round((data.endTime - data.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                                first_vm.Description = data.info;
                                first_vm.Color = first_vm.Status == "Down" ? Brushes.Red : first_vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                                vmListArray[_index].Add(first_vm);
                                pre = data;
                            }
                        }
                    }

                    if (Cutleft != null)
                    {
                        if (pre.endTime > Cutleft.startTime)
                        {
                            vmListArray[_index].Last().Duration = (int)Math.Round((Cutleft.startTime - pre.startTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                            pre.endTime = Cutleft.startTime;
                        }
                        CustomVm vm = new CustomVm();
                        vm.index = _index;
                        vm.VehicleName = Cutleft.vehicle_id;
                        vm.Status = Cutleft.status;
                        vm.Duration = (int)Math.Round((Cutleft.endTime - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                        vm.Description = Cutleft.info;
                        vm.Color = vm.Status == "Down" ? Brushes.Red : vm.Status == "Idle" ? Brushes.LightGray : Brushes.Lime;
                        vmListArray[_index].Add(vm);
                        pre = Cutleft;
                        Cutleft = null;
                    }

                    if (pre == null)
                    {
                        CustomVm idle_vm = new CustomVm();
                        idle_vm.index = _index;
                        idle_vm.VehicleName = vh.VEHICLE_ID.Trim();
                        idle_vm.Status = "Idle";
                        idle_vm.Duration = (int)Math.Round((selected_day_plus - selected_day).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                        TimeSpan totalTime = selected_day_plus - selected_day;
                        idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{selected_day.ToString("HH:mm:ss")}] EndTime:[{selected_day_plus.ToString("HH:mm:ss")}]";
                        idle_vm.Color = Brushes.LightGray;
                        vmListArray[_index].Add(idle_vm);

                    }
                    else if (pre.endTime < selected_day_plus)//看看最後要不要加IDLE
                    {
                        CustomVm idle_vm = new CustomVm();
                        idle_vm.index = _index;
                        idle_vm.VehicleName = pre.vehicle_id;
                        idle_vm.Status = "Idle";
                        idle_vm.Duration = (int)Math.Round((selected_day_plus - pre.endTime).TotalSeconds, 0, MidpointRounding.AwayFromZero);
                        TimeSpan totalTime = selected_day_plus - pre.endTime;
                        idle_vm.Description = $"Duration:[{myTimeSpanToString(totalTime)}] Start Time:[{pre.endTime.ToString("HH:mm:ss")}] EndTime:[{selected_day_plus.ToString("HH:mm:ss")}]";
                        idle_vm.Color = Brushes.LightGray;
                        vmListArray[_index].Add(idle_vm);
                    }


                    int count_sec = 0;
                    foreach (CustomVm vm in vmListArray[_index])
                    {
                        count_sec += vm.Duration;
                        SeriesCollection.Add(new StackedRowSeries
                        {
                            Values = new ChartValues<CustomVm>()
                    {
                        vm
                    },
                            Fill = vm.Color,
                            Stroke = Brushes.Transparent
                        });
                    }
                    _index++;
                }
                //barchart.Series = SeriesCollection;
                #endregion barchart

                int pie_index = 0;
                int down_time = 0;
                int run_time = 0;
                int idle_time = 0;
                var csv = new StringBuilder();

                var headfirst = "Vehicle ID";
                var headsecond = "Run Time(Sec)";
                var headthird = "Down Time(Sec)";
                var headfourth = "Idle Time(Sec)";
                var headnewLine = string.Format("{0},{1},{2},{3}", headfirst, headsecond, headthird, headfourth); ;
                csv.AppendLine(headnewLine);

                foreach (AVEHICLE vh in vehicleList)
                {
                    down_time = 0;
                    run_time = 0;
                    idle_time = 0;
                    foreach (CustomVm vm in vmListArray[pie_index])
                    {
                        if (vm.VehicleName.Trim() == vh.VEHICLE_ID.Trim())
                        {
                            switch (vm.Status)
                            {
                                case "Down":
                                    down_time += vm.Duration;
                                    break;
                                case "Run":
                                    run_time += vm.Duration;
                                    break;
                                case "Idle":
                                    idle_time += vm.Duration;
                                    break;
                            }
                        }
                    }
                    var first = vehicleList[pie_index].VEHICLE_ID.Trim();
                    var second = run_time.ToString();
                    var third = down_time.ToString();
                    var fourth = idle_time.ToString();
                    var newLine = string.Format("{0},{1},{2},{3}", first, second, third, fourth); ;
                    csv.AppendLine(newLine);
                    pie_index++;
                }

                Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\Export\\");
                string filepath = System.Environment.CurrentDirectory + "\\Export\\" + selected_day.Date.ToString("yyyyMMdd") + ".csv";
                //File.WriteAllText(System.Environment.CurrentDirectory+"\\Export\\"+ selected_day.Date, csv.ToString());
                File.WriteAllText(filepath, csv.ToString());
            }
            





            DataContext = this;











        //refresh();//先取資料
            //for(int i = 0;i< pieCharts.Count(); i++)
            //{
            //    pieCharts[i].
            //}
        }


        public string myTimeSpanToString(TimeSpan Ts)
        {
            if (Ts.TotalDays > 1d)
                return Ts.ToString("d'd:'h'h:'m'm:'s's'");

            if (Ts.TotalHours > 1d)
                return Ts.ToString("h'h:'m'm:'s's'");

            if (Ts.TotalMinutes > 1d)
                return Ts.ToString("m'm:'s's'");

            if (Ts.TotalSeconds > 1d)
                return Ts.ToString("s's'");

            return Ts.ToString();
        }
        private void Chart_OnDataClick(object sender, ChartPoint chartpoint)
        {
            var chart = (LiveCharts.Wpf.PieChart)chartpoint.ChartView;

            //clear selected slice.
            foreach (PieSeries series in chart.Series)
                series.PushOut = 0;

            var selectedSeries = (PieSeries)chartpoint.SeriesView;
            selectedSeries.PushOut = 8;
        }

        private void btn_Set_Click(object sender, RoutedEventArgs e)
        {
            refresh();
        }

        private void btn_Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                export();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
    }
}
