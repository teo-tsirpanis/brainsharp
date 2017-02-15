using System;

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
        Func<int> readAction = () => Console.Read();
        var output = DoIt(readAction);
        Console.Write(output);
    }
}