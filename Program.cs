using JHolloway.SteamLibrary;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;

namespace GmodWorkshopShare;

public static class Program
{
    enum ArgumentTask
    {
        None,
        Load,
        Save,
    }

    [STAThread]
    static void Main(string[] args)
    {

        Console.WriteLine("Gmod Workshop Share v1.0 by ryi3r - 2025");
        var argTask = ArgumentTask.None;
        var filePath = string.Empty;
        var interactiveMode = false;
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "load":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Specify a file path in the arguments.");
                        return;
                    }
                    if (!File.Exists(args[1]))
                    {
                        Console.WriteLine("The file specified doesn't exist.");
                        return;
                    }
                    filePath = args[1];
                    argTask = ArgumentTask.Load;
                    break;
                case "save":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Specify a file path in the arguments.");
                        return;
                    }
                    filePath = args[1];
                    argTask = ArgumentTask.Save;
                    break;
                case "help":
                    Console.WriteLine("\tload <FilePath> -- Subscribe to all items in the specified file of the workshop");
                    Console.WriteLine("\tsave <FilePath> -- Save all subscribed items of Gmod to the specified file");
                    Console.WriteLine("\thelp -- Show this text");
                    break;
            }
        }
        else
        {
            interactiveMode = true;
            Console.WriteLine("=== Using interactive mode ===");
            Console.WriteLine("Write \"load\" to subscribe to all items of a specified file in the workshop");
            Console.WriteLine("Write \"save\" to save all subscribed workshop items to a specified file");
            var valid = false;
            while (!valid)
            {
                switch (Console.ReadLine()!)
                {
                    case "load":
                        valid = true;
                        Console.WriteLine("Write the file path or drag the file into the window and press enter.");
                        filePath = Console.ReadLine()!;
                        argTask = ArgumentTask.Load;
                        break;
                    case "save":
                        valid = true;
                        Console.WriteLine("Write the file path or drag the file into the window and press enter.");
                        filePath = Console.ReadLine()!;
                        argTask = ArgumentTask.Save;
                        break;
                }
            }
        }

        switch (argTask)
        {
            case ArgumentTask.None:
                Console.WriteLine("Invalid argument task!?");
                break;
            case ArgumentTask.Load:
                {
                    var loadUrls = new List<string>();
                    var fd = string.Empty;
                    foreach (var lib in SteamLibrary.GetSteamLibraries())
                    {
                        var sa = lib.SteamAppsPath;
                        if (sa == null || !Directory.Exists($"{sa}/workshop/content/4000/"))
                            continue;
                        fd = $"{sa}/workshop/content/4000/";
                        break;
                    }
                    using var f = File.OpenText(filePath);
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
                            else if (Directory.Exists($"{fd}/{match.Value[(match.Value.LastIndexOf('=') + 1)..]}/"))
                                Console.WriteLine($"Url already installed: {match.Value}");
                            else
                                loadUrls.Add(match.Value);
                            match = match.NextMatch();
                        }
                    }
                    if (loadUrls.Count == 0)
                    {
                        Console.WriteLine("Finished!");
                        return;
                    }
                    Console.WriteLine($"Found {loadUrls.Count} urls to install");
                    
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
            case ArgumentTask.Save:
                {
                    var fd = string.Empty;
                    foreach (var lib in SteamLibrary.GetSteamLibraries())
                    {
                        var sa = lib.SteamAppsPath;
                        if (sa == null || !Directory.Exists($"{sa}/workshop/content/4000/"))
                            continue;
                        fd = $"{sa}/workshop/content/4000/";
                        break;
                    }
                    using var f = File.CreateText(filePath);
                    var tot = 0;
                    foreach (var path in Directory.EnumerateDirectories(fd).Select((x => x.Replace("\\", "/"))))
                    {
                        var url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={path[(path.LastIndexOf('/') + 1)..]}";
                        Console.WriteLine($"Saving url: {url}");
                        f.WriteLine(url);
                        tot++;
                    }
                    Console.WriteLine($"OK! Saved {tot} urls.");
                }
                break;
        }

        if (interactiveMode)
        {
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }
    }
}