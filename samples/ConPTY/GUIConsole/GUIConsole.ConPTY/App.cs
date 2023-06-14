using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GUIConsole.ConPTY;

public class App
{
    private const string ScriptFilename = "OSC-seq-example.ps1";
    private Terminal _terminal;

    static void Main()
    {
        new App().Start();
    }

    private void Start()
    {
        Console.WriteLine($"Starting {ScriptFilename}");
        _terminal = new Terminal();
        var stdoutReaderTask = Task.FromResult("");
        _terminal.OutputReady += (_, _) => stdoutReaderTask = Task.Run(ReadBlockingByOneChar);
        _terminal.Start($"powershell.exe -ExecutionPolicy Bypass -File {FindFileUp(ScriptFilename)}", 80, 24);
        stdoutReaderTask.Wait();
        string stdout = stdoutReaderTask.Result;

        Console.WriteLine($"stdout (length={stdout.Length}): {stdout}");

        var sequences = SplitByEsc(stdout);
        Console.WriteLine($"Found {sequences.Count} sequences:");
        foreach (var str in sequences)
        {
            Console.WriteLine(str.Replace("\x1B", "ESC").Replace("\x07", "^G"));
        }

        File.WriteAllText("stdout.txt", stdout, Encoding.ASCII);
    }

    private static string FindFileUp(string fileName)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        while (dir != null)
        {
            string filePath = Path.Combine(dir, fileName);
            if (File.Exists(filePath)) return filePath;
            dir = Path.GetDirectoryName(dir);
        }
        throw new IOException($@"Cannot find {fileName}");
    }

    private string ReadBlockingByOneChar()
    {
        StreamReader reader = new StreamReader(_terminal.ConsoleOutStream, Encoding.UTF8);
        // Read the console's output 1 character at a time
        int charsRead;
        char[] buf = new char[1];
        string text = "";
        while ((charsRead = reader.ReadBlock(buf, 0, 1)) != 0)
        {
            text += new string(buf.Take(charsRead).ToArray());
        }
        return text;
    }

    private static List<string> SplitByEsc(string text)
    {
        List<string> result = new List<string>();
        int prevInd = 0;
        int ind = text.IndexOf('\x1B', 1);
        while (ind > 0)
        {
            result.Add(text.Substring(prevInd, ind - prevInd));
            prevInd = ind;
            ind = text.IndexOf('\x1B', prevInd + 1);
        }
        result.Add(text.Substring(prevInd));
        return result;
    }
}
