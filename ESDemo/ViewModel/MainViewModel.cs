using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ElasticsearchIO;

namespace QZ.Demo.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            Config.Init();
            ESClientInst.Init();

            FuckPrevCommand = new RelayCommand(FuckPrev);
            FuckNextCommand = new RelayCommand(FuckNext);
            FuckCommand = new RelayCommand(Fuck);
            PageFuckCommand = new RelayCommand(PageFuck);
            SwitchCommand = new RelayCommand(Switch);
            GeneralVisibility = Visibility.Visible;
            SpecialVisibility = Visibility.Collapsed;
        }
        private ObservableCollection<Model.VCompany> _companies;
        public ObservableCollection<Model.VCompany> Companies
        {
            get { return _companies; }
            set
            {
                _companies = value;
                RaisePropertyChanged("Companies");
            }
        }

        public ICommand FuckPrevCommand { get; set; }
        public ICommand FuckNextCommand { get; set; }
        public ICommand FuckCommand { get; set; }
        public ICommand PageFuckCommand { get; set; }
        public ICommand SwitchCommand { get; set; }
        private int _page = 0;
        public int Page
        {
            get { return _page; }
            set
            {
                _page = value;
                RaisePropertyChanged("Page");
            }
        }
        private int _pageinput;
        public int PageInput
        {
            get => _pageinput;
            set
            {
                _pageinput = value;
                RaisePropertyChanged("PageInput");
            }
        }

        private int _generalIndex;
        public int GeneralIndex
        {
            get { return _generalIndex; }
            set
            {
                _generalIndex = value;
                RaisePropertyChanged("GeneralIndex");
            }
        }

        private int _specialIndex;
        public int SpecialIndex
        {
            get { return  _specialIndex; }
            set
            {
                _specialIndex = value;
                RaisePropertyChanged(() => SpecialIndex);
            }
        }
        private bool _nextButtonEnabled;
        public bool NextButtonEnabled
        {
            get { return _nextButtonEnabled; }
            set
            {
                _nextButtonEnabled = value;
                RaisePropertyChanged("NextButtonEnabled");
            }
        }
        private bool _prevButtonEnabled;
        public bool PrevButtonEnabled
        {
            get { return _prevButtonEnabled; }
            set
            {
                _prevButtonEnabled = value;
                RaisePropertyChanged("PrevButtonEnabled");
            }
        }
        private string _noneVisibility;
        public string NoneVisibility
        {
            get { return _noneVisibility; }
            set
            {
                _noneVisibility = value;
                RaisePropertyChanged("NoneVisibility");
            }
        }

        private Visibility _generalVisibility;
        public Visibility GeneralVisibility
        {
            get { return _generalVisibility; }
            set
            {
                _generalVisibility = value;
                RaisePropertyChanged(() => this.GeneralVisibility);
            }
        }
        private Visibility _specialVisibility;
        public Visibility SpecialVisibility
        {
            get { return _specialVisibility; }
            set
            {
                _specialVisibility = value;
                RaisePropertyChanged(() => SpecialVisibility);
            }
        }

        private string _input;
        public string Input
        {
            get { return _input; }
            set
            {
                _input = value;
                RaisePropertyChanged("Input");
            }
        }
        private string _waitHint;
        public string WaitHint
        {
            get { return _waitHint; }
            set
            {
                _waitHint = value;
                RaisePropertyChanged("WaitHint");
            }
        }

        private int _total;
        public string _totl;
        public string Total
        {
            get { return _totl; }
            set
            {
                _totl = value;
                RaisePropertyChanged("Total");
            }
        }
        private SearchParam _ci;


        private void FuckPrev()
        {
            if(Page>0)
            {
                Page--;
                //_ci.QT = (ComQueryType)(ComboBoxIndex + 1);


                //_ci.From = Page * 10;
                //if (SpecialVisibility == Visibility.Visible)
                //    SpecialSearch();
                //else
                //    GeneralSearch(GeneralSearch_1());


                _ci.from = Page * 10;
                GeneralSearch_1();

                if (_nextButtonEnabled == false)
                    NextButtonEnabled = true;
            }
            if(Page <=0)
            {
                PrevButtonEnabled = false;
            }
        }
        private void FuckNext()
        {
            if (Page < _total - 1)
            {
                Page++;
                //_ci.QT = (ComQueryType)(ComboBoxIndex + 1);


                //_ci.From = Page * 10;
                //if (SpecialVisibility == Visibility.Visible)
                //    SpecialSearch();
                //else
                //    GeneralSearch(GeneralSearch_1());

                _ci.from = Page * 10;
                GeneralSearch_1();

                if (Page == 1)
                    PrevButtonEnabled = true;
            }

            if(Page >= _total - 1)
            {
                NextButtonEnabled = false;
            }
        }
        private void Switch()
        {
            var lastSpecial = SpecialVisibility;
            SpecialVisibility = GeneralVisibility;
            GeneralVisibility = lastSpecial;
            Input = "";
            Page = 0;
            PrevButtonEnabled = false;
            NextButtonEnabled = false;
        }
        private void Fuck()
        {
            Page = 0;
            //GeneralSearch(GeneralSearch_1());
            GeneralSearch_1();

            NextButtonEnabled = _total > 1;
            PrevButtonEnabled = false;
        }

        private void PageFuck()
        {
            if (PageInput >= 10000)
            {
                PageInput = 0;
            }

            Page = PageInput;
            _ci.from = Page * 10;
            GeneralSearch_1();
        }

        

        private ESWrapper<Person> GeneralSearch_1()
        {
            if (_ci == null)
            {
                _ci = new SearchParam();
                _ci.keyword = Input;


            }
            else
            {
                _ci.keyword = Input;
                
                _ci.from = 0;
                Page = 0;

            }

            //if (SpecialVisibility == Visibility.Visible)
            //    SpecialSearch();
            //else
            //{
            //}

            //var fi = new ESFinanceInput("<fuck style='color:red;'>");
            //fi.AddFilter(FinanceField.fn_kw, "华为");
            //fi.AddFilter(FinanceField.fn_pub_time, "2018/01/10-2018/01/11");
            //ESClient.Instance.Finance_Query(fi);
            //var sw = new Stopwatch();
            //sw.Start();
            //_ci.AddFilter(ComField.org_types, "股份有限公司");

            //_ci.AddFilter(ComField.oc_area, "11");
            //var output = ESClient.Instance.Advanced_Query(_ci);
            var output = ReadPipeline.SearchAndHandle(_ci, ESReader.SearchByCom, SearchPostHandler.PostHandle);

            return output;
        }



        private void GeneralSearch(ESWrapper<Person> output)
        {
            var t = (int)output.total;
            if (t % 10 == 0)
                _total = t / 10;
            else
                _total = t / 10 + 1;

            var coms = new List<Model.VCompany>();
            for (int i = 0; i < output.docs.Count; i++)
            {

                var doc = output.docs[i];
                var com = new Model.VCompany();
                //if (doc.doc.oc_status < CharUtil.Statuss.Length)
                //    com.Status = CharUtil.Statuss[doc.doc.oc_status];
                //com.Code = doc.doc.oc_code;

                //com.Score = output.scores[i];

                com.RegDate = doc.doc.graduate.ToString("yyyy-MM-dd");
                //if (doc.hl.ContainsKey("od_bussiness"))
                //    com.Bussiness = "<html>" + doc.hl["od_bussiness"] + "</html>";
                //else if(!string.IsNullOrEmpty(doc.doc.od_bussiness))
                //{
                //    if(doc.doc.od_bussiness.Length > 20)
                //        com.Bussiness = doc.doc.od_bussiness.Substring(0, 20);
                //    else
                //        com.Bussiness = doc.doc.od_bussiness;
                //}

                if (doc.hl.ContainsKey("tags"))
                {
                    com.Bussiness = "鸠占鹊巢，公司品牌：" + doc.hl["tags"];
                }
                else
                    com.Bussiness = "没有品牌匹配到";
                    
                //if (doc.hl.ContainsKey("od_faren"))
                //{
                //    com.LawPerson = "<html>" + doc.hl["od_faren"] + "</html>";
                //}
                //else if (doc.hl.ContainsKey("od_gds"))
                //    com.LawPerson = "<html>" + doc.hl["od_gds"] + "</html>";
                //else if (doc.hl.ContainsKey("oc_members"))
                //    com.LawPerson = "<html>" + doc.hl["oc_members"] + "</html>";
                //else
                //    com.LawPerson = doc.doc.od_faren;

                //if (doc.doc.oc_tels != null && doc.doc.oc_tels.Count > 0)
                //    com.Tel = doc.doc.oc_tels[0];

                if (doc.hl.ContainsKey("oc_name"))
                {
                    com.Name = "<html>" + doc.hl["oc_name"] + "</html>";
                }
                else
                    com.Name = doc.doc.name;

                com.Weight = 0 + "/" + doc.score;
                if (doc.hl.ContainsKey("oc_address"))
                {
                    com.Addr = "<html>" + doc.hl["oc_address"] + "</html>";
                }
                else
                {
                    
                        com.Addr = "";
                }
                if (doc.hl.ContainsKey("oc_brands"))
                {
                    com.Brand = "<html>" + doc.hl["oc_brands"] + "</html>";
                }
                else
                {

                    //var brands = "";
                    //if (brands != null && brands.Count > 0)
                    //    com.Brand = brands[0];
                    //else
                    //    com.Brand = "null";
                }


                coms.Add(com);
            }

            Companies = new ObservableCollection<Model.VCompany>(coms);
            //if (string.IsNullOrWhiteSpace(Input))
                Input += "/总数： " + output.total;

            //sw.Stop();
            //Total = "耗时: " + sw.ElapsedMilliseconds;
            //if (Companies.Count == 0)
            //    NoneVisibility = "NO FUCKING RESULT!!! PLEASE CHANGE YOUR POSE.";
            //else
            //    NoneVisibility = "";
        }
    }
}