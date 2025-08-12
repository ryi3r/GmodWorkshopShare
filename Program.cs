using JHolloway.SteamLibrary;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;

namespace GmodWorkshopShare;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Console.WriteLine("Gmod Workshop Share v1.0 by ryi3r - 2025");
        var useHelp = true;
        var loadUrls = new List<string>();
        if (args.Length > 0)
        {
            useHelp = false;
            switch (args[0])
            {
                case "load":
                    {
                        if (args.Length <= 1)
                        {
                            Console.WriteLine("Specify a file.");
                            return;
                        }
                        using var f = File.OpenText(args[1]);
                        while (true)
                        {
                            var l = f.ReadLine();
                            if (l == null)
                                break;
                            var match = Regex.Match(l, @"https:\/\/steamcommunity.com\/sharedfiles\/filedetails\/\?id=\d+");
                            while (match.Success)
                            {
                                if (loadUrls.Contains(match.Value))
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Found duplicate url: {match.Value}");
                                    Console.ResetColor();
                                }
                                else
                                    loadUrls.Add(match.Value);
                                match = match.NextMatch();
                            }
                        }
                    }
                    break;
                case "save":
                    {
                        if (args.Length <= 1)
                        {
                            Console.WriteLine("Specify a file.");
                            return;
                        }
                        var fd = string.Empty;
                        foreach (var lib in SteamLibrary.GetSteamLibraries())
                        {
                            var sa = lib.SteamAppsPath;
                            if (sa == null || !Directory.Exists($"{sa}/common/GarrysMod/garrysmod"))
                                continue;
                            fd = $"{sa}/common/GarrysMod/garrysmod";
                            break;
                        }
                        using var f = File.CreateText(args[1]);
                        foreach (var path in Directory.EnumerateFiles($"{fd}/cache/workshop").Where(x => x.EndsWith(".gma")).Select((x => x.Replace("\\", "/"))))
                        {
                            //Console.WriteLine(path);
                            var id = path[(path.LastIndexOf('/') + 1)..^4];
                            //Console.WriteLine(id);
                            f.WriteLine($"https://steamcommunity.com/sharedfiles/filedetails/?id={id}");
                        }
                    }
                    return;
                default:
                    {
                        useHelp = true;
                    }
                    break;
            }
        }
        if (!useHelp)
        {
            var driver = new ChromeDriver();
            driver.Url = "https://steamcommunity.com/login/home/";
            var loggedIn = false;
            var changed = false;
            var waitSubscribe = false;
            var waitAdditional = false;
            var startCount = loadUrls.Count;
            while (true)
            {
                if (loggedIn)
                {
                    if (changed)
                    {
                        var s = driver.FindElements(By.Id("SubscribeItemOptionSubscribed"));
                        if (s.Count > 0)
                        {
                            if ((s[0].GetAttribute("class") ?? string.Empty).Split(' ').Contains("selected"))
                                changed = false; // Already subscribed to it
                            else if (!waitSubscribe)
                            {
                                s = driver.FindElements(By.Id("SubscribeItemBtn"));
                                if (s.Count > 0)
                                {
                                    s[0].Click();
                                    waitSubscribe = true;
                                }
                            }
                            else if (!waitAdditional)
                            {
                                s = driver.FindElements(By.ClassName("btn_blue_steamui"));
                                if (s.Count > 0)
                                {
                                    s[0].Click();
                                    waitAdditional = true;
                                }
                            }
                        }
                        else if (driver.FindElements(By.ClassName("error_ctn")).Count > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Got error in url: {driver.Url}, ignoring");
                            Console.ResetColor();
                            changed = false;
                        }
                    }
                    else if (loadUrls.Count > 0)
                    {
                        if (driver.Url != loadUrls[0])
                        {
                            {
                                var done = false;
                                while (!done)
                                {
                                    try
                                    {
                                        driver.Url = loadUrls[0];
                                        done = true;
                                    }
                                    catch
                                    {
                                        // tab may crash so we'll handle it here
                                    }
                                }
                            }
                            changed = true;
                            waitSubscribe = false;
                            waitAdditional = false;
                            Console.WriteLine($"Workshop items left: {loadUrls.Count - 1} ({(startCount - (loadUrls.Count - 1)) * 100d / startCount:0.00}% done)");
                            loadUrls.RemoveAt(0);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Finished!");
                        try
                        {
                            driver.Close();
                        }
                        catch
                        {
                            // ignore any errors closing the driver
                        }
                        return;
                    }
                }
                else
                {
                    if (driver.FindElements(By.Id("account_pulldown")).Count > 0)
                        loggedIn = true;
                }
                Thread.Sleep(5);
            }
        }
        Console.WriteLine("\tload <FilePath> -- Subscribe to all items in the specified file of the workshop");
        Console.WriteLine("\tsave <FilePath> -- Save all subscribed items of Gmod to the specified file");
        Console.WriteLine("\thelp -- Show this text");
    }
}