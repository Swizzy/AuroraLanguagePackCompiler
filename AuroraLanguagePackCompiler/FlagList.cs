namespace AuroraLanguagePackCompiler {
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Windows.Media.Imaging;
    using AuroraLanguagePackCompiler.Properties;

    internal class FlagList {
        public readonly Image Flag;

        public FlagList(Image flag, string name) {
            Flag = flag;
            Name = name;
        }

        public string Name { get; private set; }

        public BitmapImage FlagSource { get { return LoadFlagFromMemory(Flag); } }

        internal static BitmapImage LoadFlagFromMemory(Image flag) {
            var ms = new MemoryStream();
            if(flag != null)
                flag.Save(ms, ImageFormat.Png);
            else
                Resources.missingflag.Save(ms, ImageFormat.Png); // Failsafe
            ms.Seek(0, SeekOrigin.Begin);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }

        public override string ToString() { return Name; }
    }
}