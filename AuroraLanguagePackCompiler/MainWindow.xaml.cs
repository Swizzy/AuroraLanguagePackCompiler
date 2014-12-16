namespace AuroraLanguagePackCompiler {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization.Json;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Threading;
    using XuiTools;
    using Image = System.Drawing.Image;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private readonly DataContractJsonSerializer _json = new DataContractJsonSerializer(typeof(LanguagePackData));
        private Image _customImage;
        private LanguagePackData _packData = new LanguagePackData();

        public MainWindow() {
            InitializeComponent();
            var ver = Assembly.GetAssembly(typeof(MainWindow)).GetName().Version;
            var xver = Assembly.GetAssembly(typeof(Resx2Bin)).GetName().Version;
            Title = string.Format(Title, ver.Major, ver.Minor, xver.Major, xver.Minor);
            translatornamebox.Text = Environment.UserName;
            var bw = new BackgroundWorker();
            bw.DoWork += (sender, args) => {
                             var langList = CultureInfo.GetCultures(CultureTypes.AllCultures).Select(cinfo => new LangList(cinfo)).ToList();
                             langList.Sort(LangList.LangListCompare);
                             AddItemControlsItems(presetbox, langList);
                             var sprites = Properties.Resources.sprites;
                             foreach(var line in Properties.Resources.spritesdata.Split(Environment.NewLine.ToCharArray())) {
                                 if(string.IsNullOrEmpty(line))
                                     continue;
                                 var x = line.Substring(line.IndexOf("x=", StringComparison.OrdinalIgnoreCase) + 2);
                                 x = x.Substring(0, x.IndexOf(" ", StringComparison.Ordinal));
                                 AddSelectorItem(comboBox1,
                                                 new FlagList(
                                                     sprites.Clone(new Rectangle(int.Parse(x), int.Parse(line.Substring(line.IndexOf("y=", StringComparison.OrdinalIgnoreCase) + 2)), 64, 64),
                                                                   sprites.PixelFormat), line.Substring(3, line.LastIndexOf("\"", StringComparison.Ordinal) - 3)));
                             }
                             AddSelectorItem(comboBox1, new FlagList(null, "Custom"));
                         };
            bw.RunWorkerCompleted += (sender, args) => {
                                         SetCurrentLanguage();
                                         Load.IsEnabled = true;
                                         Save.IsEnabled = true;
                                         Compile.IsEnabled = true;
                                     };
            bw.RunWorkerAsync();
        }

        private void AddSelectorItem(Selector control, object item, bool setIndex0 = true) {
            if(!Dispatcher.Thread.Equals(Thread.CurrentThread)) {
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => AddSelectorItem(control, item, setIndex0)));
                return;
            }
            control.Items.Add(item);
            if(setIndex0 && control.Items.Count == 1)
                control.SelectedIndex = 0;
        }

        private void AddItemControlsItems(ItemsControl control, IEnumerable<object> items) {
            if(!Dispatcher.Thread.Equals(Thread.CurrentThread)) {
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => AddItemControlsItems(control, items)));
                return;
            }
            foreach(var item in items)
                control.Items.Add(item);
        }

        private void SetCurrentLanguage() {
            if(!Dispatcher.Thread.Equals(Thread.CurrentThread)) {
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(SetCurrentLanguage));
                return;
            }
            foreach(var lang in presetbox.Items.Cast<LangList>().Where(lang => lang.CultureInfo.Equals(CultureInfo.CurrentCulture))) {
                presetbox.SelectedItem = lang;
                break;
            }
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var flaglist = (FlagList)comboBox1.SelectedItem;
            if(flaglist.Flag == null) {
                _packData.Index = -1;
                PictureBox1.Source = FlagList.LoadFlagFromMemory(_customImage);
                return;
            }
            _packData.Index = comboBox1.SelectedIndex;
            PictureBox1.Source = flaglist.FlagSource;
        }

        private static Bitmap ScaleImage(Image image, int maxWidth, int maxHeight) {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            Graphics.FromImage(newImage).DrawImage(image, 0, 0, newWidth, newHeight);
            var bmp = new Bitmap(newImage);

            return bmp;
        }

        private void IconClick(object sender, MouseButtonEventArgs e) {
            var ofd = new OpenFileDialog {
                                             Filter = @"Image Files|*.bmp;*.jpg;*.jpeg;*.tiff;*.png"
                                         };
            if(ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            var data = File.ReadAllBytes(ofd.FileName);
            using(var ms = new MemoryStream(data)) {
                var tmp = ScaleImage(Image.FromStream(ms), 64, 64);
                using(var ms2 = new MemoryStream()) {
                    tmp.Save(ms2, ImageFormat.Png);
                    _customImage = Image.FromStream(ms2);
                }
            }
            comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
            comboBox1_SelectionChanged(this, null);
        }

        private void presetbox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var lang = ((LangList)presetbox.SelectedItem).CultureInfo;
            try {
                var region = new RegionInfo(lang.LCID);
                foreach(var item in comboBox1.Items.Cast<object>().Where(item => ((FlagList)item).Name.ToLower().Contains(region.EnglishName.ToLower()))) {
                    comboBox1.SelectedItem = item;
                    break;
                }
            }
            catch(Exception) {}
            languagebox.Text = lang.NativeName;
            codebox.Text = lang.Name;
        }

        private void SaveClick(object sender, RoutedEventArgs e) {
            var sfd = new SaveFileDialog {
                                             FileName = "language.dat"
                                         };
            if(sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            SaveDat(sfd.FileName, "0.4");
        }

        private void SaveDat(string file, string version) {
            _packData.Translator = translatornamebox.Text;
            _packData.Displayname = languagebox.Text;
            _packData.Languagecode = codebox.Text;
            _packData.Version = version;
            using(Stream s = File.OpenWrite(file))
                _json.WriteObject(s, _packData);
            if (_packData.Index != -1 || _customImage == null)
                return;
            // ReSharper disable once AssignNullToNotNullAttribute
            var icopath = Path.Combine(Path.GetDirectoryName(file), "icon.png");
            _customImage.Save(icopath, ImageFormat.Png);
        }

        private void SaveDat(string version, out byte[] dat, out byte[] icon) {
            _packData.Translator = translatornamebox.Text;
            _packData.Displayname = languagebox.Text;
            _packData.Languagecode = codebox.Text;
            _packData.Version = version;
            using (var s = new MemoryStream()) {
                _json.WriteObject(s, _packData);
                dat = s.ToArray();
            }
            using (var ms = new MemoryStream()) {
                if(_packData.Index == -1)
                    if(_customImage != null)
                        _customImage.Save(ms, ImageFormat.Png);
                    else
                        Properties.Resources.missingflag.Save(ms, ImageFormat.Png);
                else
                    ((FlagList)comboBox1.Items[_packData.Index]).Flag.Save(ms, ImageFormat.Png);
                icon = ms.ToArray();
            }
        }

        private void LoadClick(object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog {
                                             FileName = "language.dat",
                                             Filter = @"Aurora Data Files|*.dat"
                                         };
            if(ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            LoadDat(ofd.FileName);
        }

        private void LoadDat(string fileName) {
            using(Stream s = File.OpenRead(fileName))
                _packData = (LanguagePackData)_json.ReadObject(s);
            if(_packData.Index != -1)
                comboBox1.SelectedIndex = _packData.Index;
            else
                comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
            languagebox.Text = _packData.Displayname;
            codebox.Text = _packData.Languagecode;
            translatornamebox.Text = _packData.Translator;
        }

        private void CompileClick(object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog {
                                             Title = @"Select translation to compile",
                                             FileName = codebox.Text + ".xml",
                                             Filter = @"Aurora Translation|*.xml;*.resx"
                                         };
            if(ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            var loc = new Resxloc();
            var localized = loc.SplitResxMaster(File.OpenRead(ofd.FileName));
            var compiler = new Resx2Bin();
            var pkg = new XuiPkg();
            foreach(var resx in localized) {
                if(resx.Key.Equals("DynamicStrings")) {
                    pkg.AddFile("Aurora_Strings.xus", compiler.ConvertToIndexBasedTables(resx.Value.ToArray()));
                    foreach(var resxObj in resx.Value.Where(resxObj => resxObj.Key.EndsWith("LOCALE_VERSION", StringComparison.CurrentCultureIgnoreCase))) {
                        byte[] dat;
                        byte[] icon;
                        SaveDat(resxObj.Content, out dat, out icon);
                        pkg.AddFile("language.dat", dat);
                        pkg.AddFile("icon.png", icon);
                        break;
                    }
                }
                else
                    pkg.AddFile(resx.Key + ".xus", compiler.ConvertToKeyBasedTable(resx.Value.ToArray()));
            }
            // ReSharper disable once AssignNullToNotNullAttribute
            pkg.SaveToFile(File.OpenWrite(Path.Combine(Path.GetDirectoryName(ofd.FileName), Path.GetFileNameWithoutExtension(ofd.FileName) + ".xzp")));
        }
    }
}