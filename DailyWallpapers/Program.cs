using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DailyWallpapers
{
    public class Program
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public const int SW_HIDE = 0;

        public static async Task Main(string[] args)
        {
            Console.Title = "Daily Wallpapers";
            Console.WriteLine("[APP] Starting...");

            IntPtr winHandle = Process.GetCurrentProcess().MainWindowHandle;
            ShowWindow(winHandle, SW_HIDE);

            new WallaperApp().InitializeAsync().GetAwaiter().GetResult();
            await Task.Delay(-1);
        }
    }
    
    public class WallaperApp
    {
        public int OldDate { get; set; } = 0;
        public int NewDate { get; set; } = 0;

        public WallaperApp()
        {
            var task = Task.Run(async () => await GetWallpaper());
        }

        public async Task InitializeAsync()
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("[APP] Initializing...");

            var timer = new System.Threading.Timer(async (e) =>
            {
                Console.WriteLine("[APP] Starting auto-check timer...");
                await GetWallpaper();
            }, null, TimeSpan.Zero, TimeSpan.FromDays(1));

            Console.WriteLine("[APP] Initialized");

            await Task.Delay(-1);
        }

        public async Task GetWallpaper()
        {
            Console.WriteLine("[WALLPAPER] Fetching today's wallpaper...");
            string url = @"https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US";
            var result = await ResponseAsync(url);
            var parsed = JsonConvert.DeserializeObject<JObject>(result);

            NewDate = Int32.Parse(parsed["images"][0]["startdate"].ToString());

            if (NewDate > OldDate)
            {
                UpdateBackground($"https://bing.com{parsed["images"][0]["url"]}");
                Console.WriteLine("[WALLPAPER] Updating Wallpaper");
            }
            else
            {
                OldDate = Int32.Parse(parsed["images"][0]["enddate"].ToString());
            }

            Console.WriteLine($"[WALLPAPER] Finished fetching today's wallpaper");
        }

        public void UpdateBackground(string url)
        {
            Wallpaper.Set(new Uri(url), Wallpaper.Style.Fill);
        }

        public static async Task<string> ResponseAsync(string url)
            => await new StreamReader((await ((HttpWebRequest)WebRequest.Create(url)).GetResponseAsync()).GetResponseStream()).ReadToEndAsync().ConfigureAwait(false);
    }

    public sealed class Wallpaper
    {
        Wallpaper() { }

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public enum Style : int
        {
            Fill,
            Fit,
            Tiled,
            Centered,
            Stretched
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static void Set(Uri uri, Style style)
        {
            string randomName = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 5).Select(s => s[new Random().Next(s.Length)]).ToArray());
            string TemporaryPath = Path.Combine(Path.GetTempPath(), $"{randomName}.bmp");

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(uri, TemporaryPath);
            }

            RegistryKey RegKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            switch (style)
            {
                case Style.Fill:
                    RegKey.SetValue(@"WallpaperStyle", 10.ToString());
                    RegKey.SetValue(@"TileWallpaper", 0.ToString());
                    break;
                case Style.Fit:
                    RegKey.SetValue(@"WallpaperStyle", 6.ToString());
                    RegKey.SetValue(@"TileWallpaper", 0.ToString());
                    break;
                case Style.Centered:
                    RegKey.SetValue(@"WallpaperStyle", 1.ToString());
                    RegKey.SetValue(@"TileWallpaper", 0.ToString());
                    break;
                case Style.Stretched:
                    RegKey.SetValue(@"WallpaperStyle", 2.ToString());
                    RegKey.SetValue(@"TileWallpaper", 0.ToString());
                    break;
                case Style.Tiled:
                    RegKey.SetValue(@"WallpaperStyle", 1.ToString());
                    RegKey.SetValue(@"TileWallpaper", 1.ToString());
                    break;
            }

            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, TemporaryPath, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}