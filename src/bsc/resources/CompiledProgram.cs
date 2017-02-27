using System;
using System.Diagnostics;

public static class MyBrainsharpProgram
{
    const int MemorySize = @MemorySize;
    static byte[] mem = new byte[MemorySize];
    static int p = 0;
    public static void SetPointer(int ofs)
    {
        var temp = (p + ofs) % MemorySize;
        if (temp < 0)
        {
            p = MemorySize - temp;
        }
        else
        {
            p = temp;
        }
    }
    @TheMethod
    public static void Main()
    {
        var stopwatch = new Stopwatch();
        Func<int> readAction = () => Console.Read();
        Action<char> writeAction = c => Console.Write(c);
        stopwatch.Start();
        DoIt(readAction, writeAction);
        stopwatch.Stop();
        #if PROFILE            
            Console.Error.WriteLine($"Runtime: {stopwatch.Elapsed}")
        #endif
    }
}